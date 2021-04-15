module Elsea

open LoadersAndCopiers
open CardOverflow.Api
open CardOverflow.Pure
open Domain
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Entity.Anki
open System
open System.Linq
open Thoth.Json.Net
open FsToolkit.ErrorHandling
open Helpers
open System.IO
open System.IO.Compression
open System.Security.Cryptography
open System.Collections.Generic
open Microsoft.EntityFrameworkCore
open NodaTime
open NodaTime.Serialization.JsonNet
open Nest
open FSharp.Control.Tasks
open Newtonsoft.Json
open Elasticsearch.Net
open Domain.Projection

// This exists because Example's Summary's FieldValues's is a Map<string, string>, and serializing user input to the key of a JSON object causes elasticsearch problems (e.g. camelcasing, having a "." at the start/end of a key https://discuss.elastic.co/t/elasticsearch-mapping-cannot-index-a-field-having-name-starting-with-a-dot/163804)
type MapStringStringConverter() =
    inherit JsonConverter<Map<string,string>>()
    let keysPropertyName   = "keys" // do not change these values without regenerating elasticsearch's indexes
    let valuesPropertyName = "values"

    override _.WriteJson((writer: JsonWriter), (kvps: Map<string,string>), (_serializer: JsonSerializer)) =
        writer.WriteStartObject()
        writer.WritePropertyName keysPropertyName
        writer.WriteStartArray()
        for kvp in kvps do
            writer.WriteValue kvp.Key
        writer.WriteEndArray()
        writer.WritePropertyName valuesPropertyName
        writer.WriteStartArray()
        for kvp in kvps do
            writer.WriteValue kvp.Value
        writer.WriteEndArray()
        writer.WriteEndObject()
    
    override _.ReadJson((reader: JsonReader), (_objectType: Type), (_existingValue: Map<string,string>), (_hasExistingValue: bool), (_serializer: JsonSerializer)) =
        let rec readArray r =
            reader.Read() |> ignore
            match reader.TokenType with
            | JsonToken.EndArray -> r
            | _ -> reader.Value :?> string :: r |> readArray
        
        let mutable keys = []
        let mutable values = []
        let originalDepth = reader.Depth // so we don't leave the current json object and iterate the entire reader
        while reader.Read() && originalDepth <> reader.Depth do
            if reader.TokenType = JsonToken.StartArray then
                if reader.Path.EndsWith keysPropertyName then
                    keys <- readArray []
                elif reader.Path.EndsWith valuesPropertyName then
                    values <- readArray []
        
        List.zip keys values |> Map.ofList

type ElseJsonSerializer (builtinSerializer, connectionSettings) =
    inherit Nest.JsonNetSerializer.ConnectionSettingsAwareSerializerBase(builtinSerializer, connectionSettings)

    override _.CreateJsonConverters() = MapStringStringConverter() :> JsonConverter |> Seq.singleton

    override _.CreateJsonSerializerSettings() =
        JsonSerializerSettings().ConfigureForNodaTime DateTimeZoneProviders.Tzdb

let sourceSerializerFactory =
    ConnectionSettings.SourceSerializerFactory
        (fun x y -> ElseJsonSerializer (x, y) :> IElasticsearchSerializer)

module Example =
    open Example
    let getExampleSearch (client: ElasticClient) (exampleId: string) =
        exampleId
        |> Id
        |> DocumentPath
        |> client.GetAsync<ExampleSearch>
        |> Task.map (fun x -> x.Source)
        |> Async.AwaitTask
    let getExampleSearchFor (client: ElasticClient) (callerId: UserId) (exampleId: ExampleId) =
        Elsea.Example.GetFor(client, string callerId, string exampleId)
    let upsertExampleSearch (kvs: KeyValueStore) (client: ElasticClient) (exampleId: ExampleId) event =
        match event with
        | Events.Created summary -> task {
            let! user = kvs.GetUser summary.AuthorId // lowTODO optimize by only fetching displayname
            let! templateRevision = kvs.GetTemplateRevision summary.TemplateRevisionId
            let search = ExampleSearch.fromSummary summary user.DisplayName templateRevision
            return! Elsea.Example.UpsertSearch(client, string exampleId,search)
            }
        | Events.Edited edited -> task {
            let! exampleSearch = exampleId |> string |> getExampleSearch client
            let! templateRevision =
                if exampleSearch.TemplateRevision.Id = edited.TemplateRevisionId then
                    exampleSearch.TemplateRevision |> Async.singleton
                else
                    kvs.GetTemplateRevision edited.TemplateRevisionId
            let search = ExampleSearch.fromEdited edited templateRevision
            return! Elsea.Example.UpsertSearch(client, string exampleId, search)
        }

module Template =
    open Template
    let getTemplateSearch (client: ElasticClient) (templateId: string) =
        templateId
        |> Id
        |> DocumentPath
        |> client.GetAsync<TemplateSearch>
        |> Task.map (fun x -> x.Source)
        |> Async.AwaitTask
    let getTemplateSearchFor (client: ElasticClient) (callerId: UserId) (templateId: TemplateId) =
        Elsea.Template.GetFor(client, string callerId, string templateId)
    let upsertTemplateSearch (kvs: KeyValueStore) (client: ElasticClient) (templateId: TemplateId) event =
        match event with
        | Events.Created summary -> task {
            let! user = kvs.GetUser summary.AuthorId // lowTODO optimize by only fetching displayname
            let search = TemplateSearch.fromSummary summary user.DisplayName
            return! Elsea.Template.UpsertSearch(client, string templateId, search)
            }
        | Events.Edited edited -> task {
            let search = TemplateSearch.fromEdited edited
            return! Elsea.Template.UpsertSearch(client, string templateId, search)
        }

module Stack =
    open Stack
    let getStackSearch (client: ElasticClient) (stackId: string) =
        client.GetAsync<StackSearch>(
            stackId |> Id |> DocumentPath
        ) |> Task.map (fun x -> x.Source)
        |> Async.AwaitTask
    let upsertStackSearch (client: ElasticClient) (kvs: KeyValueStore) (stackId: StackId) event =
        match event with
        | Events.Created summary -> task {
            let! revision = kvs.GetExampleRevision summary.ExampleRevisionId
            let t1 = Elsea.Example.HandleCollected(client, { ExampleId   = revision.ParentedExampleId.ExampleId
                                                             CollectorId = summary.AuthorId
                                                             RevisionId  = summary.ExampleRevisionId }) |> Async.AwaitTask
            let t2 =
                revision.ParentedExampleId.ExampleId
                |> StackSearch.fromSummary summary
                |> client.IndexDocumentAsync<StackSearch>
                |>% ignore
                |> Async.AwaitTask
            return! [t1; t2] |> Async.Parallel |> Async.map ignore
            }
        | Events.Discarded -> task {
            let! stack = stackId |> string |> getStackSearch client
            let! revision = kvs.GetExampleRevision stack.ExampleRevisionId
            let t1 = Elsea.Example.HandleDiscarded(client, { ExampleId   = revision.ParentedExampleId.ExampleId
                                                             DiscarderId = stack.AuthorId }) |> Async.AwaitTask
            let t2 = stackId |> string |> Id |> DocumentPath<StackSearch> |> client.DeleteAsync |>% ignore |> Async.AwaitTask
            return! [t1; t2] |> Async.Parallel |> Async.map ignore
            }
        | Events.TagsChanged tagsChanged ->
            let n = StackSearch.fromTagsChanged tagsChanged
            task {
                return! Elsea.Stack.UpsertSearch(client, string stackId, n)
            }
        | Events.CardStateChanged cardStateChanged ->
            stackId
            |> string
            |> getStackSearch client
            |> Async.map (
                StackSearch.fromCardStateChanged cardStateChanged
                >> client.IndexDocumentAsync
                >> ignore
            ) |> Async.StartAsTask
        | Events.RevisionChanged revisionChanged -> task {
            let! stack = kvs.GetStack stackId
            let! revision = kvs.GetExampleRevision revisionChanged.RevisionId
            let t1 = Elsea.Example.HandleCollected(client, { ExampleId   = revision.ParentedExampleId.ExampleId
                                                             CollectorId = stack.AuthorId
                                                             RevisionId  = revision.Id }) |> Async.AwaitTask
            let n = StackSearch.fromRevisionChanged revisionChanged
            let t2 = Elsea.Stack.UpsertSearch(client, string stackId, n) |> Async.AwaitTask
            return! [t1; t2] |> Async.Parallel |> Async.map ignore
            }

open System.Threading.Tasks

type IClient =
   abstract member GetExampleSearch    : ExampleId -> Async<ExampleSearch>
   abstract member GetExampleSearchFor : UserId    -> ExampleId -> Task<Option<ExampleSearch>>
   abstract member UpsertExampleSearch : ExampleId -> (Example.Events.Event -> Task<unit>)
   
   abstract member GetTemplateSearch    : TemplateId -> Async<TemplateSearch>
   abstract member GetTemplateSearchFor : UserId     -> TemplateId -> Task<Option<TemplateSearch>>
   abstract member UpsertTemplateSearch : TemplateId -> (Template.Events.Event -> Task<unit>)

   abstract member GetUsersStack       : UserId    -> ExampleId -> Task<IReadOnlyCollection<StackSearch>>
   abstract member UpsertStackSearch   : StackId   -> (Stack.Events.Event -> Task<unit>)

type Client (client: ElasticClient, kvs: KeyValueStore) =
    interface IClient with
        member    _.GetExampleSearch (exampleId: ExampleId) =
            Example.getExampleSearch client (string exampleId)
        member    _.GetExampleSearchFor callerId (exampleId: ExampleId) =
            Example.getExampleSearchFor client callerId exampleId
        member    _.UpsertExampleSearch (exampleId: ExampleId) =
            Example.upsertExampleSearch kvs client exampleId
        
        member     _.GetTemplateSearch (templateId: TemplateId) =
            Template.getTemplateSearch client (string templateId)
        member     _.GetTemplateSearchFor callerId (templateId: TemplateId) =
            Template.getTemplateSearchFor client callerId templateId
        member     _.UpsertTemplateSearch (templateId: TemplateId) =
            Template.upsertTemplateSearch kvs client templateId
    
        member _.GetUsersStack (authorId: UserId) (exampleId: ExampleId) =
            Elsea.Stack.Get(client, string authorId, string exampleId)
    
        member  _.UpsertStackSearch (stackId: StackId) =
            Stack.upsertStackSearch client kvs stackId

#if DEBUG // it could be argued that test stuff should only be in test assemblies, but I'm gonna put stuff that's tightly coupled together. Easier to make changes.
type NoopClient () =
    interface IClient with
        member _.GetExampleSearch    (exampleId: ExampleId)                        = failwith "not implemented"
        member _.GetExampleSearchFor (callerId: UserId)  (exampleId: ExampleId)    = failwith "not implemented"
        member _.UpsertExampleSearch (exampleId: ExampleId) = fun x -> Task.singleton ()
        
        member _.GetTemplateSearch    (templateId: TemplateId)                        = failwith "not implemented"
        member _.GetTemplateSearchFor (callerId: UserId)  (templateId: TemplateId)    = failwith "not implemented"
        member _.UpsertTemplateSearch (templateId: TemplateId) = fun x -> Task.singleton ()
    
        member _.GetUsersStack (authorId: UserId) (exampleId: ExampleId)           = failwith "not implemented"
    
        member _.UpsertStackSearch (stackId: StackId)       = fun x -> Task.singleton ()
#endif

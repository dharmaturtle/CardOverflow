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
    let getExample (client: ElasticClient) (exampleId: string) =
        client.GetAsync<Events.Summary>(
            exampleId |> Id |> DocumentPath
        ) |> Task.map (fun x -> x.Source)
        |> Async.AwaitTask
    let upsertExample (client: ElasticClient) (exampleId: string) event =
        match event with
        | Events.Created summary ->
            client.IndexDocumentAsync summary |> Task.map ignore
        | Events.Edited edited -> task {
            let! summary = getExample client exampleId // do NOT read from the KeyValueStore to maintain consistency! We're interested in updating elasticsearch - and not interested in what KVS thinks. Also, we can't use an anonymous record to update because it'll replace RevisionIds when we want to append. lowTODO elasticsearch can append RevisionIds
            let! _ = summary |> Fold.evolveEdited edited |> client.IndexDocumentAsync
            return ()
        }
        |> Async.AwaitTask
    let getExampleSearch (client: ElasticClient) (exampleId: string) =
        client.GetAsync<ExampleSearch>(
            exampleId |> Id |> DocumentPath
        ) |> Task.map (fun x -> x.Source)
        |> Async.AwaitTask
    let upsertExampleSearch (kvs: KeyValueStore) (client: ElasticClient) (exampleId: ExampleId) event =
        match event with
        | Events.Created summary -> task {
            let! user = kvs.GetUser summary.AuthorId // lowTODO optimize by only fetching displayname
            let! templateRevision = kvs.GetTemplateRevision summary.TemplateRevisionId
            return!
                ExampleSearch.fromSummary summary user.DisplayName templateRevision
                |> client.IndexDocumentAsync
                |>% ignore
            }
        | Events.Edited edited -> task {
            let! exampleSearch = exampleId |> string |> getExampleSearch client
            let! templateRevision =
                if exampleSearch.TemplateRevision.Id = edited.TemplateRevisionId then
                    exampleSearch.TemplateRevision |> Async.singleton
                else
                    kvs.GetTemplateRevision edited.TemplateRevisionId
            return!
                ExampleSearch.fromEdited exampleSearch edited templateRevision
                |> client.IndexDocumentAsync
                |>% ignore
        }

module Stack =
    open Stack
    // Why does this code exist??? Copy pasted it from Example, but I'm not sure why it exists there either. Delete?
    //let getStack (client: ElasticClient) (stackId: string) =
    //    client.GetAsync<Events.Summary>(
    //        stackId |> Id |> DocumentPath
    //    ) |> Task.map (fun x -> x.Source)
    //    |> Async.AwaitTask
    //let upsertStack (client: ElasticClient) (stackId: string) event =
    //    match event with
    //    | Events.Created summary ->
    //        client.IndexDocumentAsync summary |> Task.map ignore
    //    | Events.TagsChanged tagsChanged -> task {
    //        let! summary = getStack client stackId // do NOT read from the KeyValueStore to maintain consistency! We're interested in updating elasticsearch - and not interested in what KVS thinks. Also, we can't use an anonymous record to update because it'll replace RevisionIds when we want to append. lowTODO elasticsearch can append RevisionIds
    //        let! _ = summary |> Fold.evolveTagsChanged tagsChanged |> client.IndexDocumentAsync
    //        return ()
    //    }
    //    |> Async.AwaitTask
    let getStackSearch (client: ElasticClient) (stackId: string) =
        client.GetAsync<StackSearch>(
            stackId |> Id |> DocumentPath
        ) |> Task.map (fun x -> x.Source)
        |> Async.AwaitTask
    let upsertStackSearch (client: ElasticClient) (stackId: StackId) event =
        match event with
        | Events.Created summary ->
            summary
            |> StackSearch.fromSummary
            |> client.IndexDocumentAsync
            |>% ignore
        | Events.TagsChanged tagsChanged ->
            stackId
            |> string
            |> getStackSearch client
            |> Async.map (
                StackSearch.fromTagsChanged tagsChanged
                >> client.IndexDocumentAsync
                >> ignore
            ) |> Async.StartAsTask
        | Events.CardStateChanged cardStateChanged ->
            stackId
            |> string
            |> getStackSearch client
            |> Async.map (
                StackSearch.fromCardStateChanged cardStateChanged
                >> client.IndexDocumentAsync
                >> ignore
            ) |> Async.StartAsTask

type Client (client: ElasticClient, kvs: KeyValueStore) =
    // just here as reference; delete after you add more methods
    //member _.UpsertConcept' (conceptId: string) e =
    //    match e with
    //    | Concept.Events.Created summary ->
    //        client.IndexDocumentAsync summary |> Task.map ignore
    //    | Concept.Events.DefaultExampleChanged b ->
    //        client.UpdateAsync<obj>(
    //            conceptId |> Id |> DocumentPath,
    //            fun ud ->
    //                ud
    //                    .Index<Concept.Events.Summary>()
    //                    .Doc {| DefaultExampleId = b.ExampleId |}
    //                :> IUpdateRequest<_,_>
    //        ) |> Task.map ignore
    //    |> Async.AwaitTask
    //member this.UpsertConcept (conceptId: ConceptId) =
    //    conceptId.ToString() |> this.UpsertConcept'
    //member _.GetConcept (conceptId: string) =
    //    client.GetAsync<Concept.Events.Summary>(
    //        conceptId |> Id |> DocumentPath
    //    ) |> Task.map (fun x -> x.Source)
    //    |> Async.AwaitTask
    //member this.Get (conceptId: ConceptId) =
    //    conceptId.ToString() |> this.GetConcept
    member    _.UpsertExample' exampleId event =
        Example.upsertExample client exampleId event
    member    _.UpsertExample (exampleId: ExampleId) =
        Example.upsertExample client (exampleId.ToString())
    member    _.GetExample exampleId =
        Example.getExample client exampleId
    member    _.GetExample (exampleId: ExampleId) =
        Example.getExample client (exampleId.ToString())
    member    _.GetExampleSearch (exampleId: ExampleId) =
        Example.getExampleSearch client (string exampleId)
    member    _.UpsertExampleSearch (exampleId: ExampleId) =
        Example.upsertExampleSearch kvs client exampleId
    
    member  _.UpsertStackSearch (stackId: StackId) =
        Stack.upsertStackSearch client stackId

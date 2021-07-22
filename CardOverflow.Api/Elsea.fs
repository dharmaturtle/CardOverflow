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
    let get (client: IElasticClient) (exampleId: ExampleId) =
        exampleId
        |> string
        |> Id
        |> DocumentPath
        |> client.GetAsync<ExampleSearch>
        |> Task.map (fun x -> x.Source |> Core.toOption)
        |> Async.AwaitTask

module Template =
    let delete (client: IElasticClient) (templateId: TemplateId) =
        templateId
        |> string
        |> Id
        |> DocumentPath
        |> client.DeleteAsync<TemplateSearch>
        |> Async.AwaitTask
        |> Async.Ignore
    let get (client: IElasticClient) (templateId: TemplateId) =
        templateId
        |> string
        |> Id
        |> DocumentPath
        |> client.GetAsync<TemplateSearch>
        |> Task.map (fun x -> x.Source |> Core.toOption)
        |> Async.AwaitTask

module Deck =
    let delete (client: IElasticClient) (deckId: DeckId) =
        deckId
        |> string
        |> Id
        |> DocumentPath
        |> client.DeleteAsync<DeckSearch>
        |> Async.AwaitTask
        |> Async.Ignore
    let get (client: IElasticClient) (deckId: DeckId) =
        deckId
        |> string
        |> Id
        |> DocumentPath
        |> client.GetAsync<DeckSearch>
        |> Task.map (fun x -> x.Source |> Core.toOption)
        |> Async.AwaitTask

type IClient =
    abstract member UpsertExample        : ExampleId  -> Map<string, obj> -> Async<unit>
    abstract member UpsertTemplate       : TemplateId -> Map<string, obj> -> Async<unit>
    abstract member UpsertDeck           : DeckId     -> Map<string, obj> -> Async<unit>

    abstract member SetExampleCollected  : ExampleId  -> int              -> Async<unit>
    abstract member SetTemplateCollected : TemplateId -> int              -> Async<unit>
    abstract member SetDeckExampleCount  : DeckId     -> int              -> Async<unit>

    abstract member GetExample           : ExampleId                      -> Async<Option<ExampleSearch>>

    abstract member GetTemplate          : TemplateId                     -> Async<Option<TemplateSearch>>
    abstract member DeleteTemplate       : TemplateId                     -> Async<unit>

    abstract member GetDeck              : DeckId                         -> Async<Option<DeckSearch>>
    abstract member DeleteDeck           : DeckId                         -> Async<unit>

type Client (client: IElasticClient) =
    interface IClient with
        member _.UpsertExample         id x = Elsea.Example .UpsertSearch   (client, string id, x) |> Async.AwaitTask
        member _.UpsertDeck            id x = Elsea.Deck    .UpsertSearch   (client, string id, x) |> Async.AwaitTask
        member _.UpsertTemplate        id x = Elsea.Template.UpsertSearch   (client, string id, x) |> Async.AwaitTask
        
        member _.SetExampleCollected   id x = Elsea.Example .SetCollected   (client, string id, x) |> Async.AwaitTask
        member _.SetTemplateCollected  id x = Elsea.Template.SetCollected   (client, string id, x) |> Async.AwaitTask
        member _.SetDeckExampleCount   id x = Elsea.Deck    .SetExampleCount(client, string id, x) |> Async.AwaitTask
        
        member _.GetExample     exampleId               = exampleId  |> Example.get     client
        
        member _.GetTemplate    templateId              = templateId |> Template.get    client
        member _.DeleteTemplate templateId              = templateId |> Template.delete client
        
        member _.GetDeck        deckId                  = deckId     |> Deck.get        client
        member _.DeleteDeck     deckId                  = deckId     |> Deck.delete     client
    
#if DEBUG // it could be argued that test stuff should only be in test assemblies, but I'm gonna put stuff that's tightly coupled together. Easier to make changes.
type NoopClient () =
    interface IClient with
        member _.UpsertExample         _ _ = Async.singleton ()
        member _.UpsertTemplate        _ _ = Async.singleton ()
        member _.UpsertDeck            _ _ = Async.singleton ()

        member _.SetExampleCollected   _ _ = Async.singleton ()
        member _.SetTemplateCollected  _ _ = Async.singleton ()
        member _.SetDeckExampleCount   _ _ = Async.singleton ()

        member _.GetExample            _   = failwith "not implemented"
        
        member _.GetTemplate           _   = failwith "not implemented"
        member _.DeleteTemplate        _   = Async.singleton ()
        
        member _.GetDeck               _   = failwith "not implemented"
        member _.DeleteDeck            _   = Async.singleton ()
#endif

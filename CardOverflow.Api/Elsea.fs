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
    let getExampleSearch (client: IElasticClient) (exampleId: string) =
        exampleId
        |> Id
        |> DocumentPath
        |> client.GetAsync<ExampleSearch>
        |> Task.map (fun x -> x.Source |> Core.toOption)
        |> Async.AwaitTask

module Template =
    open Template
    let delete (client: IElasticClient) (templateId: TemplateId) =
        templateId
        |> string
        |> Id
        |> DocumentPath
        |> client.DeleteAsync<TemplateSearch>
        |> Async.AwaitTask
        |> Async.Ignore
    let getTemplateSearch (client: IElasticClient) (templateId: string) =
        templateId
        |> Id
        |> DocumentPath
        |> client.GetAsync<TemplateSearch>
        |> Task.map (fun x -> x.Source |> Core.toOption)
        |> Async.AwaitTask

open System.Threading.Tasks

type IClient =
   abstract member GetExampleSearch    : ExampleId -> Async<ExampleSearch option>
   
   abstract member GetTemplateSearch    : TemplateId -> Async<Option<TemplateSearch>>
   abstract member DeleteTemplate       : TemplateId -> Async<unit>

type Client (client: IElasticClient, kvs: KeyValueStore) =
    interface IClient with
        member    _.GetExampleSearch (exampleId: ExampleId) =
            Example.getExampleSearch client (string exampleId)
        
        member     _.GetTemplateSearch (templateId: TemplateId) =
            Template.getTemplateSearch client (string templateId)
        member     _.DeleteTemplate (templateId: TemplateId) =
            Template.delete client templateId
    
#if DEBUG // it could be argued that test stuff should only be in test assemblies, but I'm gonna put stuff that's tightly coupled together. Easier to make changes.
type NoopClient () =
    interface IClient with
        member _.GetExampleSearch     (exampleId: ExampleId)                          = failwith "not implemented"
        
        member _.GetTemplateSearch    (templateId: TemplateId)                        = failwith "not implemented"
        member _.DeleteTemplate       (templateId: TemplateId)                        = failwith "not implemented"
#endif

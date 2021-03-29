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
open Nest
open FSharp.Control.Tasks
open Newtonsoft.Json
open Elasticsearch.Net

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

let sourceSerializerFactory =
    ConnectionSettings.SourceSerializerFactory
        (fun x y -> ElseJsonSerializer (x, y) :> IElasticsearchSerializer)

type Client (client: ElasticClient) =
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
    member this.UpsertExample' (exampleId: string) e =
        match e with
        | Example.Events.Created summary ->
            client.IndexDocumentAsync summary |> Task.map ignore
        | Example.Events.Edited edited -> task {
            let! summary = this.GetExample exampleId |> Async.StartAsTask // do NOT read from the KeyValueStore to maintain consistency! Also, we can't use an anonymous record to update because it'll replace RevisionIds when we want to append. lowTODO elasticsearch can append RevisionIds
            let! _ = summary |> Example.Fold.evolveEdited edited |> client.IndexDocumentAsync
            return ()
        }
        |> Async.AwaitTask
    member this.UpsertExample (exampleId: ExampleId) =
        exampleId.ToString() |> this.UpsertExample'
    member _.GetExample (exampleId: string) =
        client.GetAsync<Example.Events.Summary>(
            exampleId |> Id |> DocumentPath
        ) |> Task.map (fun x -> x.Source)
        |> Async.AwaitTask
    member this.GetExample (exampleId: ExampleId) =
        exampleId.ToString() |> this.GetExample
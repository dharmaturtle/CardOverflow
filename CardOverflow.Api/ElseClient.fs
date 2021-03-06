namespace CardOverflow.Api

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

type ElseClient (client: ElasticClient) =
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
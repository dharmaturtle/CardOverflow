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

type ElseClient (client: ElasticClient) =
    member _.UpsertConcept' (conceptId: string) e =
        match e with
        | Concept.Events.Created summary ->
            client.IndexDocumentAsync summary |> Task.map ignore
        | Concept.Events.DefaultBranchChanged b ->
            client.UpdateAsync<obj>(
                conceptId |> Id |> DocumentPath,
                fun ud ->
                    ud
                        .Index<Concept.Events.Summary>()
                        .Doc {| DefaultBranchId = b.BranchId |}
                    :> IUpdateRequest<_,_>
            ) |> Task.map ignore
        |> Async.AwaitTask
    member this.UpsertConcept (conceptId: ConceptId) =
        conceptId.ToString() |> this.UpsertConcept'
    member _.GetConcept (conceptId: string) =
        client.GetAsync<Concept.Events.Summary>(
            conceptId |> Id |> DocumentPath
        ) |> Task.map (fun x -> x.Source)
        |> Async.AwaitTask
    member this.Get (conceptId: ConceptId) =
        conceptId.ToString() |> this.GetConcept
    member _.UpsertBranch' (branchId: string) e =
        match e with
        | Branch.Events.Created summary ->
            client.IndexDocumentAsync summary |> Task.map ignore
        | Branch.Events.Edited
            { LeafId             = leafId
              Title              = title
              TemplateRevisionId = templateRevisionId
              FieldValues        = fieldValues
              EditSummary        = editSummary } ->
            client.UpdateAsync<obj>(
                branchId |> Id |> DocumentPath,
                fun ud ->
                    ud
                        .Index<Branch.Events.Summary>()
                        .Doc
                        {| 
                            LeafId             = leafId
                            Title              = title
                            TemplateRevisionId = templateRevisionId
                            FieldValues        = fieldValues
                            EditSummary        = editSummary
                        |}
                    :> IUpdateRequest<_,_>
            ) |> Task.map ignore
        |> Async.AwaitTask
    member this.UpsertBranch (branchId: BranchId) =
        branchId.ToString() |> this.UpsertBranch'
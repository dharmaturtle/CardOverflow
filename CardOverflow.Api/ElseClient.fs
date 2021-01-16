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
    member _.UpsertStack' (stackId: string) e =
        match e with
        | Stack.Events.Snapshotted snapshot ->
            client.IndexDocumentAsync snapshot |> Task.map ignore
        | Stack.Events.DefaultBranchChanged b ->
            client.UpdateAsync<obj>(
                stackId |> Id |> DocumentPath,
                fun ud ->
                    ud
                        .Index<Stack.Events.Snapshotted>()
                        .Doc {| DefaultBranchId = b.BranchId |}
                    :> IUpdateRequest<_,_>
            ) |> Task.map ignore
        |> Async.AwaitTask
    member this.UpsertStack (stackId: StackId) =
        stackId.ToString() |> this.UpsertStack'
    member _.GetStack (stackId: string) =
        client.GetAsync<Stack.Events.Snapshotted>(
            stackId |> Id |> DocumentPath
        ) |> Task.map (fun x -> x.Source)
        |> Async.AwaitTask
    member this.Get (stackId: StackId) =
        stackId.ToString() |> this.GetStack
    member _.UpsertBranch' (branchId: string) e =
        match e with
        | Branch.Events.Snapshotted snapshot ->
            client.IndexDocumentAsync snapshot |> Task.map ignore
        | Branch.Events.Edited
            { LeafId      = leafId
              Title       = title
              GrompleafId = grompleafId
              FieldValues = fieldValues
              EditSummary = editSummary } ->
            client.UpdateAsync<obj>(
                branchId |> Id |> DocumentPath,
                fun ud ->
                    ud
                        .Index<Branch.Events.Snapshotted>()
                        .Doc
                        {| 
                            LeafId      = leafId
                            Title       = title
                            GrompleafId = grompleafId
                            FieldValues = fieldValues
                            EditSummary = editSummary
                        |}
                    :> IUpdateRequest<_,_>
            ) |> Task.map ignore
        |> Async.AwaitTask
    member this.UpsertBranch (branchId: BranchId) =
        branchId.ToString() |> this.UpsertBranch'
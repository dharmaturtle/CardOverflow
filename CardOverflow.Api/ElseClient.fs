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
    member _.Handle (stackId: string) = function
        | Stack.Events.Snapshotted snapshot ->
            client.IndexDocumentAsync snapshot |> Task.map ignore
        | Stack.Events.DefaultBranchChanged b ->
            client.UpdateAsync<obj>(
                stackId |> Id |> DocumentPath,
                fun ud ->
                    ud.Doc({| DefaultBranchId = b.BranchId |})
                    :> IUpdateRequest<_,_>
            ) |> Task.map ignore
    member this.Upsert (stackId: StackId) =
        stackId.ToString() |> this.Handle
    member _.GetStack (stackId: string) =
        client.GetAsync<Stack.Events.Snapshotted>(
            stackId |> Id |> DocumentPath
        ) |> Task.map (fun x -> x.Source)
    member this.Get (stackId: StackId) =
        stackId.ToString() |> this.GetStack
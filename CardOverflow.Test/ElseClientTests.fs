module ElseClientTests

open Xunit
open Serilog
open System
open Domain
open Equinox.MemoryStore
open FSharp.UMX
open FsCheck.Xunit
open CardOverflow.Pure
open CardOverflow.Test
open FsCodec
open FSharp.Control.Tasks
open System
open CardOverflow.Api
open NodaTime
open Nest
open System.Threading.Tasks
open Hedgehog
open FsToolkit.ErrorHandling
open AsyncOp

[<StandardProperty>]
let ``ElseClient can handle all Stack events`` ((snapshot:Stack.Events.Snapshot), branchId) = async {
    let client = TestEsContainer().ElseClient()

    // Snapshot
    do! snapshot
        |> Stack.Events.Event.Snapshot
        |> client.UpsertStack snapshot.Id
    
    do! client.Get snapshot.Id |>% Assert.equal snapshot

    // DefaultBranchChanged
    do! { Stack.Events.BranchId = branchId }
        |> Stack.Events.Event.DefaultBranchChanged
        |> client.UpsertStack snapshot.Id
    
    do! client.Get snapshot.Id |>% Assert.equal { snapshot with DefaultBranchId = branchId }
    }

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
open TestingInfrastructure
open System
open CardOverflow.Api
open NodaTime
open Nest
open System.Threading.Tasks
open Hedgehog
open FsToolkit.ErrorHandling

[<StandardProperty>]
let ``ElseClient can handle all Stack events`` ((snapshot:Stack.Events.Snapshotted), branchId) = task {
    let client = TestEsContainer().ElseClient()

    // Snapshot
    do! snapshot
        |> Stack.Events.Event.Snapshotted
        |> client.Upsert snapshot.Id
    
    do! client.Get snapshot.Id |>% Assert.equal snapshot

    // DefaultBranchChanged
    do! { Stack.Events.BranchId = branchId }
        |> Stack.Events.Event.DefaultBranchChanged
        |> client.Upsert snapshot.Id
    
    do! client.Get snapshot.Id |>% Assert.equal { snapshot with DefaultBranchId = branchId }
    }

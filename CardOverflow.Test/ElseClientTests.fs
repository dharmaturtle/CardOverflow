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
let ``ElseClient can handle all Stack events`` ((summary: Stack.Events.Summary), branchId) = async {
    let client = TestEsContainer().ElseClient()

    // Created
    do! summary
        |> Stack.Events.Event.Created
        |> client.UpsertStack summary.Id
    
    do! client.Get summary.Id |>% Assert.equal summary

    // DefaultBranchChanged
    do! { Stack.Events.BranchId = branchId }
        |> Stack.Events.Event.DefaultBranchChanged
        |> client.UpsertStack summary.Id
    
    do! client.Get summary.Id |>% Assert.equal { summary with DefaultBranchId = branchId }
    }

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
let ``ElseClient can handle all Concept events`` ((summary: Concept.Events.Summary), branchId) = async {
    let client = TestEsContainer().ElseClient()

    // Created
    do! summary
        |> Concept.Events.Event.Created
        |> client.UpsertConcept summary.Id
    
    do! client.Get summary.Id |>% Assert.equal summary

    // DefaultBranchChanged
    do! { Concept.Events.BranchId = branchId }
        |> Concept.Events.Event.DefaultBranchChanged
        |> client.UpsertConcept summary.Id
    
    do! client.Get summary.Id |>% Assert.equal { summary with DefaultBranchId = branchId }
    }

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
let ``ElseClient can handle all Example events`` (summary: Example.Events.Summary) (edited: Example.Events.Edited) = async {
    let client = TestEsContainer().ElseClient()

    // Created
    do! summary
        |> Example.Events.Event.Created
        |> client.UpsertExample summary.Id
    
    do! client.GetExample summary.Id |>% Assert.equal summary

    // DefaultExampleChanged
    do! edited
        |> Example.Events.Event.Edited
        |> client.UpsertExample summary.Id
    
    do! client.GetExample summary.Id |>% Assert.equal (summary |> Example.Fold.evolveEdited edited)
    }

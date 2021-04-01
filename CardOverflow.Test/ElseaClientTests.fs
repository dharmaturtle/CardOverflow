module ElseaClientTests

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
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``ElseaClient can handle all Example events`` { ExampleSummary = summary; Edit = edit } = async {
    let client = TestEsContainer().ElseaClient()

    // Created
    do! summary
        |> Example.Events.Event.Created
        |> client.UpsertExample summary.Id
    
    do! client.GetExample summary.Id |>% Assert.equal summary

    // DefaultExampleChanged
    do! edit
        |> Example.Events.Event.Edited
        |> client.UpsertExample summary.Id
    
    do! client.GetExample summary.Id |>% Assert.equal (summary |> Example.Fold.evolveEdited edit)
    }

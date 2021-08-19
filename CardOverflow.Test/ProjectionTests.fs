module ProjectionTests

open Xunit
open CardOverflow.Pure
open Serilog
open System
open Domain
open Equinox.MemoryStore
open FSharp.UMX
open FsCheck.Xunit
open CardOverflow.Pure
open CardOverflow.Test
open EventAppender
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp
open Domain.Summary
open Domain.Projection

[<StandardProperty>]
let ``ToUrl.example and ToUrl.parse roundtrips`` (exampleRevisionId: ExampleRevisionId) =
    exampleRevisionId
    |> ToUrl.example
    |> ToUrl.parse
    |> Option.get
    |> (fun (guid, ordinal) -> (% guid, % ordinal))
    |> Assert.equal exampleRevisionId

[<StandardProperty>]
let ``ToUrl.template and ToUrl.parse roundtrips`` (templateRevisionId: TemplateRevisionId) =
    templateRevisionId
    |> ToUrl.template
    |> ToUrl.parse
    |> Option.get
    |> (fun (guid, ordinal) -> (% guid, % ordinal))
    |> Assert.equal templateRevisionId

[<StandardProperty>]
let ``raw and ToUrl.parse roundtrips`` (pair: Guid * int) =
    pair
    |> ToUrl.raw
    |> ToUrl.parse
    |> Option.get
    |> Assert.equal pair

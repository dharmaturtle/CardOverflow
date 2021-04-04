module ExampleSagaTests

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
open EventWriter
open Hedgehog
open CardOverflow.Api
open FsToolkit.ErrorHandling

[<StandardProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``ExampleWriter roundtrips`` { Author = author; TemplateSummary = templateSummary; ExampleSummary = exampleSummary; Edit = exampleEdited } ease = asyncResult {
    let       stackId = % Guid.NewGuid()
    let cardSettingId = % Guid.NewGuid()
    let        deckId = % Guid.NewGuid()
    let c = TestEsContainer()
    do! c.UserSagaWriter().Create author
    do! c.TemplateWriter().Create templateSummary
    let exampleSaga = c.ExampleSagaWriter()
    
    (***   when created, then azure table updated   ***)
    do! exampleSaga.Create exampleSummary stackId cardSettingId ease deckId
    
    let! actual = c.KeyValueStore().GetExample exampleSummary.Id
    Assert.equal exampleSummary actual
    
    (***   when edited, then azure table updated   ***)
    //do! exampleSaga.Edit (exampleEdited, exampleSummary.Id, author.Id)
    
    //let! actual = c.KeyValueStore().GetExample exampleSummary.Id
    //exampleSummary |> Example.Fold.evolveEdited exampleEdited |> Assert.equal actual
    }

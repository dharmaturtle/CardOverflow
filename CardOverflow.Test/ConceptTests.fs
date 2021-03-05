module ConceptTests

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
open EventWriter
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp

[<StandardProperty>]
let ``ChangeDefaultExample works`` (authorId, { NewOriginal = s; NewExample = b; Template = template }) = asyncResult {
    let c = TestEsContainer()
    do! c.TemplateWriter().Create template
    let conceptExampleWriter = c.ConceptExampleWriter()
    let conceptWriter = c.ConceptWriter()
    do! conceptExampleWriter.Upsert authorId s
    do! conceptExampleWriter.Upsert authorId b

    do! conceptWriter.ChangeDefaultExample (% s.Ids.ConceptId) (% b.Ids.ExampleId) authorId

    % b.Ids.ConceptId
    |> c.ConceptEvents
    |> Seq.last
    |> Assert.equal (Concept.Events.DefaultExampleChanged { ExampleId = % b.Ids.ExampleId })
    }

[<StandardProperty>]
let ``ChangeDefaultExample fails when example is on a different concept`` (authorId, { NewOriginal = s1; Template = template1 }, { NewOriginal = s2; NewExample = b2; Template = template2 }) = asyncResult {
    let c = TestEsContainer()
    do! c.TemplateWriter().Create template1
    do! c.TemplateWriter().Create template2
    let conceptExampleWriter = c.ConceptExampleWriter()
    let conceptWriter = c.ConceptWriter()
    do! conceptExampleWriter.Upsert authorId s1
    do! conceptExampleWriter.Upsert authorId s2
    do! conceptExampleWriter.Upsert authorId b2

    do! conceptWriter.ChangeDefaultExample (% s1.Ids.ConceptId) (% b2.Ids.ExampleId) authorId
        
    |>% Result.getError
    |>% Assert.equal $"Example {b2.Ids.ExampleId} doesn't belong to Concept {s1.Ids.ConceptId}"
    }

[<StandardProperty>]
let ``ChangeDefaultExample fails when example author tries to be default`` (conceptAuthorId, exampleAuthorId, { NewOriginal = s; NewExample = b; Template = template }) = asyncResult {
    let c = TestEsContainer()
    do! c.TemplateWriter().Create template
    let conceptExampleWriter = c.ConceptExampleWriter()
    let conceptWriter = c.ConceptWriter()
    do! conceptExampleWriter.Upsert conceptAuthorId s
    do! conceptExampleWriter.Upsert exampleAuthorId b

    do! conceptWriter.ChangeDefaultExample (% s.Ids.ConceptId) (% b.Ids.ExampleId) exampleAuthorId

    |>% Result.getError
    |>% Assert.equal $"Concept {s.Ids.ConceptId} doesn't belong to User {exampleAuthorId}"
    }

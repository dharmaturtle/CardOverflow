module ConceptExampleTests

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
let ``ConceptExampleWriter.Upsert persists both summaries`` (authorId, command, tags) = asyncResult {
    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags }
    let c = TestEsContainer()
    let conceptExampleWriter = c.ConceptExampleWriter()
    let expectedConcept, expectedExample = ConceptExample.conceptExample authorId command None "Default"
    
    do! conceptExampleWriter.Upsert(authorId, command)
    
    // memory store roundtrips
    % expectedConcept.Id
    |> c.ConceptEvents
    |> Assert.Single
    |> Assert.equal (Concept.Events.Created expectedConcept)
    % expectedExample.Id
    |> c.ExampleEvents
    |> Assert.Single
    |> Assert.equal (Example.Events.Created expectedExample)
    
    // azure table roundtrips
    let! actual, _ = c.TableClient().GetConcept (string command.Ids.ConceptId)
    Assert.equal expectedConcept actual
    let! actual, _ = c.TableClient().GetExample (string command.Ids.ExampleId)
    Assert.equal expectedExample actual
    let! actual, _ = c.TableClient().GetExampleRevision (string command.Ids.RevisionId)
    Assert.equal (Example.toRevisionSummary expectedExample) actual
    }
    
[<StandardProperty>]
let ``ConceptExampleWriter.Upsert persists edit`` (authorId, command1, command2, tags, title) = asyncResult {
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewRevision_Title title; Ids = { command1.Ids with RevisionId = command2.Ids.RevisionId } }
    let c = TestEsContainer()
    let conceptExampleWriter = c.ConceptExampleWriter()
    let expectedConcept, expectedBanchSummary1 = ConceptExample.conceptExample authorId command1 None "Default"
    let expectedExampleEdit : Example.Events.Edited =
        let _, b = ConceptExample.conceptExample authorId command2 None title
        { RevisionId             = b.RevisionIds.Head
          Title              = b.Title
          TemplateRevisionId = b.TemplateRevisionId
          FieldValues        = b.FieldValues
          EditSummary        = b.EditSummary }
    do! conceptExampleWriter.Upsert(authorId, command1)
        
    do! conceptExampleWriter.Upsert(authorId, command2)

    % command2.Ids.ExampleId
    |> c.ExampleEvents
    |> Seq.last
    |> Assert.equal (Example.Events.Edited expectedExampleEdit)
    
    // azure table roundtrips
    let! actual, _ = c.TableClient().GetConcept (string command2.Ids.ConceptId)
    Assert.equal expectedConcept actual
    let! actual, _ = c.TableClient().GetExample (string command2.Ids.ExampleId)
    let evolvedSummary = Example.Fold.evolveEdited expectedExampleEdit expectedBanchSummary1
    Assert.equal evolvedSummary actual
    let! actual, _ = c.TableClient().GetExampleRevision (string command1.Ids.RevisionId)
    Assert.equal (Example.toRevisionSummary expectedBanchSummary1) actual
    let! actual, _ = c.TableClient().GetExampleRevision (string command2.Ids.RevisionId)
    Assert.equal (Example.toRevisionSummary evolvedSummary) actual
    }
    
[<StandardProperty>]
let ``ConceptExampleWriter.Upsert persists new example`` (authorId, { NewOriginal = newOriginal; NewExample = newExample; ExampleTitle = title }) =
    let c = TestEsContainer()
    let conceptExampleWriter = c.ConceptExampleWriter()
    let expectedExample : Example.Events.Summary =
        ConceptExample.conceptExample authorId newExample None title
        |> snd
    conceptExampleWriter.Upsert(authorId, newOriginal) |> RunSynchronously.OkEquals ()
        
    conceptExampleWriter.Upsert(authorId, newExample) |> RunSynchronously.OkEquals ()

    % newExample.Ids.ExampleId
    |> c.ExampleEvents
    |> Seq.exactlyOne
    |> Assert.equal (Example.Events.Created expectedExample)

[<StandardProperty>]
let ``ConceptExampleWriter.Upsert fails to persist edit with duplicate revisionId`` (authorId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewRevision_Title title; Ids = command1.Ids }
    let c = TestEsContainer()
    let conceptExampleWriter = c.ConceptExampleWriter()
    conceptExampleWriter.Upsert(authorId, command1) |> RunSynchronously.OkEquals ()
        
    conceptExampleWriter.Upsert(authorId, command2)

    |> RunSynchronously.ErrorEquals $"Duplicate revisionId:{command1.Ids.RevisionId}"

[<StandardProperty>]
let ``ConceptExampleWriter.Upsert fails to persist edit with another author`` (authorId, hackerId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewRevision_Title title; Ids = command1.Ids }
    let c = TestEsContainer()
    let conceptExampleWriter = c.ConceptExampleWriter()
    conceptExampleWriter.Upsert(authorId, command1) |> RunSynchronously.OkEquals ()
        
    conceptExampleWriter.Upsert(hackerId, command2)

    |> RunSynchronously.ErrorEquals $"You ({hackerId}) aren't the author"

[<StandardProperty>]
let ``ConceptExampleWriter.Upsert fails to insert twice`` (authorId, command, tags) =
    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags }
    let c = TestEsContainer()
    let conceptExampleWriter = c.ConceptExampleWriter()
    conceptExampleWriter.Upsert(authorId, command) |> RunSynchronously.OkEquals ()
        
    conceptExampleWriter.Upsert(authorId, command)

    |> RunSynchronously.ErrorEquals $"Concept '{command.Ids.ConceptId}' already exists."

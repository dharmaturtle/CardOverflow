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

//[<StandardProperty>]
//let ``ConceptExampleWriter.Upsert persists both summaries`` (authorId, command, tags, (templateSummary: Template.Events.Summary)) = asyncResult {
//    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let c = TestEsContainer()
//    do! c.TemplateWriter().Create templateSummary
//    let conceptExampleWriter = c.ConceptExampleWriter()
//    let expectedConcept, expectedExample = ConceptExample.conceptExample authorId command None "Default"
    
//    do! conceptExampleWriter.Upsert authorId command
    
//    // memory store roundtrips
//    % expectedConcept.Id
//    |> c.ConceptEvents
//    |> Assert.Single
//    |> Assert.equal (Concept.Events.Created expectedConcept)
//    % expectedExample.Id
//    |> c.ExampleEvents
//    |> Assert.Single
//    |> Assert.equal (Example.Events.Created expectedExample)
    
//    // azure table roundtrips
//    let! actual, _ = c.KeyValueStore().GetConcept (string command.Ids.ConceptId)
//    Assert.equal expectedConcept actual
//    let! actual, _ = c.KeyValueStore().GetExample (string command.Ids.ExampleId)
//    Assert.equal expectedExample actual
//    let! actual, _ = c.KeyValueStore().GetExampleRevision (string command.Ids.RevisionId)
//    let! template, _ = c.KeyValueStore().GetTemplateRevision actual.TemplateRevision.Id
//    Assert.equal (Example.toRevisionSummary template expectedExample) actual
//    }
    
//[<StandardProperty>]
//let ``ConceptExampleWriter.Upsert persists edit`` (authorId, command1, command2, tags, title, (templateSummary: Template.Events.Summary)) = asyncResult {
//    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let command2 = { command2 with Kind = UpsertKind.NewRevision_Title title; TemplateRevisionId = templateSummary.RevisionIds.Head; Ids = { command1.Ids with RevisionId = command2.Ids.RevisionId } }
//    let c = TestEsContainer()
//    do! c.TemplateWriter().Create templateSummary
//    let conceptExampleWriter = c.ConceptExampleWriter()
//    let expectedConcept, expectedBanchSummary1 = ConceptExample.conceptExample authorId command1 None "Default"
//    let expectedExampleEdit : Example.Events.Edited =
//        let _, b = ConceptExample.conceptExample authorId command2 None title
//        { RevisionId             = b.RevisionIds.Head
//          Title              = b.Title
//          TemplateRevisionId = b.TemplateRevisionId
//          FieldValues        = b.FieldValues
//          EditSummary        = b.EditSummary }
//    do! conceptExampleWriter.Upsert authorId command1
        
//    do! conceptExampleWriter.Upsert authorId command2

//    % command2.Ids.ExampleId
//    |> c.ExampleEvents
//    |> Seq.last
//    |> Assert.equal (Example.Events.Edited expectedExampleEdit)
    
//    // azure table roundtrips
//    let! actual, _ = c.KeyValueStore().GetConcept (string command2.Ids.ConceptId)
//    Assert.equal expectedConcept actual
//    let! actual, _ = c.KeyValueStore().GetExample (string command2.Ids.ExampleId)
//    let evolvedSummary = Example.Fold.evolveEdited expectedExampleEdit expectedBanchSummary1
//    Assert.equal evolvedSummary actual
//    let! actual, _ = c.KeyValueStore().GetExampleRevision (string command1.Ids.RevisionId)
//    let! template, _ = c.KeyValueStore().GetTemplateRevision actual.TemplateRevision.Id
//    Assert.equal (Example.toRevisionSummary template expectedBanchSummary1) actual
//    let! actual, _ = c.KeyValueStore().GetExampleRevision (string command2.Ids.RevisionId)
//    Assert.equal (Example.toRevisionSummary template evolvedSummary) actual
//    }
    
//[<StandardProperty>]
//let ``ConceptExampleWriter.Upsert persists new example`` (authorId, { NewOriginal = newOriginal; NewExample = newExample; Template = template; ExampleTitle = title }) = asyncResult {
//    let c = TestEsContainer()
//    do! c.TemplateWriter().Create template
//    let conceptExampleWriter = c.ConceptExampleWriter()
//    let expectedExample : Example.Events.Summary =
//        ConceptExample.conceptExample authorId newExample None title
//        |> snd
//    do! conceptExampleWriter.Upsert authorId newOriginal
        
//    do! conceptExampleWriter.Upsert authorId newExample

//    % newExample.Ids.ExampleId
//    |> c.ExampleEvents
//    |> Seq.exactlyOne
//    |> Assert.equal (Example.Events.Created expectedExample)
//    }

//[<StandardProperty>]
//let ``ConceptExampleWriter.Upsert rejects edit with duplicate revisionId`` (authorId, command1, command2, tags, title, (templateSummary: Template.Events.Summary)) = asyncResult {
//    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let command2 = { command2 with Kind = UpsertKind.NewRevision_Title title; TemplateRevisionId = templateSummary.RevisionIds.Head; Ids = command1.Ids }
//    let c = TestEsContainer()
//    do! c.TemplateWriter().Create templateSummary
//    let conceptExampleWriter = c.ConceptExampleWriter()
//    do! conceptExampleWriter.Upsert authorId command1
        
//    let! (result: Result<_,_>) = conceptExampleWriter.Upsert authorId command2

//    Assert.equal result.error $"Duplicate RevisionId:{command1.Ids.RevisionId}"
//    }

//[<StandardProperty>]
//let ``ConceptExampleWriter.Upsert fails to persist edit with another author`` (authorId, hackerId, command1, command2, tags, title, (templateSummary: Template.Events.Summary)) = asyncResult {
//    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let command2 = { command2 with Kind = UpsertKind.NewRevision_Title title; TemplateRevisionId = templateSummary.RevisionIds.Head; Ids = command1.Ids }
//    let c = TestEsContainer()
//    do! c.TemplateWriter().Create templateSummary
//    let conceptExampleWriter = c.ConceptExampleWriter()
//    do! conceptExampleWriter.Upsert authorId command1
        
//    let! (result: Result<_,_>) = conceptExampleWriter.Upsert hackerId command2

//    Assert.equal result.error $"You ({hackerId}) aren't the author of Example {command1.Ids.ExampleId}."
//    }

//[<StandardProperty>]
//let ``ConceptExampleWriter.Upsert fails to insert twice`` (authorId, command, tags, (templateSummary: Template.Events.Summary)) = asyncResult {
//    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let c = TestEsContainer()
//    do! c.TemplateWriter().Create templateSummary
//    let conceptExampleWriter = c.ConceptExampleWriter()
//    do! conceptExampleWriter.Upsert authorId command
        
//    let! (result: Result<_,_>) = conceptExampleWriter.Upsert authorId command

//    Assert.equal result.error $"Concept '{command.Ids.ConceptId}' already exists."
//    }

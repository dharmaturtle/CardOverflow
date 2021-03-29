module ExampleTests

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
let ``ExampleWriter roundtrips`` { TemplateSummary = templateSummary; ExampleSummary = exampleSummary; Edit = exampleEdited } = asyncResult {
    let c = TestEsContainer()
    do! c.TemplateWriter().Create templateSummary
    let exampleWriter = c.ExampleWriter()
    
    (***   when created, then azure table updated   ***)
    do! exampleWriter.Create exampleSummary
    
    let! actual = c.KeyValueStore().GetExample exampleSummary.Id
    Assert.equal exampleSummary actual
    
    (***   when edited, then azure table updated   ***)
    do! exampleWriter.Edit (exampleEdited, exampleSummary.Id, exampleSummary.AuthorId)
    
    let! actual = c.KeyValueStore().GetExample exampleSummary.Id
    exampleSummary |> Example.Fold.evolveEdited exampleEdited |> Assert.equal actual
    }
    
//[<StandardProperty>]
//let ``ExampleWriter.Upsert persists new example`` (authorId, { NewOriginal = newOriginal; NewExample = newExample; Template = template; ExampleTitle = title }) = asyncResult {
//    let c = TestEsContainer()
//    do! c.TemplateWriter().Create template
//    let exampleWriter = c.ExampleWriter()
//    let expectedExample : Example.Events.Summary =
//        Example.example authorId newExample None title
//        |> snd
//    do! exampleWriter.Upsert authorId newOriginal
        
//    do! exampleWriter.Upsert authorId newExample

//    % newExample.Ids.ExampleId
//    |> c.ExampleEvents
//    |> Seq.exactlyOne
//    |> Assert.equal (Example.Events.Created expectedExample)
//    }

//[<StandardProperty>]
//let ``ExampleWriter.Upsert rejects edit with duplicate revisionId`` (authorId, command1, command2, tags, title, (templateSummary: Template.Events.Summary)) = asyncResult {
//    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let command2 = { command2 with Kind = UpsertKind.NewRevision_Title title; TemplateRevisionId = templateSummary.RevisionIds.Head; Ids = command1.Ids }
//    let c = TestEsContainer()
//    do! c.TemplateWriter().Create templateSummary
//    let exampleWriter = c.ExampleWriter()
//    do! exampleWriter.Upsert authorId command1
        
//    let! (result: Result<_,_>) = exampleWriter.Upsert authorId command2

//    Assert.equal result.error $"Duplicate RevisionId:{command1.Ids.RevisionId}"
//    }

//[<StandardProperty>]
//let ``ExampleWriter.Upsert fails to persist edit with another author`` (authorId, hackerId, command1, command2, tags, title, (templateSummary: Template.Events.Summary)) = asyncResult {
//    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let command2 = { command2 with Kind = UpsertKind.NewRevision_Title title; TemplateRevisionId = templateSummary.RevisionIds.Head; Ids = command1.Ids }
//    let c = TestEsContainer()
//    do! c.TemplateWriter().Create templateSummary
//    let exampleWriter = c.ExampleWriter()
//    do! exampleWriter.Upsert authorId command1
        
//    let! (result: Result<_,_>) = exampleWriter.Upsert hackerId command2

//    Assert.equal result.error $"You ({hackerId}) aren't the author of Example {command1.Ids.ExampleId}."
//    }

//[<StandardProperty>]
//let ``ExampleWriter.Upsert fails to insert twice`` (authorId, command, tags, (templateSummary: Template.Events.Summary)) = asyncResult {
//    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let c = TestEsContainer()
//    do! c.TemplateWriter().Create templateSummary
//    let exampleWriter = c.ExampleWriter()
//    do! exampleWriter.Upsert authorId command
        
//    let! (result: Result<_,_>) = exampleWriter.Upsert authorId command

//    Assert.equal result.error $"Concept '{command.Ids.ConceptId}' already exists."
//    }

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
let ``ExampleWriter roundtrips`` { Author = author; TemplateSummary = templateSummary; ExampleSummary = exampleSummary; Edit = exampleEdited; Stack = stackSummary} = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaWriter().Create author
    do! c.TemplateWriter().Create templateSummary
    let exampleWriter = c.ExampleWriter()
    let stackWriter = c.StackWriter()
    
    (***   when Example created, then azure table updated   ***)
    do! exampleWriter.Create exampleSummary
    
    let! actual = c.KeyValueStore().GetExample exampleSummary.Id
    Assert.equal exampleSummary actual

    (***   when Stack created, then azure table updated   ***)
    do! stackWriter.Create stackSummary

    let! actual = c.KeyValueStore().GetStack stackSummary.Id
    Assert.equal stackSummary actual
    
    (***   when edited, then azure table updated   ***)
    do! exampleWriter.Edit (exampleEdited, exampleSummary.Id, author.Id)
    
    let! actual = c.KeyValueStore().GetExample exampleSummary.Id
    exampleSummary |> Example.Fold.evolveEdited exampleEdited |> Assert.equal actual

    (***   when Stack's Revision changed, then azure table updated   ***)
    let revisionChanged : Stack.Events.RevisionChanged = { RevisionId = exampleEdited.RevisionId }
    do! stackWriter.ChangeRevision revisionChanged author.Id stackSummary.Id
    
    let! actual = c.KeyValueStore().GetStack stackSummary.Id
    stackSummary |> Stack.Fold.evolveRevisionChanged revisionChanged |> Assert.equal actual
    }
    
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

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
open EventAppender
open Hedgehog
open CardOverflow.Api
open FsToolkit.ErrorHandling

[<StandardProperty>]
let ``ExampleAppender roundtrips`` { Author = author; TemplateCreated = templateCreated; ExampleCreated = exampleCreated; Edit = exampleEdited; Stack = stackSummary} = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create author
    do! c.TemplateComboAppender().Create templateCreated
    let exampleAppender = c.ExampleAppender()
    let stackAppender = c.StackAppender()
    
    (***   when Example created, then azure table updated   ***)
    do! exampleAppender.Create exampleCreated
    
    let! actual = c.KeyValueStore().GetExample exampleCreated.Id
    let exampleSummary = Example.Fold.evolveCreated exampleCreated
    Assert.equal exampleSummary actual

    (***   when Stack created, then azure table updated   ***)
    do! stackAppender.Create stackSummary

    let! actual = c.KeyValueStore().GetStack stackSummary.Id
    Assert.equal stackSummary actual
    
    (***   when edited, then azure table updated   ***)
    do! exampleAppender.Edit exampleEdited exampleCreated.Id author.Id
    
    let! actual = c.KeyValueStore().GetExample exampleCreated.Id
    exampleSummary |> Example.Fold.evolveEdited exampleEdited |> Assert.equal actual

    (***   when Stack's Revision changed, then azure table updated   ***)
    let revisionChanged : Stack.Events.RevisionChanged = { RevisionId = exampleCreated.Id, exampleEdited.Revision }
    do! stackAppender.ChangeRevision revisionChanged author.Id stackSummary.Id
    
    let! actual = c.KeyValueStore().GetStack stackSummary.Id
    stackSummary |> Stack.Fold.evolveRevisionChanged revisionChanged |> Assert.equal actual
    }
    
//[<StandardProperty>]
//let ``ExampleAppender.Upsert rejects edit with duplicate revisionId`` (authorId, command1, command2, tags, title, (templateSummary: Template.Events.Summary)) = asyncResult {
//    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let command2 = { command2 with Kind = UpsertKind.NewRevision_Title title; TemplateRevisionId = templateSummary.RevisionIds.Head; Ids = command1.Ids }
//    let c = TestEsContainer()
//    do! c.TemplateAppender().Create templateSummary
//    let exampleAppender = c.ExampleAppender()
//    do! exampleAppender.Upsert authorId command1
        
//    let! (result: Result<_,_>) = exampleAppender.Upsert authorId command2

//    Assert.equal result.error $"Duplicate RevisionId:{command1.Ids.RevisionId}"
//    }

//[<StandardProperty>]
//let ``ExampleAppender.Upsert fails to persist edit with another author`` (authorId, hackerId, command1, command2, tags, title, (templateSummary: Template.Events.Summary)) = asyncResult {
//    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let command2 = { command2 with Kind = UpsertKind.NewRevision_Title title; TemplateRevisionId = templateSummary.RevisionIds.Head; Ids = command1.Ids }
//    let c = TestEsContainer()
//    do! c.TemplateAppender().Create templateSummary
//    let exampleAppender = c.ExampleAppender()
//    do! exampleAppender.Upsert authorId command1
        
//    let! (result: Result<_,_>) = exampleAppender.Upsert hackerId command2

//    Assert.equal result.error $"You ({hackerId}) aren't the author of Example {command1.Ids.ExampleId}."
//    }

//[<StandardProperty>]
//let ``ExampleAppender.Upsert fails to insert twice`` (authorId, command, tags, (templateSummary: Template.Events.Summary)) = asyncResult {
//    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let c = TestEsContainer()
//    do! c.TemplateAppender().Create templateSummary
//    let exampleAppender = c.ExampleAppender()
//    do! exampleAppender.Upsert authorId command
        
//    let! (result: Result<_,_>) = exampleAppender.Upsert authorId command

//    Assert.equal result.error $"Concept '{command.Ids.ConceptId}' already exists."
//    }

[<StandardProperty>]
let ``ExampleRevisionId ser des roundtrips`` id =
    id |>ExampleRevisionId.ser |> ExampleRevisionId.des = id

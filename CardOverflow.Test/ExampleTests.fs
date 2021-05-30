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
open Domain.Projection

[<StandardProperty>]
let ``ExampleAppender roundtrips`` { SignedUp = signedUp; TemplateCreated = templateCreated; TemplateEdited = templateEdited; ExampleCreated = exampleCreated; ExampleCreated2 = exampleCreated2; Edit = exampleEdited; StackCreated = stackCreated } (meta1: Meta) (meta2: Meta) (meta3: Meta) = asyncResult {
    let meta1 = { meta1 with UserId = signedUp.Meta.UserId }
    let meta2 = { meta2 with UserId = signedUp.Meta.UserId }
    let meta3 = { meta3 with UserId = signedUp.Meta.UserId }
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    do! c.TemplateAppender().Create templateCreated
    let exampleAppender = c.ExampleAppender()
    let stackAppender = c.StackAppender()
    let collectors  () = c.KeyValueStore().GetExample exampleCreated .Id |> Async.map (fun x -> x.Revisions |> List.map (fun x -> x.Collectors))
    let collectors2 () = c.KeyValueStore().GetExample exampleCreated2.Id |> Async.map (fun x -> x.Revisions |> List.map (fun x -> x.Collectors))
    
    (***   when Example created, then azure table updated   ***)
    do! exampleAppender.Create exampleCreated
    
    let! actual = c.KeyValueStore().GetExample exampleCreated.Id
    let exampleSummary = Example.Fold.evolveCreated exampleCreated
    Assert.equal exampleSummary (actual |> Kvs.toExample)

    let! cs = collectors()
    Assert.equal [0] cs

    (***   when Stack created, then azure table updated   ***)
    let stackSummary = Stack.Fold.evolveCreated stackCreated
    do! stackAppender.Create stackCreated

    let! actual = c.KeyValueStore().GetStack stackCreated.Id
    Assert.equal stackSummary actual

    let! cs = collectors()
    Assert.equal [1] cs
    
    (***   when edited, then azure table updated   ***)
    do! exampleAppender.Edit exampleEdited exampleCreated.Id
    
    let! actual = c.KeyValueStore().GetExample exampleCreated.Id
    let exampleSummary = exampleSummary |> Example.Fold.evolveEdited exampleEdited
    Assert.equal (actual |> Kvs.toExample) exampleSummary

    let! cs = collectors()
    Assert.equal [0;1] cs

    (***   when template edited, then azure table updated   ***)
    do! c.TemplateAppender().Edit templateEdited templateCreated.Id
    let exampleEdited_T =
        { exampleEdited with
            Meta = meta3
            TemplateRevisionId = templateCreated.Id, templateEdited.Ordinal
            Ordinal = exampleEdited.Ordinal + 1<exampleRevisionOrdinal> }
    do! exampleAppender.Edit exampleEdited_T exampleCreated.Id
    
    let! actual = c.KeyValueStore().GetExample exampleCreated.Id
    let exampleSummary = exampleSummary |> Example.Fold.evolveEdited exampleEdited_T
    Assert.equal (actual |> Kvs.toExample) exampleSummary

    let! cs = collectors()
    Assert.equal [0;0;1] cs

    (***   when Stack's Revision changed, then azure table updated   ***)
    let revisionChanged : Stack.Events.RevisionChanged = { Meta = meta1; RevisionId = exampleCreated.Id, exampleEdited_T.Ordinal }
    do! stackAppender.ChangeRevision revisionChanged stackCreated.Id
    
    let! actual = c.KeyValueStore().GetStack stackCreated.Id
    let stackSummary = stackSummary |> Stack.Fold.evolveRevisionChanged revisionChanged
    Assert.equal actual stackSummary
    
    let! cs = collectors()
    Assert.equal [1;0;0] cs

    (***   when Stack's Revision changed to new Example, then azure table updated   ***)
    do! exampleAppender.Create exampleCreated2
    let revisionChanged : Stack.Events.RevisionChanged = { Meta = meta2; RevisionId = exampleCreated2.Id, Example.Fold.initialExampleRevisionOrdinal }
    do! stackAppender.ChangeRevision revisionChanged stackCreated.Id
    
    let! actual = c.KeyValueStore().GetStack stackCreated.Id
    let stackSummary = stackSummary |> Stack.Fold.evolveRevisionChanged revisionChanged
    Assert.equal actual stackSummary
    
    let! cs = collectors()
    Assert.equal [0;0;0] cs
    let! cs = collectors2()
    Assert.equal [1] cs
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

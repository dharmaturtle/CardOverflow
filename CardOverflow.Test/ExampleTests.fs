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
    
//[<StandardProperty>]
//let ``ExampleAppender.Upsert rejects edit with duplicate revisionId`` (authorId, command1, command2, tags, title, (templateSummary: PublicTemplate.Events.Summary)) = asyncResult {
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
//let ``ExampleAppender.Upsert fails to persist edit with another author`` (authorId, hackerId, command1, command2, tags, title, (templateSummary: PublicTemplate.Events.Summary)) = asyncResult {
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
//let ``ExampleAppender.Upsert fails to insert twice`` (authorId, command, tags, (templateSummary: PublicTemplate.Events.Summary)) = asyncResult {
//    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags; TemplateRevisionId = templateSummary.RevisionIds.Head }
//    let c = TestEsContainer()
//    do! c.TemplateAppender().Create templateSummary
//    let exampleAppender = c.ExampleAppender()
//    do! exampleAppender.Upsert authorId command
        
//    let! (result: Result<_,_>) = exampleAppender.Upsert authorId command

//    Assert.equal result.error $"Concept '{command.Ids.ConceptId}' already exists."
//    }

[<FastProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``Search works`` signedUp { TemplateCreated = templateCreated; ExampleCreated = exampleCreated  } = asyncResult {
    let c = TestEsContainer(true)
    do! c.UserSagaAppender().Create signedUp
    do! c.PublicTemplateAppender().Create templateCreated
    do! c.ExampleAppender().Create exampleCreated
    let! _ = c.ElasticClient().Indices.RefreshAsync()
    let elsea = c.ElseaClient()
    
    let expected =
        let expectedMap =
            let exampleSummary = Example.Fold.evolveCreated exampleCreated
            templateCreated |> PublicTemplate.Fold.evolveCreated |> toCurrentTemplateInstance |> ExampleSearch.fromSummary exampleSummary signedUp.DisplayName
        let n = Unchecked.defaultof<ExampleSearch>
        { Id               = expectedMap.[nameof n.Id               ] |> unbox
          ParentId         = expectedMap.[nameof n.ParentId         ] |> unbox
          CurrentOrdinal   = expectedMap.[nameof n.CurrentOrdinal   ] |> unbox
          Title            = expectedMap.[nameof n.Title            ] |> unbox
          AuthorId         = expectedMap.[nameof n.AuthorId         ] |> unbox
          Author           = expectedMap.[nameof n.Author           ] |> unbox
          TemplateInstance = expectedMap.[nameof n.TemplateInstance ] |> unbox
          FieldValues      = expectedMap.[nameof n.FieldValues      ] |> unbox
          ServerCreatedAt  = expectedMap.[nameof n.ServerCreatedAt  ] |> unbox
          ServerModifiedAt = expectedMap.[nameof n.ServerModifiedAt ] |> unbox
          Collectors       = 0
          EditSummary      = expectedMap.[nameof n.EditSummary      ] |> unbox }
    
    // SearchExample works for Title
    let! actual = elsea.SearchExample exampleCreated.Title 1
    actual.Results |> Seq.exactlyOne |> Assert.equal expected
    
    // SearchExample works for emptystring
    let! actual = elsea.SearchExample ""                   1
    actual.Results |> Seq.exactlyOne |> Assert.equal expected
    
    // SearchExample works for all field values
    for fieldValue in exampleCreated.FieldValues do
        let! actual = elsea.SearchExample fieldValue.Value 1
        actual.Results |> Seq.exactlyOne |> Assert.equal expected
    }

[<StandardProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``Example comment tests`` commentAdded signedUp { TemplateCreated = templateCreated; ExampleCreated = exampleCreated } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    do! c.PublicTemplateAppender().Create templateCreated
    let exampleAppender = c.ExampleAppender()
    let kvs = c.KeyValueStore()
    do! exampleAppender.Create exampleCreated
    let exampleId = exampleCreated.Id
    
    (***   When Comment added...   ***)
    do! exampleAppender.AddComment commentAdded exampleId
    
    // ...then Example updated.
    let expected =
        let templates = templateCreated |> PublicTemplate.Fold.evolveCreated |> Projection.toTemplateInstance PublicTemplate.Fold.initialOrdinal |> List.singleton
        exampleCreated |> Example.Fold.evolveCreated |> Example.Fold.evolveCommentAdded commentAdded |> Kvs.toKvsExample signedUp.DisplayName Map.empty templates
    let! actual = kvs.GetExample_ exampleId
    Assert.equal expected actual

    // ...then Concept updated.
    let! actual = kvs.GetConcept_ exampleId
    let expected = expected |> Concept.FromExample []
    Assert.equal expected actual
    }

[<StandardProperty>]
let ``ExampleRevisionId ser des roundtrips`` id =
    id |>ExampleRevisionId.ser |> ExampleRevisionId.des = id

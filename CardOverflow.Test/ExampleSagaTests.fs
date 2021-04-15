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
open Domain.Projection

[<StandardProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``ExampleWriter roundtrips`` { Author = author; TemplateSummary = templateSummary; ExampleSummary = exampleSummary; Edit = exampleEdited } ease = asyncResult {
    let       stackId = % Guid.NewGuid()
    let cardSettingId = % Guid.NewGuid()
    let        deckId = % Guid.NewGuid()
    let c = TestEsContainer(true)
    do! c.UserSagaWriter().Create author
    do! c.TemplateSagaWriter().Create templateSummary
    let exampleSaga = c.ExampleSagaWriter()
    
    (***   when created, then azure table updated   ***)
    do! exampleSaga.Create exampleSummary stackId cardSettingId ease deckId
    
    let! actual = c.KeyValueStore().GetExample exampleSummary.Id
    Assert.equal exampleSummary actual

    (***   Creating an Example also creates a Stack which is indexed   ***)
    let! stack = c.KeyValueStore().GetStack stackId
    let! _ = c.ElasticClient().Indices.RefreshAsync()
    let! actualStackSearch = c.ElseaClient().GetUsersStack author.Id exampleSummary.Id
    
    Assert.equal
        (StackSearch.fromSummary stack exampleSummary.Id)
        (actualStackSearch |> Seq.exactlyOne)
    
    (***   Creating an Example also creates an ExampleSearch   ***)
    let expected = Template.toRevisionSummary templateSummary |> ExampleSearch.fromSummary exampleSummary author.DisplayName
    let! (actualExampleSearch: ExampleSearch Option) = c.ElseaClient().GetExampleSearchFor author.Id exampleSummary.Id
    
    let actualExampleSearch = actualExampleSearch.Value
    Assert.equal actualExampleSearch
        { Id               = expected.[nameof actualExampleSearch.Id               ] |> unbox
          ParentId         = expected.[nameof actualExampleSearch.ParentId         ] |> unbox
          RevisionId       = expected.[nameof actualExampleSearch.RevisionId       ] |> unbox
          Title            = expected.[nameof actualExampleSearch.Title            ] |> unbox
          AuthorId         = expected.[nameof actualExampleSearch.AuthorId         ] |> unbox
          Author           = expected.[nameof actualExampleSearch.Author           ] |> unbox
          TemplateRevision = expected.[nameof actualExampleSearch.TemplateRevision ] |> unbox
          FieldValues      = expected.[nameof actualExampleSearch.FieldValues      ] |> unbox
          Collected        = exampleSummary.RevisionIds.Head |> Some
          EditSummary      = expected.[nameof actualExampleSearch.EditSummary      ] |> unbox }
    
    (***   when Example edited, then azure table updated   ***)
    do! exampleSaga.Edit exampleEdited exampleSummary.Id stack.Id author.Id
    
    let! actual = c.KeyValueStore().GetExample exampleSummary.Id
    let exampleSummary = exampleSummary |> Example.Fold.evolveEdited exampleEdited
    Assert.equal exampleSummary actual 

    (***   Editing an Example also updates the user's Stack   ***)
    let! stack = c.KeyValueStore().GetStack stackId
    Assert.equal stack.ExampleRevisionId exampleEdited.RevisionId
    let! _ = c.ElasticClient().Indices.RefreshAsync()
    let! actualStackSearch = c.ElseaClient().GetUsersStack author.Id exampleSummary.Id
    
    Assert.equal
        (StackSearch.fromSummary (stack |> Stack.Fold.evolveRevisionChanged { RevisionId = exampleEdited.RevisionId }) exampleSummary.Id)
        (actualStackSearch |> Seq.exactlyOne)
    
    (***   Editing an Example also edits ExampleSearch   ***)
    let expected = Template.toRevisionSummary templateSummary |> ExampleSearch.fromSummary exampleSummary author.DisplayName
    let! (actualExampleSearch: ExampleSearch Option) = c.ElseaClient().GetExampleSearchFor author.Id exampleSummary.Id
    
    let actualExampleSearch = actualExampleSearch.Value
    Assert.equal actualExampleSearch
        { Id               = expected.[nameof actualExampleSearch.Id               ] |> unbox
          ParentId         = expected.[nameof actualExampleSearch.ParentId         ] |> unbox
          RevisionId       = expected.[nameof actualExampleSearch.RevisionId       ] |> unbox
          Title            = expected.[nameof actualExampleSearch.Title            ] |> unbox
          AuthorId         = expected.[nameof actualExampleSearch.AuthorId         ] |> unbox
          Author           = expected.[nameof actualExampleSearch.Author           ] |> unbox
          TemplateRevision = expected.[nameof actualExampleSearch.TemplateRevision ] |> unbox
          FieldValues      = expected.[nameof actualExampleSearch.FieldValues      ] |> unbox
          Collected        = exampleEdited.RevisionId |> Some
          EditSummary      = expected.[nameof actualExampleSearch.EditSummary      ] |> unbox }

    (***   A different user's ExampleSearch has a Collected = None   ***)
    let! (actualExampleSearch: ExampleSearch Option) = c.ElseaClient().GetExampleSearchFor (% Guid.NewGuid()) exampleSummary.Id
    
    let actualExampleSearch = actualExampleSearch.Value
    Assert.equal actualExampleSearch
        { Id               = expected.[nameof actualExampleSearch.Id               ] |> unbox
          ParentId         = expected.[nameof actualExampleSearch.ParentId         ] |> unbox
          RevisionId       = expected.[nameof actualExampleSearch.RevisionId       ] |> unbox
          Title            = expected.[nameof actualExampleSearch.Title            ] |> unbox
          AuthorId         = expected.[nameof actualExampleSearch.AuthorId         ] |> unbox
          Author           = expected.[nameof actualExampleSearch.Author           ] |> unbox
          TemplateRevision = expected.[nameof actualExampleSearch.TemplateRevision ] |> unbox
          FieldValues      = expected.[nameof actualExampleSearch.FieldValues      ] |> unbox
          Collected        = None
          EditSummary      = expected.[nameof actualExampleSearch.EditSummary      ] |> unbox }
    
    (***   Searching for a nonexistant ExampleSearch yields None   ***)
    let! actualExampleSearch = c.ElseaClient().GetExampleSearchFor (% Guid.NewGuid()) (% Guid.NewGuid())

    Assert.equal None actualExampleSearch

    (***   Discarding a stack removes it from kvs   ***)
    do! c.StackWriter().Discard stackId

    let! actual = c.KeyValueStore().TryGet stackId
    Assert.equal None actual

    (***   Discarding a stack removes it from StackSearch and ExampleSearch   ***)
    let! _ = c.ElasticClient().Indices.RefreshAsync()

    let! actual = c.ElseaClient().GetUsersStack author.Id exampleSummary.Id
    Assert.Empty actual
    let! (actual: ExampleSearch Option) = c.ElseaClient().GetExampleSearchFor author.Id exampleSummary.Id
    Assert.equal None actual.Value.Collected
    }
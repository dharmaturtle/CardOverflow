module ExampleComboTests

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

[<FastProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``ExampleAppender roundtrips`` { SignedUp = signedUp; TemplateCreated = templateCreated; ExampleCreated = exampleCreated; Edit = exampleEdited } ease = asyncResult {
    let       stackId = % Guid.NewGuid()
    let cardSettingId = % Guid.NewGuid()
    let        deckId = % Guid.NewGuid()
    let c = TestEsContainer(true)
    do! c.UserSagaAppender().Create signedUp
    do! c.TemplateComboAppender().Create templateCreated
    let template = templateCreated |> Template.Fold.evolveCreated
    let exampleCombo = c.ExampleComboAppender()
    
    (***   when created, then azure table updated   ***)
    do! exampleCombo.Create exampleCreated stackId cardSettingId ease deckId
    
    let! actual = c.KeyValueStore().GetExample exampleCreated.Id
    let exampleSummary = Example.Fold.evolveCreated exampleCreated
    Assert.equal exampleSummary (actual |> Kvs.toExample)

    (***   Creating an Example also creates a Stack   ***)
    let! stack = c.KeyValueStore().GetStack stackId
    Assert.NotNull stack
    
    (***   Creating an Example also creates an ExampleSearch   ***)
    let! _ = c.ElasticClient().Indices.RefreshAsync()
    let expected = template |> toCurrentTemplateInstance |> ExampleSearch.fromSummary exampleSummary signedUp.DisplayName
    let! (actualExampleSearch: ExampleSearch Option) = c.ElseaClient().GetExampleSearch exampleSummary.Id
    
    let actualExampleSearch = actualExampleSearch.Value
    Assert.equal actualExampleSearch
        { Id               = expected.[nameof actualExampleSearch.Id               ] |> unbox
          ParentId         = expected.[nameof actualExampleSearch.ParentId         ] |> unbox
          CurrentOrdinal   = expected.[nameof actualExampleSearch.CurrentOrdinal   ] |> unbox
          Title            = expected.[nameof actualExampleSearch.Title            ] |> unbox
          AuthorId         = expected.[nameof actualExampleSearch.AuthorId         ] |> unbox
          Author           = expected.[nameof actualExampleSearch.Author           ] |> unbox
          TemplateInstance = expected.[nameof actualExampleSearch.TemplateInstance ] |> unbox
          FieldValues      = expected.[nameof actualExampleSearch.FieldValues      ] |> unbox
          Collectors       = 1
          EditSummary      = expected.[nameof actualExampleSearch.EditSummary      ] |> unbox }
    
    (***   when Example edited, then azure table updated   ***)
    do! exampleCombo.Edit exampleEdited exampleSummary.Id stack.Id
    
    let! actual = c.KeyValueStore().GetExample exampleSummary.Id
    let exampleSummary = exampleSummary |> Example.Fold.evolveEdited exampleEdited
    Assert.equal exampleSummary (actual |> Kvs.toExample)

    (***   Editing an Example also updates the user's Stack   ***)
    let! stack = c.KeyValueStore().GetStack stackId
    Assert.equal stack.ExampleRevisionId (exampleSummary.Id, exampleEdited.Ordinal)
    
    (***   Editing an Example also edits ExampleSearch   ***)
    let expected = template |> toCurrentTemplateInstance |> ExampleSearch.fromSummary exampleSummary signedUp.DisplayName
    let! _ = c.ElasticClient().Indices.RefreshAsync()
    let! (actualExampleSearch: ExampleSearch Option) = c.ElseaClient().GetExampleSearch exampleSummary.Id
    
    let actualExampleSearch = actualExampleSearch.Value
    Assert.equal actualExampleSearch
        { Id               = expected.[nameof actualExampleSearch.Id               ] |> unbox
          ParentId         = expected.[nameof actualExampleSearch.ParentId         ] |> unbox
          CurrentOrdinal   = expected.[nameof actualExampleSearch.CurrentOrdinal   ] |> unbox
          Title            = expected.[nameof actualExampleSearch.Title            ] |> unbox
          AuthorId         = expected.[nameof actualExampleSearch.AuthorId         ] |> unbox
          Author           = expected.[nameof actualExampleSearch.Author           ] |> unbox
          TemplateInstance = expected.[nameof actualExampleSearch.TemplateInstance ] |> unbox
          FieldValues      = expected.[nameof actualExampleSearch.FieldValues      ] |> unbox
          Collectors       = 1
          EditSummary      = expected.[nameof actualExampleSearch.EditSummary      ] |> unbox }

    (***   Searching for a nonexistant ExampleSearch yields None   ***)
    let! actualExampleSearch = c.ElseaClient().GetExampleSearch (% Guid.NewGuid())

    Assert.equal None actualExampleSearch

    (***   Discarding a stack removes it from kvs   ***)
    do! c.StackAppender().Discard { Meta = signedUp.Meta } stackId

    let! actual = c.KeyValueStore().TryGet stackId
    Assert.equal None actual

    (***   Discarding a stack removes it from ExampleSearch   ***)
    let! _ = c.ElasticClient().Indices.RefreshAsync()

    let! (actual: ExampleSearch Option) = c.ElseaClient().GetExampleSearch exampleSummary.Id
    Assert.equal 0 actual.Value.Collectors
    }

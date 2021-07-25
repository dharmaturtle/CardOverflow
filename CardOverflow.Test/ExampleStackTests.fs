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
open Hedgehog.Xunit

// Kvs.Example is aware of Stack events (for populating Collectors) so we remove it before testing for equality with Example (which is not aware of Stack events)
let stripStackEventMeta (stackCommandId: CommandId) (actual: Kvs.Example) =
    { actual with
        CommandIds = actual.CommandIds |> Set.remove stackCommandId }

[<FastProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``ElasticSearch Example & Stack tests`` signedUp revisionChanged discarded { TemplateCreated = templateCreated; ExampleCreated = exampleCreated; Edit = exampleEdited; StackCreated = stackCreated  } = asyncResult {
    let c = TestEsContainer(true)
    do! c.UserSagaAppender().Create signedUp
    do! c.TemplateAppender().Create templateCreated
    let template = templateCreated |> Template.Fold.evolveCreated
    let exampleAppender = c.ExampleAppender()
    let stackAppender = c.StackAppender()
    
    (***   when Example created, then azure table updated   ***)
    do! exampleAppender.Create exampleCreated
    
    let! actual = c.KeyValueStore().GetExample exampleCreated.Id
    let exampleSummary = Example.Fold.evolveCreated exampleCreated
    Assert.equal exampleSummary (actual |> Kvs.toExample)

    // setting Collectors = 1
    do! stackAppender.Create stackCreated

    (***   Creating an Example also creates an ExampleSearch   ***)
    let! _ = c.ElasticClient().Indices.RefreshAsync()
    let expected = template |> toCurrentTemplateInstance |> ExampleSearch.fromSummary exampleSummary signedUp.DisplayName
    let! (actualExampleSearch: ExampleSearch Option) = c.ElseaClient().GetExample exampleSummary.Id
    
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
          ServerCreatedAt  = expected.[nameof actualExampleSearch.ServerCreatedAt  ] |> unbox
          ServerModifiedAt = expected.[nameof actualExampleSearch.ServerModifiedAt ] |> unbox
          Collectors       = 1
          EditSummary      = expected.[nameof actualExampleSearch.EditSummary      ] |> unbox }
    
    (***   when Example edited, then azure table updated   ***)
    do! exampleAppender.Edit exampleEdited exampleSummary.Id
    
    let! actual = c.KeyValueStore().GetExample exampleSummary.Id
    let exampleSummary = exampleSummary |> Example.Fold.evolveEdited exampleEdited
    actual |> stripStackEventMeta stackCreated.Meta.CommandId |> Kvs.toExample |> Assert.equal exampleSummary

    (***   Stack's ChangeRevision works   ***)
    do! stackAppender.ChangeRevision revisionChanged stackCreated.Id
    
    let! actual = c.KeyValueStore().GetStack stackCreated.Id
    Assert.equal actual.ExampleRevisionId (exampleSummary.Id, exampleEdited.Ordinal)
    stackCreated |> Stack.Fold.evolveCreated |> Stack.Fold.evolveRevisionChanged revisionChanged |> Assert.equal actual
    
    (***   Editing an Example also edits ExampleSearch   ***)
    let expected = template |> toCurrentTemplateInstance |> ExampleSearch.fromSummary exampleSummary signedUp.DisplayName
    let! _ = c.ElasticClient().Indices.RefreshAsync()
    let! (actualExampleSearch: ExampleSearch Option) = c.ElseaClient().GetExample exampleSummary.Id
    
    let actualExampleSearch = actualExampleSearch.Value
    Assert.equal actualExampleSearch
        { Id               = expected.[nameof actualExampleSearch.Id                 ] |> unbox
          ParentId         = expected.[nameof actualExampleSearch.ParentId           ] |> unbox
          CurrentOrdinal   = expected.[nameof actualExampleSearch.CurrentOrdinal     ] |> unbox
          Title            = expected.[nameof actualExampleSearch.Title              ] |> unbox
          AuthorId         = expected.[nameof actualExampleSearch.AuthorId           ] |> unbox
          Author           = expected.[nameof actualExampleSearch.Author             ] |> unbox
          TemplateInstance = expected.[nameof actualExampleSearch.TemplateInstance   ] |> unbox
          FieldValues      = expected.[nameof actualExampleSearch.FieldValues        ] |> unbox
          ServerCreatedAt  = expected.[nameof actualExampleSearch.ServerCreatedAt    ] |> unbox
          ServerModifiedAt = expected.[nameof actualExampleSearch.ServerModifiedAt   ] |> unbox
          Collectors       = 1
          EditSummary      = expected.[nameof actualExampleSearch.EditSummary        ] |> unbox }

    (***   Searching for a nonexistant ExampleSearch yields None   ***)
    let! actualExampleSearch = c.ElseaClient().GetExample (% Guid.NewGuid())

    Assert.equal None actualExampleSearch

    (***   Discarding a stack removes it from ExampleSearch   ***)
    do! c.StackAppender().Discard discarded stackCreated.Id
    let! _ = c.ElasticClient().Indices.RefreshAsync()

    let! (actual: ExampleSearch Option) = c.ElseaClient().GetExample exampleSummary.Id
    Assert.equal 0 actual.Value.Collectors
    }

[<StandardProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``Example & Stack tests`` seed (signedUp: User.Events.SignedUp) { TemplateCreated = templateCreated; ExampleCreated = exampleCreated; Edit = exampleEdited; StackCreated = stackCreated } (meta1: Meta) (meta2: Meta) (meta3: Meta) (meta4: Meta) (meta5: Meta) = asyncResult {
    IdempotentTest.init 1.0 seed
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    do! c.TemplateAppender().Create templateCreated
    let exampleAppender = c.ExampleAppender()
    let stackAppender = c.StackAppender()
    let kvs = c.KeyValueStore()
    let collectors' (exampleCreated: Example.Events.Created) = async {
        let! example = kvs.GetExample exampleCreated.Id |> Async.map (fun x -> x.Revisions |> List.map (fun x -> x.Collectors))
        let! concept = kvs.GetConcept exampleCreated.Id
        example |> Seq.sum |> Assert.equal concept.Collectors
        return example
        }
    let exampleCreated2 = { exampleCreated with Meta = meta4; Id = % Guid.NewGuid() }
    let collectors  () = collectors' exampleCreated
    let collectors2 () = collectors' exampleCreated2
    
    (***   when Example created, then azure table updated   ***)
    do! exampleAppender.Create exampleCreated
    
    let! actual = kvs.GetExample exampleCreated.Id
    let exampleSummary = Example.Fold.evolveCreated exampleCreated
    Assert.equal exampleSummary (actual |> Kvs.toExample)

    let expected = actual |> Concept.FromExample []
    let! actual = kvs.GetConcept exampleCreated.Id
    Assert.equal actual expected

    let! cs = collectors()
    Assert.equal [0] cs

    (***   when Stack created, then azure table updated   ***)
    let stackSummary = Stack.Fold.evolveCreated stackCreated
    do! stackAppender.Create stackCreated

    let! actual = kvs.GetStack stackCreated.Id
    Assert.equal stackSummary actual

    let! cs = collectors()
    Assert.equal [1] cs
    
    (***   when edited, then azure table updated   ***)
    do! exampleAppender.Edit exampleEdited exampleCreated.Id
    
    let! actual = kvs.GetExample exampleCreated.Id
    let exampleSummary = exampleSummary |> Example.Fold.evolveEdited exampleEdited
    actual |> stripStackEventMeta stackCreated.Meta.CommandId |> Kvs.toExample |> Assert.equal exampleSummary

    let expected = actual |> Concept.FromExample []
    let! actual = kvs.GetConcept exampleCreated.Id
    Assert.equal actual expected

    let! cs = collectors()
    Assert.equal [0;1] cs

    (***   when template edited, then azure table updated   ***)
    let templateEdited : Template.Events.Edited =
        { Meta          = meta5
          Ordinal       = Template.Fold.initialTemplateRevisionOrdinal + 1<templateRevisionOrdinal>
          Name          = "something new"
          Css           = templateCreated.Css
          Fields        = templateCreated.Fields
          LatexPre      = templateCreated.LatexPre
          LatexPost     = templateCreated.LatexPost
          CardTemplates = templateCreated.CardTemplates
          EditSummary   = "done got edited" }
    do! c.TemplateAppender().Edit templateEdited templateCreated.Id
    let exampleEdited_T =
        { exampleEdited with
            Meta = meta3
            TemplateRevisionId = templateCreated.Id, templateEdited.Ordinal
            Ordinal = exampleEdited.Ordinal + 1<exampleRevisionOrdinal> }
    do! exampleAppender.Edit exampleEdited_T exampleCreated.Id
    
    let! actual = kvs.GetExample exampleCreated.Id
    let exampleSummary = exampleSummary |> Example.Fold.evolveEdited exampleEdited_T
    actual |> stripStackEventMeta stackCreated.Meta.CommandId |> Kvs.toExample |> Assert.equal exampleSummary

    let expected = actual |> Concept.FromExample []
    let! actual = kvs.GetConcept exampleCreated.Id
    Assert.equal actual expected

    let! cs = collectors()
    Assert.equal [0;0;1] cs

    (***   when Stack's Revision changed, then azure table updated   ***)
    let revisionChanged : Stack.Events.RevisionChanged = { Meta = meta1; RevisionId = exampleCreated.Id, exampleEdited_T.Ordinal }
    do! stackAppender.ChangeRevision revisionChanged stackCreated.Id
    
    let! actual = kvs.GetStack stackCreated.Id
    let stackSummary = stackSummary |> Stack.Fold.evolveRevisionChanged revisionChanged
    Assert.equal actual stackSummary
    
    let! cs = collectors()
    Assert.equal [1;0;0] cs

    (***   when Stack's Revision changed to new Example, then azure table updated   ***)
    do! exampleAppender.Create exampleCreated2
    let revisionChanged : Stack.Events.RevisionChanged = { Meta = meta2; RevisionId = exampleCreated2.Id, Example.Fold.initialExampleRevisionOrdinal }
    do! stackAppender.ChangeRevision revisionChanged stackCreated.Id
    
    let! actual = kvs.GetStack stackCreated.Id
    let stackSummary = stackSummary |> Stack.Fold.evolveRevisionChanged revisionChanged
    Assert.equal actual stackSummary
    
    let! cs = collectors()
    Assert.equal [0;0;0] cs
    let! cs = collectors2()
    Assert.equal [1] cs
    }
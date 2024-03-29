module TemplateTests

open Xunit
open CardOverflow.Pure
open Serilog
open System
open Domain
open Equinox.MemoryStore
open FSharp.UMX
open FsCheck.Xunit
open CardOverflow.Pure
open CardOverflow.Test
open EventAppender
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp
open Domain.PublicTemplate
open Domain.Projection

[<StandardProperty>]
let ``Create summary roundtrips`` signedUp { TemplateCreated = templateCreated; TemplateEdit = edited } = asyncResult {
    let c = TestEsContainer()
    let kvs = c.KeyValueStore()
    do! c.UserSagaAppender().Create signedUp
    let templateAppender = c.PublicTemplateAppender()

    (***   Creating a Template...   ***)
    do! templateAppender.Create templateCreated

    // ...updates KVS.
    let! actual = kvs.GetTemplate_ templateCreated.Id |>% Kvs.toTemplate
    let expected = Fold.evolveCreated templateCreated
    Assert.equal expected actual

    (***   Editing a Template...   ***)
    do! templateAppender.Edit edited templateCreated.Id

    // ...updates KVS.
    let expected = expected |> Fold.evolveEdited edited
    let! actual = kvs.GetTemplate_ templateCreated.Id |>% Kvs.toTemplate
    Assert.equal expected actual
    }

let withCommandId commandId (template: Kvs.Template) =
    { template with CommandIds = template.CommandIds |> Set.add commandId }

[<FastProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``Search works`` signedUp { TemplateCreated = templateCreated; TemplateCollected = templateCollected; TemplateDiscarded = templateDiscarded } = asyncResult {
    let c = TestEsContainer(true)
    let elseaClient  = c.ElseaClient()
    let userAppender = c.UserAppender()
    let kvs          = c.KeyValueStore()
    do! c.UserSagaAppender().Create signedUp
    let templateId = templateCreated.Id
    
    (***   Creating a Template...   ***)
    do! c.PublicTemplateAppender().Create templateCreated

    // ...inserts into Elsea.
    let expected =
        { Id               = templateCreated.Id
          CurrentOrdinal   = Fold.initialOrdinal
          AuthorId         = signedUp.Meta.UserId
          Author           = signedUp.DisplayName
          Name             = templateCreated.Name
          Css              = templateCreated.Css
          Fields           = templateCreated.Fields
          ServerCreatedAt  = templateCreated.Meta.ServerReceivedAt.Value
          ServerModifiedAt = templateCreated.Meta.ServerReceivedAt.Value
          LatexPre         = templateCreated.LatexPre
          LatexPost        = templateCreated.LatexPost
          CardTemplates    = templateCreated.CardTemplates
          Collectors       = 0 }
        |> Some
    do! elseaClient.GetTemplate templateId |>% Assert.equal expected

    (***   Collecting a Template...   ***)
    do! userAppender.TemplateCollected templateCollected
    
    // ...adds it to a User's CollectedTemplates.
    let! user = kvs.GetUser_ signedUp.Meta.UserId
    Assert.equal [templateCollected.TemplateRevisionId] user.CollectedTemplates

    // ...increments Elsea's Collectors.
    let! actual = elseaClient.GetTemplate templateId
    Assert.equal { expected.Value with Collectors = 1 } actual.Value

    // ...increments Kvs's Collectors.
    let expectedKvs collectors =
        let collectorsByOrdinal = (Fold.initialOrdinal, collectors) |> List.singleton |> Map.ofList
        templateCreated
        |> Fold.evolveCreated
        |> Kvs.toKvsTemplate signedUp.DisplayName collectorsByOrdinal
        |> withCommandId templateCollected.Meta.CommandId
    do! kvs.GetTemplate_ templateId
        |>% Assert.equal (expectedKvs 1)

    (***   Discarding a Template...   ***)
    do! userAppender.TemplateDiscarded templateDiscarded
    
    // ...clears a User's CollectedTemplates.
    let! user = kvs.GetUser_ signedUp.Meta.UserId
    Assert.equal [] user.CollectedTemplates

    // ...decrements Elsea's Collectors.
    do! elseaClient.GetTemplate templateId |>% Assert.equal expected

    // ...decrements Kvs's Collectors.
    do! let expected = expectedKvs 0 |> withCommandId templateDiscarded.Meta.CommandId
        kvs.GetTemplate_ templateId
        |>% Assert.equal expected

    (***   Deleting a Template...   ***)
    do! elseaClient.DeleteTemplate templateCreated.Id

    // ...removes it from Elsea.
    do! elseaClient.GetTemplate templateId |>% Assert.equal None
    }

[<FastProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``SearchTemplate works`` signedUp { TemplateEdit.TemplateCreated = templateCreated } = asyncResult {
    let c = TestEsContainer(true)
    let elseaClient  = c.ElseaClient()
    do! c.UserSagaAppender().Create signedUp
    do! c.PublicTemplateAppender().Create templateCreated
    let! _ = c.ElasticClient().Indices.RefreshAsync()
    let expected =
        { Id               = templateCreated.Id
          CurrentOrdinal   = Fold.initialOrdinal
          AuthorId         = signedUp.Meta.UserId
          Author           = signedUp.DisplayName
          Name             = templateCreated.Name
          Css              = templateCreated.Css
          Fields           = templateCreated.Fields
          ServerCreatedAt  = templateCreated.Meta.ServerReceivedAt.Value
          ServerModifiedAt = templateCreated.Meta.ServerReceivedAt.Value
          LatexPre         = templateCreated.LatexPre
          LatexPost        = templateCreated.LatexPost
          CardTemplates    = templateCreated.CardTemplates
          Collectors       = 0 }

    // SearchTemplate works for Name
    do! elseaClient.SearchTemplate templateCreated.Name 1 |>% (fun x -> x.Results) |>% Seq.exactlyOne |>% Assert.equal expected
    
    // SearchTemplate works for emptystring
    do! elseaClient.SearchTemplate ""                   1 |>% (fun x -> x.Results) |>% Seq.exactlyOne |>% Assert.equal expected
    }

[<StandardProperty>]
let ``TemplateRevisionId ser des roundtrips`` id =
    id |>TemplateRevisionId.ser |> TemplateRevisionId.des = id

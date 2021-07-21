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
open Domain.Template
open Domain.Projection

[<StandardProperty>]
let ``Create summary roundtrips`` signedUp { TemplateEdit.TemplateCreated = templateCreated } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    let userAppender = c.UserAppender()
    let templateAppender = c.TemplateAppender()

    do! templateAppender.Create templateCreated

    // memory store roundtrips
    templateCreated.Id
    |> c.TemplateEvents
    |> Seq.exactlyOne
    |> Assert.equal (Template.Events.Created templateCreated)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetTemplate templateCreated.Id |>% Kvs.toTemplate
    let expected = Template.Fold.evolveCreated templateCreated
    Assert.equal expected actual
    let revisionId = expected.CurrentRevisionId
    let! actual = c.KeyValueStore().GetTemplate (fst revisionId)
    Assert.equal expected (actual |> Kvs.toTemplate)
    }

[<StandardProperty>]
let ``Edited roundtrips`` signedUp { TemplateCreated = templateCreated; TemplateEdit = edited } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    let templateAppender = c.TemplateAppender()
    do! templateAppender.Create templateCreated
    
    do! templateAppender.Edit edited templateCreated.Id

    // event store roundtrips
    templateCreated.Id
    |> c.TemplateEvents
    |> Seq.last
    |> Assert.equal (Template.Events.Edited edited)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetTemplate templateCreated.Id |>% Kvs.toTemplate
    let expected = templateCreated |> Template.Fold.evolveCreated |> Fold.evolveEdited edited
    Assert.equal expected actual
    let! actual = expected.CurrentRevisionId |> fst |> c.KeyValueStore().GetTemplate
    Assert.equal expected (actual |> Kvs.toTemplate)

    // editing upgrades user's collected revision to new revision
    //let expected = User.upgradeRevision signedUp.CollectedTemplates expected.CurrentRevisionId (templateCreated.Id, edited.Ordinal)
    
    //let! user = c.KeyValueStore().GetUser signedUp.Meta.UserId
    
    //Assert.equal expected user.CollectedTemplates
    }

[<FastProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``Search works`` signedUp { TemplateCreated = templateCreated; TemplateCollected = templateCollected; TemplateDiscarded = templateDiscarded } = asyncResult {
    let c = TestEsContainer(true)
    do! c.UserSagaAppender().Create signedUp
    do! c.TemplateAppender().Create templateCreated
    let templateId = templateCreated.Id
    let elseaClient = c.ElseaClient()
    
    // get works
    let expected =
        { Id               = templateCreated.Id
          CurrentOrdinal   = Template.Fold.initialTemplateRevisionOrdinal
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

    let! actual = elseaClient.GetTemplate templateId

    Assert.equal expected actual

    // collecting a template works
    let userAppender = c.UserAppender()
    do! userAppender.TemplateCollected templateCollected
    
    let! user = c.KeyValueStore().GetUser signedUp.Meta.UserId
    
    Assert.equal [templateCollected.TemplateRevisionId] user.CollectedTemplates

    // ...and increments collectors
    let! actual = elseaClient.GetTemplate templateId
    
    Assert.equal { expected.Value with Collectors = 1 } actual.Value

    // discarding a template works
    do! userAppender.TemplateDiscarded templateDiscarded
    
    let! user = c.KeyValueStore().GetUser signedUp.Meta.UserId
    
    Assert.equal [] user.CollectedTemplates

    // ...and decrements collectors
    let! actual = elseaClient.GetTemplate templateId
    
    Assert.equal expected actual

    // delete works
    do! elseaClient.DeleteTemplate templateCreated.Id

    do! elseaClient.GetTemplate templateId |>% Assert.equal None
    }

[<StandardProperty>]
let ``TemplateRevisionId ser des roundtrips`` id =
    id |>TemplateRevisionId.ser |> TemplateRevisionId.des = id

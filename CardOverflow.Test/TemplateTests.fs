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
let ``Create summary roundtrips`` { SignedUp = signedUp; TemplateCreated = templateCreated; TemplateCollected = templateCollected; TemplateDiscarded = templateDiscarded } = asyncResult {
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

    // collecting a template works
    do! userAppender.TemplateCollected templateCollected
    
    let! user = c.KeyValueStore().GetUser signedUp.Meta.UserId
    
    Assert.equal [templateCollected.TemplateRevisionId] user.CollectedTemplates

    // discarding a template works
    do! userAppender.TemplateDiscarded templateDiscarded
    
    let! user = c.KeyValueStore().GetUser signedUp.Meta.UserId
    
    Assert.equal [] user.CollectedTemplates
    }

[<StandardProperty>]
let ``Edited roundtrips`` { SignedUp = signedUp; TemplateCreated = templateCreated; TemplateEdit = edited } = asyncResult {
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

[<StandardProperty>]
let ``TemplateRevisionId ser des roundtrips`` id =
    id |>TemplateRevisionId.ser |> TemplateRevisionId.des = id

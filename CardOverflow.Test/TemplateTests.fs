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

[<StandardProperty>]
let ``Create summary roundtrips`` { Author = author; TemplateSummary = templateSummary; TemplateEdit = _ } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create author
    let templateComboAppender = c.TemplateComboAppender()

    do! templateComboAppender.Create templateSummary

    // memory store roundtrips
    templateSummary.Id
    |> c.TemplateEvents
    |> Seq.exactlyOne
    |> Assert.equal (Template.Events.Created templateSummary)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetTemplate templateSummary.Id
    Assert.equal templateSummary actual
    let revisionId = templateSummary.CurrentRevisionId
    let! actual = c.KeyValueStore().GetTemplateRevision revisionId
    Assert.equal (Template.toRevisionSummary templateSummary) actual

    // creating template adds it to user's collected templates
    let expected = User.upgradeRevision author.CollectedTemplates revisionId revisionId
    
    let! user = c.KeyValueStore().GetUser author.Id
    
    Assert.equal expected user.CollectedTemplates
    }

[<StandardProperty>]
let ``Edited roundtrips`` { Author = author; TemplateSummary = templateSummary; TemplateEdit = edited } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create author
    let templateComboAppender = c.TemplateComboAppender()
    do! templateComboAppender.Create templateSummary
    
    do! templateComboAppender.Edit edited templateSummary.AuthorId templateSummary.Id

    // event store roundtrips
    templateSummary.Id
    |> c.TemplateEvents
    |> Seq.last
    |> Assert.equal (Template.Events.Edited edited)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetTemplate templateSummary.Id
    Assert.equal (templateSummary |> Fold.evolveEdited edited) actual
    let! actual = templateSummary.CurrentRevisionId |> c.KeyValueStore().GetTemplateRevision
    Assert.equal (Template.toRevisionSummary templateSummary) actual

    // editing upgrades user's collected revision to new revision
    let expected = User.upgradeRevision author.CollectedTemplates templateSummary.CurrentRevisionId (templateSummary.Id, edited.Revision)
    
    let! user = c.KeyValueStore().GetUser author.Id
    
    Assert.equal expected user.CollectedTemplates
    }

[<StandardProperty>]
let ``TemplateRevisionId ser des roundtrips`` id =
    id |>TemplateRevisionId.ser |> TemplateRevisionId.des = id

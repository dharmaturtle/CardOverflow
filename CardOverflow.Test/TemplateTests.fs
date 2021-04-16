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
open EventWriter
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp
open Domain.Template

[<StandardProperty>]
let ``Create summary roundtrips`` (templateSummary: Template.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let templateSagaWriter = c.TemplateSagaWriter()

    do! templateSagaWriter.Create templateSummary

    // memory store roundtrips
    templateSummary.Id
    |> c.TemplateEvents
    |> Seq.exactlyOne
    |> Assert.equal (Template.Events.Created templateSummary)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetTemplate templateSummary.Id
    Assert.equal templateSummary actual
    let! actual = templateSummary.RevisionIds |> Seq.exactlyOne |> c.KeyValueStore().GetTemplateRevision
    Assert.equal (Template.toRevisionSummary templateSummary) actual
    }

[<StandardProperty>]
let ``Edited roundtrips`` { Author = author; TemplateSummary = templateSummary; TemplateEdit = edited } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaWriter().Create author
    let templateSagaWriter = c.TemplateSagaWriter()
    do! templateSagaWriter.Create templateSummary
    
    do! templateSagaWriter.Edit edited templateSummary.AuthorId templateSummary.Id

    // event store roundtrips
    templateSummary.Id
    |> c.TemplateEvents
    |> Seq.last
    |> Assert.equal (Template.Events.Edited edited)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetTemplate templateSummary.Id
    Assert.equal (templateSummary |> Fold.evolveEdited edited) actual
    let! actual = templateSummary.RevisionIds |> Seq.exactlyOne |> c.KeyValueStore().GetTemplateRevision
    Assert.equal (Template.toRevisionSummary templateSummary) actual

    // editing upgrades user's collected revision to new revision
    let expected = User.upgradeRevision author.CollectedTemplates (Seq.exactlyOne templateSummary.RevisionIds) edited.RevisionId
    
    let! user = c.KeyValueStore().GetUser author.Id
    
    Assert.equal expected user.CollectedTemplates
    }

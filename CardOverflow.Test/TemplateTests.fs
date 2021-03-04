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
    let templateWriter = c.TemplateWriter()

    do! templateWriter.Create templateSummary

    // memory store roundtrips
    templateSummary.Id
    |> c.TemplateEvents
    |> Seq.exactlyOne
    |> Assert.equal (Template.Events.Created templateSummary)

    // azure table roundtrips
    let! actual, _ = c.KeyValueStore().GetTemplate templateSummary.Id
    Assert.equal templateSummary actual
    let! actual, _ = templateSummary.RevisionIds |> Seq.exactlyOne |> c.KeyValueStore().GetTemplateRevision
    Assert.equal (Template.toRevisionSummary templateSummary) actual
    }

[<StandardProperty>]
let ``Edited roundtrips`` (((templateSummary, edited): Template.Events.Summary * Template.Events.Edited), ``unused; necessary to force usage of the tuple gen``: bool) = asyncResult {
    let c = TestEsContainer()
    let templateWriter = c.TemplateWriter()
    do! templateWriter.Create templateSummary
    
    do! templateWriter.Edit edited templateSummary.AuthorId templateSummary.Id

    // event store roundtrips
    templateSummary.Id
    |> c.TemplateEvents
    |> Seq.last
    |> Assert.equal (Template.Events.Edited edited)

    // azure table roundtrips
    let! actual, _ = c.KeyValueStore().GetTemplate templateSummary.Id
    Assert.equal (templateSummary |> Fold.evolveEdited edited) actual
    let! actual, _ = templateSummary.RevisionIds |> Seq.exactlyOne |> c.KeyValueStore().GetTemplateRevision
    Assert.equal (Template.toRevisionSummary templateSummary) actual
    }

module StackTests

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
open Domain.Stack

[<StandardProperty>]
let ``Create summary roundtrips`` (stackSummary: Stack.Events.Summary) (templateSummary: Template.Events.Summary) (exampleSummary: Example.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    do! c.TemplateWriter().Create templateSummary
    let exampleSummary = { exampleSummary with TemplateRevisionId = templateSummary.RevisionIds.Head }
    let stackWriter = c.StackWriter()
    do! c.ExampleWriter().Create exampleSummary
    let stackSummary = { stackSummary with ExampleRevisionId = exampleSummary.RevisionIds.Head }

    do! stackWriter.Create stackSummary

    // event store roundtrips
    stackSummary.Id
    |> c.StackEvents
    |> Seq.exactlyOne
    |> Assert.equal (Stack.Events.Created stackSummary)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetStack stackSummary.Id
    Assert.equal stackSummary actual
    }

[<StandardProperty>]
let ``Edited roundtrips`` (stackSummary: Stack.Events.Summary) (templateSummary: Template.Events.Summary) (exampleSummary: Example.Events.Summary) tagsChanged = asyncResult {
    let c = TestEsContainer()
    do! c.TemplateWriter().Create templateSummary
    let exampleSummary = { exampleSummary with TemplateRevisionId = templateSummary.RevisionIds.Head }
    do! c.ExampleWriter().Create exampleSummary
    let stackSummary = { stackSummary with ExampleRevisionId = exampleSummary.RevisionIds.Head }
    let stackWriter = c.StackWriter()
    do! stackWriter.Create stackSummary
    
    do! stackWriter.ChangeTags tagsChanged stackSummary.AuthorId stackSummary.Id

    // event store roundtrips
    stackSummary.Id
    |> c.StackEvents
    |> Seq.last
    |> Assert.equal (Stack.Events.TagsChanged tagsChanged)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetStack stackSummary.Id
    Assert.equal (stackSummary |> Fold.evolveTagsChanged tagsChanged) actual
    }

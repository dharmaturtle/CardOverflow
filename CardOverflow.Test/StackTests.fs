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
[<NCrunch.Framework.TimeoutAttribute(600_0000)>]
let ``Create Stack summary roundtrips`` { Author = author; TemplateSummary = templateSummary; ExampleSummary = exampleSummary; Stack = stackSummary } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaWriter().Create author
    do! c.TemplateWriter().Create templateSummary
    do! c.ExampleWriter().Create exampleSummary
    let stackWriter = c.StackWriter()

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
[<NCrunch.Framework.TimeoutAttribute(600_0000)>]
let ``Changing tags roundtrips`` { Author = author; TemplateSummary = templateSummary; ExampleSummary = exampleSummary; Stack = stackSummary } tagsChanged = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaWriter().Create author
    do! c.TemplateWriter().Create templateSummary
    do! c.ExampleWriter().Create exampleSummary
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

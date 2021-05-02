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
open EventAppender
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp
open Domain.Stack

[<StandardProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_0000)>]
let ``Changing tags roundtrips`` { Author = author; TemplateCreated = templateSummary; ExampleSummary = exampleSummary; Stack = stackSummary } tagsChanged = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create author
    do! c.TemplateComboAppender().Create templateSummary
    do! c.ExampleAppender().Create exampleSummary
    let stackAppender = c.StackAppender()
    do! stackAppender.Create stackSummary
    
    do! stackAppender.ChangeTags tagsChanged stackSummary.AuthorId stackSummary.Id

    // event store roundtrips
    stackSummary.Id
    |> c.StackEvents
    |> Seq.last
    |> Assert.equal (Stack.Events.TagsChanged tagsChanged)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetStack stackSummary.Id
    Assert.equal (stackSummary |> Fold.evolveTagsChanged tagsChanged) actual
    }

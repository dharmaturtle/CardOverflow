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
let ``Changing tags roundtrips`` { SignedUp = signedUp; TemplateCreated = templateSummary; ExampleCreated = exampleSummary; StackCreated = stackCreated; TagsChanged = tagsChanged } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    do! c.TemplateComboAppender().Create templateSummary
    do! c.ExampleAppender().Create exampleSummary
    let stackAppender = c.StackAppender()
    do! stackAppender.Create stackCreated
    
    do! stackAppender.ChangeTags tagsChanged stackCreated.Id

    // event store roundtrips
    stackCreated.Id
    |> c.StackEvents
    |> Seq.last
    |> Assert.equal (Stack.Events.TagsChanged tagsChanged)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetStack stackCreated.Id
    Assert.equal (stackCreated |> Fold.evolveCreated |> Fold.evolveTagsChanged tagsChanged) actual
    }

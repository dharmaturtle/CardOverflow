module DeckTests

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

[<StandardProperty>]
let ``Create summary roundtrips (event store)`` signedUp { DeckCreated = deckCreated } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    let deckAppender = c.DeckAppender()

    do! deckAppender.Create deckCreated

    deckCreated.Id
    |> c.DeckEvents
    |> Seq.exactlyOne
    |> Assert.equal (Deck.Events.Created deckCreated)
    }

[<StandardProperty>]
let ``Create summary roundtrips (azure table)`` signedUp { DeckCreated = deckCreated } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    let deckAppender = c.DeckAppender()
    let keyValueStore = c.KeyValueStore()
    let expected =
        let extra =
            signedUp.DisplayName
            |> Projection.Kvs.DeckExtra.init
            |> serializeToJson
        deckCreated
        |> Deck.Fold.evolveCreated
        |> fun x -> { x with Extra = extra }
        |> Deck.Fold.Active

    do! deckAppender.Create deckCreated

    let! actual = keyValueStore.GetDeck deckCreated.Id
    Assert.equal expected actual
    }

[<StandardProperty>]
let ``Edited roundtrips (event store)`` signedUp { DeckCreated = deckCreated; DeckEdited = edited } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    let deckAppender = c.DeckAppender()
    do! deckAppender.Create deckCreated
    
    do! deckAppender.Edit edited deckCreated.Id

    deckCreated.Id
    |> c.DeckEvents
    |> Seq.last
    |> Assert.equal (Deck.Events.Edited edited)
    }

[<StandardProperty>]
let ``Edited roundtrips (azure table)`` signedUp { DeckCreated = deckCreated; DeckEdited = edited } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    let deckAppender = c.DeckAppender()
    let keyValueStore = c.KeyValueStore()
    do! deckAppender.Create deckCreated
    let expected =
        let extra =
            signedUp.DisplayName
            |> Projection.Kvs.DeckExtra.init
            |> serializeToJson
        deckCreated
        |> Deck.Fold.evolveCreated
        |> Deck.Fold.evolveEdited edited
        |> fun x -> { x with Extra = extra }
        |> Deck.Fold.Active
    
    do! deckAppender.Edit edited deckCreated.Id

    let! actual = keyValueStore.GetDeck deckCreated.Id
    Assert.equal expected actual
    }

[<FastProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_000)>]
let ``ElasticSearch works`` signedUp meta stackDiscarded deckDiscarded { DeckCreated = deckCreated; DeckEdited = edited } { TemplateCreated = templateCreated; ExampleCreated = exampleCreated; StackCreated = stackCreated } = asyncResult {
    let c = TestEsContainer(true)
    do! c.UserSagaAppender().Create signedUp
    let elsea = c.ElseaClient()
    let deckAppender = c.DeckAppender()
    let expected = deckCreated |> Deck.Fold.evolveCreated
    
    // Create works
    do! deckAppender.Create deckCreated

    let! actual = elsea.GetDeckSearch deckCreated.Id
    expected |> Projection.DeckSearch.fromSummary' signedUp.DisplayName 0 0 |> Assert.equal actual.Value

    // Edit works
    let expected = expected |> Deck.Fold.evolveEdited edited |> Projection.DeckSearch.fromSummary' signedUp.DisplayName 0 0

    do! deckAppender.Edit edited expected.Id
    
    let! actual = elsea.GetDeckSearch deckCreated.Id
    Assert.equal expected actual.Value

    // ExampleCount increments for Created Stack
    let expected = { expected with ExampleCount = 1 }
    do! c.TemplateAppender().Create templateCreated
    do! c.ExampleAppender().Create exampleCreated
    let stackAppender = c.StackAppender()
    
    do! stackAppender.Create { stackCreated with DeckIds = Set.singleton deckCreated.Id }
    
    let! actual = elsea.GetDeckSearch deckCreated.Id
    Assert.equal expected actual.Value
    
    // ExampleCount increments and decrements for `DecksChanged` Stack
    let kvs = c.KeyValueStore()
    let! defaultDeck =
        signedUp.Meta.UserId
        |> kvs.GetProfile
        |>% fun x -> x.Decks
        |>% Set.filter (fun x -> x.Id <> deckCreated.Id)
        |>% Set.exactlyOne
    let expectedDecrement = { expected    with ExampleCount = 0 }
    let expectedIncrement = { defaultDeck with ExampleCount = 1 }
    let deckChanged: Stack.Events.DecksChanged = { Meta    = meta
                                                   DeckIds = defaultDeck.Id |> Set.singleton }

    do! stackAppender.ChangeDecks deckChanged stackCreated.Id

    let! actual = elsea.GetDeckSearch deckCreated.Id
    Assert.equal expectedDecrement actual.Value
    let! actual = elsea.GetDeckSearch defaultDeck.Id
    Assert.equal expectedIncrement actual.Value

    // Discarding Stack decrements
    let expected = { expectedIncrement with ExampleCount = 0 }
    
    do! stackAppender.Discard stackDiscarded stackCreated.Id

    let! actual = elsea.GetDeckSearch expected.Id
    Assert.equal expected actual.Value

    // discarding deck works
    do! deckAppender.Discard deckDiscarded deckCreated.Id
    
    let! actual = elsea.GetDeckSearch deckCreated.Id
    Assert.True actual.IsNone
    }

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
let ``Create summary roundtrips (event store)`` { DeckEdit.DeckCreated = deckCreated } = asyncResult {
    let c = TestEsContainer()
    let deckAppender = c.DeckAppender()

    do! deckAppender.Create deckCreated

    deckCreated.Id
    |> c.DeckEvents
    |> Seq.exactlyOne
    |> Assert.equal (Deck.Events.Created deckCreated)
    }

[<StandardProperty>]
let ``Create summary roundtrips (azure table)`` { DeckEdit.DeckCreated = deckCreated } = asyncResult {
    let c = TestEsContainer()
    let deckAppender = c.DeckAppender()
    let keyValueStore = c.KeyValueStore()

    do! deckAppender.Create deckCreated

    let! actual = keyValueStore.GetDeck deckCreated.Id
    deckCreated |> Deck.Fold.evolveCreated |> Assert.equal actual
    }

[<StandardProperty>]
let ``Edited roundtrips (event store)`` { DeckCreated = deckCreated; DeckEdited = edited } = asyncResult {
    let c = TestEsContainer()
    let deckAppender = c.DeckAppender()
    do! deckAppender.Create deckCreated
    
    do! deckAppender.Edit edited deckCreated.Meta.UserId deckCreated.Id

    deckCreated.Id
    |> c.DeckEvents
    |> Seq.last
    |> Assert.equal (Deck.Events.Edited edited)
    }

[<StandardProperty>]
let ``Edited roundtrips (azure table)`` { DeckCreated = deckCreated; DeckEdited = edited } = asyncResult {
    let c = TestEsContainer()
    let deckAppender = c.DeckAppender()
    let keyValueStore = c.KeyValueStore()
    do! deckAppender.Create deckCreated
    
    do! deckAppender.Edit edited deckCreated.Meta.UserId deckCreated.Id

    let! actual = keyValueStore.GetDeck deckCreated.Id
    deckCreated |> Deck.Fold.evolveCreated |> Deck.Fold.evolveEdited edited |> Assert.equal actual
    }

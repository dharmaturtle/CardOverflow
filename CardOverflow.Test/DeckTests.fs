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
open Domain.Summary

[<StandardProperty>]
let ``Create summary roundtrips (event store)`` (deckSummary: Deck) = asyncResult {
    let c = TestEsContainer()
    let deckAppender = c.DeckAppender()

    do! deckAppender.Create deckSummary

    deckSummary.Id
    |> c.DeckEvents
    |> Seq.exactlyOne
    |> Assert.equal (Deck.Events.Created deckSummary)
    }

[<StandardProperty>]
let ``Create summary roundtrips (azure table)`` (deckSummary: Deck) = asyncResult {
    let c = TestEsContainer()
    let deckAppender = c.DeckAppender()
    let keyValueStore = c.KeyValueStore()

    do! deckAppender.Create deckSummary

    let! actual = keyValueStore.GetDeck deckSummary.Id
    Assert.equal deckSummary actual
    }

[<StandardProperty>]
let ``Edited roundtrips (event store)`` (deckSummary: Deck) (edited: Deck.Events.Edited) = asyncResult {
    let c = TestEsContainer()
    let deckAppender = c.DeckAppender()
    do! deckAppender.Create deckSummary
    
    do! deckAppender.Edit edited deckSummary.AuthorId deckSummary.Id

    deckSummary.Id
    |> c.DeckEvents
    |> Seq.last
    |> Assert.equal (Deck.Events.Edited edited)
    }

[<StandardProperty>]
let ``Edited roundtrips (azure table)`` (deckSummary: Deck) (edited: Deck.Events.Edited) = asyncResult {
    let c = TestEsContainer()
    let deckAppender = c.DeckAppender()
    let keyValueStore = c.KeyValueStore()
    do! deckAppender.Create deckSummary
    
    do! deckAppender.Edit edited deckSummary.AuthorId deckSummary.Id

    let! actual = keyValueStore.GetDeck deckSummary.Id
    Assert.equal (Deck.Fold.evolveEdited edited deckSummary) actual
    }

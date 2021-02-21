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
open EventService
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp

[<StandardProperty>]
let ``Create summary roundtrips (event store)`` (deckSummary: Deck.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let deckService = c.DeckService()

    do! deckService.Create deckSummary

    deckSummary.Id
    |> c.DeckEvents
    |> Seq.exactlyOne
    |> Assert.equal (Deck.Events.Created deckSummary)
    }

[<StandardProperty>]
let ``Create summary roundtrips (azure table)`` (deckSummary: Deck.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let deckService = c.DeckService()
    let tableClient = c.TableClient()

    do! deckService.Create deckSummary

    let! actual, _ = tableClient.GetDeck deckSummary.Id
    Assert.equal deckSummary actual
    }

[<StandardProperty>]
let ``Edited roundtrips (event store)`` (deckSummary: Deck.Events.Summary) (edited: Deck.Events.Edited) = asyncResult {
    let c = TestEsContainer()
    let deckService = c.DeckService()
    do! deckService.Create deckSummary
    
    do! deckService.Edit edited deckSummary.UserId deckSummary.Id

    deckSummary.Id
    |> c.DeckEvents
    |> Seq.last
    |> Assert.equal (Deck.Events.Edited edited)
    }

[<StandardProperty>]
let ``Edited roundtrips (azure table)`` (deckSummary: Deck.Events.Summary) (edited: Deck.Events.Edited) = asyncResult {
    let c = TestEsContainer()
    let deckService = c.DeckService()
    let tableClient = c.TableClient()
    do! deckService.Create deckSummary
    
    do! deckService.Edit edited deckSummary.UserId deckSummary.Id

    let! actual, _ = tableClient.GetDeck deckSummary.Id
    Assert.equal (Deck.Fold.evolveEdited edited deckSummary) actual
    }

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
open EventWriter
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp

[<StandardProperty>]
let ``Create summary roundtrips (event store)`` (deckSummary: Deck.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let deckWriter = c.DeckWriter()

    do! deckWriter.Create deckSummary

    deckSummary.Id
    |> c.DeckEvents
    |> Seq.exactlyOne
    |> Assert.equal (Deck.Events.Created deckSummary)
    }

[<StandardProperty>]
let ``Create summary roundtrips (azure table)`` (deckSummary: Deck.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let deckWriter = c.DeckWriter()
    let tableClient = c.TableClient()

    do! deckWriter.Create deckSummary

    let! actual, _ = tableClient.GetDeck deckSummary.Id
    Assert.equal deckSummary actual
    }

[<StandardProperty>]
let ``Edited roundtrips (event store)`` (deckSummary: Deck.Events.Summary) (edited: Deck.Events.Edited) = asyncResult {
    let c = TestEsContainer()
    let deckWriter = c.DeckWriter()
    do! deckWriter.Create deckSummary
    
    do! deckWriter.Edit edited deckSummary.AuthorId deckSummary.Id

    deckSummary.Id
    |> c.DeckEvents
    |> Seq.last
    |> Assert.equal (Deck.Events.Edited edited)
    }

[<StandardProperty>]
let ``Edited roundtrips (azure table)`` (deckSummary: Deck.Events.Summary) (edited: Deck.Events.Edited) = asyncResult {
    let c = TestEsContainer()
    let deckWriter = c.DeckWriter()
    let tableClient = c.TableClient()
    do! deckWriter.Create deckSummary
    
    do! deckWriter.Edit edited deckSummary.AuthorId deckSummary.Id

    let! actual, _ = tableClient.GetDeck deckSummary.Id
    Assert.equal (Deck.Fold.evolveEdited edited deckSummary) actual
    }

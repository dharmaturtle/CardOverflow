module DeckRepositoryTests

open CardOverflow.Entity
open CardOverflow.Debug
open CardOverflow.Pure
open Microsoft.EntityFrameworkCore
open CardOverflow.Api
open CardOverflow.Test
open System
open System.Linq
open Xunit
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Control.Tasks
open CardOverflow.Sanitation
open FsToolkit.ErrorHandling

[<Fact>]
let ``SanitizeDeckRepository works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3

    // get yields default deck
    let! (decks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    
    let defaultDeckId = 3
    Assert.Equal<ViewDeck list>(
        [{  Id = defaultDeckId
            IsPublic = false
            Name = "Default Deck"
            Count = 0 }
        ], decks)

    // adding a new deck
    let deckName = Guid.NewGuid().ToString()

    let! deckId = SanitizeDeckRepository.create c.Db userId deckName

    Assert.Equal(4, deckId)
    Assert.SingleI(c.Db.Deck.Where(fun x -> x.Name = deckName))

    // get yields 2 decks
    let! (decks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    Assert.Equal<ViewDeck list>(
        [{  Id = defaultDeckId
            IsPublic = false
            Name = "Default Deck"
            Count = 0 }
         {  Id = deckId
            IsPublic = false
            Name = deckName
            Count = 0 }
        ], decks)

    // new cards are in the "Default" deck
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId []
    let stackId = 1
    let! (decks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId

    Assert.Equal<ViewDeck list>(
        [{  Id = defaultDeckId
            IsPublic = false
            Name = "Default Deck"
            Count = 1 }
         {  Id = deckId
            IsPublic = false
            Name = deckName
            Count = 0 }
        ], decks)
    let! (card: AcquiredCard ResizeArray) = StackRepository.GetAcquired c.Db userId stackId
    let card = card.Single()
    Assert.Equal(defaultDeckId, card.DeckId)

    // switching to new deck works
    do! SanitizeDeckRepository.switch c.Db userId deckId card.AcquiredCardId
    
    let! (decks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    Assert.Equal<ViewDeck list>(
        [{  Id = defaultDeckId
            IsPublic = false
            Name = "Default Deck"
            Count = 0 }
         {  Id = deckId
            IsPublic = false
            Name = deckName
            Count = 1 }
        ], decks)
    let! (card: AcquiredCard ResizeArray) = StackRepository.GetAcquired c.Db userId stackId
    let card = card.Single()
    Assert.Equal(deckId, card.DeckId)
    
    // switching is idempotent
    do! SanitizeDeckRepository.switch c.Db userId deckId card.AcquiredCardId
    
    let! (decks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    Assert.Equal<ViewDeck list>(
        [{  Id = defaultDeckId
            IsPublic = false
            Name = "Default Deck"
            Count = 0 }
         {  Id = deckId
            IsPublic = false
            Name = deckName
            Count = 1 }
        ], decks)
    let! (card: AcquiredCard ResizeArray) = StackRepository.GetAcquired c.Db userId stackId
    let card = card.Single()
    Assert.Equal(deckId, card.DeckId)
    
    // can switch back to default deck
    do! SanitizeDeckRepository.switch c.Db userId defaultDeckId card.AcquiredCardId
    
    let! (decks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    Assert.Equal<ViewDeck list>(
        [{  Id = defaultDeckId
            IsPublic = false
            Name = "Default Deck"
            Count = 1 }
         {  Id = deckId
            IsPublic = false
            Name = deckName
            Count = 0 }
        ], decks)
    let! (card: AcquiredCard ResizeArray) = StackRepository.GetAcquired c.Db userId stackId
    let card = card.Single()
    Assert.Equal(defaultDeckId, card.DeckId)


    // errors
    let! (x: Result<_,_>) = SanitizeDeckRepository.create c.Db userId deckName
    Assert.Equal(sprintf "User #%i already has a Deck named '%s'" userId deckName, x.error)

    let invalidDeckName = Random.cryptographicString 251
    let! (x: Result<_,_>) = SanitizeDeckRepository.create c.Db userId invalidDeckName
    Assert.Equal(sprintf "Deck name '%s' is too long. It must be less than 250 characters." invalidDeckName, x.error)
    
    let invalidDeckId = 1337
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId invalidDeckId card.AcquiredCardId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)
    
    let invalidAcquiredCardId = 1337
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId deckId invalidAcquiredCardId
    Assert.Equal(sprintf "Either AcquiredCard #%i doesn't belong to you or it doesn't exist" invalidAcquiredCardId, x.error)
    
    let nonauthor = 1
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db nonauthor deckId card.AcquiredCardId
    Assert.Equal(sprintf "Either AcquiredCard #%i doesn't belong to you or it doesn't exist" card.AcquiredCardId, x.error)
    } |> TaskResult.getOk)

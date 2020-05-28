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
    let defaultDeckId = 3
    let defaultDeck =
        {   Id = defaultDeckId
            IsPublic = false
            IsDefault = true
            Name = "Default Deck"
            Count = 0 }

    let! (actualDecks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    
    Assert.areEquivalent [defaultDeck] actualDecks
    
    // can't delete default deck
    let! (x: Result<_,_>) = SanitizeDeckRepository.delete c.Db userId defaultDeckId
    
    Assert.equal "You can't delete your default deck. Make another deck default first." x.error

    // adding a new deck
    let newDeckName = Guid.NewGuid().ToString()

    let! actualDeckId = SanitizeDeckRepository.create c.Db userId newDeckName

    let newDeckId = 4
    Assert.equal newDeckId actualDeckId
    Assert.SingleI <| c.Db.Deck.Where(fun x -> x.Name = newDeckName)

    // get yields 2 decks
    let newDeck =
        {   defaultDeck with
                Id = newDeckId
                IsDefault = false
                Name = newDeckName }

    let! (actualDecks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId

    Assert.areEquivalent [ newDeck; defaultDeck] actualDecks

    // new cards are in the "Default" deck
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId []
    let stackId = 1
    let acquiredCardId = 1
    let assertDeckId expectedDeckId = taskResult {
        let! (card: AcquiredCard ResizeArray) = StackRepository.GetAcquired c.Db userId stackId
        let card = card.Single()
        Assert.equal expectedDeckId card.DeckId
        Assert.equal acquiredCardId card.AcquiredCardId
    }
    
    let! (actualDecks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId

    Assert.areEquivalent [ newDeck; { defaultDeck with Count = 1 }] actualDecks
    do! assertDeckId defaultDeckId

    // switching to new deck works
    do! SanitizeDeckRepository.switch c.Db userId newDeckId acquiredCardId
    
    let! (actualDecks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    
    Assert.areEquivalent [ { newDeck with Count = 1 }; defaultDeck] actualDecks
    do! assertDeckId newDeckId
    
    // switching is idempotent
    do! SanitizeDeckRepository.switch c.Db userId newDeckId acquiredCardId
    
    let! (actualDecks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    
    Assert.areEquivalent [ { newDeck with Count = 1 }; defaultDeck] actualDecks
    do! assertDeckId newDeckId
    
    // can switch back to default deck
    do! SanitizeDeckRepository.switch c.Db userId defaultDeckId acquiredCardId
    
    let! (actualDecks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    
    Assert.areEquivalent [ newDeck; { defaultDeck with Count = 1 }] actualDecks 
    do! assertDeckId defaultDeckId
    
    // can delete new deck
    do! SanitizeDeckRepository.delete c.Db userId newDeckId
    
    let! (actualDecks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    
    Assert.areEquivalent [{ defaultDeck with Count = 1 }] actualDecks 
    do! assertDeckId defaultDeckId

    // can add new deck with same name
    let! actualDeckId = SanitizeDeckRepository.create c.Db userId newDeckName
    
    let newDeckId = 5
    Assert.equal newDeckId actualDeckId
    Assert.SingleI <| c.Db.Deck.Where(fun x -> x.Name = newDeckName)

    // get yields 2 decks
    let newDeck = { newDeck with Id = newDeckId }

    let! (actualDecks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId

    Assert.areEquivalent [ newDeck; { defaultDeck with Count = 1} ] actualDecks

    // set newDeck as default
    do! SanitizeDeckRepository.setDefault c.Db userId newDeckId

    let! (actualDecks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    Assert.areEquivalent [
        { newDeck with IsDefault = true}
        { defaultDeck with IsDefault = false; Count = 1} ] actualDecks

    // deleting deck with cards moves them to new default
    do! SanitizeDeckRepository.delete c.Db userId defaultDeckId
    
    let! (card: AcquiredCard ResizeArray) = StackRepository.GetAcquired c.Db userId stackId
    let card = card.Single()
    Assert.equal newDeckId card.DeckId
    let! (actualDecks: ViewDeck list) = SanitizeDeckRepository.get c.Db userId
    Assert.areEquivalent [{ newDeck with IsDefault = true; Count = 1} ] actualDecks

    // errors
    let! (x: Result<_,_>) = SanitizeDeckRepository.create c.Db userId newDeckName
    Assert.Equal(sprintf "User #%i already has a Deck named '%s'" userId newDeckName, x.error)

    let invalidDeckName = Random.cryptographicString 251
    let! (x: Result<_,_>) = SanitizeDeckRepository.create c.Db userId invalidDeckName
    Assert.Equal(sprintf "Deck name '%s' is too long. It must be less than 250 characters." invalidDeckName, x.error)
    
    let invalidDeckId = 1337
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId invalidDeckId acquiredCardId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.delete c.Db userId invalidDeckId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.setDefault c.Db userId invalidDeckId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)
    
    let invalidAcquiredCardId = 1337
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId newDeckId invalidAcquiredCardId
    Assert.Equal(sprintf "Either AcquiredCard #%i doesn't belong to you or it doesn't exist" invalidAcquiredCardId, x.error)
    
    let nonauthor = 1
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db nonauthor newDeckId acquiredCardId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" actualDeckId, x.error)

    let! _ = FacetRepositoryTests.addBasicStack c.Db nonauthor []
    let nonauthorAcquiredCardId = 2
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId newDeckId nonauthorAcquiredCardId
    Assert.Equal(sprintf "Either AcquiredCard #%i doesn't belong to you or it doesn't exist" nonauthorAcquiredCardId, x.error)
    
    let! (x: Result<_,_>) = SanitizeDeckRepository.delete c.Db nonauthor newDeckId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" actualDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.setDefault c.Db nonauthor newDeckId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" actualDeckId, x.error)
    } |> TaskResult.getOk)

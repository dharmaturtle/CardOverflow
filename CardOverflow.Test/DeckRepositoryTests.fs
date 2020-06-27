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
open FsToolkit.ErrorHandling.Operator.Task
open FacetRepositoryTests

[<Fact>]
let ``SanitizeDeckRepository works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let withCount count deck =
        { deck with
            DueCount = count
            AllCount = count }
    let getTomorrow () =
        SanitizeDeckRepository.get c.Db userId <| DateTime.UtcNow + TimeSpan.FromDays 1.
    let getYesterday () =
        SanitizeDeckRepository.get c.Db userId <| DateTime.UtcNow - TimeSpan.FromDays 1.

    // get yields default deck
    let defaultDeckId = 3
    let defaultDeck =
        {   Id = defaultDeckId
            IsPublic = false
            IsDefault = true
            Name = "Default Deck"
            AllCount = 0
            DueCount = 0 }

    let! actualDecks = getTomorrow ()
    
    Assert.areEquivalent [defaultDeck] actualDecks
    
    // set default deck to public
    do! SanitizeDeckRepository.setIsPublic c.Db userId defaultDeckId true

    let! actualDecks = getTomorrow ()
    Assert.areEquivalent [ { defaultDeck with IsPublic = true } ] actualDecks
    
    // set default deck to not public
    do! SanitizeDeckRepository.setIsPublic c.Db userId defaultDeckId false

    let! actualDecks = getTomorrow ()
    Assert.areEquivalent [ defaultDeck ] actualDecks
    
    // setIsPublic is idempotent
    do! SanitizeDeckRepository.setIsPublic c.Db userId defaultDeckId false

    let! actualDecks = getTomorrow ()
    Assert.areEquivalent [ defaultDeck ] actualDecks
    
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

    let! actualDecks = getTomorrow ()

    Assert.areEquivalent [ newDeck; defaultDeck] actualDecks

    // can rename default deck
    let newDefaultDeckName = Guid.NewGuid().ToString()
    let defaultDeck = { defaultDeck with Name = newDefaultDeckName }

    do! SanitizeDeckRepository.rename c.Db userId defaultDeckId newDefaultDeckName

    let! actualDecks = getTomorrow ()
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
    
    let! actualDecks = getTomorrow ()

    Assert.areEquivalent [ newDeck; defaultDeck |> withCount 1 ] actualDecks
    do! assertDeckId defaultDeckId

    // switching to new deck works
    do! SanitizeDeckRepository.switch c.Db userId newDeckId acquiredCardId
    
    let! actualDecks = getTomorrow ()
    
    Assert.areEquivalent [ newDeck |> withCount 1 ; defaultDeck] actualDecks
    do! assertDeckId newDeckId
    
    // switching is idempotent
    do! SanitizeDeckRepository.switch c.Db userId newDeckId acquiredCardId
    
    let! actualDecks = getTomorrow ()
    
    Assert.areEquivalent [ newDeck |> withCount 1; defaultDeck] actualDecks
    do! assertDeckId newDeckId
    
    // can switch back to default deck
    do! SanitizeDeckRepository.switch c.Db userId defaultDeckId acquiredCardId
    
    let! actualDecks = getTomorrow ()
    
    Assert.areEquivalent [ newDeck; defaultDeck |> withCount 1 ] actualDecks 
    do! assertDeckId defaultDeckId
    
    // can delete new deck
    do! SanitizeDeckRepository.delete c.Db userId newDeckId
    
    let! actualDecks = getTomorrow ()
    
    Assert.areEquivalent [ defaultDeck |> withCount 1 ] actualDecks 
    do! assertDeckId defaultDeckId

    // can add new deck with same name
    let! actualDeckId = SanitizeDeckRepository.create c.Db userId newDeckName
    
    let newDeckId = 5
    Assert.equal newDeckId actualDeckId
    Assert.SingleI <| c.Db.Deck.Where(fun x -> x.Name = newDeckName)

    // get yields 2 decks
    let newDeck = { newDeck with Id = newDeckId }

    let! actualDecks = getTomorrow ()

    Assert.areEquivalent [ newDeck; defaultDeck |> withCount 1 ] actualDecks

    // set newDeck as default
    do! SanitizeDeckRepository.setDefault c.Db userId newDeckId

    let! actualDecks = getTomorrow ()
    Assert.areEquivalent [
        { newDeck with IsDefault = true}
        { (defaultDeck |> withCount 1) with IsDefault = false} ] actualDecks

    // deleting deck with cards moves them to new default
    do! SanitizeDeckRepository.delete c.Db userId defaultDeckId
    
    let! (card: AcquiredCard ResizeArray) = StackRepository.GetAcquired c.Db userId stackId
    let card = card.Single()
    Assert.equal newDeckId card.DeckId
    let! actualDecks = getTomorrow ()
    Assert.areEquivalent [{ (newDeck |> withCount 1) with IsDefault = true } ] actualDecks

    // getYesterday isn't due
    let! actualDecks = getYesterday ()

    Assert.areEquivalent
        [ { newDeck with
              IsDefault = true
              AllCount = 1
              DueCount = 0 } ]
        actualDecks

    // errors
    let! (x: Result<_,_>) = SanitizeDeckRepository.create c.Db userId newDeckName
    Assert.Equal(sprintf "User #%i already has a Deck named '%s'" userId newDeckName, x.error)
    
    let! (x: Result<_,_>) = SanitizeDeckRepository.rename c.Db userId newDeckId newDeckName
    Assert.Equal(sprintf "User #%i already has a Deck named '%s'" userId newDeckName, x.error)

    let invalidDeckName = Random.cryptographicString 251
    let! (x: Result<_,_>) = SanitizeDeckRepository.create c.Db userId invalidDeckName
    Assert.Equal(sprintf "Deck name '%s' is too long. It must be less than 250 characters." invalidDeckName, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.rename c.Db userId newDeckId invalidDeckName
    Assert.Equal(sprintf "Deck name '%s' is too long. It must be less than 250 characters." invalidDeckName, x.error)
    
    let invalidDeckId = 1337
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId invalidDeckId acquiredCardId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.delete c.Db userId invalidDeckId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.setDefault c.Db userId invalidDeckId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)
    
    let! (x: Result<_,_>) = SanitizeDeckRepository.setIsPublic c.Db userId invalidDeckId true
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)
    
    let invalidAcquiredCardId = 1337
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId newDeckId invalidAcquiredCardId
    Assert.Equal(sprintf "Either AcquiredCard #%i doesn't belong to you or it doesn't exist" invalidAcquiredCardId, x.error)
    
    let nonauthor = 1
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db nonauthor newDeckId acquiredCardId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" newDeckId, x.error)

    let! _ = FacetRepositoryTests.addBasicStack c.Db nonauthor []
    let nonauthorAcquiredCardId = 2
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId newDeckId nonauthorAcquiredCardId
    Assert.Equal(sprintf "Either AcquiredCard #%i doesn't belong to you or it doesn't exist" nonauthorAcquiredCardId, x.error)
    
    let! (x: Result<_,_>) = SanitizeDeckRepository.delete c.Db nonauthor newDeckId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" newDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.setDefault c.Db nonauthor newDeckId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" newDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.setIsPublic c.Db nonauthor newDeckId true
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" newDeckId, x.error)
    } |> TaskResult.getOk)

[<Fact>]
let ``SanitizeDeckRepository follow works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = 3
    let followerId = 1
    let publicDeck =
        {   Id = 4
            Name = Guid.NewGuid().ToString()
            AuthorId = authorId
            AuthorName = "RoboTurtle"
            IsFollowed = false
            FollowCount = 0
        }
    do! SanitizeDeckRepository.create c.Db authorId publicDeck.Name |>%% Assert.equal publicDeck.Id
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeck.Id true
    do! SanitizeDeckRepository.setDefault c.Db authorId publicDeck.Id

    // getPublic yields expected deck
    do! Assert.Single >> Assert.equal publicDeck <!> DeckRepository.getPublic c.Db authorId   authorId
    do! Assert.Single >> Assert.equal publicDeck <!> DeckRepository.getPublic c.Db followerId authorId

    // follow works
    do! SanitizeDeckRepository.follow c.Db followerId publicDeck.Id
    
    do! Assert.Single 
        >> Assert.equal { publicDeck with IsFollowed = true; FollowCount = 1 }
        <%> DeckRepository.getPublic c.Db followerId authorId

    //adding a card notifies
    let! _ = addBasicStack c.Db authorId []
    let notificationId = 1
    let stackId = 1
    let branchId = 1
    let authorAcquiredId = 1
    
    let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
    
    let n = ns.Results |> Assert.Single
    n.TimeStamp |> Assert.dateTimeEqual 60. DateTime.UtcNow
    n |> Assert.equal
        {   Id = notificationId
            SenderId = authorId
            SenderDisplayName = "RoboTurtle"
            TimeStamp = n.TimeStamp // cheating, but whatever
            Message = DeckAddedStack { DeckId = publicDeck.Id
                                       NewStackId = stackId
                                       NewBranchId = branchId
                                       NewBranchInstanceId = 1001 } }

    // can remove notification
    do! NotificationRepository.remove c.Db followerId notificationId

    Assert.Empty c.Db.ReceivedNotification
    
    // can remove notification, idempotent
    do! NotificationRepository.remove c.Db followerId notificationId

    Assert.Empty c.Db.ReceivedNotification

    // edit card notifies follower
    let instance2 = 1002
    let notificationId = 2
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeStackRepository.getUpsert c.Db (VUpdateBranchId branchId)
    let updated = {
        old with
            ViewEditStackCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db authorId updated
    Assert.Equal(branchId, actualBranchId)
    let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
    
    let n = ns.Results |> Assert.Single
    n.TimeStamp |> Assert.dateTimeEqual 60. DateTime.UtcNow
    n |> Assert.equal
        { Id = 2
          SenderId = authorId
          SenderDisplayName = "RoboTurtle"
          TimeStamp = n.TimeStamp  // cheating, but whatever
          Message = DeckUpdatedStack { DeckId = publicDeck.Id
                                       NewStackId = stackId
                                       NewBranchId = branchId
                                       NewBranchInstanceId = instance2
                                       AcquiredStackId = None
                                       AcquiredBranchId = None
                                       AcquiredBranchInstanceId = None } }

    do! NotificationRepository.remove c.Db followerId notificationId
    // editing card's state doesn't notify follower
    do! StackRepository.editState c.Db authorId authorAcquiredId Suspended
    
    let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
    
    ns.Results |> Assert.Empty

    // Update notifies with follower's acquired card
    do! StackRepository.AcquireCardAsync c.Db followerId instance2
    let instance3 = 1003
    let notificationId = 3
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeStackRepository.getUpsert c.Db (VUpdateBranchId branchId)
    let updated = {
        old with
            ViewEditStackCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db authorId updated
    Assert.Equal(branchId, actualBranchId)
    let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
    
    let n = ns.Results |> Assert.Single
    n.TimeStamp |> Assert.dateTimeEqual 60. DateTime.UtcNow
    n |> Assert.equal
        { Id = notificationId
          SenderId = authorId
          SenderDisplayName = "RoboTurtle"
          TimeStamp = n.TimeStamp  // cheating, but whatever
          Message = DeckUpdatedStack { DeckId = publicDeck.Id
                                       NewStackId = stackId
                                       NewBranchId = branchId
                                       NewBranchInstanceId = instance3
                                       AcquiredStackId = Some 1
                                       AcquiredBranchId = Some 1
                                       AcquiredBranchInstanceId = Some instance2 } }
    
    do! NotificationRepository.remove c.Db followerId notificationId
    // deleting acquiredCard from deck has notification
    do! StackRepository.unacquireStack c.Db authorId stackId

    let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
    let n = ns.Results |> Assert.Single
    n.TimeStamp |> Assert.dateTimeEqual 60. DateTime.UtcNow
    n |> Assert.equal
        { Id = 4
          SenderId = authorId
          SenderDisplayName = "RoboTurtle"
          TimeStamp = n.TimeStamp  // cheating, but whatever
          Message = DeckDeletedStack { DeckId = publicDeck.Id
                                       DeletedStackId = stackId
                                       DeletedBranchId = branchId
                                       DeletedBranchInstanceId = instance3 } }

    // diff says a stack was removed
    let! diffs = SanitizeDeckRepository.diff c.Db followerId publicDeck.Id followerId

    Assert.equal
        [ RemovedStack { StackId = stackId
                         BranchId = branchId
                         BranchInstanceId = instance2 } ]
        diffs

    // unfollow works
    do! SanitizeDeckRepository.unfollow c.Db followerId publicDeck.Id
    
    do! Assert.Single 
        >> Assert.equal publicDeck
        <%> DeckRepository.getPublic c.Db followerId authorId

    // second unfollow fails
    do! SanitizeDeckRepository.unfollow c.Db followerId publicDeck.Id
        |> TaskResult.getError
        |>% Assert.equal "Either the deck doesn't exist or you are not following it."
    
    // unfollow nonexisting deck fails
    do! SanitizeDeckRepository.unfollow c.Db followerId 1337
        |> TaskResult.getError
        |>% Assert.equal "Either the deck doesn't exist or you are not following it."

    // second follow fails
    do! SanitizeDeckRepository.follow c.Db followerId publicDeck.Id
    do! SanitizeDeckRepository.follow c.Db followerId publicDeck.Id
        |> TaskResult.getError
        |>% Assert.equal "Either the deck doesn't exist or you are already following it."

    // nonexistant deck fails
    do! SanitizeDeckRepository.follow c.Db followerId 1337
        |> TaskResult.getError
        |>% Assert.equal "Either the deck doesn't exist or you are already following it."

    // can delete followed deck
    do! SanitizeDeckRepository.setDefault c.Db authorId authorId
    do! SanitizeDeckRepository.delete c.Db authorId publicDeck.Id
    do! DeckRepository.getPublic c.Db followerId authorId |>% Assert.Empty
    } |> TaskResult.getOk)

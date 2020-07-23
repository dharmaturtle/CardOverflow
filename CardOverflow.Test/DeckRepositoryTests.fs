module DeckRepositoryTests

open Helpers
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
open CardOverflow.Sanitation.SanitizeDeckRepository
open CardOverflow.Entity

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

let getRealError = function
    | RealError e -> e
    | _ -> failwith "dude"
let getEditExistingIsNull_BranchInstanceIdsByDeckId = function
    | EditExistingIsNull_BranchInstanceIdsByDeckId e -> e
    | _ -> failwith "dude"

[<Fact>]
let ``SanitizeDeckRepository.follow works with "NoDeck true None"``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = 3
    let authorDefaultDeckId = 3
    let followerId = 1
    let publicDeck =
        {   Id = 4
            Name = Guid.NewGuid().ToString()
            AuthorId = authorId
            AuthorName = "RoboTurtle"
            IsFollowed = false
            FollowCount = 0
        }
    let theirDeck = { Id = publicDeck.Id
                      Name = publicDeck.Name }
    do! SanitizeDeckRepository.create c.Db authorId publicDeck.Name |>%% Assert.equal publicDeck.Id
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeck.Id true
    do! SanitizeDeckRepository.setDefault c.Db authorId publicDeck.Id
    let assertNotificationThenDelete expected = task {
        let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
        let n = ns.Results |> Assert.Single
        n.TimeStamp |> Assert.dateTimeEqual 60. DateTime.UtcNow
        n |> Assert.equal
            {   expected with
                    TimeStamp = n.TimeStamp // cheating, but whatever
            }
        do! NotificationRepository.remove c.Db followerId n.Id
    }
    let follow deckId = SanitizeDeckRepository.follow c.Db followerId deckId NoDeck true None // mind the test name

    // getPublic yields expected deck
    do! Assert.Single >> Assert.equal publicDeck <!> DeckRepository.getPublic c.Db authorId   authorId
    do! Assert.Single >> Assert.equal publicDeck <!> DeckRepository.getPublic c.Db followerId authorId

    // follow works
    do! follow publicDeck.Id
    
    do! Assert.Single 
        >> Assert.equal { publicDeck with IsFollowed = true; FollowCount = 1 }
        <%> DeckRepository.getPublic c.Db followerId authorId

    //adding a card notifies
    let! _ = addBasicStack c.Db authorId []
    let notificationId = 1
    let stackId = 1
    let branchId = 1
    let authorAcquiredId = 1
    let instance1 =
        {   StackId = stackId
            BranchId = branchId
            BranchInstanceId = 1001 }
    
    do! assertNotificationThenDelete
            {   Id = notificationId
                SenderId = authorId
                SenderDisplayName = "RoboTurtle"
                TimeStamp = DateTime.MinValue
                Message = DeckAddedStack { TheirDeck = theirDeck
                                           New = instance1 } }

    // notification deleted
    Assert.Empty c.Db.ReceivedNotification
    
    // can remove notification, idempotent
    do! NotificationRepository.remove c.Db followerId notificationId

    Assert.Empty c.Db.ReceivedNotification

    // edit card notifies follower
    let instance2 = { instance1 with BranchInstanceId = 1002 }
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeStackRepository.getUpsert c.Db (VUpdateBranchId branchId)
    let updated = {
        old with
            ViewEditStackCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db authorId [] updated
    Assert.Equal(branchId, actualBranchId)
    do! assertNotificationThenDelete
            { Id = 2
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              TimeStamp = DateTime.MinValue
              Message = DeckUpdatedStack { TheirDeck = theirDeck
                                           New = instance2
                                           Collected = None } }

    // editing card's state doesn't notify follower
    do! StackRepository.editState c.Db authorId authorAcquiredId Suspended
    
    let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
    
    ns.Results |> Assert.Empty

    // Update notifies with follower's acquired card
    do! StackRepository.AcquireCardAsync c.Db followerId instance2.BranchInstanceId
    let instance3 = { instance2 with BranchInstanceId = 1003 }
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeStackRepository.getUpsert c.Db (VUpdateBranchId branchId)
    let updated = {
        old with
            ViewEditStackCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db authorId [] updated
    Assert.Equal(branchId, actualBranchId)
    do! assertNotificationThenDelete
            { Id = 3
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              TimeStamp = DateTime.MinValue
              Message = DeckUpdatedStack { TheirDeck = theirDeck
                                           New = instance3
                                           Collected = Some instance2 } }

    // changing to private deck has notification
    do! SanitizeDeckRepository.switch c.Db authorId authorDefaultDeckId authorAcquiredId

    do! assertNotificationThenDelete
            { Id = 4
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              TimeStamp = DateTime.MinValue
              Message = DeckDeletedStack { TheirDeck = theirDeck
                                           Deleted = instance3 } }

    // changing back to public deck has notification
    do! SanitizeDeckRepository.switch c.Db authorId publicDeck.Id authorAcquiredId

    do! assertNotificationThenDelete
            { Id = 5
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              TimeStamp = DateTime.MinValue
              Message = DeckAddedStack { TheirDeck = theirDeck
                                         New = instance3 } }

    // changing to another public deck that's also followed generates 2 notifications
    do! SanitizeDeckRepository.setIsPublic c.Db authorId authorDefaultDeckId true
    do! follow authorDefaultDeckId

    do! SanitizeDeckRepository.switch c.Db authorId authorDefaultDeckId authorAcquiredId

    let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
    let a = ns.Results.ToList().[0]
    let b = ns.Results.ToList().[1]
    a.TimeStamp |> Assert.dateTimeEqual 60. DateTime.UtcNow
    b.TimeStamp |> Assert.dateTimeEqual 60. DateTime.UtcNow
    a |> Assert.equal
        { Id = 6
          SenderId = 3
          SenderDisplayName = "RoboTurtle"
          TimeStamp = a.TimeStamp
          Message = DeckAddedStack { TheirDeck =
                                        { Id = authorDefaultDeckId
                                          Name = "Default Deck" }
                                     New = instance3 } }
    b |> Assert.equal
        { Id = 7
          SenderId = 3
          SenderDisplayName = "RoboTurtle"
          TimeStamp = b.TimeStamp
          Message = DeckDeletedStack
                     { TheirDeck = theirDeck
                       Deleted = instance3 } }
    
    // back to public deck and some cleanup
    do! NotificationRepository.remove c.Db followerId a.Id
    do! NotificationRepository.remove c.Db followerId b.Id
    do! SanitizeDeckRepository.switch c.Db authorId publicDeck.Id authorAcquiredId
    do! NotificationRepository.remove c.Db followerId 8
    do! NotificationRepository.remove c.Db followerId 9
    do! SanitizeDeckRepository.setIsPublic c.Db authorId authorDefaultDeckId false

    // deleting acquiredCard from deck has notification
    do! StackRepository.unacquireStack c.Db authorId stackId

    do! assertNotificationThenDelete
            { Id = 10
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              TimeStamp = DateTime.MinValue
              Message = DeckDeletedStack { TheirDeck = theirDeck
                                           Deleted = instance3 } }

    // diff says a stack was removed
    do! SanitizeDeckRepository.diff c.Db followerId publicDeck.Id followerId

    |>%% Assert.equal
        {   Unchanged = []
            BranchInstanceChanged = []
            BranchChanged = []
            AddedStack = []
            RemovedStack =
                [ { StackId = stackId
                    BranchId = branchId
                    BranchInstanceId = instance2.BranchInstanceId
                    Index = 0s
                    DeckId = followerId }]
        }

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
    do! follow publicDeck.Id
    do! follow publicDeck.Id
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal (sprintf "You're already following Deck #%i" publicDeck.Id)

    // nonexistant deck fails
    do! follow 1337
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal "Either Deck #1337 doesn't exist or it isn't public."

    // private deck fails
    do! follow authorDefaultDeckId
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal "Either Deck #3 doesn't exist or it isn't public."

    // can delete followed deck
    do! SanitizeDeckRepository.setDefault c.Db authorId authorId
    do! SanitizeDeckRepository.delete c.Db authorId publicDeck.Id
    do! DeckRepository.getPublic c.Db followerId authorId |>% Assert.Empty
    } |> TaskResult.getOk)

[<Fact>]
let ``SanitizeDeckRepository.follow works with "OldDeck false *"``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = 3
    let publicDeckId = 3
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeckId true
    do! FacetRepositoryTests.addBasicStack c.Db authorId []
    let stackId = 1
    let branchId = 1
    let branchInstanceId = 1001
    let followerId = 1
    let followerDeckId = 1
    let! newFollowerDeckId = SanitizeDeckRepository.create c.Db followerId <| Guid.NewGuid().ToString()
    let follow oldDeckId editExisting = SanitizeDeckRepository.follow c.Db followerId publicDeckId (OldDeck oldDeckId) false editExisting // mind the test name

    // follow targeting newFollowerDeckId with extant card in default deck fails
    do! StackRepository.AcquireCardAsync c.Db followerId branchInstanceId

    do! follow newFollowerDeckId None
        |> TaskResult.getError
        |>% getEditExistingIsNull_BranchInstanceIdsByDeckId
        |>% Assert.Single
        |>% Assert.equal (followerDeckId, ResizeArray.singleton branchInstanceId)

    // follow targeting default deck with extant card in default deck works
    do! follow followerDeckId None
        |> TaskResult.getOk
    
    // follow with someone else's deckId fails
    do! StackRepository.unacquireStack c.Db followerId stackId
    do! follow 2 None
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal "Either Deck #2 doesn't exist or it doesn't belong to you."
    
    // follow with "OldDeck false None" works
    
    do! follow followerDeckId None |> TaskResult.getOk
    
    let! ac =
        StackRepository.GetAcquired c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { AcquiredCardId = 3
          UserId = followerId
          StackId = stackId
          BranchId = branchId
          BranchInstanceMeta = ac.BranchInstanceMeta // untested
          Index = 0s
          CardState = Normal
          IsLapsed = false
          EaseFactorInPermille = 0s
          IntervalOrStepsIndex = NewStepsIndex 0uy
          Due = ac.Due // untested
          CardSettingId = followerId
          Tags = []
          DeckId = followerDeckId }
        ac
    
    // follow with "editExisting false" after update, doesn't update
    do! FacetRepositoryTests.update c authorId
            (VUpdateBranchId branchId) id branchId
    let newBranchInstanceId = branchInstanceId + 1
    
    do! follow followerDeckId (Some false) |> TaskResult.getOk

    let! ac2 =
        StackRepository.GetAcquired c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { ac with
            BranchInstanceMeta = ac2.BranchInstanceMeta // untested
        }   // unchanged
        ac2
    Assert.equal
        branchInstanceId
        ac2.BranchInstanceMeta.Id
    
    // follow with "editExisting true" after update, updates
    do! follow followerDeckId (Some true) |> TaskResult.getOk

    let! ac3 =
        StackRepository.GetAcquired c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { ac with
            BranchInstanceMeta = ac3.BranchInstanceMeta // untested
        }   // unchanged
        ac3
    Assert.equal
        newBranchInstanceId
        ac3.BranchInstanceMeta.Id
    } |> TaskResult.getOk)

[<Fact>]
let ``SanitizeDeckRepository.follow works with "OldDeck false None" pair``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = 3
    let publicDeckId = 3
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeckId true
    do! FacetRepositoryTests.addReversedBasicStack c.Db authorId []
    let acId1 = 1
    let stackId = 1
    let branchId = 1
    let followerId = 1
    let followerDeckId = 1
    let follow oldDeckId editExisting = SanitizeDeckRepository.follow c.Db followerId publicDeckId (OldDeck oldDeckId) false editExisting // mind the test name

    // follow with "OldDeck false None" and both of a pair works
    do! follow followerDeckId None |> TaskResult.getOk
    
    let! (acs: AcquiredCard ResizeArray) =
        StackRepository.GetAcquired c.Db followerId stackId
        |>%% (Seq.sortBy (fun x -> x.Index) >> ResizeArray)
    Assert.equal 2 acs.Count
    let a, b = acs.[0], acs.[1]
    Assert.equal
        { AcquiredCardId = 3
          UserId = followerId
          StackId = stackId
          BranchId = 1
          BranchInstanceMeta = a.BranchInstanceMeta // untested
          Index = 0s
          CardState = Normal
          IsLapsed = false
          EaseFactorInPermille = 0s
          IntervalOrStepsIndex = NewStepsIndex 0uy
          Due = a.Due // untested
          CardSettingId = followerId
          Tags = []
          DeckId = followerDeckId }
        a
    Assert.equal
        { a with
            AcquiredCardId = 4
            BranchInstanceMeta = b.BranchInstanceMeta // untested
            Index = 1s }
        b
    
    // follow with "OldDeck false None" and one of a pair works
    do! StackRepository.unacquireStack c.Db followerId stackId
    let! newDeckId = SanitizeDeckRepository.create c.Db authorId <| Guid.NewGuid().ToString()
    do! SanitizeDeckRepository.switch c.Db authorId newDeckId acId1
    
    do! follow followerDeckId None |> TaskResult.getOk
    
    let! (ac: AcquiredCard) =
        StackRepository.GetAcquired c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { b with
            BranchInstanceMeta = ac.BranchInstanceMeta // untested
            Due = ac.Due // untested
            AcquiredCardId = 5 }
        ac
    
    // follow with "editExisting false" after update, doesn't update
    do! FacetRepositoryTests.update c authorId
            (VUpdateBranchId branchId) id branchId
    
    do! follow followerDeckId (Some false) |> TaskResult.getOk
    
    let! (ac: AcquiredCard) =
        StackRepository.GetAcquired c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { b with
            BranchInstanceMeta = ac.BranchInstanceMeta // untested
            Due = ac.Due // untested
            AcquiredCardId = 5 }
        ac
    
    // follow with "editExisting true" after update, updates
    do! StackRepository.unacquireStack c.Db followerId stackId
    do! SanitizeDeckRepository.switch c.Db authorId publicDeckId acId1

    do! follow followerDeckId (Some true) |> TaskResult.getOk

    let! (acs: AcquiredCard ResizeArray) =
        StackRepository.GetAcquired c.Db followerId stackId
        |>%% (Seq.sortBy (fun x -> x.Index) >> ResizeArray)
    Assert.equal 2 acs.Count
    let a, b = acs.[0], acs.[1]
    Assert.equal
        { AcquiredCardId = 7
          UserId = followerId
          StackId = stackId
          BranchId = 1
          BranchInstanceMeta = a.BranchInstanceMeta // untested
          Index = 0s
          CardState = Normal
          IsLapsed = false
          EaseFactorInPermille = 0s
          IntervalOrStepsIndex = NewStepsIndex 0uy
          Due = a.Due // untested
          CardSettingId = followerId
          Tags = []
          DeckId = followerDeckId }
        a
    Assert.equal
        { a with
            AcquiredCardId = 6
            BranchInstanceMeta = b.BranchInstanceMeta // untested
            Index = 1s }
        b
    } |> TaskResult.getOk)

[<Fact>]
let ``SanitizeDeckRepository.follow works with "NewDeck false *"``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = 3
    let publicDeckId = 3
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeckId true
    do! FacetRepositoryTests.addBasicStack c.Db authorId []
    let stackId = 1
    let branchId = 1
    let branchInstanceId = 1001
    let followerId = 1
    let followerDeckId = 1
    let follow deckName editExisting = SanitizeDeckRepository.follow c.Db followerId publicDeckId (NewDeck deckName) false editExisting // mind the test name

    // follow with extant card fails and doesn't add a deck
    Assert.equal 3 <| c.Db.Deck.Count()
    do! StackRepository.AcquireCardAsync c.Db followerId branchInstanceId

    do! follow (Guid.NewGuid().ToString()) None
        
        |> TaskResult.getError
        |>% getEditExistingIsNull_BranchInstanceIdsByDeckId
        |>% Assert.Single
        |>% Assert.equal (followerDeckId, ResizeArray.singleton branchInstanceId)
    Assert.equal 3 <| c.Db.Deck.Count()

    // follow with huge name fails
    do! StackRepository.unacquireStack c.Db followerId stackId
    let longDeckName = Random.cryptographicString 251
    do! follow longDeckName None
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal (sprintf "Deck name '%s' is too long. It must be less than 250 characters." longDeckName)
    
    // follow with "OldDeck false None" works
    let newDeckId = 4
    
    do! follow (Guid.NewGuid().ToString()) None |> TaskResult.getOk
    
    let! ac =
        StackRepository.GetAcquired c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { AcquiredCardId = 3
          UserId = followerId
          StackId = stackId
          BranchId = branchId
          BranchInstanceMeta = ac.BranchInstanceMeta // untested
          Index = 0s
          CardState = Normal
          IsLapsed = false
          EaseFactorInPermille = 0s
          IntervalOrStepsIndex = NewStepsIndex 0uy
          Due = ac.Due // untested
          CardSettingId = followerId
          Tags = []
          DeckId = newDeckId }
        ac
    
    // follow with "editExisting false" after update, doesn't update
    do! FacetRepositoryTests.update c authorId
            (VUpdateBranchId branchId) id branchId
    let newBranchInstanceId = branchInstanceId + 1
    
    do! follow (Guid.NewGuid().ToString()) (Some false) |> TaskResult.getOk

    let! ac2 =
        StackRepository.GetAcquired c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { ac with
            BranchInstanceMeta = ac2.BranchInstanceMeta // untested
        }   // unchanged
        ac2
    Assert.equal
        branchInstanceId
        ac2.BranchInstanceMeta.Id
    
    // follow with "editExisting true" after update, updates
    do! follow (Guid.NewGuid().ToString()) (Some true) |> TaskResult.getOk
    let newestDeckId = newDeckId + 2

    let! ac3 =
        StackRepository.GetAcquired c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { ac with
            DeckId = newestDeckId
            BranchInstanceMeta = ac3.BranchInstanceMeta // untested
        }   // unchanged
        ac3
    Assert.equal
        newBranchInstanceId
        ac3.BranchInstanceMeta.Id
    } |> TaskResult.getOk)

[<Fact>]
let ``SanitizeDeckRepository.diff works``(): Task<unit> = (taskResult {
    let authorId = 3
    let publicDeckId = 3
    use c = new TestContainer()
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeckId true
    do! FacetRepositoryTests.addBasicStack c.Db authorId []
    let stackId = 1
    let branchId = 1
    let branchInstanceId = 1001
    let followerId = 1
    let followerDeckId = 1
    let standardIds =
        { StackId = stackId
          BranchId = branchId
          BranchInstanceId = branchInstanceId
          Index = 0s
          DeckId = followerDeckId }

    // diffing two decks with the same card yields Unchanged
    do! StackRepository.AcquireCardAsync c.Db followerId branchInstanceId
    
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = [ standardIds ]
            BranchInstanceChanged = []
            BranchChanged = []
            AddedStack = []
            RemovedStack = []
        }

    // diffing two decks, reversed, with the same card yields Unchanged
    do! SanitizeDeckRepository.diff c.Db followerId followerDeckId publicDeckId
    
    |>%% Assert.equal
        {   Unchanged = [{ standardIds with DeckId = publicDeckId }]
            BranchInstanceChanged = []
            BranchChanged = []
            AddedStack = []
            RemovedStack = []
        }

    // diffing with a deck that isn't public fails
    let nonpublicDeckId = 2
    do! SanitizeDeckRepository.diff c.Db followerId nonpublicDeckId followerDeckId
    
    |>% Result.getError
    |>% Assert.equal "Either Deck #2 doesn't exist, or it isn't public, or you don't own it."

    // diffing with a deck that doesn't exist fails
    let nonexistantDeckId = 1337
    do! SanitizeDeckRepository.diff c.Db followerId nonexistantDeckId followerDeckId
    
    |>% Result.getError
    |>% Assert.equal "Either Deck #1337 doesn't exist, or it isn't public, or you don't own it."

    // diffing with a deck that isn't public fails
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId nonpublicDeckId
    
    |>% Result.getError
    |>% Assert.equal "Either Deck #2 doesn't exist, or it isn't public, or you don't own it."

    // diffing with a deck that doesn't exist fails
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId nonexistantDeckId
    
    |>% Result.getError
    |>% Assert.equal "Either Deck #1337 doesn't exist, or it isn't public, or you don't own it."

    // moving card to newDeck _ is reflected in the diff
    let! newDeckId = SanitizeDeckRepository.create c.Db followerId <| Guid.NewGuid().ToString()
    let! (acs: AcquiredCard ResizeArray) = StackRepository.GetAcquired c.Db followerId stackId
    let acId = acs.Single().AcquiredCardId
    do! SanitizeDeckRepository.switch c.Db followerId newDeckId acId
     
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = []
            BranchInstanceChanged = []
            BranchChanged = []
            AddedStack = [{ standardIds with DeckId = newDeckId }]
            RemovedStack = []
        }

    // Testing simple adding (by unacquiring a stack)
    do! StackRepository.unacquireStack c.Db followerId stackId

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = []
            BranchInstanceChanged = []
            BranchChanged = []
            AddedStack = [{ standardIds with DeckId = publicDeckId }]
            RemovedStack = []
        }

    do! StackRepository.unacquireStack c.Db authorId stackId
    // Unchanged with two clozes
    let! actualBranchId = FacetRepositoryTests.addCloze "{{c1::Portland::city}} was founded in {{c2::1845}}." c.Db authorId []
    let! (acs: AcquiredCardEntity ResizeArray) = c.Db.AcquiredCard.Where(fun x -> x.BranchId = actualBranchId).ToListAsync()
    do! StackRepository.AcquireCardAsync c.Db followerId <| acs.First().BranchInstanceId
    let ids =
        {   StackId = 2
            BranchId = actualBranchId
            BranchInstanceId = 1002
            Index = 0s
            DeckId = followerDeckId }

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged =
                [ ids
                  { ids with Index = 1s } ]
            BranchInstanceChanged = []
            BranchChanged = []
            AddedStack = []
            RemovedStack = []
        }

    // two clozes, but different decks, index 1
    let! (acs: AcquiredCardEntity list) =
        c.Db.AcquiredCard
            .Where(fun x -> x.BranchId = actualBranchId && x.UserId = followerId)
            .ToListAsync()
        |>% Seq.toList
    Assert.equal 0s acs.[0].Index
    do! SanitizeDeckRepository.switch c.Db followerId newDeckId acs.[0].Id

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = [ { ids with Index = 1s } ]
            BranchInstanceChanged = []
            BranchChanged = []
            AddedStack = [ { ids with DeckId = newDeckId } ]
            RemovedStack = []
        }

    // two clozes, but different decks, index 2
    do! SanitizeDeckRepository.switch c.Db followerId followerDeckId acs.[0].Id
    do! SanitizeDeckRepository.switch c.Db followerId newDeckId      acs.[1].Id

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = [ ids ]
            BranchInstanceChanged = []
            BranchChanged = []
            AddedStack = [ { ids with Index = 1s; DeckId = newDeckId } ]
            RemovedStack = []
        }
    } |> TaskResult.getOk)

[<Fact>]
let ``SanitizeDeckRepository.diff works on Branch(Instance)Changed and deckchanges``(): Task<unit> = (taskResult {
    let authorId = 3
    let publicDeckId = 3
    use c = new TestContainer()
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeckId true
    do! FacetRepositoryTests.addBasicStack c.Db authorId []
    let stackId = 1
    let branchId = 1
    let branchInstanceId = 1001
    let followerId = 1
    let followerDeckId = 1
    let standardIds =
        { StackId = stackId
          BranchId = branchId
          BranchInstanceId = branchInstanceId
          Index = 0s
          DeckId = followerDeckId }

    // diffing two decks with the same card yields Unchanged
    do! StackRepository.AcquireCardAsync c.Db followerId branchInstanceId
    
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = [ standardIds ]
            BranchInstanceChanged = []
            BranchChanged = []
            AddedStack = []
            RemovedStack = []
        }

    // author switches to new branch
    let! stackCommand = SanitizeStackRepository.getUpsert c.Db (VNewBranchSourceStackId standardIds.StackId)
    do! SanitizeStackRepository.Update c.Db authorId [] stackCommand
    let newBranchIds =
        { standardIds with
              BranchId = standardIds.BranchId + 1
              BranchInstanceId = standardIds.BranchInstanceId + 1 }

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = []
            BranchInstanceChanged = []
            BranchChanged = [ ({ newBranchIds with DeckId = publicDeckId }, standardIds) ]
            AddedStack = []
            RemovedStack = []
        }

    // author switches to new branch, and follower's old card is in different deck
    let! newFollowerDeckId = SanitizeDeckRepository.create c.Db followerId <| Guid.NewGuid().ToString()
    let! (ac: AcquiredCardEntity) = c.Db.AcquiredCard.SingleAsync(fun x -> x.StackId = stackId && x.UserId = followerId)
    do! SanitizeDeckRepository.switch c.Db followerId newFollowerDeckId ac.Id
    
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = []
            BranchInstanceChanged = []
            BranchChanged = [ ({ newBranchIds with DeckId = publicDeckId }, { standardIds with DeckId = newFollowerDeckId }) ]
            AddedStack = []
            RemovedStack = []
        }

    do! SanitizeDeckRepository.switch c.Db followerId followerDeckId ac.Id
    do! StackRepository.AcquireCardAsync c.Db followerId newBranchIds.BranchInstanceId
    // author switches to new branchinstance
    let! stackCommand = SanitizeStackRepository.getUpsert c.Db (VUpdateBranchId newBranchIds.BranchId)
    do! SanitizeStackRepository.Update c.Db authorId [] stackCommand
    let newBranchInstanceIds =
        { newBranchIds with
              BranchInstanceId = newBranchIds.BranchInstanceId + 1 }

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = []
            BranchInstanceChanged = [ ({ newBranchInstanceIds with DeckId = publicDeckId }, newBranchIds) ]
            BranchChanged = []
            AddedStack = []
            RemovedStack = []
        }

    // author switches to new branchinstance, and follower's old card is in different deck
    do! SanitizeDeckRepository.switch c.Db followerId newFollowerDeckId ac.Id
    
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = []
            BranchInstanceChanged = [ ({ newBranchInstanceIds with DeckId = publicDeckId }, { newBranchIds with DeckId = newFollowerDeckId }) ]
            BranchChanged = []
            AddedStack = []
            RemovedStack = []
        }

    do! SanitizeDeckRepository.switch c.Db followerId followerDeckId ac.Id
    // author on new branch with new branchinstance, follower on old branch & instance
    do! StackRepository.AcquireCardAsync c.Db followerId standardIds.BranchInstanceId

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = []
            BranchInstanceChanged = []
            BranchChanged = [ ({ newBranchInstanceIds with DeckId = publicDeckId }, standardIds) ]
            AddedStack = []
            RemovedStack = []
        }

    // author on new branch with new branchinstance, follower on old branch & instance and different deck
    do! SanitizeDeckRepository.switch c.Db followerId newFollowerDeckId ac.Id

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   Unchanged = []
            BranchInstanceChanged = []
            BranchChanged = [ ({ newBranchInstanceIds with DeckId = publicDeckId }, { standardIds with DeckId = newFollowerDeckId }) ]
            AddedStack = []
            RemovedStack = []
        }
    } |> TaskResult.getOk)

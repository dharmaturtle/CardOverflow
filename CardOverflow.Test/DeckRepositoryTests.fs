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

let emptyDiffStateSummary =
    {   Unchanged = []
        BranchInstanceChanged = []
        BranchChanged = []
        AddedStack = []
        RemovedStack = []
        MoveToAnotherDeck = []
    }

[<Fact>]
let ``SanitizeDeckRepository.setSource works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = 1
    let sourceDeckId = 1
    let followerId = 3
    let expectedDeck =
        {   Id = 3
            IsPublic = false
            IsDefault = true
            Name = "Default Deck"
            AllCount = 0
            DueCount = 0
            SourceDeck =
                {   Id = sourceDeckId
                    Name = "Default Deck" } |> Some }
    let setSource = SanitizeDeckRepository.setSource c.Db followerId expectedDeck.Id
    let getEquals expected =
        SanitizeDeckRepository.get c.Db followerId DateTime.UtcNow
        |>% Seq.exactlyOne
        |>% Assert.equal expected
    
    // nonpublic fails
    let! (error: Result<unit, string>) = setSource <| Some sourceDeckId
    Assert.equal "Either Deck #1 doesn't exist or it isn't public." error.error
    
    // nonexistant fails
    let! (error: Result<unit, string>) = setSource <| Some 1337
    Assert.equal "Either Deck #1337 doesn't exist or it isn't public." error.error

    // public works
    do! SanitizeDeckRepository.setIsPublic c.Db authorId sourceDeckId true

    do! setSource <| Some sourceDeckId
    
    do! getEquals expectedDeck

    // unset works
    do! setSource None
    
    do! getEquals { expectedDeck with SourceDeck = None }
    } |> TaskResult.getOk)

[<Fact>]
let ``SanitizeDeckRepository.search works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    use! conn = c.Conn()
    let searchAssert query expected =
        SanitizeDeckRepository.search conn userId query
        |>% Assert.areEquivalent expected
        
    let deck1 =
        {   Id = 1
            Name = "Default Deck"
            AuthorId = 1
            AuthorName = "Admin"
            IsFollowed = false
            FollowCount = 0 }
    let deck2 =
        {   deck1 with
                Id = 2
                AuthorId = 2
                AuthorName = "The Collective"  }
    let deck3 =
        {   deck1 with
                Id = 3
                AuthorId = 3
                AuthorName = "RoboTurtle"  }

    // doesn't reveal private decks
    do! searchAssert "" [deck3]

    // empty string lists all
    do! SanitizeDeckRepository.setIsPublic c.Db 1 1 true
    do! SanitizeDeckRepository.setIsPublic c.Db 2 2 true

    do! searchAssert "" [deck1; deck2; deck3]

    // search works
    let name1 = "one"
    let name2 = "two"
    let name3 = sprintf "%s %s" name1 name2
    do! SanitizeDeckRepository.rename c.Db 1 1 name1
    do! SanitizeDeckRepository.rename c.Db 2 2 name2
    do! SanitizeDeckRepository.rename c.Db 3 3 name3
    let deck1 = { deck1 with Name = name1 }
    let deck2 = { deck2 with Name = name2 }
    let deck3 = { deck3 with Name = name3 }

    do! searchAssert name1 [deck1; deck3]
    do! searchAssert name2 [deck2; deck3]
    do! searchAssert name3 [deck3]

    // injection attack fails
    do! searchAssert "'" []
    } |> TaskResult.getOk)

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
            DueCount = 0
            SourceDeck = None }

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
    let collectedCardId = 1
    let assertDeckId expectedDeckId = taskResult {
        let! (card: CollectedCard ResizeArray) = StackRepository.GetCollected c.Db userId stackId
        let card = card.Single()
        Assert.equal expectedDeckId card.DeckId
        Assert.equal collectedCardId card.CollectedCardId
    }
    
    let! actualDecks = getTomorrow ()

    Assert.areEquivalent [ newDeck; defaultDeck |> withCount 1 ] actualDecks
    do! assertDeckId defaultDeckId

    // switching to new deck works
    do! SanitizeDeckRepository.switch c.Db userId newDeckId collectedCardId
    
    let! actualDecks = getTomorrow ()
    
    Assert.areEquivalent [ newDeck |> withCount 1 ; defaultDeck] actualDecks
    do! assertDeckId newDeckId
    
    // switching is idempotent
    do! SanitizeDeckRepository.switch c.Db userId newDeckId collectedCardId
    
    let! actualDecks = getTomorrow ()
    
    Assert.areEquivalent [ newDeck |> withCount 1; defaultDeck] actualDecks
    do! assertDeckId newDeckId
    
    // can switch back to default deck
    do! SanitizeDeckRepository.switch c.Db userId defaultDeckId collectedCardId
    
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
    
    let! (card: CollectedCard ResizeArray) = StackRepository.GetCollected c.Db userId stackId
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
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId invalidDeckId collectedCardId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.delete c.Db userId invalidDeckId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.setDefault c.Db userId invalidDeckId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)
    
    let! (x: Result<_,_>) = SanitizeDeckRepository.setIsPublic c.Db userId invalidDeckId true
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" invalidDeckId, x.error)
    
    let invalidCollectedCardId = 1337
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId newDeckId invalidCollectedCardId
    Assert.Equal(sprintf "Either CollectedCard #%i doesn't belong to you or it doesn't exist" invalidCollectedCardId, x.error)
    
    let nonauthor = 1
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db nonauthor newDeckId collectedCardId
    Assert.Equal(sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" newDeckId, x.error)

    let! _ = FacetRepositoryTests.addBasicStack c.Db nonauthor []
    let nonauthorCollectedCardId = 2
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId newDeckId nonauthorCollectedCardId
    Assert.Equal(sprintf "Either CollectedCard #%i doesn't belong to you or it doesn't exist" nonauthorCollectedCardId, x.error)
    
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
    let followerDefaultDeckId = 1
    let otherDudeId = 2
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

    // getPublic and getDeckWithFollowMeta doesn't work on not-quite-yet-public deck
    do! DeckRepository.getPublic                     c.Db authorId   authorId |>% Assert.Empty
    do! DeckRepository.getPublic                     c.Db followerId authorId |>% Assert.Empty
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db authorId   publicDeck.Id             |>%% Assert.equal publicDeck
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db followerId publicDeck.Id             |>% (fun x -> Assert.equal "Either Deck #4 doesn't exist or it isn't public." x.error)

    // getPublic and getDeckWithFollowMeta yield expected deck
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeck.Id true

    do! DeckRepository.getPublic                     c.Db authorId   authorId |>% Assert.Single |>% Assert.equal publicDeck
    do! DeckRepository.getPublic                     c.Db followerId authorId |>% Assert.Single |>% Assert.equal publicDeck
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db authorId   publicDeck.Id             |>%% Assert.equal publicDeck
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db followerId publicDeck.Id             |>%% Assert.equal publicDeck

    // follow works
    do! follow publicDeck.Id
    
    do! DeckRepository.getPublic                     c.Db authorId   authorId |>% Assert.Single |>% Assert.equal { publicDeck with FollowCount = 1 }
    do! DeckRepository.getPublic                     c.Db followerId authorId |>% Assert.Single |>% Assert.equal { publicDeck with FollowCount = 1; IsFollowed = true }
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db authorId   publicDeck.Id             |>%% Assert.equal { publicDeck with FollowCount = 1 }
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db followerId publicDeck.Id             |>%% Assert.equal { publicDeck with FollowCount = 1; IsFollowed = true }

    //adding a card notifies
    do! SanitizeDeckRepository.setDefault c.Db authorId publicDeck.Id
    let! _ = addBasicStack c.Db authorId []
    let notificationId = 1
    let stackId = 1
    let branchId = 1
    let authorCollectedId = 1
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
                                           MyDeck = None
                                           New = instance1
                                           Collected = None } }

    // *both* notifications deleted
    Assert.Empty c.Db.ReceivedNotification
    Assert.Empty c.Db.Notification
    
    // can remove notification, idempotent
    do! NotificationRepository.remove c.Db followerId notificationId

    Assert.Empty c.Db.ReceivedNotification
    Assert.Empty c.Db.Notification

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
                                           MyDeck = None
                                           New = instance2
                                           Collected = None } }

    // editing card's state doesn't notify follower
    do! StackRepository.editState c.Db authorId authorCollectedId Suspended
    
    let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
    
    ns.Results |> Assert.Empty

    // Update notifies with follower's collected card
    do! StackRepository.CollectCard c.Db followerId instance2.BranchInstanceId
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
    let collected =
        {   StackId = instance2.StackId
            BranchId = instance2.BranchId
            BranchInstanceId = instance2.BranchInstanceId } |> Some
    do! assertNotificationThenDelete
            { Id = 3
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              TimeStamp = DateTime.MinValue
              Message = DeckUpdatedStack { TheirDeck = theirDeck
                                           MyDeck = None
                                           New = instance3
                                           Collected = collected } }

    // changing to private deck has notification
    do! SanitizeDeckRepository.switch c.Db authorId authorDefaultDeckId authorCollectedId

    do! assertNotificationThenDelete
            { Id = 4
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              TimeStamp = DateTime.MinValue
              Message = DeckDeletedStack { TheirDeck = theirDeck
                                           MyDeck = None
                                           Collected = collected
                                           Deleted = instance3 } }

    // changing back to public deck has notification
    do! SanitizeDeckRepository.switch c.Db authorId publicDeck.Id authorCollectedId

    do! assertNotificationThenDelete
            { Id = 5
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              TimeStamp = DateTime.MinValue
              Message = DeckAddedStack { TheirDeck = theirDeck
                                         MyDeck = None
                                         New = instance3
                                         Collected = collected } }

    // changing to another public deck that's also followed generates 2 notifications
    do! SanitizeDeckRepository.setIsPublic c.Db authorId authorDefaultDeckId true
    do! follow authorDefaultDeckId

    do! SanitizeDeckRepository.switch c.Db authorId authorDefaultDeckId authorCollectedId

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
                                     MyDeck = None
                                     New = instance3
                                     Collected = collected } }
    b |> Assert.equal
        { Id = 7
          SenderId = 3
          SenderDisplayName = "RoboTurtle"
          TimeStamp = b.TimeStamp
          Message = DeckDeletedStack
                     { TheirDeck = theirDeck
                       MyDeck = None
                       Collected = collected
                       Deleted = instance3 } }
    
    // back to public deck and some cleanup
    do! NotificationRepository.remove c.Db followerId a.Id
    do! NotificationRepository.remove c.Db followerId b.Id
    do! SanitizeDeckRepository.switch c.Db authorId publicDeck.Id authorCollectedId
    do! NotificationRepository.remove c.Db followerId 8
    do! NotificationRepository.remove c.Db followerId 9
    do! SanitizeDeckRepository.setIsPublic c.Db authorId authorDefaultDeckId false

    // deleting collectedCard from deck has notification
    do! StackRepository.uncollectStack c.Db authorId stackId

    do! assertNotificationThenDelete
            { Id = 10
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              TimeStamp = DateTime.MinValue
              Message = DeckDeletedStack { TheirDeck = theirDeck
                                           MyDeck = None
                                           Collected = collected
                                           Deleted = instance3 } }

    // diff says a stack was removed
    do! SanitizeDeckRepository.diff c.Db followerId publicDeck.Id followerId

    |>%% Assert.equal
        {   emptyDiffStateSummary with
                RemovedStack =
                    [ { StackId = stackId
                        BranchId = branchId
                        BranchInstanceId = instance2.BranchInstanceId
                        Index = 0s
                        DeckId = followerId }] }

    // unfollow works
    do! SanitizeDeckRepository.unfollow c.Db followerId publicDeck.Id
    
    do! DeckRepository.getPublic                     c.Db authorId   authorId |>% Assert.Single |>% Assert.equal publicDeck
    do! DeckRepository.getPublic                     c.Db followerId authorId |>% Assert.Single |>% Assert.equal publicDeck
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db authorId   publicDeck.Id             |>%% Assert.equal publicDeck
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db followerId publicDeck.Id             |>%% Assert.equal publicDeck

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
    
    // someone ele following the deck bumps count to 2
    do! SanitizeDeckRepository.follow c.Db otherDudeId publicDeck.Id NoDeck true None
    
    do! DeckRepository.getPublic                     c.Db authorId   authorId |>% Assert.Single |>% Assert.equal { publicDeck with FollowCount = 2 }
    do! DeckRepository.getPublic                     c.Db followerId authorId |>% Assert.Single |>% Assert.equal { publicDeck with FollowCount = 2; IsFollowed = true }
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db authorId   publicDeck.Id             |>%% Assert.equal { publicDeck with FollowCount = 2 }
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db followerId publicDeck.Id             |>%% Assert.equal { publicDeck with FollowCount = 2; IsFollowed = true }
    
    // can delete followed deck that is the source of another deck
    do! SanitizeDeckRepository.setSource c.Db followerId followerDefaultDeckId (Some publicDeck.Id)
    Assert.equal (Some publicDeck.Id |> Option.toNullable) (c.Db.Deck.Single(fun x -> x.Id = followerDefaultDeckId).SourceId)
    do! SanitizeDeckRepository.setDefault c.Db authorId authorId

    do! SanitizeDeckRepository.delete c.Db authorId publicDeck.Id

    Assert.equal (Nullable()) (c.Db.Deck.Single(fun x -> x.Id = followerDefaultDeckId).SourceId)
    do! DeckRepository.getPublic                     c.Db authorId   authorId |>% Assert.Empty
    do! DeckRepository.getPublic                     c.Db followerId authorId |>% Assert.Empty
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db authorId   publicDeck.Id             |>% (fun x -> Assert.equal "Either Deck #4 doesn't exist or it isn't public." x.error)
    do! SanitizeDeckRepository.getDeckWithFollowMeta c.Db followerId publicDeck.Id             |>% (fun x -> Assert.equal "Either Deck #4 doesn't exist or it isn't public." x.error)
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
    do! StackRepository.CollectCard c.Db followerId branchInstanceId

    do! follow newFollowerDeckId None
        |> TaskResult.getError
        |>% getEditExistingIsNull_BranchInstanceIdsByDeckId
        |>% Assert.Single
        |>% Assert.equal (followerDeckId, ResizeArray.singleton branchInstanceId)

    // follow targeting default deck with extant card in default deck works
    do! follow followerDeckId None
        |> TaskResult.getOk
    
    // follow with someone else's deckId fails
    do! StackRepository.uncollectStack c.Db followerId stackId
    do! follow 2 None
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal "Either Deck #2 doesn't exist or it doesn't belong to you."
    
    // follow with "OldDeck false None" works
    
    do! follow followerDeckId None |> TaskResult.getOk
    
    let! cc =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { CollectedCardId = 3
          UserId = followerId
          StackId = stackId
          BranchId = branchId
          BranchInstanceMeta = cc.BranchInstanceMeta // untested
          Index = 0s
          CardState = Normal
          IsLapsed = false
          EaseFactorInPermille = 0s
          IntervalOrStepsIndex = NewStepsIndex 0uy
          Due = cc.Due // untested
          CardSettingId = followerId
          Tags = []
          DeckId = followerDeckId }
        cc
    
    // follow with "editExisting false" after update, doesn't update
    do! FacetRepositoryTests.update c authorId
            (VUpdateBranchId branchId) id branchId
    let newBranchInstanceId = branchInstanceId + 1
    
    do! follow followerDeckId (Some false) |> TaskResult.getOk

    let! ac2 =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { cc with
            BranchInstanceMeta = ac2.BranchInstanceMeta // untested
        }   // unchanged
        ac2
    Assert.equal
        branchInstanceId
        ac2.BranchInstanceMeta.Id
    
    // follow with "editExisting true" after update, updates
    do! follow followerDeckId (Some true) |> TaskResult.getOk

    let! ac3 =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { cc with
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
    let ccId1 = 1
    let stackId = 1
    let branchId = 1
    let followerId = 1
    let followerDeckId = 1
    let follow oldDeckId editExisting = SanitizeDeckRepository.follow c.Db followerId publicDeckId (OldDeck oldDeckId) false editExisting // mind the test name

    // follow with "OldDeck false None" and both of a pair works
    do! follow followerDeckId None |> TaskResult.getOk
    
    let! (ccs: CollectedCard ResizeArray) =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% (Seq.sortBy (fun x -> x.Index) >> ResizeArray)
    Assert.equal 2 ccs.Count
    let a, b = ccs.[0], ccs.[1]
    Assert.equal
        { CollectedCardId = 3
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
            CollectedCardId = 4
            BranchInstanceMeta = b.BranchInstanceMeta // untested
            Index = 1s }
        b
    
    // follow with "OldDeck false None" and one of a pair works
    do! StackRepository.uncollectStack c.Db followerId stackId
    let! newDeckId = SanitizeDeckRepository.create c.Db authorId <| Guid.NewGuid().ToString()
    do! SanitizeDeckRepository.switch c.Db authorId newDeckId ccId1
    
    do! follow followerDeckId None |> TaskResult.getOk
    
    let! (cc: CollectedCard) =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { b with
            BranchInstanceMeta = cc.BranchInstanceMeta // untested
            Due = cc.Due // untested
            CollectedCardId = 5 }
        cc
    
    // follow with "editExisting false" after update, doesn't update
    do! FacetRepositoryTests.update c authorId
            (VUpdateBranchId branchId) id branchId
    
    do! follow followerDeckId (Some false) |> TaskResult.getOk
    
    let! (cc: CollectedCard) =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { b with
            BranchInstanceMeta = cc.BranchInstanceMeta // untested
            Due = cc.Due // untested
            CollectedCardId = 5 }
        cc
    
    // follow with "editExisting true" after update, updates
    do! StackRepository.uncollectStack c.Db followerId stackId
    do! SanitizeDeckRepository.switch c.Db authorId publicDeckId ccId1

    do! follow followerDeckId (Some true) |> TaskResult.getOk

    let! (ccs: CollectedCard ResizeArray) =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% (Seq.sortBy (fun x -> x.Index) >> ResizeArray)
    Assert.equal 2 ccs.Count
    let a, b = ccs.[0], ccs.[1]
    Assert.equal
        { CollectedCardId = 7
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
            CollectedCardId = 6
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
    do! StackRepository.CollectCard c.Db followerId branchInstanceId

    do! follow (Guid.NewGuid().ToString()) None
        
        |> TaskResult.getError
        |>% getEditExistingIsNull_BranchInstanceIdsByDeckId
        |>% Assert.Single
        |>% Assert.equal (followerDeckId, ResizeArray.singleton branchInstanceId)
    Assert.equal 3 <| c.Db.Deck.Count()

    // follow with huge name fails
    do! StackRepository.uncollectStack c.Db followerId stackId
    let longDeckName = Random.cryptographicString 251
    do! follow longDeckName None
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal (sprintf "Deck name '%s' is too long. It must be less than 250 characters." longDeckName)
    
    // follow with "NewDeck false None" works
    let newDeckId = 4
    
    do! follow (Guid.NewGuid().ToString()) None |> TaskResult.getOk
    
    let! cc =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { CollectedCardId = 3
          UserId = followerId
          StackId = stackId
          BranchId = branchId
          BranchInstanceMeta = cc.BranchInstanceMeta // untested
          Index = 0s
          CardState = Normal
          IsLapsed = false
          EaseFactorInPermille = 0s
          IntervalOrStepsIndex = NewStepsIndex 0uy
          Due = cc.Due // untested
          CardSettingId = followerId
          Tags = []
          DeckId = newDeckId }
        cc
    
    // follow with "editExisting false" after update, doesn't update
    do! FacetRepositoryTests.update c authorId
            (VUpdateBranchId branchId) id branchId
    let newBranchInstanceId = branchInstanceId + 1
    
    do! follow (Guid.NewGuid().ToString()) (Some false) |> TaskResult.getOk

    let! ac2 =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { cc with
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
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { cc with
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
    do! StackRepository.CollectCard c.Db followerId branchInstanceId
    
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                Unchanged = [ standardIds ] }

    // diffing two decks, reversed, with the same card yields Unchanged
    do! SanitizeDeckRepository.diff c.Db followerId followerDeckId publicDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                Unchanged = [{ standardIds with DeckId = publicDeckId }] }

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
    let! (ccs: CollectedCard ResizeArray) = StackRepository.GetCollected c.Db followerId stackId
    let ccId = ccs.Single().CollectedCardId
    do! SanitizeDeckRepository.switch c.Db followerId newDeckId ccId
     
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                AddedStack = [{ standardIds with DeckId = newDeckId }] }

    // Testing simple adding (by uncollecting a stack)
    do! StackRepository.uncollectStack c.Db followerId stackId

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                AddedStack = [{ standardIds with DeckId = publicDeckId }] }

    do! StackRepository.uncollectStack c.Db authorId stackId
    // Unchanged with two clozes
    let! actualBranchId = FacetRepositoryTests.addCloze "{{c1::Portland::city}} was founded in {{c2::1845}}." c.Db authorId []
    let! (ccs: CollectedCardEntity ResizeArray) = c.Db.CollectedCard.Where(fun x -> x.BranchId = actualBranchId).ToListAsync()
    do! StackRepository.CollectCard c.Db followerId <| ccs.First().BranchInstanceId
    let ids =
        {   StackId = 2
            BranchId = actualBranchId
            BranchInstanceId = 1002
            Index = 0s
            DeckId = followerDeckId }

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                Unchanged =
                    [ ids
                      { ids with Index = 1s } ] }

    // two clozes, but different decks, index 1
    let! (ccs: CollectedCardEntity list) =
        c.Db.CollectedCard
            .Where(fun x -> x.BranchId = actualBranchId && x.UserId = followerId)
            .ToListAsync()
        |>% Seq.toList
    Assert.equal 0s ccs.[0].Index
    do! SanitizeDeckRepository.switch c.Db followerId newDeckId ccs.[0].Id

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                Unchanged = [ { ids with Index = 1s } ]
                AddedStack = [ { ids with DeckId = newDeckId } ] }

    // two clozes, but different decks, index 2
    do! SanitizeDeckRepository.switch c.Db followerId followerDeckId ccs.[0].Id
    do! SanitizeDeckRepository.switch c.Db followerId newDeckId      ccs.[1].Id

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                Unchanged = [ ids ]
                AddedStack = [ { ids with Index = 1s; DeckId = newDeckId } ] }
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
    do! StackRepository.CollectCard c.Db followerId branchInstanceId
    
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                Unchanged = [ standardIds ] }

    // author switches to new branch
    let! stackCommand = SanitizeStackRepository.getUpsert c.Db (VNewBranchSourceStackId standardIds.StackId)
    do! SanitizeStackRepository.Update c.Db authorId [] stackCommand
    let newBranchIds =
        { standardIds with
              BranchId = standardIds.BranchId + 1
              BranchInstanceId = standardIds.BranchInstanceId + 1 }

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                BranchChanged = [ ({ newBranchIds with DeckId = publicDeckId }, standardIds) ] }

    // author switches to new branch, and follower's old card is in different deck
    let! newFollowerDeckId = SanitizeDeckRepository.create c.Db followerId <| Guid.NewGuid().ToString()
    let! (cc: CollectedCardEntity) = c.Db.CollectedCard.SingleAsync(fun x -> x.StackId = stackId && x.UserId = followerId)
    do! SanitizeDeckRepository.switch c.Db followerId newFollowerDeckId cc.Id
    
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                BranchChanged = 
                    [ ({ newBranchIds with DeckId = publicDeckId },
                       { standardIds with DeckId = newFollowerDeckId }) ] }

    do! SanitizeDeckRepository.switch c.Db followerId followerDeckId cc.Id
    do! StackRepository.CollectCard c.Db followerId newBranchIds.BranchInstanceId
    // author switches to new branchinstance
    let! stackCommand = SanitizeStackRepository.getUpsert c.Db (VUpdateBranchId newBranchIds.BranchId)
    do! SanitizeStackRepository.Update c.Db authorId [] stackCommand
    let newBranchInstanceIds =
        { newBranchIds with
              BranchInstanceId = newBranchIds.BranchInstanceId + 1 }

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                BranchInstanceChanged = [ ({ newBranchInstanceIds with DeckId = publicDeckId }, newBranchIds) ] }

    // author switches to new branchinstance, and follower's old card is in different deck
    do! SanitizeDeckRepository.switch c.Db followerId newFollowerDeckId cc.Id
    
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                BranchInstanceChanged =
                    [ ({ newBranchInstanceIds with DeckId = publicDeckId },
                       { newBranchIds with DeckId = newFollowerDeckId }) ] }

    do! SanitizeDeckRepository.switch c.Db followerId followerDeckId cc.Id
    // author on new branch with new branchinstance, follower on old branch & instance
    do! StackRepository.CollectCard c.Db followerId standardIds.BranchInstanceId

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                BranchChanged = [ ({ newBranchInstanceIds with DeckId = publicDeckId }, standardIds) ] }

    // author on new branch with new branchinstance, follower on old branch & instance and different deck
    do! SanitizeDeckRepository.switch c.Db followerId newFollowerDeckId cc.Id

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                BranchChanged =
                    [ ({ newBranchInstanceIds with DeckId = publicDeckId },
                       { standardIds with DeckId = newFollowerDeckId }) ] }
    } |> TaskResult.getOk)

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
open FsCheck
open LoadersAndCopiers

let emptyDiffStateSummary =
    {   Unchanged = []
        LeafChanged = []
        BranchChanged = []
        AddedStack = []
        RemovedStack = []
        MoveToAnotherDeck = []
    }

[<Fact>]
let ``SanitizeDeckRepository.setSource works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = user_1
    let sourceDeckId = deck_1
    let followerId = user_3
    let expectedDeck =
        {   Id = deck_3
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
    Assert.equal "Either Deck #00000000-0000-0000-0000-decc00000001 doesn't exist or it isn't public." error.error
    
    // nonexistant fails
    let nonexistant = newGuid
    let! (error: Result<unit, string>) = setSource <| Some nonexistant
    Assert.equal (sprintf "Either Deck #%A doesn't exist or it isn't public." nonexistant) error.error

    // public works
    do! SanitizeDeckRepository.setIsPublic c.Db authorId sourceDeckId true

    do! setSource <| Some sourceDeckId
    
    do! getEquals expectedDeck

    // unset works
    do! setSource None
    
    do! getEquals { expectedDeck with SourceDeck = None }
    } |> TaskResult.getOk)
    
type SansTsvRank() = 
    interface IEqualityComparer<DeckWithFollowMeta> with
        member _.GetHashCode x = x.GetHashCode()
        member _.Equals(x, y) =
            x.Id = y.Id &&
            x.Name = y.Name &&
            x.AuthorId = y.AuthorId &&
            x.AuthorName = y.AuthorName &&
            x.IsFollowed = y.IsFollowed &&
            x.FollowCount = y.FollowCount

[<Fact>]
let ``SanitizeDeckRepository.search works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    use! conn = c.Conn()
    let areEquivalent = SansTsvRank () |> Assert.areEquivalentCustom
    let equals        = SansTsvRank () |> Assert.equalsCustom
    let searchAssert query expected =
        SanitizeDeckRepository.search conn userId query (Relevance None)
        |>% areEquivalent expected
        
    let deck1 =
        {   Id = deck_1
            Name = "Default Deck"
            AuthorId = user_1
            AuthorName = "Admin"
            IsFollowed = false
            FollowCount = 0
            TsvRank = 0. }
    let deck2 =
        {   deck1 with
                Id = deck_2
                AuthorId = user_2
                AuthorName = "The Collective"  }
    let deck3 =
        {   deck1 with
                Id = deck_3
                AuthorId = user_3
                AuthorName = "RoboTurtle"  }

    // doesn't reveal private decks
    do! searchAssert "" [deck3]

    // empty string lists all
    do! SanitizeDeckRepository.setIsPublic c.Db user_1 deck_1 true
    do! SanitizeDeckRepository.setIsPublic c.Db user_2 deck_2 true

    do! searchAssert "" [deck1; deck2; deck3]

    // search works
    let name1 = "one"
    let name2 = "two"
    let name3 = sprintf "%s %s" name1 name2
    do! SanitizeDeckRepository.rename c.Db user_1 deck_1 name1
    do! SanitizeDeckRepository.rename c.Db user_2 deck_2 name2
    do! SanitizeDeckRepository.rename c.Db user_3 deck_3 name3
    let deck1 = { deck1 with Name = name1 }
    let deck2 = { deck2 with Name = name2 }
    let deck3 = { deck3 with Name = name3 }

    do! searchAssert name1 [deck1; deck3]
    do! searchAssert name2 [deck2; deck3]
    do! searchAssert name3 [deck3]

    // injection attack fails
    do! searchAssert "'" []

    // sort by relevance works
    let searchAssert query order expected =
        SanitizeDeckRepository.search conn userId query (order None)
        |>% equals expected
    let x, y, z = Generators.differentPositives 3 |> Gen.sample1Gen |> fun x -> x.[0], x.[1], x.[2]
    let nameX = "jam "
    let deck1 = { deck1 with Name = String.replicate x nameX }
    let deck2 = { deck2 with Name = String.replicate y nameX }
    let deck3 = { deck3 with Name = String.replicate z nameX }
    do! SanitizeDeckRepository.rename c.Db user_1 deck_1 deck1.Name
    do! SanitizeDeckRepository.rename c.Db user_2 deck_2 deck2.Name
    do! SanitizeDeckRepository.rename c.Db user_3 deck_3 deck3.Name
    do! [deck1; deck2; deck3] |> List.sortByDescending (fun x -> x.Name.Length) |> searchAssert nameX Relevance

    // sort by popularity works
    let x, y, z = Generators.differentPositives 3 |> Gen.sample1Gen |> fun x -> x.[0], x.[1], x.[2]
    use db = c.Db
    db.Deck.Single(fun x -> x.Id = deck_1).Followers <- x
    db.Deck.Single(fun x -> x.Id = deck_2).Followers <- y
    db.Deck.Single(fun x -> x.Id = deck_3).Followers <- z
    do! db.SaveChangesAsyncI()
    let deck1 = { deck1 with FollowCount = x }
    let deck2 = { deck2 with FollowCount = y }
    let deck3 = { deck3 with FollowCount = z }

    do! [deck1; deck2; deck3] |> List.sortByDescending (fun x -> x.FollowCount) |> searchAssert "" Popularity

    // search by username works
    do! [deck1] |> searchAssert deck1.AuthorName Popularity
    do! [deck2] |> searchAssert deck2.AuthorName Popularity
    do! [deck3] |> searchAssert deck3.AuthorName Popularity
    do! [deck1] |> searchAssert deck1.AuthorName Relevance
    do! [deck2] |> searchAssert deck2.AuthorName Relevance
    do! [deck3] |> searchAssert deck3.AuthorName Relevance

    // keyset works
    let newMax = 40
    let intsGen = Arb.Default.PositiveInt() |> Arb.toGen |> Gen.map (fun x -> x.Get) |> Gen.listOfLength newMax
    for i in intsGen |> Gen.sample1Gen do
        do! SanitizeDeckRepository.create c.Db userId ((String.replicate i nameX) + Guid.NewGuid().ToString()) Ulid.create
    //let ids = [deck1; deck2; deck3] |> List.sortByDescending (fun x -> x.FollowCount) |> List.map (fun x -> x.Id)
    //let expectedIds = ids @ [for i in 0 .. (19 - ids.Length) do yield (newMax + 3 - i)]
    
    //let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId "" (Popularity None)

    //Assert.equal expectedIds (decks |> List.map (fun x -> x.Id))
    
    //// page 2
    //let expectedIds = [for i in 1 .. 20 do yield (expectedIds.Last() - i)]

    //let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId "" (Popularity (Some (decks |> List.last |> fun x -> x.Id, x.FollowCount)))
    
    //Assert.equal expectedIds (decks |> List.map (fun x -> x.Id))

    //// page 3
    //let expectedIds = [expectedIds.Last() - 1 .. -1 .. ids.Max() + 1]

    //let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId "" (Popularity (Some (decks |> List.last |> fun x -> x.Id, x.FollowCount)))
    
    //Assert.equal expectedIds (decks |> List.map (fun x -> x.Id))
    
    //// page 4
    //let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId "" (Popularity (Some (decks |> List.last |> fun x -> x.Id, x.FollowCount)))

    //Assert.Empty decks

    // keyset works with more followers
    use db = c.Db
    let! (followedDecks: DeckEntity ResizeArray) = db.Deck.OrderBy(fun x -> x.Id).ToListAsync()

    for i, followers in intsGen |> Gen.sample1Gen |> List.mapi (fun i x -> i, x) do
        followedDecks.[i].Followers <- followers
    do! db.SaveChangesAsyncI()
    let followedSorted = followedDecks |> Seq.sortByDescending (fun x -> x.Followers, x.Id) |> Seq.map (fun x -> x.Id) |> Seq.toList

    let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId "" (Popularity None)
    Assert.equal
        (followedSorted |> List.take 20)
        (decks |> List.map (fun x -> x.Id))

    let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId "" (Popularity (Some (decks |> List.last |> fun x -> x.Id, x.FollowCount)))
    Assert.equal
        (followedSorted |> List.skip 20 |> List.take 20)
        (decks |> List.map (fun x -> x.Id))
    
    let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId "" (Popularity (Some (decks |> List.last |> fun x -> x.Id, x.FollowCount)))
    Assert.equal
        (followedSorted |> List.skip 40)
        (decks |> List.map (fun x -> x.Id))

    let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId "" (Popularity (Some (decks |> List.last |> fun x -> x.Id, x.FollowCount)))
    Assert.Empty decks

    // keyset works with Relevance
    use db = c.Db
    let psuedoRank = String.split ' ' >> Array.filter (fun x -> x = nameX.Trim()) >> Array.length
    let followedSorted = followedDecks |> Seq.sortByDescending (fun x -> psuedoRank x.Name, x.Id) |> Seq.map (fun x -> x.Id) |> Seq.toList

    let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId nameX (Relevance None)
    Assert.equal
        (followedSorted |> List.take 20)
        (decks |> List.map (fun x -> x.Id))

    let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId nameX (Relevance (Some (decks |> List.last |> fun x -> x.Id, x.TsvRank)))
    Assert.equal
        (followedSorted |> List.skip 20 |> List.take 20)
        (decks |> List.map (fun x -> x.Id))
    
    let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId nameX (Relevance (Some (decks |> List.last |> fun x -> x.Id, x.TsvRank)))
    Assert.equal
        (followedSorted |> List.skip 40)
        (decks |> List.map (fun x -> x.Id))

    let! (decks: DeckWithFollowMeta list) = SanitizeDeckRepository.search conn userId nameX (Relevance (Some (decks |> List.last |> fun x -> x.Id, x.TsvRank)))
    Assert.Empty decks
    } |> TaskResult.getOk)

//[<Fact>]
let ``SanitizeDeckRepository.search works x50``(): Task<unit> = task {
    for _ in [1..50] do
        do! ``SanitizeDeckRepository.search works``()
    }

[<Fact>]
let ``SanitizeDeckRepository works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let withCount count deck =
        { deck with
            DueCount = count
            AllCount = count }
    let getTomorrow () =
        SanitizeDeckRepository.get c.Db userId <| DateTime.UtcNow + TimeSpan.FromDays 1.
    let getYesterday () =
        SanitizeDeckRepository.get c.Db userId <| DateTime.UtcNow - TimeSpan.FromDays 1.

    // get yields default deck
    let defaultDeckId = deck_3
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
    let newDeckId = Ulid.create

    do! SanitizeDeckRepository.create c.Db userId newDeckName newDeckId

    Assert.equal newDeckId <| c.Db.Deck.Single(fun x -> x.Name = newDeckName).Id

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
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    let stackId = stack_1
    let cardId = card_1
    let assertDeckId expectedDeckId = taskResult {
        let! (card: Card ResizeArray) = StackRepository.GetCollected c.Db userId stackId
        let card = card.Single()
        Assert.equal expectedDeckId card.DeckId
        Assert.equal cardId card.CardId
    }
    
    let! actualDecks = getTomorrow ()

    Assert.areEquivalent [ newDeck; defaultDeck |> withCount 1 ] actualDecks
    do! assertDeckId defaultDeckId

    // switching to new deck works
    do! SanitizeDeckRepository.switch c.Db userId newDeckId cardId
    
    let! actualDecks = getTomorrow ()
    
    Assert.areEquivalent [ newDeck |> withCount 1 ; defaultDeck] actualDecks
    do! assertDeckId newDeckId
    
    // switching is idempotent
    do! SanitizeDeckRepository.switch c.Db userId newDeckId cardId
    
    let! actualDecks = getTomorrow ()
    
    Assert.areEquivalent [ newDeck |> withCount 1; defaultDeck] actualDecks
    do! assertDeckId newDeckId
    
    // can switch back to default deck
    do! SanitizeDeckRepository.switch c.Db userId defaultDeckId cardId
    
    let! actualDecks = getTomorrow ()
    
    Assert.areEquivalent [ newDeck; defaultDeck |> withCount 1 ] actualDecks 
    do! assertDeckId defaultDeckId
    
    // can delete new deck
    do! SanitizeDeckRepository.delete c.Db userId newDeckId
    
    let! actualDecks = getTomorrow ()
    
    Assert.areEquivalent [ defaultDeck |> withCount 1 ] actualDecks 
    do! assertDeckId defaultDeckId

    // can add new deck with same name
    let newDeckId = Ulid.create
    do! SanitizeDeckRepository.create c.Db userId newDeckName newDeckId
    
    Assert.equal newDeckId <| c.Db.Deck.Single(fun x -> x.Name = newDeckName).Id

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
    
    let! (card: Card ResizeArray) = StackRepository.GetCollected c.Db userId stackId
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
    let! (x: Result<_,_>) = SanitizeDeckRepository.create c.Db userId newDeckName Ulid.create
    Assert.Equal(sprintf "User #%A already has a Deck named '%s'" userId newDeckName, x.error)
    
    let! (x: Result<_,_>) = SanitizeDeckRepository.rename c.Db userId newDeckId newDeckName
    Assert.Equal(sprintf "User #%A already has a Deck named '%s'" userId newDeckName, x.error)

    let invalidDeckName = Random.cryptographicString 251
    let! (x: Result<_,_>) = SanitizeDeckRepository.create c.Db userId invalidDeckName Ulid.create
    Assert.Equal(sprintf "Deck name '%s' is too long. It must be less than 250 characters." invalidDeckName, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.rename c.Db userId newDeckId invalidDeckName
    Assert.Equal(sprintf "Deck name '%s' is too long. It must be less than 250 characters." invalidDeckName, x.error)
    
    let invalidDeckId = newGuid
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId invalidDeckId cardId
    Assert.Equal(sprintf "Either Deck #%A doesn't belong to you or it doesn't exist" invalidDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.delete c.Db userId invalidDeckId
    Assert.Equal(sprintf "Either Deck #%A doesn't belong to you or it doesn't exist" invalidDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.setDefault c.Db userId invalidDeckId
    Assert.Equal(sprintf "Either Deck #%A doesn't belong to you or it doesn't exist" invalidDeckId, x.error)
    
    let! (x: Result<_,_>) = SanitizeDeckRepository.setIsPublic c.Db userId invalidDeckId true
    Assert.Equal(sprintf "Either Deck #%A doesn't belong to you or it doesn't exist" invalidDeckId, x.error)
    
    let invalidCardId = newGuid
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId newDeckId invalidCardId
    Assert.Equal(sprintf "Either Card #%A doesn't belong to you or it doesn't exist" invalidCardId, x.error)
    
    let nonauthor = user_1
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db nonauthor newDeckId cardId
    Assert.Equal(sprintf "Either Deck #%A doesn't belong to you or it doesn't exist" newDeckId, x.error)

    let! _ = FacetRepositoryTests.addBasicStack c.Db nonauthor [] (stack_1, branch_1, leaf_1, [card_1])
    let nonauthorCardId = user_2
    let! (x: Result<_,_>) = SanitizeDeckRepository.switch c.Db userId newDeckId nonauthorCardId
    Assert.Equal(sprintf "Either Card #%A doesn't belong to you or it doesn't exist" nonauthorCardId, x.error)
    
    let! (x: Result<_,_>) = SanitizeDeckRepository.delete c.Db nonauthor newDeckId
    Assert.Equal(sprintf "Either Deck #%A doesn't belong to you or it doesn't exist" newDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.setDefault c.Db nonauthor newDeckId
    Assert.Equal(sprintf "Either Deck #%A doesn't belong to you or it doesn't exist" newDeckId, x.error)

    let! (x: Result<_,_>) = SanitizeDeckRepository.setIsPublic c.Db nonauthor newDeckId true
    Assert.Equal(sprintf "Either Deck #%A doesn't belong to you or it doesn't exist" newDeckId, x.error)
    } |> TaskResult.getOk)

let getRealError = function
    | RealError e -> e
    | _ -> failwith "dude"
let getEditExistingIsNull_LeafIdsByDeckId = function
    | EditExistingIsNull_LeafIdsByDeckId e -> e
    | _ -> failwith "dude"

[<Fact>]
let ``SanitizeDeckRepository.follow works with "NoDeck true None"``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = user_3
    let authorDefaultDeckId = deck_3
    let followerId = user_1
    let followerDefaultDeckId = deck_1
    let otherDudeId = user_2
    let publicDeck =
        {   Id = deck_ 4
            Name = Guid.NewGuid().ToString()
            AuthorId = authorId
            AuthorName = "RoboTurtle"
            IsFollowed = false
            FollowCount = 0
            TsvRank = 0. }
    let theirDeck = { Id = publicDeck.Id
                      Name = publicDeck.Name }
    do! SanitizeDeckRepository.create c.Db authorId publicDeck.Name Ulid.create //|>%% Assert.equal publicDeck.Id
    let assertNotificationThenDelete expected = task {
        let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
        let n = ns.Results |> Assert.Single
        n.Created |> Assert.dateTimeEqual 60. DateTime.UtcNow
        n |> Assert.equal
            {   expected with
                    Created = n.Created // cheating, but whatever
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
    let! _ = addBasicStack c.Db authorId [] (stack_1, branch_1, leaf_1, [card_1])
    let notificationId = notification_1
    let stackId = stack_1
    let branchId = branch_1
    let authorCollectedId = card_1
    let leaf1 =
        {   StackId = stackId
            BranchId = branchId
            LeafId = leaf_1 }
    
    do! assertNotificationThenDelete
            {   Id = notificationId
                SenderId = authorId
                SenderDisplayName = "RoboTurtle"
                Created = DateTime.MinValue
                Message = DeckAddedStack { TheirDeck = theirDeck
                                           MyDeck = None
                                           New = leaf1
                                           Collected = None } }

    // *both* notifications deleted
    Assert.Empty c.Db.ReceivedNotification
    Assert.Empty c.Db.Notification
    
    // can remove notification, idempotent
    do! NotificationRepository.remove c.Db followerId notificationId

    Assert.Empty c.Db.ReceivedNotification
    Assert.Empty c.Db.Notification

    // edit card notifies follower
    let leaf2 = { leaf1 with LeafId = leaf_2 }
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeStackRepository.getUpsert c.Db (VUpdate_BranchId branchId) ids_1
    let updated = {
        old with
            ViewEditStackCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db authorId [] [ Ulid.create ] updated
    Assert.Equal(branchId, actualBranchId)
    do! assertNotificationThenDelete
            { Id = notification_2
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              Created = DateTime.MinValue
              Message = DeckUpdatedStack { TheirDeck = theirDeck
                                           MyDeck = None
                                           New = leaf2
                                           Collected = None } }

    // editing card's state doesn't notify follower
    do! StackRepository.editState c.Db authorId authorCollectedId Suspended
    
    let! (ns: _ PagedList) = NotificationRepository.get c.Db followerId 1
    
    ns.Results |> Assert.Empty

    // Update notifies with follower's collected card
    do! StackRepository.CollectCard c.Db followerId leaf2.LeafId [ Ulid.create ]
    let leaf3 = { leaf2 with LeafId = leaf_3 }
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeStackRepository.getUpsert c.Db (VUpdate_BranchId branchId) ids_1
    let updated = {
        old with
            ViewEditStackCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db authorId [] [ Ulid.create ] updated
    Assert.Equal(branchId, actualBranchId)
    let collected =
        {   StackId = leaf2.StackId
            BranchId = leaf2.BranchId
            LeafId = leaf2.LeafId } |> Some
    do! assertNotificationThenDelete
            { Id = notification_3
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              Created = DateTime.MinValue
              Message = DeckUpdatedStack { TheirDeck = theirDeck
                                           MyDeck = None
                                           New = leaf3
                                           Collected = collected } }

    // changing to private deck has notification
    do! SanitizeDeckRepository.switch c.Db authorId authorDefaultDeckId authorCollectedId

    do! assertNotificationThenDelete
            { Id = notification_ 4
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              Created = DateTime.MinValue
              Message = DeckDeletedStack { TheirDeck = theirDeck
                                           MyDeck = None
                                           Collected = collected
                                           Deleted = leaf3 } }

    // changing back to public deck has notification
    do! SanitizeDeckRepository.switch c.Db authorId publicDeck.Id authorCollectedId

    do! assertNotificationThenDelete
            { Id = notification_ 5
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              Created = DateTime.MinValue
              Message = DeckAddedStack { TheirDeck = theirDeck
                                         MyDeck = None
                                         New = leaf3
                                         Collected = collected } }

    // changing to another public deck that's also followed generates 2 notifications
    do! SanitizeDeckRepository.setIsPublic c.Db authorId authorDefaultDeckId true
    do! follow authorDefaultDeckId

    do! SanitizeDeckRepository.switch c.Db authorId authorDefaultDeckId authorCollectedId

    let! (ns: Notification PagedList) = NotificationRepository.get c.Db followerId 1
    let a = ns.Results.ToList().[0]
    let b = ns.Results.ToList().[1]
    a.Created |> Assert.dateTimeEqual 60. DateTime.UtcNow
    b.Created |> Assert.dateTimeEqual 60. DateTime.UtcNow
    a |> Assert.equal
        { Id = notification_ 6
          SenderId = user_3
          SenderDisplayName = "RoboTurtle"
          Created = a.Created
          Message = DeckAddedStack { TheirDeck =
                                        { Id = authorDefaultDeckId
                                          Name = "Default Deck" }
                                     MyDeck = None
                                     New = leaf3
                                     Collected = collected } }
    b |> Assert.equal
        { Id = notification_ 7
          SenderId = user_3
          SenderDisplayName = "RoboTurtle"
          Created = b.Created
          Message = DeckDeletedStack
                     { TheirDeck = theirDeck
                       MyDeck = None
                       Collected = collected
                       Deleted = leaf3 } }
    
    // back to public deck and some cleanup
    do! NotificationRepository.remove c.Db followerId a.Id
    do! NotificationRepository.remove c.Db followerId b.Id
    do! SanitizeDeckRepository.switch c.Db authorId publicDeck.Id authorCollectedId
    do! NotificationRepository.remove c.Db followerId 8
    do! NotificationRepository.remove c.Db followerId 9
    do! SanitizeDeckRepository.setIsPublic c.Db authorId authorDefaultDeckId false

    // deleting card from deck has notification
    do! StackRepository.uncollectStack c.Db authorId stackId

    do! assertNotificationThenDelete
            { Id = notification_ 10
              SenderId = authorId
              SenderDisplayName = "RoboTurtle"
              Created = DateTime.MinValue
              Message = DeckDeletedStack { TheirDeck = theirDeck
                                           MyDeck = None
                                           Collected = collected
                                           Deleted = leaf3 } }

    // diff says a stack was removed
    do! SanitizeDeckRepository.diff c.Db followerId publicDeck.Id followerId

    |>%% Assert.equal
        {   emptyDiffStateSummary with
                RemovedStack =
                    [ { StackId = stackId
                        BranchId = branchId
                        LeafId = leaf2.LeafId
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
    do! SanitizeDeckRepository.unfollow c.Db followerId newGuid
        |> TaskResult.getError
        |>% Assert.equal "Either the deck doesn't exist or you are not following it."

    // second follow fails
    do! follow publicDeck.Id
    do! follow publicDeck.Id
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal (sprintf "You're already following Deck #%A" publicDeck.Id)

    // nonexistant deck fails
    do! follow newGuid
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal "Either Deck #1337 doesn't exist or it isn't public."

    // private deck fails
    do! follow authorDefaultDeckId
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal "Either Deck #3 doesn't exist or it isn't public."
    
    // someone else following the deck bumps count to 2
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
    let authorId = user_3
    let publicDeckId = deck_3
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeckId true
    do! FacetRepositoryTests.addBasicStack c.Db authorId [] (stack_1, branch_1, leaf_1, [card_1])
    let stackId = stack_1
    let branchId = branch_1
    let leafId = leaf_1
    let followerId = user_1
    let followerDeckId = deck_1
    let newFollowerDeckId = Ulid.create
    do! SanitizeDeckRepository.create c.Db followerId (Guid.NewGuid().ToString()) newFollowerDeckId
    let follow oldDeckId editExisting = SanitizeDeckRepository.follow c.Db followerId publicDeckId (OldDeck oldDeckId) false editExisting // mind the test name

    // follow targeting newFollowerDeckId with extant card in default deck fails
    do! StackRepository.CollectCard c.Db followerId leafId [ Ulid.create ]

    do! follow newFollowerDeckId None
        |> TaskResult.getError
        |>% getEditExistingIsNull_LeafIdsByDeckId
        |>% Assert.Single
        |>% Assert.equal (followerDeckId, ResizeArray.singleton leafId)

    // follow targeting default deck with extant card in default deck works
    do! follow followerDeckId None
        |> TaskResult.getOk
    
    // follow with someone else's deckId fails
    do! StackRepository.uncollectStack c.Db followerId stackId
    do! follow deck_2 None
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal "Either Deck #2 doesn't exist or it doesn't belong to you."
    
    // follow with "OldDeck false None" works
    
    do! follow followerDeckId None |> TaskResult.getOk
    
    let! cc =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { CardId = card_3
          UserId = followerId
          StackId = stackId
          BranchId = branchId
          LeafMeta = cc.LeafMeta // untested
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
            (VUpdate_BranchId branchId) id branchId
    let newLeafId = leaf_2
    
    do! follow followerDeckId (Some false) |> TaskResult.getOk

    let! ac2 =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { cc with
            LeafMeta = ac2.LeafMeta // untested
        }   // unchanged
        ac2
    Assert.equal
        leafId
        ac2.LeafMeta.Id
    
    // follow with "editExisting true" after update, updates
    do! follow followerDeckId (Some true) |> TaskResult.getOk

    let! ac3 =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { cc with
            LeafMeta = ac3.LeafMeta // untested
        }   // unchanged
        ac3
    Assert.equal
        newLeafId
        ac3.LeafMeta.Id
    } |> TaskResult.getOk)

[<Fact>]
let ``SanitizeDeckRepository.follow works with "OldDeck false None" pair``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = user_3
    let publicDeckId = deck_3
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeckId true
    do! FacetRepositoryTests.addReversedBasicStack c.Db authorId [] (stack_1, branch_1, leaf_1, [card_1])
    let ccId1 = card_1
    let stackId = stack_1
    let branchId = branch_1
    let followerId = user_1
    let followerDeckId = deck_1
    let follow oldDeckId editExisting = SanitizeDeckRepository.follow c.Db followerId publicDeckId (OldDeck oldDeckId) false editExisting // mind the test name

    // follow with "OldDeck false None" and both of a pair works
    do! follow followerDeckId None |> TaskResult.getOk
    
    let! (ccs: Card ResizeArray) =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% (Seq.sortBy (fun x -> x.Index) >> ResizeArray)
    Assert.equal 2 ccs.Count
    let a, b = ccs.[0], ccs.[1]
    Assert.equal
        { CardId = card_3
          UserId = followerId
          StackId = stackId
          BranchId = branch_1
          LeafMeta = a.LeafMeta // untested
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
            CardId = card_ 4
            LeafMeta = b.LeafMeta // untested
            Index = 1s }
        b
    
    // follow with "OldDeck false None" and one of a pair works
    do! StackRepository.uncollectStack c.Db followerId stackId
    let newDeckId = Ulid.create
    do! SanitizeDeckRepository.create c.Db authorId (Guid.NewGuid().ToString()) newDeckId
    do! SanitizeDeckRepository.switch c.Db authorId newDeckId ccId1
    
    do! follow followerDeckId None |> TaskResult.getOk
    
    let! (cc: Card) =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { b with
            LeafMeta = cc.LeafMeta // untested
            Due = cc.Due // untested
            CardId = card_ 5 }
        cc
    
    // follow with "editExisting false" after update, doesn't update
    do! FacetRepositoryTests.update c authorId
            (VUpdate_BranchId branchId) id branchId
    
    do! follow followerDeckId (Some false) |> TaskResult.getOk
    
    let! (cc: Card) =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { b with
            LeafMeta = cc.LeafMeta // untested
            Due = cc.Due // untested
            CardId = card_ 5 }
        cc
    
    // follow with "editExisting true" after update, updates
    do! StackRepository.uncollectStack c.Db followerId stackId
    do! SanitizeDeckRepository.switch c.Db authorId publicDeckId ccId1

    do! follow followerDeckId (Some true) |> TaskResult.getOk

    let! (ccs: Card ResizeArray) =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% (Seq.sortBy (fun x -> x.Index) >> ResizeArray)
    Assert.equal 2 ccs.Count
    let a, b = ccs.[0], ccs.[1]
    Assert.equal
        { CardId = card_ 7
          UserId = followerId
          StackId = stackId
          BranchId = branch_1
          LeafMeta = a.LeafMeta // untested
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
            CardId = card_ 6
            LeafMeta = b.LeafMeta // untested
            Index = 1s }
        b
    } |> TaskResult.getOk)

[<Fact>]
let ``SanitizeDeckRepository.follow works with "NewDeck false *"``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = user_3
    let publicDeckId = deck_3
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeckId true
    do! FacetRepositoryTests.addBasicStack c.Db authorId [] (stack_1, branch_1, leaf_1, [card_1])
    let stackId = stack_1
    let branchId = branch_1
    let leafId = leaf_1
    let followerId = user_1
    let followerDeckId = deck_1
    let follow deckName editExisting = SanitizeDeckRepository.follow c.Db followerId publicDeckId (NewDeck (Ulid.create, deckName)) false editExisting // mind the test name

    // follow with extant card fails and doesn't add a deck
    Assert.equal 3 <| c.Db.Deck.Count()
    do! StackRepository.CollectCard c.Db followerId leafId [ Ulid.create ]

    do! follow (Guid.NewGuid().ToString()) None
        
        |> TaskResult.getError
        |>% getEditExistingIsNull_LeafIdsByDeckId
        |>% Assert.Single
        |>% Assert.equal (followerDeckId, ResizeArray.singleton leafId)
    Assert.equal 3 <| c.Db.Deck.Count()

    // follow with huge name fails
    do! StackRepository.uncollectStack c.Db followerId stackId
    let longDeckName = Random.cryptographicString 251
    do! follow longDeckName None
        |> TaskResult.getError
        |>% getRealError
        |>% Assert.equal (sprintf "Deck name '%s' is too long. It must be less than 250 characters." longDeckName)
    
    // follow with "NewDeck false None" works
    let newDeckId = deck_ 4
    
    do! follow (Guid.NewGuid().ToString()) None |> TaskResult.getOk
    
    let! cc =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { CardId = card_3
          UserId = followerId
          StackId = stackId
          BranchId = branchId
          LeafMeta = cc.LeafMeta // untested
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
            (VUpdate_BranchId branchId) id branchId
    let newLeafId = leaf_2
    
    do! follow (Guid.NewGuid().ToString()) (Some false) |> TaskResult.getOk

    let! ac2 =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { cc with
            LeafMeta = ac2.LeafMeta // untested
        }   // unchanged
        ac2
    Assert.equal
        leafId
        ac2.LeafMeta.Id
    
    // follow with "editExisting true" after update, updates
    do! follow (Guid.NewGuid().ToString()) (Some true) |> TaskResult.getOk
    let newestDeckId = deck_ 6

    let! ac3 =
        StackRepository.GetCollected c.Db followerId stackId
        |>%% Assert.Single
    Assert.equal
        { cc with
            DeckId = newestDeckId
            LeafMeta = ac3.LeafMeta // untested
        }   // unchanged
        ac3
    Assert.equal
        newLeafId
        ac3.LeafMeta.Id
    } |> TaskResult.getOk)

[<Fact>]
let ``SanitizeDeckRepository.diff works``(): Task<unit> = (taskResult {
    let authorId = user_3
    let publicDeckId = deck_3
    use c = new TestContainer()
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeckId true
    do! FacetRepositoryTests.addBasicStack c.Db authorId [] (stack_1, branch_1, leaf_1, [card_1])
    let stackId = stack_1
    let branchId = branch_1
    let leafId = leaf_1
    let followerId = user_1
    let followerDeckId = deck_1
    let standardIds =
        { StackId = stackId
          BranchId = branchId
          LeafId = leafId
          Index = 0s
          DeckId = followerDeckId }

    // diffing two decks with the same card yields Unchanged
    do! StackRepository.CollectCard c.Db followerId leafId [ Ulid.create ]
    
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
    let nonpublicDeckId = deck_2
    do! SanitizeDeckRepository.diff c.Db followerId nonpublicDeckId followerDeckId
    
    |>% Result.getError
    |>% Assert.equal "Either Deck #2 doesn't exist, or it isn't public, or you don't own it."

    // diffing with a deck that doesn't exist fails
    let nonexistantDeckId = newGuid
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
    let newDeckId = Ulid.create
    do! SanitizeDeckRepository.create c.Db followerId (Guid.NewGuid().ToString()) newDeckId
    let! (ccs: Card ResizeArray) = StackRepository.GetCollected c.Db followerId stackId
    let ccId = ccs.Single().CardId
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
    let! actualBranchId = FacetRepositoryTests.addCloze "{{c1::Portland::city}} was founded in {{c2::1845}}." c.Db authorId [] (stack_1, branch_1, leaf_1, [card_1])
    let! (ccs: CardEntity ResizeArray) = c.Db.Card.Where(fun x -> x.BranchId = actualBranchId).ToListAsync()
    do! StackRepository.CollectCard c.Db followerId (ccs.First().LeafId) [ Ulid.create ]
    let ids =
        {   StackId = stack_2
            BranchId = actualBranchId
            LeafId = leaf_2
            Index = 0s
            DeckId = followerDeckId }

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                Unchanged =
                    [ ids
                      { ids with Index = 1s } ] }

    // two clozes, but different decks, index 1
    let! (ccs: CardEntity list) =
        c.Db.Card
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
let ``SanitizeDeckRepository.diff works on Branch(Leaf)Changed and deckchanges``(): Task<unit> = (taskResult {
    let authorId = user_3
    let publicDeckId = deck_3
    use c = new TestContainer()
    do! SanitizeDeckRepository.setIsPublic c.Db authorId publicDeckId true
    do! FacetRepositoryTests.addBasicStack c.Db authorId [] (stack_1, branch_1, leaf_1, [card_1])
    let stackId = stack_1
    let branchId = branch_1
    let leafId = leaf_1
    let followerId = user_1
    let followerDeckId = deck_1
    let standardIds =
        { StackId = stackId
          BranchId = branchId
          LeafId = leafId
          Index = 0s
          DeckId = followerDeckId }

    // diffing two decks with the same card yields Unchanged
    do! StackRepository.CollectCard c.Db followerId leafId [ Ulid.create ]
    
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                Unchanged = [ standardIds ] }

    // author switches to new branch
    let! stackCommand = SanitizeStackRepository.getUpsert c.Db (VNewBranch_SourceStackId standardIds.StackId) ids_1
    do! SanitizeStackRepository.Update c.Db authorId [] [ Ulid.create ] stackCommand
    let newBranchIds =
        { standardIds with
              BranchId = branch_2
              LeafId = leaf_2 }

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                BranchChanged = [ ({ newBranchIds with DeckId = publicDeckId }, standardIds) ] }

    // author switches to new branch, and follower's old card is in different deck
    let newFollowerDeckId = Ulid.create
    do! SanitizeDeckRepository.create c.Db followerId (Guid.NewGuid().ToString()) newFollowerDeckId
    let! (cc: CardEntity) = c.Db.Card.SingleAsync(fun x -> x.StackId = stackId && x.UserId = followerId)
    do! SanitizeDeckRepository.switch c.Db followerId newFollowerDeckId cc.Id
    
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                BranchChanged = 
                    [ ({ newBranchIds with DeckId = publicDeckId },
                       { standardIds with DeckId = newFollowerDeckId }) ] }

    do! SanitizeDeckRepository.switch c.Db followerId followerDeckId cc.Id
    do! StackRepository.CollectCard c.Db followerId newBranchIds.LeafId [ Ulid.create ]
    // author switches to new leaf
    let! stackCommand = SanitizeStackRepository.getUpsert c.Db (VUpdate_BranchId newBranchIds.BranchId) ids_1
    do! SanitizeStackRepository.Update c.Db authorId [] [ Ulid.create ] stackCommand
    let newLeafIds =
        { newBranchIds with
              LeafId = leaf_3 }

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                LeafChanged = [ ({ newLeafIds with DeckId = publicDeckId }, newBranchIds) ] }

    // author switches to new leaf, and follower's old card is in different deck
    do! SanitizeDeckRepository.switch c.Db followerId newFollowerDeckId cc.Id
    
    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                LeafChanged =
                    [ ({ newLeafIds with DeckId = publicDeckId },
                       { newBranchIds with DeckId = newFollowerDeckId }) ] }

    do! SanitizeDeckRepository.switch c.Db followerId followerDeckId cc.Id
    // author on new branch with new leaf, follower on old branch & leaf
    do! StackRepository.CollectCard c.Db followerId standardIds.LeafId [ Ulid.create ]

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                BranchChanged = [ ({ newLeafIds with DeckId = publicDeckId }, standardIds) ] }

    // author on new branch with new leaf, follower on old branch & leaf and different deck
    do! SanitizeDeckRepository.switch c.Db followerId newFollowerDeckId cc.Id

    do! SanitizeDeckRepository.diff c.Db followerId publicDeckId followerDeckId
    
    |>%% Assert.equal
        {   emptyDiffStateSummary with
                BranchChanged =
                    [ ({ newLeafIds with DeckId = publicDeckId },
                       { standardIds with DeckId = newFollowerDeckId }) ] }
    } |> TaskResult.getOk)

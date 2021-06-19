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
open NodaTime

let emptyDiffStateSummary =
    {   Unchanged = []
        RevisionChanged = []
        ExampleChanged = []
        AddedConcept = []
        RemovedConcept = []
        MoveToAnotherDeck = []
    }

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

[<Fact(Skip=PgSkip.reason)>]
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

let getRealError = function
    | RealError e -> e
    | _ -> failwith "dude"
let getEditExistingIsNull_RevisionIdsByDeckId = function
    | EditExistingIsNull_RevisionIdsByDeckId e -> e
    | _ -> failwith "dude"

module ConceptRepositoryTests

open CardOverflow.Pure.Core
open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Api
open ContainerExtensions
open LoadersAndCopiers
open Helpers
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Test
open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Quotations
open System.Linq
open Xunit
open System
open SimpleInjector
open SimpleInjector.Lifestyles
open System.Diagnostics
open FSharp.Control.Tasks
open System.Threading.Tasks

[<Fact>]
let ``Getting 10 pages of GetAcquiredConceptsAsync takes less than 1 minute``() =
    use c = new Container()
    c.RegisterStuff
    c.RegisterStandardConnectionString
    use __ = AsyncScopedLifestyle.BeginScope c
    let db = c.GetInstance<CardOverflowDb>()
    let userId = 3

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        (CardRepository.GetAcquiredPages db userId i)
            .GetAwaiter()
            .GetResult()
            .Results
            .ToList()
            |> ignore
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)

[<Fact>]
let ``GetForUser isn't empty``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    FacetRepositoryTests.addBasicCard c.Db userId ["a"; "b"]
    do! CommentCardEntity (
            CardId = 1,
            UserId = userId,
            Text = "text",
            Created = DateTime.UtcNow
        ) |> CommentRepository.addAndSaveAsync c.Db
    let cardId = 1
        
    let! card = CardRepository.Get c.Db cardId userId
        
    let front, _, _, _ = card.LatestInstance.FrontBackFrontSynthBackSynth
    Assert.DoesNotContain("{{Front}}", front)
    Assert.NotEmpty <| card.Comments
    Assert.True card.LatestInstance.IsAcquired
    Assert.Equal(
        [{  Name = "a"
            Count = 1
            IsAcquired = true }
         {  Name = "b"
            Count = 1
            IsAcquired = true }],
        card.Tags
    )}

[<Fact>]
let ``Getting 10 pages of GetAsync takes less than 1 minute, and has users``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    FacetRepositoryTests.addBasicCard c.Db userId ["a"; "b"]

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        (CardRepository.GetAsync c.Db userId i)
            .GetAwaiter()
            .GetResult()
            .Results
            .ToList()
            |> ignore
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)
    
    let! cards = CardRepository.GetAsync c.Db userId 1
    Assert.Equal(1, cards.Results.Single().Users)
    Assert.Equal(
        [{  Name = "a"
            Count = 1
            IsAcquired = true }
         {  Name = "b"
            Count = 1
            IsAcquired = true }],
        cards.Results.Single().Tags
    )}

let testGetAcquired (cardIds: int list) addCards name = task {
    use c = new TestContainer(name)
    
    let userId = 3 // creates the card
    addCards |> Seq.iter (fun addCard -> addCard c.Db userId ["a"])
    let! acquiredCards = CardRepository.GetAcquiredPages c.Db userId 1
    let! card = CardRepository.GetAcquired c.Db userId 1
    Assert.Equal(
        cardIds.Count(),
        acquiredCards.Results.Count()
    )
    Assert.Equal(
        userId,
        card |> Result.getOk |> fun x -> x.UserId
    )

    let userId = 1 // acquires the card
    do! CardRepository.AcquireCardsAsync c.Db userId [1]
    let! card = CardRepository.Get c.Db 1 userId
    Assert.Equal(
        [{  Name = "a"
            Count = 1
            IsAcquired = false }],
        card.Tags
    )

    let userId = 2 // never acquires the card
    let! cards = CardRepository.GetAsync c.Db userId 1
    Assert.Equal(
        cardIds.Count(),
        cards.Results.Count()
    )
    Assert.Equal(
        [{  Name = "a"
            Count = 1
            IsAcquired = false }],
        cards.Results.SelectMany(fun x -> x.Tags).Distinct()
    )
    //Assert.Equal<string seq>( // medTODO uncomment when tags work
    //    ["a"],
    //    results.Results.SelectMany(fun x -> x.AcquiredCards.SelectMany(fun x -> x.Cards.SelectMany(fun x -> x.Tags)))
    //)
    }
    
[<Fact>]
let rec ``GetAcquired works when acquiring 1 basic card``(): Task<unit> =
    testGetAcquired
        [1]
        [ FacetRepositoryTests.addBasicCard ]
        <| nameof <@ ``GetAcquired works when acquiring 1 basic card`` @>

[<Fact>]
let rec ``GetAcquired works when acquiring 1 card of a pair``(): Task<unit> = 
    testGetAcquired
        [1]
        [ FacetRepositoryTests.addReversedBasicCard ]
        <| nameof <@ ``GetAcquired works when acquiring 1 card of a pair`` @>

[<Fact>]
let rec ``GetAcquired works when acquiring 2 cards of a pair``(): Task<unit> =
    testGetAcquired
        [1; 2]
        [ FacetRepositoryTests.addBasicCard; FacetRepositoryTests.addReversedBasicCard ]
        <| nameof <@ ``GetAcquired works when acquiring 2 cards of a pair`` @>

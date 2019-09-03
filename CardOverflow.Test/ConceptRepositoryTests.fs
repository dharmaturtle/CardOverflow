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
        (ConceptRepository.GetAcquiredPages db userId i)
            .GetAwaiter()
            .GetResult()
            .Results
            .ToList()
            |> ignore
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)

[<Fact>]
let ``Get isn't empty``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    FacetRepositoryTests.addBasicConcept c.Db userId []
    do! CommentFacetEntity (
            FacetId = 1,
            UserId = userId,
            Text = "text",
            Created = DateTime.UtcNow
        ) |> CommentRepository.addAndSaveAsync c.Db
    let conceptId = 1
        
    let! concept = ConceptRepository.Get c.Db conceptId 0
        
    concept.Facets
    |> Seq.collect (fun x -> x.LatestInstance.Cards.Select(fun x -> x.Front))
    |> Seq.iter (fun x -> Assert.DoesNotContain("{{Front}}", x))
    Assert.NotEmpty <| concept.Facets
    Assert.NotEmpty <| concept.Facets.Select(fun x -> x.Comments) }

[<Fact>]
let ``GetForUser isn't empty``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    FacetRepositoryTests.addBasicConcept c.Db userId []
    do! CommentFacetEntity (
            FacetId = 1,
            UserId = userId,
            Text = "text",
            Created = DateTime.UtcNow
        ) |> CommentRepository.addAndSaveAsync c.Db
    let conceptId = 1
        
    let! concept = ConceptRepository.Get c.Db conceptId userId
        
    concept.Facets
    |> Seq.collect (fun x -> x.LatestInstance.Cards.Select(fun x -> x.Front))
    |> Seq.iter (fun x -> Assert.DoesNotContain("{{Front}}", x))
    Assert.NotEmpty <| concept.Facets
    Assert.NotEmpty <| concept.Facets.Select(fun x -> x.Comments)
    Assert.NotEmpty <| 
        concept.Facets.SelectMany(fun x -> x.LatestInstance.Cards.Select(fun x -> x.IsAcquired))
    Assert.All(
        concept.Facets.SelectMany(fun x -> x.LatestInstance.Cards.Select(fun x -> x.IsAcquired)),
        fun x -> Assert.True(x))
    }

[<Fact>]
let ``Getting 10 pages of GetAsync takes less than 1 minute``() =
    use c = new Container()
    c.RegisterStuff
    c.RegisterStandardConnectionString
    use __ = AsyncScopedLifestyle.BeginScope c
    let db = c.GetInstance<CardOverflowDb>()
    let userId = 3

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        (ConceptRepository.GetAsync db userId i)
            .GetAwaiter()
            .GetResult()
            .Results
            .ToList()
            |> ignore
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)

let testGetAcquiredAsync cardIds addConcept name = task {
    use c = new TestContainer(name)
    let userId = 2
    addConcept c.Db 3 []
    do! CardRepository.AcquireCardsAsync c.Db userId cardIds

    let! results = ConceptRepository.GetAcquiredPages c.Db userId 1
    
    Assert.Equal(
        cardIds.Count(),
        results.Results.SelectMany(fun x -> x.AcquiredFacets.SelectMany(fun x -> x.Cards)).Count()
    )
    //Assert.Equal<string seq>( // medTODO uncomment when tags work
    //    ["a"],
    //    results.Results.SelectMany(fun x -> x.AcquiredFacets.SelectMany(fun x -> x.Cards.SelectMany(fun x -> x.Tags)))
    //)
    }
    
[<Fact>]
let rec ``GetAcquiredAsync works when acquiring 1 basic card``(): Task<unit> =
    testGetAcquiredAsync
        [1]
        FacetRepositoryTests.addBasicConcept
        <| nameof <@ ``GetAcquiredAsync works when acquiring 1 basic card`` @>

[<Fact>]
let rec ``GetAcquiredAsync works when acquiring 1 card of a pair``(): Task<unit> = 
    testGetAcquiredAsync
        [1]
        FacetRepositoryTests.addBasicAndReversedConcept
        <| nameof <@ ``GetAcquiredAsync works when acquiring 1 card of a pair`` @>

[<Fact>]
let rec ``GetAcquiredAsync works when acquiring 2 cards of a pair``(): Task<unit> =
    testGetAcquiredAsync
        [1; 2]
        FacetRepositoryTests.addBasicAndReversedConcept
        <| nameof <@ ``GetAcquiredAsync works when acquiring 2 cards of a pair`` @>

module ConceptRepositoryTests

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
        (ConceptRepository.GetAcquiredConceptsAsync db userId i)
            .GetAwaiter()
            .GetResult()
            .Results
            .ToList()
            |> ignore
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)

[<Fact>]
let ``Get isn't empty``(): unit =
    use c = new Container()
    c.RegisterStuff
    c.RegisterStandardConnectionString
    use __ = AsyncScopedLifestyle.BeginScope c
    let conceptId = 1
    (task {
        let db = c.GetInstance<CardOverflowDb>()
        
        let! concept = ConceptRepository.Get db conceptId
        
        concept.Facets
        |> Seq.collect (fun x -> x.LatestInstance.Cards.Select(fun x -> x.Front))
        |> Seq.iter (fun x -> Assert.DoesNotContain("{{Front}}", x))
        Assert.NotEmpty concept.Facets
        return ()
    }).GetAwaiter().GetResult()

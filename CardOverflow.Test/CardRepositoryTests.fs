module CardRepositoryTests

open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Debug
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open CardOverflow.Test
open System
open System.Linq
open Xunit
open CardOverflow.Pure
open System.Collections.Generic
open FSharp.Control.Tasks
open System.Threading.Tasks

[<Fact>]
let ``AcquireCards works``() = task {
    use c = new TestContainer()
    let maintainerId = 3
    FacetRepositoryTests.addBasicConcept c.Db maintainerId []
    let cardIds = [1]
    let userId = 2

    do! CardRepository.AcquireCardsAsync c.Db userId cardIds

    Assert.SingleI
        <| c.Db.AcquiredCard.Where(fun x -> x.UserId = userId && x.CardId = cardIds.Head)
    }

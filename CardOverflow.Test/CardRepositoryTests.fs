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
open CardOverflow.Pure

[<Fact>]
let ``AcquireCards works``() = task {
    use c = new TestContainer()
    
    let maintainerId = 3
    
    let c1 = 1
    let ci1_1 = 1
    FacetRepositoryTests.addBasicCard c.Db maintainerId []
    Assert.Equal(1, c.Db.Card.Single().Users)
    Assert.Equal(1, c.Db.CardInstance.Single().Users)
    Assert.Equal(1, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(1, c.Db.CardInstance.Single(fun x -> x.Id = ci1_1).Users)
    
    let c2 = 2
    let ci2_1 = 2
    FacetRepositoryTests.addReversedBasicCard c.Db maintainerId []
    Assert.Equal(1, c.Db.Card.Single(fun x -> x.Id = c2).Users)
    Assert.Equal(1, c.Db.CardInstance.Single(fun x -> x.Id = ci2_1).Users)
    
    let acquirerId = 1
    do! CardRepository.AcquireCardAsync c.Db acquirerId ci1_1
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(2, c.Db.CardInstance.Single(fun x -> x.Id = ci1_1).Users)
    do! CardRepository.AcquireCardAsync c.Db acquirerId ci2_1
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c2).Users)
    Assert.Equal(2, c.Db.CardInstance.Single(fun x -> x.Id = ci2_1).Users)
    // misc
    Assert.Equal(2, c.Db.CardInstance.Count())
    Assert.Equal(4, c.Db.AcquiredCard.Count())
    Assert.Equal(2, c.Db.AcquiredCard.Count(fun x -> x.CardInstanceId = ci1_1));

    let! ac = CardRepository.GetAcquired c.Db maintainerId c1
    let ac = ac |> Result.getOk
    let ac = { ac with CardInstance = { ac.CardInstance with FieldValues = [].ToList() } }
    do! CardRepository.UpdateFieldsToNewInstance c.Db ac
    let ci1_2 = 3
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(1, c.Db.CardInstance.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.CardInstance.Count())
    Assert.Equal(4, c.Db.AcquiredCard.Count())
    Assert.Equal(1, c.Db.AcquiredCard.Count(fun x -> x.CardInstanceId = ci1_2))
    
    do! CardRepository.AcquireCardAsync c.Db acquirerId ci1_2
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(2, c.Db.CardInstance.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.CardInstance.Count())
    Assert.Equal(4, c.Db.AcquiredCard.Count())
    Assert.Equal(2, c.Db.AcquiredCard.Count(fun x -> x.CardInstanceId = ci1_2));
    }

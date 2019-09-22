module TagRepositoryTests

open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open CardOverflow.Api
open CardOverflow.Test
open System
open System.Linq
open Xunit
open System.Collections.Generic

[<Fact>]
let ``TagRepository can add a new tag``() =
    use c = new TestContainer()
    let tagName = Guid.NewGuid().ToString()

    tagName |> List.singleton |> TagRepository.Add c.Db 1

    Assert.Single(c.Db.Tag.Where(fun x -> x.Name = tagName).ToList())

[<Fact>]
let ``When TagRepository adds a tag twice, only one is added``() =
    use c = new TestContainer()
    let tagName = Guid.NewGuid().ToString()

    tagName |> List.singleton |> TagRepository.Add c.Db 1
    tagName |> List.singleton |> TagRepository.Add c.Db 1

    Assert.Single(c.Db.Tag.Where(fun x -> x.Name = tagName).ToList())

[<Fact>]
let ``TagRepository AddTo works``() =
    use c = new TestContainer()
    let userId = 3
    FacetRepositoryTests.addBasicCard c.Db userId []
    let cardId = 1
    let tagName = Guid.NewGuid().ToString()

    TagRepository.AddTo c.Db userId tagName cardId
    //TagRepository.AddTo c.Db userId tagName cardId medTODO uncomment and fix; make it idempotent

    Assert.SingleI(c.Db.Tag.Where(fun x -> x.Name = tagName).ToList())
    Assert.Equal(
        tagName,
        c.Db.AcquiredCard
            .Include(fun x -> x.Tag_AcquiredCards :> IEnumerable<_>)
                .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
            .Single(fun x -> x.Id = 1)
            .Tag_AcquiredCards
            .Single(fun x -> x.Tag.Name = tagName).Tag.Name
        )

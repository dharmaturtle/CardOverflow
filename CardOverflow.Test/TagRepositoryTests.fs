module TagRepositoryTests

open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open CardOverflow.Api
open CardOverflow.Test
open System
open System.Linq
open Xunit
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Control.Tasks

[<Fact>]
let ``TagRepository can add a new tag`` (): unit =
    use c = new TestContainer()
    let tagName = Guid.NewGuid().ToString()

    tagName |> List.singleton |> TagRepository.Add c.Db 1

    Assert.SingleI(c.Db.Tag.Where(fun x -> x.Name = tagName).ToList())

[<Fact>]
let ``When TagRepository adds a tag twice, only one is added`` (): unit =
    use c = new TestContainer()
    let tagName = Guid.NewGuid().ToString()

    tagName |> List.singleton |> TagRepository.Add c.Db 1
    tagName |> List.singleton |> TagRepository.Add c.Db 1

    Assert.SingleI(c.Db.Tag.Where(fun x -> x.Name = tagName).ToList())

[<Fact>]
let ``TagRepository AddTo/DeleteFrom works``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId []
    let cardId = 1
    let tagName = Guid.NewGuid().ToString()

    do! TagRepository.AddTo c.Db tagName cardId
    //TagRepository.AddTo c.Db userId tagName cardId medTODO uncomment and fix; make it idempotent

    Assert.SingleI(c.Db.Tag.Where(fun x -> x.Name = tagName).ToList())
    let joinTable () =
        c.Db.AcquiredCard
            .Include(fun x -> x.Tag_AcquiredCards :> IEnumerable<_>)
                .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
            .Single(fun x -> x.Id = 1)
            .Tag_AcquiredCards
    Assert.Equal(
        tagName,
        joinTable().Single(fun x -> x.Tag.Name = tagName).Tag.Name
    )
    
    do! TagRepository.DeleteFrom c.Db tagName cardId
    Assert.Empty <| joinTable ()
    }

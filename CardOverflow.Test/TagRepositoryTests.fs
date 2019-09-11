module TagRepositoryTests

open CardOverflow.Api
open CardOverflow.Test
open System
open System.Linq
open Xunit

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

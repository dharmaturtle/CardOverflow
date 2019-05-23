module PrivateTagRepositoryTests

open CardOverflow.Api
open CardOverflow.Test
open System
open System.Linq
open Xunit

[<Fact>]
let ``PrivateTagRepository can add a new tag``() =
    use c = new TestContainer()
    let tagName = Guid.NewGuid().ToString()

    tagName |> List.singleton |> PrivateTagRepository.Add c.Db 1

    Assert.Single(c.Db.PrivateTags.Where(fun x -> x.Name = tagName).ToList())

[<Fact>]
let ``When PrivateTagRepository adds a tag twice, only one is added``() =
    use c = new TestContainer()
    let tagName = Guid.NewGuid().ToString()

    tagName |> List.singleton |> PrivateTagRepository.Add c.Db 1
    tagName |> List.singleton |> PrivateTagRepository.Add c.Db 1

    Assert.Single(c.Db.PrivateTags.Where(fun x -> x.Name = tagName).ToList())

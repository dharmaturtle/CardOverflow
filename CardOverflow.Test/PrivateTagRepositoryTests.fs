module PrivateTagRepositoryTests

open CardOverflow.Api
open CardOverflow.Test
open System
open System.Linq
open Xunit

[<Fact>]
let ``PrivateTagRepository can add a new tag``() =
    use p = new SqlTempDbProvider()
    let tagName = Guid.NewGuid().ToString()
    let repository = PrivateTagRepository(p.DbService, 1)

    tagName |> List.singleton |> repository.Add

    p.DbService.Query(fun x -> x.PrivateTags.Single(fun x -> x.Name = tagName)) |> ignore

[<Fact>]
let ``When PrivateTagRepository adds a tag twice, only one is added``() =
    use p = new SqlTempDbProvider()
    let tagName = Guid.NewGuid().ToString()
    let repository = PrivateTagRepository(p.DbService, 1)

    tagName |> List.singleton |> repository.Add
    tagName |> List.singleton |> repository.Add

    p.DbService.Query(fun x -> x.PrivateTags.Single(fun x -> x.Name = tagName)) |> ignore

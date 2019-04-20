module PrivateTagRepositoryTests

open CardOverflow.Api
open CardOverflow.Test
open System
open System.Linq
open Xunit

[<Fact>]
let ``PrivateTagRepository can add a new tag``() =
    use tempDb = new TempDbService()
    let tagName = Guid.NewGuid().ToString()
    let repository = PrivateTagRepository(tempDb.DbService, 1)

    tagName |> List.singleton |> repository.Add

    tempDb.DbService.Query(fun x -> x.PrivateTags.Single(fun x -> x.Name = tagName)) |> ignore

[<Fact>]
let ``When PrivateTagRepository adds a tag twice, only one is added``() =
    use tempDb = new TempDbService()
    let tagName = Guid.NewGuid().ToString()
    let repository = PrivateTagRepository(tempDb.DbService, 1)

    tagName |> List.singleton |> repository.Add
    tagName |> List.singleton |> repository.Add

    tempDb.DbService.Query(fun x -> x.PrivateTags.Single(fun x -> x.Name = tagName)) |> ignore

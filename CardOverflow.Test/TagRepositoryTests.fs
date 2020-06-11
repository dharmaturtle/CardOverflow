module TagRepositoryTests

open CardOverflow.Entity
open CardOverflow.Debug
open CardOverflow.Pure
open Microsoft.EntityFrameworkCore
open CardOverflow.Api
open CardOverflow.Test
open System
open System.Linq
open Xunit
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Control.Tasks
open CardOverflow.Sanitation
open FsToolkit.ErrorHandling

[<Fact>]
let ``SanitizeTagRepository AddTo/DeleteFrom works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId []
    let stackId = 1
    let tagName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase

    do! SanitizeTagRepository.AddTo c.Db userId tagName stackId

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
    
    do! SanitizeTagRepository.DeleteFrom c.Db userId tagName stackId
    Assert.Empty <| joinTable ()

    // Can't add tag to a card twice
    do! SanitizeTagRepository.AddTo c.Db userId tagName stackId
    Assert.Equal(
        tagName,
        joinTable().Single(fun x -> x.Tag.Name = tagName).Tag.Name
    )
    let! error = SanitizeTagRepository.AddTo c.Db userId tagName stackId |> TaskResult.getError
    Assert.Equal(sprintf "Stack #%i for User #%i already has tag \"%s\"" stackId userId tagName, error)

    // Can't add tag to a card twice, even if different casing
    let caps = tagName.ToUpper()
    let! error = SanitizeTagRepository.AddTo c.Db userId caps stackId |> TaskResult.getError
    Assert.Equal(sprintf "Stack #%i for User #%i already has tag \"%s\"" stackId userId (caps |> MappingTools.toTitleCase), error)
    let lows = tagName.ToLower()
    let! error = SanitizeTagRepository.AddTo c.Db userId lows stackId |> TaskResult.getError
    Assert.Equal(sprintf "Stack #%i for User #%i already has tag \"%s\"" stackId userId (lows |> MappingTools.toTitleCase), error)
    
    // Can't delete a tag that doesn't exist
    do! SanitizeTagRepository.DeleteFrom c.Db userId tagName stackId
    let! error = SanitizeTagRepository.DeleteFrom c.Db userId tagName stackId |> TaskResult.getError
    Assert.Equal(sprintf "Stack #%i for User #%i doesn't have the tag \"%s\"" stackId userId tagName, error)
    // again
    let tagName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    let! error = SanitizeTagRepository.DeleteFrom c.Db userId tagName stackId |> TaskResult.getError
    Assert.Equal(sprintf "Stack #%i for User #%i doesn't have the tag \"%s\"" stackId userId tagName, error)
    
    // Can't delete a tag from a card that ain't yours
    let otherUser = 2
    let! _ = FacetRepositoryTests.addBasicStack c.Db otherUser [tagName]
    let stackId = 2
    let! error = SanitizeTagRepository.DeleteFrom c.Db userId tagName stackId |> TaskResult.getError
    Assert.Equal(sprintf "User #%i doesn't have Stack #%i." userId stackId, error)

    // Can't add a tag to a card that ain't yours
    let! error = SanitizeTagRepository.AddTo c.Db userId tagName stackId |> TaskResult.getError
    Assert.Equal(sprintf "User #%i doesn't have Stack #%i." userId stackId, error)
    } |> TaskResult.getOk)

[<Fact>]
let ``Tag counts work``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let assertTagUserCount expected =
        c.Db.StackTagCount.SingleAsync() |> Task.map (fun x -> Assert.Equal(expected, x.Count))
    let author = 1
    let acquirer = 2
    let stackId = 1
    let branchInstanceId = 1001

    // initial tag has 1 user
    let tagName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    let! _ = FacetRepositoryTests.addBasicStack c.Db author [tagName]
    do! assertTagUserCount 1

    // initial tag has 2 users after acquisition
    do! StackRepository.AcquireCardAsync c.Db acquirer branchInstanceId
    do! SanitizeTagRepository.AddTo c.Db acquirer tagName stackId
    do! assertTagUserCount 2

    // suspending a card decrements the tag count
    let! (ac: _ ResizeArray) = StackRepository.GetAcquired c.Db acquirer stackId
    let ac = ac.Single()
    do! StackRepository.editState c.Db acquirer ac.AcquiredCardId Suspended
    do! assertTagUserCount 1

    // deleting a card decrements the tag count
    let! (ac: AcquiredCard ResizeArray) = StackRepository.GetAcquired c.Db author stackId
    let ac = ac.Single()
    do! StackRepository.unacquireStack c.Db author ac.StackId
    Assert.Empty <| c.Db.StackTagCount.ToList()
    } |> TaskResult.getOk)

open TagRepository
[<Fact>]
let ``Tag "a" parses ``(): unit =
    ["a"]

    |> TagRepository.parse

    |> Assert.equal
        [{  Id = "a"
            ParentId = ""
            Name = "a"
            IsExpanded = false
            HasChildren = false }]

[<Fact>]
let ``Tags "a" and "a/b" parse``(): unit =
    let a = "a"
    let a_b = a +/+ "b"
    [a; a_b]

    |> TagRepository.parse

    |> Assert.equal
        [{  Id = "a"
            ParentId = ""
            Name = "a"
            IsExpanded = false
            HasChildren = true }
         {  Id = "a/b"
            ParentId = "a"
            Name = "b"
            IsExpanded = false
            HasChildren = false }]

[<Fact>]
let ``Tag "a/b" parses``(): unit =
    "a" +/+ "b"
    |> List.singleton

    |> TagRepository.parse

    |> Assert.equal
        [{  Id = "a"
            ParentId = ""
            Name = "a"
            IsExpanded = false
            HasChildren = true }
         {  Id = "a/b"
            ParentId = "a"
            Name = "b"
            IsExpanded = false
            HasChildren = false }]

[<Fact>]
let ``Tag "a/b/c" parses``(): unit =
    "a" +/+ "b" +/+ "c"
    |> List.singleton

    |> TagRepository.parse

    |> Assert.equal
        [{  Id = "a"
            ParentId = ""
            Name = "a"
            IsExpanded = false
            HasChildren = true }
         {  Id = "a/b"
            ParentId = "a"
            Name = "b"
            IsExpanded = false
            HasChildren = true }
         {  Id = "a/b/c"
            ParentId = "a/b"
            Name = "c"
            IsExpanded = false
            HasChildren = false }]

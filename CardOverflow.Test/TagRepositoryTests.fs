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
open LoadersAndCopiers

[<Fact>]
let ``SanitizeTagRepository AddTo/DeleteFrom works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = FacetRepositoryTests.addBasicConcept c.Db userId [] (concept_1, branch_1, leaf_1, [card_1])
    let conceptId = concept_1
    let tagName = Guid.NewGuid().ToString() |> SanitizeTagRepository.sanitize

    do! SanitizeTagRepository.AddTo c.Db userId tagName conceptId

    Assert.SingleI(c.Db.Card.Single().Tags.Where((=) tagName).ToList())
    let joinTable () =
        c.Db.Card.Single(fun x -> x.Id = card_1).Tags
    Assert.Equal(
        tagName,
        joinTable().Single(fun x -> x = tagName)
    )
    
    do! SanitizeTagRepository.DeleteFrom c.Db userId tagName conceptId
    Assert.Empty <| joinTable ()

    // Can't add tag to a card twice
    do! SanitizeTagRepository.AddTo c.Db userId tagName conceptId
    Assert.Equal(
        tagName,
        joinTable().Single(fun x -> x = tagName)
    )
    let! error = SanitizeTagRepository.AddTo c.Db userId tagName conceptId |> TaskResult.getError
    Assert.Equal(sprintf "Concept #%A for User #%A already has tag \"%s\"" conceptId userId tagName, error)

    // Can't add tag to a card twice, even if different casing
    let caps = tagName.ToUpper()
    let! error = SanitizeTagRepository.AddTo c.Db userId caps conceptId |> TaskResult.getError
    Assert.Equal(sprintf "Concept #%A for User #%A already has tag \"%s\"" conceptId userId (caps |> MappingTools.toTitleCase), error)
    let lows = tagName.ToLower()
    let! error = SanitizeTagRepository.AddTo c.Db userId lows conceptId |> TaskResult.getError
    Assert.Equal(sprintf "Concept #%A for User #%A already has tag \"%s\"" conceptId userId (lows |> MappingTools.toTitleCase), error)
    
    // Can't delete a tag that doesn't exist
    do! SanitizeTagRepository.DeleteFrom c.Db userId tagName conceptId
    let! error = SanitizeTagRepository.DeleteFrom c.Db userId tagName conceptId |> TaskResult.getError
    Assert.Equal(sprintf "Concept #%A for User #%A doesn't have the tag \"%s\"" conceptId userId tagName, error)
    // again
    let tagName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    let! error = SanitizeTagRepository.DeleteFrom c.Db userId tagName conceptId |> TaskResult.getError
    Assert.Equal(sprintf "Concept #%A for User #%A doesn't have the tag \"%s\"" conceptId userId tagName, error)
    
    // Can't delete a tag from a card that ain't yours
    let otherUser = user_2
    let! _ = FacetRepositoryTests.addBasicConcept c.Db otherUser [tagName] (concept_2, branch_2, leaf_2, [card_2])
    let conceptId = concept_2
    let! error = SanitizeTagRepository.DeleteFrom c.Db userId tagName conceptId |> TaskResult.getError
    Assert.Equal(sprintf "User #%A doesn't have Concept #%A." userId conceptId, error)

    // Can't add a tag to a card that ain't yours
    let! error = SanitizeTagRepository.AddTo c.Db userId tagName conceptId |> TaskResult.getError
    Assert.Equal(sprintf "User #%A doesn't have Concept #%A." userId conceptId, error)
    } |> TaskResult.getOk)

[<Fact(Skip=PgSkip.reason)>]
let ``Tag counts work``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let assertTagUserCount expected =
        c.Db.Concept.SingleAsync()
        |>% (fun x -> Assert.Equal(expected, x.TagsCount.Single()))
    let author = user_1
    let collector = user_2
    let conceptId = concept_1
    let leafId = leaf_1

    // initial tag has 1 user
    let tagName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    let! _ = FacetRepositoryTests.addBasicConcept c.Db author [tagName] (concept_1, branch_1, leaf_1, [card_1])
    do! assertTagUserCount 1

    // initial tag has 2 users after collecting
    do! ConceptRepository.CollectCard c.Db collector leafId [ Ulid.create ]
    do! SanitizeTagRepository.AddTo c.Db collector tagName conceptId
    do! assertTagUserCount 2

    // suspending a card decrements the tag count
    let! (cc: Card ResizeArray) = ConceptRepository.GetCollected c.Db collector conceptId
    let cc = cc.Single()
    do! ConceptRepository.editState c.Db collector cc.CardId Suspended
    do! assertTagUserCount 1

    // deleting a card decrements the tag count
    let! (cc: Card ResizeArray) = ConceptRepository.GetCollected c.Db author conceptId
    let cc = cc.Single()
    do! ConceptRepository.uncollectConcept c.Db author cc.ConceptId
    Assert.Empty <| c.Db.Concept.Single().TagsCount.ToList()
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

[<Fact(Skip=PgSkip.reason)>]
let ``TagRepository.getAll works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = FacetRepositoryTests.addBasicConcept c.Db userId ["ax" +/+ "by" +/+ "cz"] (concept_1, branch_1, leaf_1, [card_1])
    
    let! actual = TagRepository.getAll c.Db userId

    Assert.equal
        [{  Id = "Ax"
            ParentId = ""
            Name = "Ax"
            IsExpanded = false
            HasChildren = true }
         {  Id = "Ax/By"
            ParentId = "Ax"
            Name = "By"
            IsExpanded = false
            HasChildren = true }
         {  Id = "Ax/By/Cz"
            ParentId = "Ax/By"
            Name = "Cz"
            IsExpanded = false
            HasChildren = false }]
        actual
    } |> TaskResult.getOk)

[<Theory>]
[<InlineData("a/b/c", "A/B/C")>]
[<InlineData(" a / b / c ", "A/B/C")>]
[<InlineData(" ax / by / cz ", "Ax/By/Cz")>]
[<InlineData(" aX / bY / cZ ", "Ax/By/Cz")>]
[<InlineData("! aX @/ #bY &/% cZ        & ",
             "! Ax @/#By &/% Cz &")>]
let ``SanitizeTagRepository.sanitize works`` input expected: unit =
    input

    |> SanitizeTagRepository.sanitize

    |> Assert.equal expected

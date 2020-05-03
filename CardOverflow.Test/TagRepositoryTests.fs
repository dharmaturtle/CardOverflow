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
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId []
    let cardId = 1
    let tagName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase

    do! SanitizeTagRepository.AddTo c.Db userId tagName cardId

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
    
    do! SanitizeTagRepository.DeleteFrom c.Db userId tagName cardId
    Assert.Empty <| joinTable ()

    // Can't add tag to a card twice
    do! SanitizeTagRepository.AddTo c.Db userId tagName cardId
    Assert.Equal(
        tagName,
        joinTable().Single(fun x -> x.Tag.Name = tagName).Tag.Name
    )
    let! error = SanitizeTagRepository.AddTo c.Db userId tagName cardId |> TaskResult.getError
    Assert.Equal(sprintf "Card #%i for User #%i already has tag \"%s\"" cardId userId tagName, error)

    // Can't add tag to a card twice, even if different casing
    let caps = tagName.ToUpper()
    let! error = SanitizeTagRepository.AddTo c.Db userId caps cardId |> TaskResult.getError
    Assert.Equal(sprintf "Card #%i for User #%i already has tag \"%s\"" cardId userId (caps |> MappingTools.toTitleCase), error)
    let lows = tagName.ToLower()
    let! error = SanitizeTagRepository.AddTo c.Db userId lows cardId |> TaskResult.getError
    Assert.Equal(sprintf "Card #%i for User #%i already has tag \"%s\"" cardId userId (lows |> MappingTools.toTitleCase), error)
    
    // Can't delete a tag that doesn't exist
    do! SanitizeTagRepository.DeleteFrom c.Db userId tagName cardId
    let! error = SanitizeTagRepository.DeleteFrom c.Db userId tagName cardId |> TaskResult.getError
    Assert.Equal(sprintf "Card #%i for User #%i doesn't have the tag \"%s\"" cardId userId tagName, error)
    // again
    let tagName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    let! error = SanitizeTagRepository.DeleteFrom c.Db userId tagName cardId |> TaskResult.getError
    Assert.Equal(sprintf "Card #%i for User #%i doesn't have the tag \"%s\"" cardId userId tagName, error)
    
    // Can't delete a tag from a card that ain't yours
    let otherUser = 2
    let! _ = FacetRepositoryTests.addBasicCard c.Db otherUser [tagName]
    let cardId = 2
    let! error = SanitizeTagRepository.DeleteFrom c.Db userId tagName cardId |> TaskResult.getError
    Assert.Equal(sprintf "User #%i doesn't have Card #%i." userId cardId, error)

    // Can't add a tag to a card that ain't yours
    let! error = SanitizeTagRepository.AddTo c.Db userId tagName cardId |> TaskResult.getError
    Assert.Equal(sprintf "User #%i doesn't have Card #%i." userId cardId, error)
    } |> TaskResult.getOk)

[<Fact>]
let ``Tag counts work``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let assertTagUserCount expected =
        c.Db.CardTagCount.SingleAsync() |> Task.map (fun x -> Assert.Equal(expected, x.Count))
    let author = 1
    let acquirer = 2
    let cardId = 1
    let cardInstanceId = 1001

    // initial tag has 1 user
    let tagName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    let! _ = FacetRepositoryTests.addBasicCard c.Db author [tagName]
    do! assertTagUserCount 1

    // initial tag has 2 users after acquisition
    do! CardRepository.AcquireCardAsync c.Db acquirer cardInstanceId
    do! SanitizeTagRepository.AddTo c.Db acquirer tagName cardId
    do! assertTagUserCount 2

    // suspending a card decrements the tag count
    let! ac = CardRepository.GetAcquired c.Db acquirer cardId
    do! CardRepository.editState c.Db acquirer ac.AcquiredCardId Suspended
    do! assertTagUserCount 1

    // deleting a card decrements the tag count
    let! ac = CardRepository.GetAcquired c.Db author cardId
    do! CardRepository.deleteAcquired c.Db author ac.AcquiredCardId
    Assert.Empty <| c.Db.CardTagCount.ToList()
    } |> TaskResult.getOk)

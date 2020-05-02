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
open CardOverflow.Sanitation
open FsToolkit.ErrorHandling

[<Fact>]
let ``SanitizeTagRepository AddTo/DeleteFrom works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId []
    let cardId = 1
    let tagName = Guid.NewGuid().ToString()

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
    Assert.Equal(sprintf "Card #%i for User#%i already has tag \"%s\"" cardId userId tagName, error)
    } |> TaskResult.getOk)

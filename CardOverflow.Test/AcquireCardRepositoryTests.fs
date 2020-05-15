module AcquireCardRepositoryTests

open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Debug
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open CardOverflow.Test
open System
open System.Linq
open Xunit
open CardOverflow.Pure
open System.Collections.Generic
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Pure
open CardOverflow.Sanitation
open FsToolkit
open FsToolkit.ErrorHandling

[<Fact>]
let ``CardRepository.deleteAcquired works``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! actualBranchId = FacetRepositoryTests.addBasicCard c.Db userId []
    let branchId = 1
    Assert.Equal(branchId, actualBranchId)
    let! collate =
        TestCollateRepo.Search c.Db "Basic"
        |> Task.map (fun x -> x.Single(fun x -> x.Name = "Basic"))
    let getAcquired () = task { return! CardRepository.GetAcquired c.Db userId 1 }
    let! ac = getAcquired ()

    let! x = CardRepository.deleteAcquired c.Db userId ac.Value.AcquiredCardId
    Assert.Null x.Value
    Assert.Empty c.Db.AcquiredCard

    let reacquire () = task { do! CardRepository.AcquireCardAsync c.Db userId ac.Value.BranchInstanceMeta.Id |> TaskResult.getOk }
    
    do! reacquire ()
    let! ac = getAcquired ()
    let! x =
        {   EditCardCommand.EditSummary = ""
            FieldValues = [].ToList()
            CollateInstance = collate |> ViewCollateInstance.copyTo
            Source = UpdateBranchId (branchId, null)
        } |> UpdateRepository.card c.Db userId
    let actualBranchId = x.Value
    Assert.Equal(branchId, actualBranchId)
    let! x = CardRepository.deleteAcquired c.Db userId ac.Value.AcquiredCardId
    Assert.Null x.Value
    Assert.Empty c.Db.AcquiredCard // still empty after editing then deleting

    let userId = 3
    do! reacquire ()
    let! ac = getAcquired ()
    let ac = ac.Value
    let! batch = CardRepository.GetQuizBatch c.Db userId ""
    do! SanitizeHistoryRepository.AddAndSaveAsync c.Db (batch.First().Value.AcquiredCardId) Score.Easy DateTime.UtcNow (TimeSpan.FromDays(13.)) 0. (TimeSpan.FromSeconds 1.) (Interval <| TimeSpan.FromDays 13.)
    do! SanitizeTagRepository.AddTo c.Db userId "tag" ac.CardId |> TaskResult.getOk
    let! actualBranchId = FacetRepositoryTests.addBasicCard c.Db userId []
    let newCardBranchId = 2
    Assert.Equal(newCardBranchId, actualBranchId)
    let! card2 = c.Db.Card.SingleOrDefaultAsync(fun x -> x.Id <> ac.CardId)
    let card2 = card2.Id
    let addRelationshipCommand =
        {   Name = "my relationship"
            SourceCardId = 1
            TargetCardLink = string card2
        }
    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand
    Assert.Null x.Value
    Assert.NotEmpty c.Db.AcquiredCard
    Assert.NotEmpty c.Db.Relationship_AcquiredCard
    Assert.NotEmpty c.Db.History
    Assert.NotEmpty c.Db.Tag_AcquiredCard
    let! x = CardRepository.deleteAcquired c.Db userId ac.AcquiredCardId // can delete after adding a history, tag, and relationship
    Assert.Null x.Value
    Assert.Equal(card2, c.Db.AcquiredCard.Include(fun x -> x.BranchInstance).Single().BranchInstance.CardId) // from the other side of the relationship
    Assert.Empty c.Db.Relationship_AcquiredCard
    Assert.Empty c.Db.History
    Assert.Empty c.Db.Tag_AcquiredCard
    
    do! reacquire ()
    let otherUserId = 2
    let! x = CardRepository.deleteAcquired c.Db otherUserId ac.AcquiredCardId
    Assert.Equal("You don't own that card.", x.error) // other users can't delete your card
    }

[<Fact>]
let ``CardRepository.editState works``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! instanceIds = FacetRepositoryTests.addBasicCard c.Db userId []
    Assert.Equal(1001, instanceIds)
    let! collate = SanitizeCollate.AllInstances c.Db 1
    let! ac = CardRepository.GetAcquired c.Db userId 1
    
    let! x = CardRepository.editState c.Db userId ac.Value.AcquiredCardId CardState.Suspended
    Assert.Null x.Value
    let! ac = CardRepository.GetAcquired c.Db userId ac.Value.CardId
    Assert.Equal(ac.Value.CardState, CardState.Suspended)

    let! x =
        {   EditCardCommand.EditSummary = ""
            FieldValues = [].ToList()
            CollateInstance = collate.Value.Instances.Single() |> ViewCollateInstance.copyTo
            Source = NewOriginal
        } |> UpdateRepository.card c.Db userId
    let instanceIds = x.Value
    Assert.Equal(1002, instanceIds)
    let! ac = CardRepository.GetAcquired c.Db userId ac.Value.CardId
    Assert.Equal(ac.Value.CardState, CardState.Suspended) // still suspended after edit

    let userId = 2 // other users can't edit card state
    let! x = CardRepository.editState c.Db userId ac.Value.AcquiredCardId CardState.Suspended
    Assert.Equal("You don't own that card.", x.error)
    }

[<Fact>]
let ``Users can't acquire multiple instances of a card``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! actualBranchId = FacetRepositoryTests.addBasicCard c.Db userId []
    let cardId = 1
    let branchId = 1
    Assert.Equal(branchId, actualBranchId)
    let! collate =
        TestCollateRepo.Search c.Db "Basic"
        |> Task.map (fun x -> x.Single(fun x -> x.Name = "Basic"))
    let! actualBranchId = 
        {   EditCardCommand.EditSummary = ""
            FieldValues = [].ToList()
            CollateInstance = collate |> ViewCollateInstance.copyTo
            Source = UpdateBranchId (branchId, null)
        } |> UpdateRepository.card c.Db userId
    let i2 = 1002
    Assert.Equal(branchId, actualBranchId.Value)
    do! CardRepository.AcquireCardAsync c.Db userId i2 |> TaskResult.getOk // acquiring a different revision of a card doesn't create a new AcquiredCard; it only swaps out the BranchInstanceId
    Assert.Equal(i2, c.Db.AcquiredCard.Single().BranchInstanceId)
    Assert.Equal(branchId, c.Db.AcquiredCard.Single().BranchId)
    Assert.Equal(cardId, c.Db.AcquiredCard.Single().CardId)
    
    use db = c.Db
    db.AcquiredCard.AddI <|
        AcquiredCardEntity(
            CardId = cardId,
            BranchId = branchId,
            BranchInstanceId = i2,
            Due = DateTime.UtcNow,
            UserId = userId,
            CardSettingId = userId)
    let ex = Assert.Throws<DbUpdateException>(fun () -> db.SaveChanges() |> ignore)
    Assert.Equal(
        "23505: duplicate key value violates unique constraint \"IX_AcquiredCard_UserId_BranchInstanceId_Index\"",
        ex.InnerException.Message)

    let i1 = 1001
    use db = c.Db
    db.AcquiredCard.AddI <|
        AcquiredCardEntity(
            CardId = cardId,
            BranchId = branchId,
            BranchInstanceId = i1,
            Due = DateTime.UtcNow,
            UserId = userId,
            CardSettingId = userId)
    let ex = Assert.Throws<DbUpdateException>(fun () -> db.SaveChanges() |> ignore)
    Assert.Equal(
        "P0001: UserId #3 with AcquiredCard #3 tried to have BranchInstanceId #1001, but they already have BranchInstanceId #1002",
        ex.InnerException.Message)
    }

[<Fact>]
let ``AcquireCards works``(): Task<unit> = task {
    use c = new TestContainer()
    
    let authorId = 3
    
    let c1 = 1
    let b1 = 1
    let ci1_1 = 1001
    let! _ = FacetRepositoryTests.addBasicCard c.Db authorId []
    Assert.Equal(1, c.Db.Card.Single().Users)
    Assert.Equal(1, c.Db.BranchInstance.Single().Users)
    Assert.Equal(1, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(1, c.Db.BranchInstance.Single(fun x -> x.Id = ci1_1).Users)
    Assert.Equal(1, c.Db.AcquiredCard.Count())
    
    let c2 = 2
    let ci2_1 = 1002
    let! _ = FacetRepositoryTests.addReversedBasicCard c.Db authorId []
    Assert.Equal(1, c.Db.Card.Single(fun x -> x.Id = c2).Users)
    Assert.Equal(1, c.Db.BranchInstance.Single(fun x -> x.Id = ci2_1).Users)
    Assert.Equal(3, c.Db.AcquiredCard.Count())
    
    let acquirerId = 1
    do! CardRepository.AcquireCardAsync c.Db acquirerId ci1_1 |> TaskResult.getOk
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(2, c.Db.BranchInstance.Single(fun x -> x.Id = ci1_1).Users)
    Assert.Equal(4, c.Db.AcquiredCard.Count())
    do! CardRepository.AcquireCardAsync c.Db acquirerId ci2_1 |> TaskResult.getOk
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c2).Users)
    Assert.Equal(2, c.Db.BranchInstance.Single(fun x -> x.Id = ci2_1).Users)
    // misc
    Assert.Equal(2, c.Db.BranchInstance.Count())
    Assert.Equal(6, c.Db.AcquiredCard.Count())
    Assert.Equal(2, c.Db.AcquiredCard.Count(fun x -> x.BranchInstanceId = ci1_1));

    // update branch
    let! r = SanitizeCardRepository.getUpsert c.Db <| VUpdateBranchId b1
    let command =
        { r.Value with
            FieldValues = [].ToList()
            Source = UpdateBranchId (b1, null)
        }
    let! branchId = UpdateRepository.card c.Db authorId command.load |> TaskResult.getOk
    let ci1_2 = 1003
    Assert.Equal(b1, branchId)
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(1, c.Db.BranchInstance.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.BranchInstance.Count())
    Assert.Equal(6, c.Db.AcquiredCard.Count())
    Assert.Equal(1, c.Db.AcquiredCard.Count(fun x -> x.BranchInstanceId = ci1_2))
    
    do! CardRepository.AcquireCardAsync c.Db acquirerId ci1_2 |> TaskResult.getOk
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(2, c.Db.BranchInstance.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.BranchInstance.Count())
    Assert.Equal(6, c.Db.AcquiredCard.Count())
    Assert.Equal(2, c.Db.AcquiredCard.Count(fun x -> x.BranchInstanceId = ci1_2));

    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.CardId = c1 && x.UserId = authorId)
    do! CardRepository.UnacquireCardAsync c.Db ac.Id
    Assert.Equal(1, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(1, c.Db.BranchInstance.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.BranchInstance.Count())
    Assert.Equal(5, c.Db.AcquiredCard.Count())
    Assert.Equal(1, c.Db.AcquiredCard.Count(fun x -> x.BranchInstanceId = ci1_2));

    let count = CardRepository.GetDueCount c.Db acquirerId ""
    Assert.Equal(3, count)
    let count = CardRepository.GetDueCount c.Db authorId ""
    Assert.Equal(2, count)

    let! a = CardRepository.GetQuizBatch c.Db acquirerId ""
    let getId (x: Result<QuizCard, string> seq) = x.First().Value.AcquiredCardId
    do! SanitizeHistoryRepository.AddAndSaveAsync c.Db (getId a) Score.Easy DateTime.UtcNow (TimeSpan.FromDays(13.)) 0. (TimeSpan.FromSeconds 1.) (Interval <| TimeSpan.FromDays 13.)
    let! b = CardRepository.GetQuizBatch c.Db acquirerId ""
    Assert.NotEqual(getId a, getId b)

    let count = CardRepository.GetDueCount c.Db acquirerId ""
    Assert.Equal(2, count)

    // getHeatmap returns one for today
    let! actual = HistoryRepository.getHeatmap c.Db acquirerId
    Assert.Equal(0, actual.DateCountLevels.Length % 7) // returns full weeks; not partial weeks
    Assert.Equal(
        {   Date = DateTime.UtcNow.Date
            Count = 1
            Level = 10 },
        actual.DateCountLevels.Single(fun x -> x.Count <> 0)
    )}

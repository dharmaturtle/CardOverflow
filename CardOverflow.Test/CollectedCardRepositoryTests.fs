module CollectCardRepositoryTests

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
let ``StackRepository.deleteCollectedCard works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId []
    let branchId = 1
    Assert.Equal(branchId, actualBranchId)
    let getCollected () = StackRepository.GetCollected c.Db userId 1
    let! (cc: CollectedCard ResizeArray) = getCollected ()
    let cc = cc.Single()

    do! StackRepository.uncollectStack c.Db userId cc.StackId
    Assert.Empty c.Db.CollectedCard

    let recollect () = task { do! StackRepository.CollectCard c.Db userId cc.BranchInstanceMeta.Id |> TaskResult.getOk }
    
    do! recollect ()
    let! (cc: CollectedCard ResizeArray) = getCollected ()
    let cc = cc.Single()
    do! FacetRepositoryTests.update c userId
            (VUpdateBranchId branchId) id branchId
    do! StackRepository.uncollectStack c.Db userId cc.StackId
    Assert.Empty c.Db.CollectedCard // still empty after editing then deleting

    let userId = 3
    do! recollect ()
    let! (cc: CollectedCard ResizeArray) = getCollected ()
    let cc = cc.Single()
    let! (batch: Result<QuizCard, string> ResizeArray) = StackRepository.GetQuizBatch c.Db userId ""
    do! SanitizeHistoryRepository.AddAndSaveAsync c.Db (batch.First().Value.CollectedCardId) Score.Easy DateTime.UtcNow (TimeSpan.FromDays(13.)) 0. (TimeSpan.FromSeconds 1.) (Interval <| TimeSpan.FromDays 13.)
    do! SanitizeTagRepository.AddTo c.Db userId "tag" cc.StackId |> TaskResult.getOk
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId []
    let newCardBranchId = 2
    Assert.Equal(newCardBranchId, actualBranchId)
    let! (stack2: StackEntity) = c.Db.Stack.SingleOrDefaultAsync(fun x -> x.Id <> cc.StackId)
    let stack2 = stack2.Id
    let addRelationshipCommand =
        {   Name = "my relationship"
            SourceStackId = 1
            TargetStackLink = string stack2
        }
    do! SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand
    Assert.NotEmpty c.Db.CollectedCard
    Assert.NotEmpty c.Db.Relationship_CollectedCard
    Assert.NotEmpty c.Db.History
    Assert.NotEmpty c.Db.Tag_CollectedCard
    do! StackRepository.uncollectStack c.Db userId cc.StackId // can delete after adding a history, tag, and relationship
    Assert.Equal(stack2, c.Db.CollectedCard.Include(fun x -> x.BranchInstance).Single().BranchInstance.StackId) // from the other side of the relationship
    Assert.Empty c.Db.Relationship_CollectedCard
    Assert.Empty c.Db.Tag_CollectedCard

    // but history remains
    Assert.NotEmpty c.Db.History
    
    // Error when deleting something you don't own
    do! recollect ()
    let otherUserId = 2
    let! (x: Result<_, _>) = StackRepository.uncollectStack c.Db otherUserId cc.StackId
    Assert.Equal("You don't have any cards with Stack #1", x.error)
    } |> TaskResult.getOk)

[<Fact>]
let ``StackRepository.editState works``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId []
    let branchId = 1
    Assert.Equal(branchId, actualBranchId)
    let! cc = StackRepository.GetCollected c.Db userId 1
    let cc = cc.Value.Single()
    
    let! x = StackRepository.editState c.Db userId cc.CollectedCardId CardState.Suspended
    Assert.Null x.Value
    let! cc = StackRepository.GetCollected c.Db userId cc.StackId
    let cc = cc.Value.Single()
    Assert.Equal(cc.CardState, CardState.Suspended)

    do! FacetRepositoryTests.update c userId
            (VUpdateBranchId branchId) id branchId
        |> TaskResult.getOk
    let! cc = StackRepository.GetCollected c.Db userId cc.StackId
    let cc = cc.Value.Single()
    Assert.Equal(cc.CardState, CardState.Suspended) // still suspended after edit

    let otherUserId = 2 // other users can't edit card state
    let! x = StackRepository.editState c.Db otherUserId cc.CollectedCardId CardState.Suspended
    Assert.Equal("You don't own that card.", x.error)
    }

[<Fact>]
let ``Users can't collect multiple instances of a card``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId []
    let stackId = 1
    let branchId = 1
    Assert.Equal(branchId, actualBranchId)
    do! FacetRepositoryTests.update c userId
            (VUpdateBranchId branchId) id branchId
        |> TaskResult.getOk
    let i2 = 1002
    do! StackRepository.CollectCard c.Db userId i2 |> TaskResult.getOk // collecting a different revision of a card doesn't create a new CollectedCard; it only swaps out the BranchInstanceId
    Assert.Equal(i2, c.Db.CollectedCard.Single().BranchInstanceId)
    Assert.Equal(branchId, c.Db.CollectedCard.Single().BranchId)
    Assert.Equal(stackId, c.Db.CollectedCard.Single().StackId)
    
    use db = c.Db
    db.CollectedCard.AddI <|
        CollectedCardEntity(
            StackId = stackId,
            BranchId = branchId,
            BranchInstanceId = i2,
            Due = DateTime.UtcNow,
            UserId = userId,
            CardSettingId = userId)
    let ex = Assert.Throws<DbUpdateException>(fun () -> db.SaveChanges() |> ignore)
    Assert.Equal(
        "23505: duplicate key value violates unique constraint \"IX_CollectedCard_UserId_BranchInstanceId_Index\"",
        ex.InnerException.Message)

    let i1 = 1001
    use db = c.Db
    db.CollectedCard.AddI <|
        CollectedCardEntity(
            StackId = stackId,
            BranchId = branchId,
            BranchInstanceId = i1,
            Due = DateTime.UtcNow,
            UserId = userId,
            CardSettingId = userId,
            DeckId = userId)
    let ex = Assert.Throws<Npgsql.PostgresException>(fun () -> db.SaveChanges() |> ignore)
    Assert.Equal(
        "P0001: UserId #3 with CollectedCard #3 and Stack #1 tried to have BranchInstanceId #1001, but they already have BranchInstanceId #1002",
        ex.Message)
    }

[<Fact>]
let ``collect works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = 3
    do! FacetRepositoryTests.addBasicStack c.Db authorId []
    let branchId = 1
    let instanceId = 1001
    let stackId = 1
    let collectorId = 1
    let collectorDefaultDeckId = 1
    let collect = StackRepository.collect c.Db collectorId instanceId
    let assertDeck deckId =
        StackRepository.GetCollected c.Db collectorId stackId
        |>%% Assert.Single
        |>%% fun x -> x.DeckId
        |>%% Assert.equal deckId

    do! collect None
    
    do! assertDeck collectorDefaultDeckId

    // fails for author's deck
    do! StackRepository.uncollectStack c.Db collectorId stackId
    
    let! (error: Result<_,_>) = collect <| Some authorId
    
    Assert.equal "Either Deck #3 doesn't exist or it doesn't belong to you." error.error
    
    // fails for nonexisting deck
    let! (error: Result<_,_>) = collect <| Some 1337
    
    Assert.equal "Either Deck #1337 doesn't exist or it doesn't belong to you." error.error
    
    // works for nondefault deck
    let! newDeckId = SanitizeDeckRepository.create c.Db collectorId <| Guid.NewGuid().ToString()

    do! collect <| Some newDeckId
    
    do! assertDeck newDeckId

    // collecting/updating to *new* instance doesn't change deckId
    let! stackCommand = VUpdateBranchId branchId |> SanitizeStackRepository.getUpsert c.Db
    do! SanitizeStackRepository.Update c.Db authorId [] stackCommand

    do! StackRepository.collect c.Db collectorId 1002 None

    do! assertDeck newDeckId

    // collecting/updating to *old* instance doesn't change deckId
    do! StackRepository.collect c.Db collectorId 1001 None

    do! assertDeck newDeckId
    } |> TaskResult.getOk)

[<Fact>]
let ``CollectCards works``(): Task<unit> = task {
    use c = new TestContainer()
    
    let authorId = 3
    
    let s1 = 1
    let b1 = 1
    let ci1_1 = 1001
    let! _ = FacetRepositoryTests.addBasicStack c.Db authorId []
    Assert.Equal(1, c.Db.Stack.Single().Users)
    Assert.Equal(1, c.Db.BranchInstance.Single().Users)
    Assert.Equal(1, c.Db.Stack.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(1, c.Db.BranchInstance.Single(fun x -> x.Id = ci1_1).Users)
    Assert.Equal(1, c.Db.CollectedCard.Count())
    
    let s2 = 2
    let ci2_1 = 1002
    let! _ = FacetRepositoryTests.addReversedBasicStack c.Db authorId []
    Assert.Equal(1, c.Db.Stack.Single(fun x -> x.Id = s2).Users)
    Assert.Equal(1, c.Db.BranchInstance.Single(fun x -> x.Id = ci2_1).Users)
    Assert.Equal(3, c.Db.CollectedCard.Count())
    
    let collectorId = 1
    do! StackRepository.CollectCard c.Db collectorId ci1_1 |> TaskResult.getOk
    Assert.Equal(2, c.Db.Stack.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(2, c.Db.BranchInstance.Single(fun x -> x.Id = ci1_1).Users)
    Assert.Equal(4, c.Db.CollectedCard.Count())
    do! StackRepository.CollectCard c.Db collectorId ci2_1 |> TaskResult.getOk
    Assert.Equal(2, c.Db.Stack.Single(fun x -> x.Id = s2).Users)
    Assert.Equal(2, c.Db.BranchInstance.Single(fun x -> x.Id = ci2_1).Users)
    // misc
    Assert.Equal(2, c.Db.BranchInstance.Count())
    Assert.Equal(6, c.Db.CollectedCard.Count())
    Assert.Equal(2, c.Db.CollectedCard.Count(fun x -> x.BranchInstanceId = ci1_1));

    // update branch
    let! r = SanitizeStackRepository.getUpsert c.Db <| VUpdateBranchId b1
    let command =
        { r.Value with
            FieldValues = [].ToList()
            Kind = Update_BranchId_Title (b1, null)
        }
    let! branchId = SanitizeStackRepository.Update c.Db authorId [] command |> TaskResult.getOk
    let ci1_2 = 1003
    Assert.Equal(b1, branchId)
    Assert.Equal(2, c.Db.Stack.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(1, c.Db.BranchInstance.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.BranchInstance.Count())
    Assert.Equal(6, c.Db.CollectedCard.Count())
    Assert.Equal(1, c.Db.CollectedCard.Count(fun x -> x.BranchInstanceId = ci1_2))
    
    do! StackRepository.CollectCard c.Db collectorId ci1_2 |> TaskResult.getOk
    Assert.Equal(2, c.Db.Stack.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(2, c.Db.BranchInstance.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.BranchInstance.Count())
    Assert.Equal(6, c.Db.CollectedCard.Count())
    Assert.Equal(2, c.Db.CollectedCard.Count(fun x -> x.BranchInstanceId = ci1_2));

    let! cc = c.Db.CollectedCard.SingleAsync(fun x -> x.StackId = s1 && x.UserId = authorId)
    do! StackRepository.uncollectStack c.Db authorId cc.StackId |> TaskResult.getOk
    Assert.Equal(1, c.Db.Stack.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(1, c.Db.BranchInstance.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.BranchInstance.Count())
    Assert.Equal(5, c.Db.CollectedCard.Count())
    Assert.Equal(1, c.Db.CollectedCard.Count(fun x -> x.BranchInstanceId = ci1_2));

    let count = StackRepository.GetDueCount c.Db collectorId ""
    Assert.Equal(3, count)
    let count = StackRepository.GetDueCount c.Db authorId ""
    Assert.Equal(2, count)}

[<Fact>]
let ``SanitizeHistoryRepository.AddAndSaveAsync works``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3

    let! _ = FacetRepositoryTests.addReversedBasicStack c.Db userId []

    let! a = StackRepository.GetQuizBatch c.Db userId ""
    let getId (x: Result<QuizCard, string> seq) = x.First().Value.CollectedCardId
    do! SanitizeHistoryRepository.AddAndSaveAsync c.Db (getId a) Score.Easy DateTime.UtcNow (TimeSpan.FromDays(13.)) 0. (TimeSpan.FromSeconds 1.) (Interval <| TimeSpan.FromDays 13.)
    let! b = StackRepository.GetQuizBatch c.Db userId ""
    Assert.NotEqual(getId a, getId b)

    let count = StackRepository.GetDueCount c.Db userId ""
    Assert.Equal(1, count)

    // getHeatmap returns one for today
    let! actual = HistoryRepository.getHeatmap c.Db userId
    Assert.Equal(0, actual.DateCountLevels.Length % 7) // returns full weeks; not partial weeks
    Assert.Equal(
        {   Date = DateTime.UtcNow.Date
            Count = 1
            Level = 10 },
        actual.DateCountLevels.Single(fun x -> x.Count <> 0)
    )}

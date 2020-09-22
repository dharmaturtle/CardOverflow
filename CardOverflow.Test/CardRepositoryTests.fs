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
open NodaTime

[<Fact>]
let ``StackRepository.deleteCard works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    let branchId = branch_1
    Assert.Equal(branchId, actualBranchId)
    let getCollected () = StackRepository.GetCollected c.Db userId stack_1
    let! (cc: Card ResizeArray) = getCollected ()
    let cc = cc.Single()

    do! StackRepository.uncollectStack c.Db userId cc.StackId
    Assert.Empty c.Db.Card

    let recollect () = StackRepository.CollectCard c.Db userId cc.LeafMeta.Id [card_1] |> TaskResult.getOk
    
    do! recollect ()
    let! (cc: Card ResizeArray) = getCollected ()
    let cc = cc.Single()
    do! FacetRepositoryTests.update c userId
            (VUpdate_BranchId branchId) id { ids_1 with LeafId = leaf_2 } branchId
    do! StackRepository.uncollectStack c.Db userId cc.StackId
    Assert.Empty c.Db.Card // still empty after editing then deleting

    let userId = user_3
    do! recollect ()
    let! (cc: Card ResizeArray) = getCollected ()
    let cc = cc.Single()
    let! (batch: Result<QuizCard, string> ResizeArray) = StackRepository.GetQuizBatch c.Db userId ""
    do! SanitizeHistoryRepository.AddAndSaveAsync c.Db (batch.First().Value.CardId) Score.Easy (DateTimeX.UtcNow) (Duration.FromDays(13.)) 0. (Duration.FromSeconds 1.) (IntervalXX <| Duration.FromDays 13.)
    do! SanitizeTagRepository.AddTo c.Db userId "tag" cc.StackId |> TaskResult.getOk
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId [] (stack_3, branch_3, leaf_3, [card_3])
    let newCardBranchId = branch_3
    Assert.Equal(newCardBranchId, actualBranchId)
    let! (stack2: StackEntity) = c.Db.Stack.SingleOrDefaultAsync(fun x -> x.Id <> cc.StackId)
    let stack2 = stack2.Id
    let addRelationshipCommand =
        {   Name = "my relationship"
            SourceStackId = stack_1
            TargetStackLink = string stack2
        }
    do! SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand
    Assert.NotEmpty c.Db.Card
    Assert.NotEmpty c.Db.Relationship_Card
    Assert.NotEmpty c.Db.History
    Assert.NotEmpty c.Db.Tag_Card
    do! StackRepository.uncollectStack c.Db userId cc.StackId // can delete after adding a history, tag, and relationship
    Assert.Equal(stack2, c.Db.Card.Include(fun x -> x.Leaf).Single().Leaf.StackId) // from the other side of the relationship
    Assert.Empty c.Db.Relationship_Card
    Assert.Empty c.Db.Tag_Card

    // but history remains
    Assert.NotEmpty c.Db.History
    
    // Error when deleting something you don't own
    do! recollect ()
    let otherUserId = user_2
    let! (x: Result<_, _>) = StackRepository.uncollectStack c.Db otherUserId cc.StackId
    Assert.equal (sprintf "You don't have any cards with Stack #%A" stack_1) x.error
    } |> TaskResult.getOk)

[<Fact>]
let ``StackRepository.editState works``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    let branchId = branch_1
    Assert.Equal(branchId, actualBranchId.Value)
    let! cc = StackRepository.GetCollected c.Db userId stack_1
    let cc = cc.Value.Single()
    
    let! x = StackRepository.editState c.Db userId cc.CardId CardState.Suspended
    Assert.Null x.Value
    let! cc = StackRepository.GetCollected c.Db userId cc.StackId
    let cc = cc.Value.Single()
    Assert.Equal(cc.CardState, CardState.Suspended)

    do! FacetRepositoryTests.update c userId
            (VUpdate_BranchId branchId) id { ids_1 with LeafId = leaf_2 } branchId
        |> TaskResult.getOk
    let! cc = StackRepository.GetCollected c.Db userId cc.StackId
    let cc = cc.Value.Single()
    Assert.Equal(cc.CardState, CardState.Suspended) // still suspended after edit

    let otherUserId = user_2 // other users can't edit card state
    let! x = StackRepository.editState c.Db otherUserId cc.CardId CardState.Suspended
    Assert.Equal("You don't own that card.", x.error)
    }

[<Fact>]
let ``Users can't collect multiple leafs of a card``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    let stackId = stack_1
    let branchId = branch_1
    Assert.Equal(branchId, actualBranchId.Value)
    do! FacetRepositoryTests.update c userId
            (VUpdate_BranchId branchId) id { ids_1 with LeafId = leaf_2 } branchId
        |> TaskResult.getOk
    let i2 = leaf_2
    let! _ = StackRepository.CollectCard c.Db userId i2 [ card_1 ] |> TaskResult.getOk // collecting a different revision of a card doesn't create a new Card; it only swaps out the LeafId
    Assert.Equal(i2, c.Db.Card.Single().LeafId)
    Assert.Equal(branchId, c.Db.Card.Single().BranchId)
    Assert.Equal(stackId, c.Db.Card.Single().StackId)
    
    use db = c.Db
    db.Card.AddI <|
        CardEntity(
            StackId = stackId,
            BranchId = branchId,
            LeafId = i2,
            Due = DateTimeX.UtcNow,
            UserId = userId,
            CardSettingId = userId)
    let ex = Assert.Throws<DbUpdateException>(fun () -> db.SaveChanges() |> ignore)
    Assert.Equal(
        "23505: duplicate key value violates unique constraint \"card. user_id, leaf_id, index. uq idx\"",
        ex.InnerException.Message)

    let i1 = leaf_1
    use db = c.Db
    db.Card.AddI <|
        CardEntity(
            Id = card_3,
            StackId = stackId,
            BranchId = branchId,
            LeafId = i1,
            Due = DateTimeX.UtcNow,
            UserId = userId,
            CardSettingId = setting_3,
            DeckId = deck_3)
    let ex = Assert.Throws<Npgsql.PostgresException>(fun () -> db.SaveChanges() |> ignore)
    Assert.Equal(
        (sprintf "P0001: UserId #%A with Card #%A and Stack #%A tried to have LeafId #%A, but they already have LeafId #%A" user_3 card_3 stack_1 leaf_1 leaf_2),
        ex.Message)
    }

[<Fact>]
let ``collect works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = user_3
    do! FacetRepositoryTests.addBasicStack c.Db authorId [] (stack_1, branch_1, leaf_1, [card_1])
    let branchId = branch_1
    let leafId = leaf_1
    let stackId = stack_1
    let collectorId = user_1
    let collectorDefaultDeckId = deck_1
    let collect x ccId = StackRepository.collect c.Db collectorId leafId x [ ccId ]
    let assertDeck deckId =
        StackRepository.GetCollected c.Db collectorId stackId
        |>%% Assert.Single
        |>%% fun x -> x.DeckId
        |>%% Assert.equal deckId

    let! ccId = collect None card_2
    
    Assert.areEquivalent [card_2] ccId
    do! assertDeck collectorDefaultDeckId

    // fails for author's deck
    do! StackRepository.uncollectStack c.Db collectorId stackId
    
    let! (error: Result<_,_>) = collect (Some deck_3) Ulid.create
    
    Assert.equal (sprintf "Either Deck #%A doesn't exist or it doesn't belong to you." deck_3) error.error
    
    // fails for nonexisting deck
    let nonexistant = Ulid.create
    let! (error: Result<_,_>) = collect (Some nonexistant) Ulid.create
    
    Assert.equal (sprintf "Either Deck #%A doesn't exist or it doesn't belong to you." nonexistant) error.error
    
    // fails for empty list of cardIds
    let! (error: Result<_,_>) = StackRepository.collect c.Db collectorId leafId None []
    
    Assert.equal (sprintf "Leaf#%A requires 1 card id(s). You provided 0." leafId) error.error
    
    // works for nondefault deck
    let newDeckId = Ulid.create
    do! SanitizeDeckRepository.create c.Db collectorId (Guid.NewGuid().ToString()) newDeckId

    let! ccId = collect (Some newDeckId) card_3
    
    Assert.areEquivalent [card_3] ccId
    do! assertDeck newDeckId

    // collecting/updating to *new* leaf doesn't change deckId or cardId
    let! stackCommand = SanitizeStackRepository.getUpsert c.Db authorId (VUpdate_BranchId branchId) {
        StackId = stack_1
        BranchId = branch_1
        LeafId = leaf_2
        CardIds = [card_1]
    }
    let! _ = SanitizeStackRepository.Update c.Db authorId [] stackCommand
    Assert.equal card_1 <| c.Db.Card.Single(fun x -> x.LeafId = leaf_2).Id

    let! cardId = StackRepository.collect c.Db collectorId leaf_2 None [card_3]

    Assert.areEquivalent [card_3] cardId
    do! assertDeck newDeckId

    // collecting/updating to *old* leaf doesn't change deckId or ccId
    let! cardId = StackRepository.collect c.Db collectorId leaf_1 None [card_3]

    Assert.areEquivalent [card_3] cardId
    do! assertDeck newDeckId
    } |> TaskResult.getOk)

[<Fact>]
let ``CollectCards works``(): Task<unit> = task {
    use c = new TestContainer()
    
    let authorId = user_3
    
    let s1 = stack_1
    let b1 = branch_1
    let ci1_1 = leaf_1
    let! _ = FacetRepositoryTests.addBasicStack c.Db authorId [] (stack_1, branch_1, leaf_1, [card_1])
    Assert.Equal(1, c.Db.Stack.Single().Users)
    Assert.Equal(1, c.Db.Leaf.Single().Users)
    Assert.Equal(1, c.Db.Stack.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(1, c.Db.Leaf.Single(fun x -> x.Id = ci1_1).Users)
    Assert.Equal(1, c.Db.Card.Count())
    
    let s2 = stack_2
    let ci2_1 = leaf_2
    let! _ = FacetRepositoryTests.addReversedBasicStack c.Db authorId [] (stack_2, branch_2, leaf_2, [card_2; card_3])
    Assert.Equal(1, c.Db.Stack.Single(fun x -> x.Id = s2).Users)
    Assert.Equal(1, c.Db.Leaf.Single(fun x -> x.Id = ci2_1).Users)
    Assert.Equal(3, c.Db.Card.Count())
    
    let collectorId = user_1
    let! _ = StackRepository.CollectCard c.Db collectorId ci1_1 [card_ 4] |> TaskResult.getOk
    Assert.Equal(2, c.Db.Stack.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(2, c.Db.Leaf.Single(fun x -> x.Id = ci1_1).Users)
    Assert.Equal(4, c.Db.Card.Count())
    let! _ = StackRepository.CollectCard c.Db collectorId ci2_1 [Ulid.create; Ulid.create] |> TaskResult.getOk
    Assert.Equal(2, c.Db.Stack.Single(fun x -> x.Id = s2).Users)
    Assert.Equal(2, c.Db.Leaf.Single(fun x -> x.Id = ci2_1).Users)
    // misc
    Assert.Equal(2, c.Db.Leaf.Count())
    Assert.Equal(6, c.Db.Card.Count())
    Assert.Equal(2, c.Db.Card.Count(fun x -> x.LeafId = ci1_1));

    // update branch
    let! r = SanitizeStackRepository.getUpsert c.Db authorId (VUpdate_BranchId b1) ids_1
    let ci1_2 = leaf_3
    let command =
        { r.Value with
            FieldValues = [].ToList()
            Kind = NewLeaf_Title null
            Ids = {
                r.Value.Ids with
                    LeafId = ci1_2
            }
        }
    let! branchId = SanitizeStackRepository.Update c.Db authorId [] command |> TaskResult.getOk
    Assert.Equal(b1, branchId)
    Assert.Equal(2, c.Db.Stack.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(1, c.Db.Leaf.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.Leaf.Count())
    Assert.Equal(6, c.Db.Card.Count())
    Assert.Equal(1, c.Db.Card.Count(fun x -> x.LeafId = ci1_2))
    
    let! _ = StackRepository.CollectCard c.Db collectorId ci1_2 [ card_ 4 ] |> TaskResult.getOk
    Assert.Equal(2, c.Db.Stack.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(2, c.Db.Leaf.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.Leaf.Count())
    Assert.Equal(6, c.Db.Card.Count())
    Assert.Equal(2, c.Db.Card.Count(fun x -> x.LeafId = ci1_2));

    let! cc = c.Db.Card.SingleAsync(fun x -> x.StackId = s1 && x.UserId = authorId)
    do! StackRepository.uncollectStack c.Db authorId cc.StackId |> TaskResult.getOk
    Assert.Equal(1, c.Db.Stack.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(1, c.Db.Leaf.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.Leaf.Count())
    Assert.Equal(5, c.Db.Card.Count())
    Assert.Equal(1, c.Db.Card.Count(fun x -> x.LeafId = ci1_2));

    let count = StackRepository.GetDueCount c.Db collectorId ""
    Assert.Equal(3, count)
    let count = StackRepository.GetDueCount c.Db authorId ""
    Assert.Equal(2, count)}

[<Fact>]
let ``SanitizeHistoryRepository.AddAndSaveAsync works``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3

    let! _ = FacetRepositoryTests.addReversedBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1; card_2])

    let! a = StackRepository.GetQuizBatch c.Db userId ""
    let getId (x: Result<QuizCard, string> seq) = x.First().Value.CardId
    do! SanitizeHistoryRepository.AddAndSaveAsync c.Db (getId a) Score.Easy DateTimeX.UtcNow (Duration.FromDays(13.)) 0. (Duration.FromSeconds 1.) (IntervalXX <| Duration.FromDays 13.)
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

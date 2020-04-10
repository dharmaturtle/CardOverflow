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

[<Fact>]
let ``CardRepository.deleteAcquired works``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! x = FacetRepositoryTests.addBasicCard c.Db userId []
    Assert.Empty x
    let! template = SanitizeTemplate.AllInstances c.Db 1
    let getAcquired () = task { return! CardRepository.GetAcquired c.Db userId 1 }
    let! ac = getAcquired ()

    let! x = CardRepository.deleteAcquired c.Db userId ac.Value.AcquiredCardId
    Assert.Null x.Value
    Assert.Empty c.Db.AcquiredCard

    let reacquire () = task { do! CardRepository.AcquireCardAsync c.Db userId ac.Value.CardInstanceMeta.Id }
    
    do! reacquire ()
    let! ac = getAcquired ()
    let! x =
        {   EditCardCommand.EditSummary = ""
            FieldValues = [].ToList()
            TemplateInstance = template.Value.Instances.Single() |> ViewTemplateInstance.copyTo
            ParentId = None
        } |> CardRepository.UpdateFieldsToNewInstance c.Db ac.Value
    Assert.Empty x.Value
    let! x = CardRepository.deleteAcquired c.Db userId ac.Value.AcquiredCardId
    Assert.Null x.Value
    Assert.Empty c.Db.AcquiredCard // still empty after editing then deleting

    let userId = 3
    do! reacquire ()
    let! ac = getAcquired ()
    let ac = ac.Value
    let! batch = CardRepository.GetQuizBatch c.Db userId ""
    do! SanitizeHistoryRepository.AddAndSaveAsync c.Db (batch.First().Value.AcquiredCardId) Score.Easy DateTime.UtcNow (TimeSpan.FromDays(13.)) 0. (TimeSpan.FromSeconds 1.) (Interval <| TimeSpan.FromDays 13.)
    do! TagRepository.AddTo c.Db "tag" ac.AcquiredCardId
    let! x = FacetRepositoryTests.addBasicCard c.Db userId []
    Assert.Empty x
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
    Assert.Equal(card2, c.Db.AcquiredCard.Include(fun x -> x.CardInstance).Single().CardInstance.CardId) // from the other side of the relationship
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
    let! x = FacetRepositoryTests.addBasicCard c.Db userId []
    Assert.Empty x
    let! template = SanitizeTemplate.AllInstances c.Db 1
    let! ac = CardRepository.GetAcquired c.Db userId 1
    
    let! x = CardRepository.editState c.Db userId ac.Value.AcquiredCardId CardState.Suspended
    Assert.Null x.Value
    let! ac = CardRepository.GetAcquired c.Db userId ac.Value.CardId
    Assert.Equal(ac.Value.CardState, CardState.Suspended)

    let! x = 
        {   EditCardCommand.EditSummary = ""
            FieldValues = [].ToList()
            TemplateInstance = template.Value.Instances.Single() |> ViewTemplateInstance.copyTo
            ParentId = None
        } |> CardRepository.UpdateFieldsToNewInstance c.Db ac.Value
    Assert.Empty x.Value
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
    let! x = FacetRepositoryTests.addBasicCard c.Db userId []
    Assert.Empty x
    let! template = SanitizeTemplate.AllInstances c.Db 1
    let! ac = CardRepository.GetAcquired c.Db userId 1
    let! x = 
        {   EditCardCommand.EditSummary = ""
            FieldValues = [].ToList()
            TemplateInstance = template.Value.Instances.Single() |> ViewTemplateInstance.copyTo
            ParentId = None
        } |> CardRepository.UpdateFieldsToNewInstance c.Db ac.Value
    Assert.Empty x.Value

    let i2 = 1002
    do! CardRepository.AcquireCardAsync c.Db userId i2 // acquiring a different revision of a card doesn't create a new AcquiredCard; it only swaps out the CardInstanceId
    Assert.Equal(i2, c.Db.AcquiredCard.Single().CardInstanceId)
    
    use db = c.Db
    db.AcquiredCard.AddI <|
        AcquiredCardEntity(
            CardInstanceId = i2,
            Due = DateTime.UtcNow,
            UserId = userId,
            CardSettingId = userId)
    let ex = Assert.Throws<DbUpdateException>(fun () -> db.SaveChanges() |> ignore)
    Assert.Equal(
        "Cannot insert duplicate key row in object 'dbo.AcquiredCard' with unique index 'IX_AcquiredCard_UserId_CardInstanceId'. The duplicate key value is (3, 1002).
The statement has been terminated.", 
        ex.InnerException.Message)

    let i1 = 1001
    use db = c.Db
    db.AcquiredCard.AddI <|
        AcquiredCardEntity(
            CardInstanceId = i1,
            Due = DateTime.UtcNow,
            UserId = userId,
            CardSettingId = userId)
    let ex = Assert.Throws<DbUpdateException>(fun () -> db.SaveChanges() |> ignore)
    Assert.Equal(
        "Cannot insert duplicate key row in object 'dbo.UserAndCard' with unique index 'IX_UserAndCard_UserId_CardId'. The duplicate key value is (3, 1).
The statement has been terminated.", 
        ex.InnerException.Message)
    }

[<Fact>]
let ``AcquireCards works``(): Task<unit> = task {
    use c = new TestContainer()
    
    let authorId = 3
    
    let c1 = 1
    let ci1_1 = 1001
    let! _ = FacetRepositoryTests.addBasicCard c.Db authorId []
    Assert.Equal(1, c.Db.Card.Single().Users)
    Assert.Equal(1, c.Db.CardInstance.Single().Users)
    Assert.Equal(1, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(1, c.Db.CardInstance.Single(fun x -> x.Id = ci1_1).Users)
    
    let c2 = 2
    let ci2_1 = 1002
    let! _ = FacetRepositoryTests.addReversedBasicCard c.Db authorId []
    Assert.Equal(1, c.Db.Card.Single(fun x -> x.Id = c2).Users)
    Assert.Equal(1, c.Db.CardInstance.Single(fun x -> x.Id = ci2_1).Users)
    
    let acquirerId = 1
    do! CardRepository.AcquireCardAsync c.Db acquirerId ci1_1
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(2, c.Db.CardInstance.Single(fun x -> x.Id = ci1_1).Users)
    do! CardRepository.AcquireCardAsync c.Db acquirerId ci2_1
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c2).Users)
    Assert.Equal(2, c.Db.CardInstance.Single(fun x -> x.Id = ci2_1).Users)
    // misc
    Assert.Equal(2, c.Db.CardInstance.Count())
    Assert.Equal(4, c.Db.AcquiredCard.Count())
    Assert.Equal(2, c.Db.AcquiredCard.Count(fun x -> x.CardInstanceId = ci1_1));

    let! ac = CardRepository.GetAcquired c.Db authorId c1
    let! v = SanitizeCardRepository.getEdit c.Db ci1_1
    let v = { v.Value with FieldValues = [].ToList() }
    let! x = CardRepository.UpdateFieldsToNewInstance c.Db ac.Value v.load
    Assert.Empty x.Value
    let ci1_2 = 1003
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(1, c.Db.CardInstance.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.CardInstance.Count())
    Assert.Equal(4, c.Db.AcquiredCard.Count())
    Assert.Equal(1, c.Db.AcquiredCard.Count(fun x -> x.CardInstanceId = ci1_2))
    
    do! CardRepository.AcquireCardAsync c.Db acquirerId ci1_2
    Assert.Equal(2, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(2, c.Db.CardInstance.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.CardInstance.Count())
    Assert.Equal(4, c.Db.AcquiredCard.Count())
    Assert.Equal(2, c.Db.AcquiredCard.Count(fun x -> x.CardInstanceId = ci1_2));

    do! CardRepository.UnacquireCardAsync c.Db ac.Value.AcquiredCardId
    Assert.Equal(1, c.Db.Card.Single(fun x -> x.Id = c1).Users)
    Assert.Equal(1, c.Db.CardInstance.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.CardInstance.Count())
    Assert.Equal(3, c.Db.AcquiredCard.Count())
    Assert.Equal(1, c.Db.AcquiredCard.Count(fun x -> x.CardInstanceId = ci1_2));

    let count = CardRepository.GetDueCount c.Db acquirerId ""
    Assert.Equal(2, count)
    let count = CardRepository.GetDueCount c.Db authorId ""
    Assert.Equal(1, count)

    let! a = CardRepository.GetQuizBatch c.Db acquirerId ""
    let getId (x: Result<QuizCard, string> seq) = x.First().Value.AcquiredCardId
    do! SanitizeHistoryRepository.AddAndSaveAsync c.Db (getId a) Score.Easy DateTime.UtcNow (TimeSpan.FromDays(13.)) 0. (TimeSpan.FromSeconds 1.) (Interval <| TimeSpan.FromDays 13.)
    let! b = CardRepository.GetQuizBatch c.Db acquirerId ""
    Assert.NotEqual(getId a, getId b)

    let count = CardRepository.GetDueCount c.Db acquirerId ""
    Assert.Equal(1, count)

    // getHeatmap returns one for today
    let! actual = HistoryRepository.getHeatmap c.Db acquirerId
    Assert.Equal(0, actual.DateCountLevels.Length % 7) // returns full weeks; not partial weeks
    Assert.Equal(
        {   Date = DateTime.UtcNow.Date
            Count = 1
            Level = 10 },
        actual.DateCountLevels.Single(fun x -> x.Count <> 0)
    )}

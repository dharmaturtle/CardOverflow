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
open Npgsql
open Dapper.NodaTime

[<Fact>]
let ``ConceptRepository.deleteCard works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! actualExampleId = FacetRepositoryTests.addBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1])
    let exampleId = example_1
    Assert.Equal(exampleId, actualExampleId)
    let getCollected () = ConceptRepository.GetCollected c.Db userId concept_1
    let! (cc: Card ResizeArray) = getCollected ()
    let cc = cc.Single()

    do! ConceptRepository.uncollectConcept c.Db userId cc.ConceptId
    Assert.Empty c.Db.Card

    let recollect () = ConceptRepository.CollectCard c.Db userId cc.RevisionMeta.Id [card_1] |> TaskResult.getOk
    
    do! recollect ()
    let! (cc: Card ResizeArray) = getCollected ()
    let cc = cc.Single()
    do! FacetRepositoryTests.update c userId
            (VUpdate_ExampleId exampleId) id { ids_1 with RevisionId = revision_2 } exampleId
    do! ConceptRepository.uncollectConcept c.Db userId cc.ConceptId
    Assert.Empty c.Db.Card // still empty after editing then deleting

    let userId = user_3
    do! recollect ()
    let! (cc: Card ResizeArray) = getCollected ()
    let cc = cc.Single()
    let! (batch: Result<QuizCard, string> ResizeArray) = ConceptRepository.GetQuizBatch c.Db userId ""
    do! SanitizeHistoryRepository.AddAndSaveAsync c.Db (batch.First().Value.CardId) Score.Easy (DateTimeX.UtcNow) (Duration.FromDays(13.)) 0. (Duration.FromSeconds 1.) (IntervalXX <| Duration.FromDays 13.)
    do! SanitizeTagRepository.AddTo c.Db userId "tag" cc.ConceptId |> TaskResult.getOk
    let! actualExampleId = FacetRepositoryTests.addBasicConcept c.Db userId [] (concept_3, example_3, revision_3, [card_3])
    let newCardExampleId = example_3
    Assert.Equal(newCardExampleId, actualExampleId)
    let! (concept2: ConceptEntity) = c.Db.Concept.SingleOrDefaultAsync(fun x -> x.Id <> cc.ConceptId)
    let concept2 = concept2.Id
    let addRelationshipCommand =
        {   Name = "my relationship"
            SourceConceptId = concept_1
            TargetConceptLink = string concept2
        }
    do! SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand
    Assert.NotEmpty c.Db.Card
    Assert.NotEmpty c.Db.Relationship_Card
    Assert.NotEmpty c.Db.History
    Assert.NotEmpty <| c.Db.Card.ToList().SelectMany(fun x -> x.Tags :> IEnumerable<_>)
    do! ConceptRepository.uncollectConcept c.Db userId cc.ConceptId // can delete after adding a history, tag, and relationship
    Assert.Equal(concept2, c.Db.Card.Include(fun x -> x.Revision).Single().Revision.ConceptId) // from the other side of the relationship
    Assert.Empty c.Db.Relationship_Card
    Assert.Empty <| c.Db.Card.ToList().SelectMany(fun x -> x.Tags :> IEnumerable<_>)

    // but history remains
    Assert.NotEmpty c.Db.History
    
    // Error when deleting something you don't own
    do! recollect ()
    let otherUserId = user_2
    let! (x: Result<_, _>) = ConceptRepository.uncollectConcept c.Db otherUserId cc.ConceptId
    Assert.equal (sprintf "You don't have any cards with Concept #%A" concept_1) x.error
    } |> TaskResult.getOk)

[<Fact>]
let ``ConceptRepository.editState works``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let! actualExampleId = FacetRepositoryTests.addBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1])
    let exampleId = example_1
    Assert.Equal(exampleId, actualExampleId.Value)
    let! cc = ConceptRepository.GetCollected c.Db userId concept_1
    let cc = cc.Value.Single()
    
    let! x = ConceptRepository.editState c.Db userId cc.CardId CardState.Suspended
    Assert.Null x.Value
    let! cc = ConceptRepository.GetCollected c.Db userId cc.ConceptId
    let cc = cc.Value.Single()
    Assert.Equal(cc.CardState, CardState.Suspended)

    do! FacetRepositoryTests.update c userId
            (VUpdate_ExampleId exampleId) id { ids_1 with RevisionId = revision_2 } exampleId
        |> TaskResult.getOk
    let! cc = ConceptRepository.GetCollected c.Db userId cc.ConceptId
    let cc = cc.Value.Single()
    Assert.Equal(cc.CardState, CardState.Suspended) // still suspended after edit

    let otherUserId = user_2 // other users can't edit card state
    let! x = ConceptRepository.editState c.Db otherUserId cc.CardId CardState.Suspended
    Assert.Equal("You don't own that card.", x.error)
    }

[<Fact>]
let ``Users can't collect multiple revisions of a card``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let! actualExampleId = FacetRepositoryTests.addBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1])
    let conceptId = concept_1
    let exampleId = example_1
    Assert.Equal(exampleId, actualExampleId.Value)
    do! FacetRepositoryTests.update c userId
            (VUpdate_ExampleId exampleId) id { ids_1 with RevisionId = revision_2 } exampleId
        |> TaskResult.getOk
    let i2 = revision_2
    let! _ = ConceptRepository.CollectCard c.Db userId i2 [ card_1 ] |> TaskResult.getOk // collecting a different revision of a card doesn't create a new Card; it only swaps out the RevisionId
    Assert.Equal(i2, c.Db.Card.Single().RevisionId)
    Assert.Equal(exampleId, c.Db.Card.Single().ExampleId)
    Assert.Equal(conceptId, c.Db.Card.Single().ConceptId)
    
    use db = c.Db
    db.Card.AddI <|
        CardEntity(
            ConceptId = conceptId,
            ExampleId = exampleId,
            RevisionId = i2,
            Due = DateTimeX.UtcNow,
            UserId = userId,
            CardSettingId = userId)
    let ex = Assert.Throws<DbUpdateException>(fun () -> db.SaveChanges() |> ignore)
    Assert.Equal(
        "23505: duplicate key value violates unique constraint \"card. user_id, revision_id, index. uq idx\"",
        ex.InnerException.Message)

    let i1 = revision_1
    use db = c.Db
    db.Card.AddI <|
        CardEntity(
            Id = card_3,
            ConceptId = conceptId,
            ExampleId = exampleId,
            RevisionId = i1,
            Due = DateTimeX.UtcNow,
            UserId = userId,
            CardSettingId = setting_3,
            DeckId = deck_3)
    let ex = Assert.Throws<Npgsql.PostgresException>(fun () -> db.SaveChanges() |> ignore)
    Assert.Equal(
        (sprintf "P0001: UserId #%A with Card #%A and Concept #%A tried to have RevisionId #%A, but they already have RevisionId #%A" user_3 card_3 concept_1 revision_1 revision_2),
        ex.Message)
    }

[<Fact>]
let ``collect works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let authorId = user_3
    do! FacetRepositoryTests.addBasicConcept c.Db authorId [] (concept_1, example_1, revision_1, [card_1])
    let exampleId = example_1
    let revisionId = revision_1
    let conceptId = concept_1
    let collectorId = user_1
    let collectorDefaultDeckId = deck_1
    let collect x ccId = ConceptRepository.collect c.Db collectorId revisionId x [ ccId ]
    let assertDeck deckId =
        ConceptRepository.GetCollected c.Db collectorId conceptId
        |>%% Assert.Single
        |>%% fun x -> x.DeckId
        |>%% Assert.equal deckId

    let! ccId = collect None card_2
    
    Assert.areEquivalent [card_2] ccId
    do! assertDeck collectorDefaultDeckId

    // fails for author's deck
    do! ConceptRepository.uncollectConcept c.Db collectorId conceptId
    
    let! (error: Result<_,_>) = collect (Some deck_3) Ulid.create
    
    Assert.equal (sprintf "Either Deck #%A doesn't exist or it doesn't belong to you." deck_3) error.error
    
    // fails for nonexisting deck
    let nonexistant = Ulid.create
    let! (error: Result<_,_>) = collect (Some nonexistant) Ulid.create
    
    Assert.equal (sprintf "Either Deck #%A doesn't exist or it doesn't belong to you." nonexistant) error.error
    
    // fails for empty list of cardIds
    let! (error: Result<_,_>) = ConceptRepository.collect c.Db collectorId revisionId None []
    
    Assert.equal (sprintf "Revision#%A requires 1 card id(s). You provided 0." revisionId) error.error
    
    // works for nondefault deck
    let newDeckId = Ulid.create
    do! SanitizeDeckRepository.create c.Db collectorId (Guid.NewGuid().ToString()) newDeckId

    let! ccId = collect (Some newDeckId) card_3
    
    Assert.areEquivalent [card_3] ccId
    do! assertDeck newDeckId

    // collecting/updating to *new* revision doesn't change deckId or cardId
    let! conceptCommand = SanitizeConceptRepository.getUpsert c.Db authorId (VUpdate_ExampleId exampleId) {
        ConceptId = concept_1
        ExampleId = example_1
        RevisionId = revision_2
        CardIds = [card_1]
    }
    let! _ = SanitizeConceptRepository.Update c.Db authorId [] conceptCommand
    Assert.equal card_1 <| c.Db.Card.Single(fun x -> x.RevisionId = revision_2).Id

    let! cardId = ConceptRepository.collect c.Db collectorId revision_2 None [card_3]

    Assert.areEquivalent [card_3] cardId
    do! assertDeck newDeckId

    // collecting/updating to *old* revision doesn't change deckId or ccId
    let! cardId = ConceptRepository.collect c.Db collectorId revision_1 None [card_3]

    Assert.areEquivalent [card_3] cardId
    do! assertDeck newDeckId
    } |> TaskResult.getOk)

[<Fact>]
let ``CollectCards works``(): Task<unit> = task {
    use c = new TestContainer()
    
    let authorId = user_3
    
    let s1 = concept_1
    let b1 = example_1
    let ci1_1 = revision_1
    let! _ = FacetRepositoryTests.addBasicConcept c.Db authorId [] (concept_1, example_1, revision_1, [card_1])
    Assert.Equal(1, c.Db.Concept.Single().Users)
    Assert.Equal(1, c.Db.Revision.Single().Users)
    Assert.Equal(1, c.Db.Concept.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(1, c.Db.Revision.Single(fun x -> x.Id = ci1_1).Users)
    Assert.Equal(1, c.Db.Card.Count())
    
    let s2 = concept_2
    let ci2_1 = revision_2
    let! _ = FacetRepositoryTests.addReversedBasicConcept c.Db authorId [] (concept_2, example_2, revision_2, [card_2; card_3])
    Assert.Equal(1, c.Db.Concept.Single(fun x -> x.Id = s2).Users)
    Assert.Equal(1, c.Db.Revision.Single(fun x -> x.Id = ci2_1).Users)
    Assert.Equal(3, c.Db.Card.Count())
    
    let collectorId = user_1
    let! _ = ConceptRepository.CollectCard c.Db collectorId ci1_1 [card_ 4] |> TaskResult.getOk
    Assert.Equal(2, c.Db.Concept.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(2, c.Db.Revision.Single(fun x -> x.Id = ci1_1).Users)
    Assert.Equal(4, c.Db.Card.Count())
    let! _ = ConceptRepository.CollectCard c.Db collectorId ci2_1 [Ulid.create; Ulid.create] |> TaskResult.getOk
    Assert.Equal(2, c.Db.Concept.Single(fun x -> x.Id = s2).Users)
    Assert.Equal(2, c.Db.Revision.Single(fun x -> x.Id = ci2_1).Users)
    // misc
    Assert.Equal(2, c.Db.Revision.Count())
    Assert.Equal(6, c.Db.Card.Count())
    Assert.Equal(2, c.Db.Card.Count(fun x -> x.RevisionId = ci1_1));

    // update example
    let! r = SanitizeConceptRepository.getUpsert c.Db authorId (VUpdate_ExampleId b1) ids_1
    let ci1_2 = revision_3
    let command =
        { r.Value with
            FieldValues = [].ToList()
            Kind = NewRevision_Title null
            Ids = {
                r.Value.Ids with
                    RevisionId = ci1_2
            }
        }
    let! exampleId = SanitizeConceptRepository.Update c.Db authorId [] command |> TaskResult.getOk
    Assert.Equal(b1, exampleId)
    Assert.Equal(2, c.Db.Concept.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(1, c.Db.Revision.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.Revision.Count())
    Assert.Equal(6, c.Db.Card.Count())
    Assert.Equal(1, c.Db.Card.Count(fun x -> x.RevisionId = ci1_2))
    
    let! _ = ConceptRepository.CollectCard c.Db collectorId ci1_2 [ card_ 4 ] |> TaskResult.getOk
    Assert.Equal(2, c.Db.Concept.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(2, c.Db.Revision.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.Revision.Count())
    Assert.Equal(6, c.Db.Card.Count())
    Assert.Equal(2, c.Db.Card.Count(fun x -> x.RevisionId = ci1_2));

    let! cc = c.Db.Card.SingleAsync(fun x -> x.ConceptId = s1 && x.UserId = authorId)
    do! ConceptRepository.uncollectConcept c.Db authorId cc.ConceptId |> TaskResult.getOk
    Assert.Equal(1, c.Db.Concept.Single(fun x -> x.Id = s1).Users)
    Assert.Equal(1, c.Db.Revision.Single(fun x -> x.Id = ci1_2).Users)
    // misc
    Assert.Equal(3, c.Db.Revision.Count())
    Assert.Equal(5, c.Db.Card.Count())
    Assert.Equal(1, c.Db.Card.Count(fun x -> x.RevisionId = ci1_2));

    let count = ConceptRepository.GetDueCount c.Db collectorId ""
    Assert.Equal(3, count)
    let count = ConceptRepository.GetDueCount c.Db authorId ""
    Assert.Equal(2, count)}

[<Fact>]
let ``SanitizeHistoryRepository.AddAndSaveAsync works``(): Task<unit> = task {
    use c = new TestContainer()
    use! conn = c.Conn()
    let userId = user_3

    let! _ = FacetRepositoryTests.addReversedBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1; card_2])

    let! a = ConceptRepository.GetQuizBatch c.Db userId ""
    let getId (x: Result<QuizCard, string> seq) = x.First().Value.CardId
    do! SanitizeHistoryRepository.AddAndSaveAsync c.Db (getId a) Score.Easy DateTimeX.UtcNow (Duration.FromDays(13.)) 0. (Duration.FromSeconds 1.) (IntervalXX <| Duration.FromDays 13.)
    let! b = ConceptRepository.GetQuizBatch c.Db userId ""
    Assert.NotEqual(getId a, getId b)

    let count = ConceptRepository.GetDueCount c.Db userId ""
    Assert.Equal(1, count)

    // getHeatmap returns one for today
    let! actual = HistoryRepository.getHeatmap conn userId
    Assert.Equal(0, actual.DateCountLevels.Length % 7) // returns full weeks; not partial weeks
    let zone = DateTimeZoneProviders.Tzdb.["America/Chicago"] // highTODO support other timezones
    Assert.Equal(
        {   Date = DateTimeX.UtcNow.InZone(zone).Date
            Count = 1
            Level = 10 },
        actual.DateCountLevels.Single(fun x -> x.Count <> 0)
    )}

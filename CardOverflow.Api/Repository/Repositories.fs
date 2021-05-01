namespace CardOverflow.Api

open CardOverflow.Pure
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open System.Security.Cryptography
open System
open LoadersAndCopiers
open CardOverflow.Pure
open CardOverflow.Debug
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open System.Linq
open Helpers
open FSharp.Control.Tasks
open System.Collections.Generic
open X.PagedList
open System.Threading.Tasks
open Microsoft.FSharp.Core
open NeoSmart.Utils
open System.IO
open System
open System.Runtime.ExceptionServices
open System.Runtime.CompilerServices
open NUlid
open NodaTime
open Dapper
open Npgsql
open NodaTime.Text

module FeedbackRepository =
    let addAndSaveAsync (db: CardOverflowDb) userId title description priority =
        FeedbackEntity(
            Title = title,
            Description = description,
            UserId = userId,
            Priority = priority
        ) |> db.Feedback.AddI
        db.SaveChangesAsyncI()

module RelationshipRepository =
    let addAndSaveAsync (db: CardOverflowDb) (j: Relationship_CardEntity) =
        db.Relationship_Card.AddI j
        db.SaveChangesAsyncI ()
    let removeAndSaveAsync (db: CardOverflowDb) sourceConceptId targetConceptId userId name =
        db.Relationship_Card.SingleOrDefault(fun x ->
            x.SourceCard.ConceptId = sourceConceptId &&
            x.TargetCard.ConceptId = targetConceptId &&
            x.SourceCard.UserId = userId &&
            x.TargetCard.UserId = userId &&
            x.Relationship.Name = name
        ) |> function
        | null ->
            sprintf "Relationship not found between source Concept #%A and target Concept #%A with name \"%s\"." sourceConceptId targetConceptId name |> Error |> Task.FromResult
        | x ->
            db.Relationship_Card.RemoveI x
            db.SaveChangesAsyncI ()
            |> Task.map(fun () -> Ok())

module CommentRepository =
    let addAndSaveAsync (db: CardOverflowDb) (comment: CommentConceptEntity) =
        db.CommentConcept.AddI comment
        db.SaveChangesAsyncI ()

module TemplateRepository =
    let latest (db: CardOverflowDb) templateId =
        db.LatestTemplateRevision
            .SingleOrDefaultAsync(fun x -> x.TemplateId = templateId)
        |> Task.map (Result.requireNotNull <| sprintf "Template #%A not found" templateId)
        |> TaskResult.map TemplateRevision.load
    let revision (db: CardOverflowDb) revisionId =
        db.TemplateRevision
            .SingleOrDefaultAsync(fun x -> x.Id = revisionId)
        |> Task.map (Result.requireNotNull <| sprintf "Template Revision #%A not found" revisionId)
        |> TaskResult.map TemplateRevision.load
    let UpdateFieldsToNewRevision (db: CardOverflowDb) userId template (revision: TemplateRevision) = task {
        let newTemplateRevision = revision.CopyToNewRevision
        newTemplateRevision.Template <- template
        db.TemplateRevision.AddI newTemplateRevision
        db  
            .Card
            .Include(fun x -> x.Revision)
            .Where(fun x -> x.Revision.TemplateRevision.TemplateId = revision.TemplateId)
            |> Seq.iter(fun cc ->
                db.Entry(cc.Revision).State <- EntityState.Added
                cc.Revision.Id <- Ulid.create
                cc.Revision.TemplateRevision <- newTemplateRevision
            )
        let! existing = db.User_TemplateRevision.Where(fun x -> x.UserId = userId && x.TemplateRevision.TemplateId = newTemplateRevision.TemplateId).ToListAsync()
        db.User_TemplateRevision.RemoveRange existing
        User_TemplateRevisionEntity(UserId = userId, TemplateRevision = newTemplateRevision, DefaultCardSettingId = Guid.Parse("00000000-0000-0000-0000-5e7700000002")) // lowTODO do we ever use the card setting here?
        |> db.User_TemplateRevision.AddI
        return! db.SaveChangesAsyncI()
        }

module HistoryRepository =
    let getHeatmap (conn: NpgsqlConnection) userId = task {
        let oneYearishAgo = DateTimeX.UtcNow - Duration.FromDays (53. * 7. - 1.) // always show full weeks of slightly more than a year; -1 is from allDateCounts being inclusive
        let query = """
            SELECT
            	date_trunc('day', h.created AT TIME ZONE 'America/Chicago') AS date, -- highTODO support other timezones
            	COUNT(*)
            FROM history AS h
            WHERE
            	h.created >= @yearishago
            	AND h.user_id = @userid
            GROUP BY date
        """
        let! dateCounts = conn.QueryAsync<DateCount>(query, {| yearishago = oneYearishAgo; userid = userId |})
        let zone = DateTimeZoneProviders.Tzdb.["America/Chicago"] // highTODO support other timezones
        return Heatmap.get
            (oneYearishAgo.InZone(zone).Date)
            (DateTimeX.UtcNow.InZone(zone).Date)
            (dateCounts |> List.ofSeq) }

module ExploreConceptRepository =
    let getCollectedIds (db: CardOverflowDb) userId conceptId =
            db.Card
                .Include(fun x -> x.Concept.Examples :> IEnumerable<_>)
                    .ThenInclude(fun (x: ExampleEntity) -> x.Latest)
                .Where(fun x -> x.UserId = userId && x.ConceptId = conceptId)
                .Select(fun x -> x.ConceptId, x.ExampleId, x.RevisionId, x.Id)
                .ToListAsync()
            |>% Seq.toOption
            |>% Option.map (fun ids ->
                let conceptId, exampleId, revisionId = ids.Select(fun (a, b, c, _) -> (a, b, c)).Distinct().Single()
                let cardIds = ids.Select(fun (_, _, _, c) -> c) |> Seq.toList
                { ConceptId = conceptId; ExampleId = exampleId; RevisionId = revisionId; CardIds = cardIds})
    let get (db: CardOverflowDb) userId conceptId = taskResult {
        let! (r: ConceptEntity * array<string> * array<int> * List<string> * List<string>) =
            db.LatestDefaultRevision
                .Include(fun x -> x.Concept.Author)
                .Include(fun x -> x.Concept.Examples :> IEnumerable<_>)
                    .ThenInclude(fun (x: ExampleEntity) -> x.Latest.TemplateRevision)
                .Include(fun x -> x.Concept.Examples :> IEnumerable<_>)
                    .ThenInclude(fun (x: ExampleEntity) -> x.Author)
                .Include(fun x -> x.Concept.CommentConcepts :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentConceptEntity) -> x.User)
                .Include(fun x -> x.TemplateRevision)
                .Where(fun x -> x.ConceptId = conceptId)
                .Select(fun x ->
                    x.Concept,
                    x.Concept.Tags,
                    x.Concept.TagsCount,
                    x.Concept.Cards.Single(fun x -> x.UserId = userId).Relationship_CardSourceCards.Select(fun x -> x.Relationship.Name).ToList(),
                    x.Concept.Cards.Single(fun x -> x.UserId = userId).Relationship_CardTargetCards.Select(fun x -> x.Relationship.Name).ToList()
                ).SingleOrDefaultAsync()
        let! concept, t, tc, rs, rt = r |> Result.ofNullable (sprintf "Concept #%A not found" conceptId)
        let usersTags = Set.ofSeq t
        let viewTags =
            Seq.zip t tc |> Seq.map (fun (t, c) ->
                {   Name = t
                    Count = c
                    IsCollected = usersTags.Contains t
                }
            ) |> toResizeArray
        let! (rc: List<ConceptRelationshipCountEntity>) = db.ConceptRelationshipCount.Where(fun x -> x.ConceptId = conceptId).ToListAsync()
        let! collectedIds = getCollectedIds db userId conceptId
        return ExploreConcept.load concept collectedIds viewTags (Seq.append rs rt |> Set.ofSeq) rc
        }
    let revision (db: CardOverflowDb) userId revisionId = taskResult {
        let! (e: RevisionEntity) =
            db.Revision
                .Include(fun x -> x.Example.Concept.Author)
                .Include(fun x -> x.Example.Concept.CommentConcepts :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentConceptEntity) -> x.User)
                .Include(fun x -> x.TemplateRevision)
                .SingleOrDefaultAsync(fun x -> x.Id = revisionId)
            |> Task.map (Result.requireNotNull (sprintf "Example Revision #%A not found" revisionId))
        let! isCollected = db.Card.AnyAsync(fun x -> x.UserId = userId && x.RevisionId = revisionId)
        let! latest = get db userId e.ConceptId
        return RevisionMeta.load isCollected (e.Example.LatestId = e.Id) e, latest // lowTODO optimization, only send the old revision - the latest revision isn't used
    }
    let example (db: CardOverflowDb) userId exampleId = taskResult {
        let! (e: RevisionEntity) =
            db.LatestRevision
                .Include(fun x -> x.Example.Concept.Author)
                .Include(fun x -> x.Example.Concept.CommentConcepts :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentConceptEntity) -> x.User)
                .Include(fun x -> x.TemplateRevision)
                .SingleOrDefaultAsync(fun x -> x.ExampleId = exampleId)
            |> Task.map (Result.requireNotNull (sprintf "Example #%A not found" exampleId))
        let! isCollected = db.Card.AnyAsync(fun x -> x.UserId = userId && x.ExampleId = exampleId)
        let! latest = get db userId e.ConceptId
        return RevisionMeta.load isCollected (e.Example.LatestId = e.Id) e, latest // lowTODO optimization, only send the old revision - the latest revision isn't used
    }

module FileRepository =
    let get (db: CardOverflowDb) hash =
        let sha256 = UrlBase64.Decode hash
        db.File.SingleOrDefaultAsync(fun x -> x.Sha256 = sha256)
        |> Task.map (Result.requireNotNull ())
        |> TaskResult.map (fun x -> x.Data)

module ConceptViewRepository =
    let private getCollectedRevisionIds (db: CardOverflowDb) userId aId bId =
        if userId = Guid.Empty then
            [].ToListAsync()
        else 
            db.Card
                .Where(fun x -> x.UserId = userId)
                .Where(fun x -> x.RevisionId = aId || x.RevisionId = bId)
                .Select(fun x -> x.RevisionId)
                .ToListAsync()
    let revisionWithLatest (db: CardOverflowDb) a_revisionId userId = taskResult {
        let! (a: RevisionEntity) =
            db.Revision
                .Include(fun x -> x.TemplateRevision)
                .SingleOrDefaultAsync(fun x -> x.Id = a_revisionId)
            |> Task.map (Result.requireNotNull (sprintf "Example revision #%A not found" a_revisionId))
        let! (b: RevisionEntity) = // verylowTODO optimization try to get this from `a` above
            db.LatestDefaultRevision
                .Include(fun x -> x.TemplateRevision)
                .SingleAsync(fun x -> x.ConceptId = a.ConceptId)
        let! (collectedRevisionIds: Guid ResizeArray) = getCollectedRevisionIds db userId a_revisionId b.Id
        return
            RevisionView.load a,
            collectedRevisionIds.Contains a.Id,
            RevisionView.load b,
            collectedRevisionIds.Contains b.Id,
            b.Id
    }
    let revisionPair (db: CardOverflowDb) a_revisionId b_revisionId userId = taskResult {
        let! (revisions: RevisionEntity ResizeArray) =
            db.Revision
                .Include(fun x -> x.TemplateRevision)
                .Where(fun x -> x.Id = a_revisionId || x.Id = b_revisionId)
                .ToListAsync()
        let! a = Result.requireNotNull (sprintf "Example revision #%A not found" a_revisionId) <| revisions.SingleOrDefault(fun x -> x.Id = a_revisionId)
        let! b = Result.requireNotNull (sprintf "Example revision #%A not found" b_revisionId) <| revisions.SingleOrDefault(fun x -> x.Id = b_revisionId)
        let! (collectedRevisionIds: Guid ResizeArray) = getCollectedRevisionIds db userId a_revisionId b_revisionId
        return
            RevisionView.load a,
            collectedRevisionIds.Contains a.Id,
            RevisionView.load b,
            collectedRevisionIds.Contains b.Id
    }
    let revision (db: CardOverflowDb) revisionId = task {
        match!
            db.Revision
            .Include(fun x -> x.TemplateRevision)
            .SingleOrDefaultAsync(fun x -> x.Id = revisionId) with
        | null -> return Error <| sprintf "Example revision %A not found" revisionId
        | x -> return Ok <| RevisionView.load x
    }
    let get (db: CardOverflowDb) conceptId =
        db.LatestDefaultRevision
            .Include(fun x -> x.TemplateRevision)
            .SingleOrDefaultAsync(fun x -> x.ConceptId = conceptId)
        |> Task.map Ok
        |> TaskResult.bind (fun x -> Result.requireNotNull (sprintf "Concept #%A not found" conceptId) x |> Task.FromResult)
        |> TaskResult.map RevisionView.load

module CardRepository =
    let getCollected (db: CardOverflowDb) userId (testRevisionIds: Guid ResizeArray) =
        db.Card.Where(fun x -> testRevisionIds.Contains(x.RevisionId) && x.UserId = userId).Select(fun x -> x.RevisionId).ToListAsync()
    let getCollectedRevisionFromRevision (db: CardOverflowDb) userId (revisionId: Guid) =
        db.Card
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                x.RevisionId = revisionId ||
                x.Example.Revisions.Any(fun x -> x.Id = revisionId)
            )
            .Select(fun x -> x.RevisionId)
            .Distinct()
            .SingleOrDefaultAsync()
        |> Task.map (Result.requireNotEqualTo Guid.Empty <| sprintf "You don't have any cards with Example Revision #%A" revisionId)

module ConceptRepository =
    let uncollectConcept (db: CardOverflowDb) userId conceptId = taskResult {
        do! db.Card.Where(fun x -> x.ConceptId = conceptId && x.UserId = userId).ToListAsync()
            |> Task.map (Result.requireNotEmptyX <| sprintf "You don't have any cards with Concept #%A" conceptId)
            |> TaskResult.map db.Card.RemoveRange
        return! db.SaveChangesAsyncI()
    }
    let editState (db: CardOverflowDb) userId cardId (state: CardState) = taskResult {
        let! (cc: CardEntity) =
            db.Card.SingleOrDefaultAsync(fun x -> x.Id = cardId && x.UserId = userId)
            |> Task.map (Result.ofNullable "You don't own that card.")
        cc.CardState <- CardState.toDb state
        return! db.SaveChangesAsyncI()
    }
    let Revisions (db: CardOverflowDb) userId exampleId = taskResult {
        let! r =
            db.Example
                .Include(fun x -> x.Author)
                .Include(fun x -> x.Revisions :> IEnumerable<_>)
                    .ThenInclude(fun (x: RevisionEntity) -> x.TemplateRevision)
                .SingleOrDefaultAsync(fun x -> x.Id = exampleId)
            |> Task.map (Result.requireNotNull <| sprintf "ExampleId #%A not found" exampleId)
        let! collectedRevisionId =
            db.Card
                .Where(fun x -> x.UserId = userId && x.ExampleId = exampleId)
                .Select(fun x -> x.RevisionId)
                .Distinct()
                .SingleOrDefaultAsync()
        return ExampleRevision.load collectedRevisionId r
    }
    let collectConceptNoSave (db: CardOverflowDb) userId (revision: RevisionEntity) mayUpdate (cardIds: Guid list) = taskResult {
        let requiredLength = int revision.MaxIndexInclusive + 1
        do! cardIds.Length
            |> Result.requireEqualTo requiredLength
                (sprintf "Revision#%A requires %i card id(s). You provided %i." revision.Id requiredLength cardIds.Length)
        let! ((defaultCardSettingId, defaultDeckId): Guid * Guid) =
            db.User.Where(fun x -> x.Id = userId).Select(fun x ->
                x.DefaultCardSettingId,
                x.DefaultDeckId
            ).SingleAsync()
        let cardSansIndex index =
            Card.initialize
                (cardIds |> List.tryItem (int index) |> Option.defaultValue Guid.Empty) // the Guid.Empty is just to make the code compile. Eventually refactor
                userId
                defaultCardSettingId
                defaultDeckId
                []
            |> fun x -> x.copyToNew [||] index // medTODO get tags from template
        let new' =
            [0s .. revision.MaxIndexInclusive]
            |> List.map cardSansIndex
        let! (old': CardEntity list) = db.Card.Where(fun x -> x.UserId = userId && x.ConceptId = revision.ConceptId).ToListAsync() |>% Seq.toList
        for old in old' do
            match cardIds |> List.tryItem (int old.Index) with
            | Some given ->
                do! Result.requireEqual given old.Id (sprintf "Card ids don't match. Was given %A and expected %A" given old.Id)
            | _ -> ()
        return
            List.zipOn new' old' (fun new' old' -> new'.Index = old'.Index)
            |> List.map(
                function
                | None, Some old' ->
                    db.Card.RemoveI old' // highTODO add a warning on the UI that data will be lost
                    None
                | Some new', None ->
                    new'.Revision <- revision
                    new'.Example <- revision.Example
                    new'.Concept <- revision.Concept
                    new'.ConceptId <- revision.ConceptId
                    new'.RevisionId <- revision.Id
                    db.Card.AddI new'
                    Some new'
                | Some _, Some old' ->
                    if revision.ExampleId = old'.ExampleId || mayUpdate then
                        old'.ConceptId <- revision.ConceptId
                        old'.ExampleId <- revision.ExampleId
                        old'.RevisionId <- revision.Id
                        Some old'
                    else None
                | None, None -> failwith "impossible"
            ) |> ListOption.somes
    }
    let collect (db: CardOverflowDb) userId revisionId deckId (cardIds: Guid list) = taskResult {
        let! (revision: RevisionEntity) =
            db.Revision
                .Include(fun x -> x.Example.Concept)
                .SingleOrDefaultAsync(fun x -> x.Id = revisionId)
            |> Task.map (Result.requireNotNull <| sprintf "Example Revision #%A not found" revisionId)
        let! (ccs: CardEntity list) = collectConceptNoSave db userId revision true cardIds
        match deckId with
        | Some deckId ->
            do! db.Deck.AnyAsync(fun x -> x.Id = deckId && x.UserId = userId)
                |>% Result.requireTrue (sprintf "Either Deck #%A doesn't exist or it doesn't belong to you." deckId)
            ccs |> List.iter (fun cc -> cc.DeckId <- deckId)
        | None -> ()
        do! db.SaveChangesAsyncI ()
        return ccs |> List.map (fun x -> x.Id)
        }
    let CollectCard (db: CardOverflowDb) userId revisionId cardIds =
        collect db userId revisionId None cardIds
    let GetCollected (db: CardOverflowDb) (userId: Guid) (conceptId: Guid) = taskResult {
        let! (e: _ ResizeArray) =
            db.CardIsLatest
                .Include(fun x -> x.Revision.TemplateRevision)
                .Include(fun x -> x.Revision.Cards)
                .Where(fun x -> x.ConceptId = conceptId && x.UserId = userId)
                .Select(fun x ->
                    x,
                    x.Revision.Cards.Single(fun x -> x.UserId = userId).Tags
                ).ToListAsync()
        return!
            e.Select(fun (e, t) ->
                Card.load (Set.ofSeq t) e true
            ) |> Result.consolidate
            |> Result.map toResizeArray
        }
    let getNew (db: CardOverflowDb) userId = task {
        let! user = db.User.SingleAsync(fun x -> x.Id = userId)
        return Card.initialize Ulid.create userId user.DefaultCardSettingId user.DefaultDeckId []
        }
    let private searchCollected (db: CardOverflowDb) userId (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.Card
            .Include(fun x -> x.Revision.TemplateRevision)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                String.IsNullOrWhiteSpace searchTerm ||
                x.Revision.Tsv.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
            )
    let private searchCollectedIsLatest (db: CardOverflowDb) userId (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.CardIsLatest
            .Include(fun x -> x.Revision.TemplateRevision)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                String.IsNullOrWhiteSpace searchTerm ||
                x.Revision.Tsv.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
            )
    let private collectedByDeck (db: CardOverflowDb) deckId =
        db.Card
            .Include(fun x -> x.Revision.TemplateRevision)
            .Where(fun x -> x.DeckId = deckId)
    let GetCollectedPages (db: CardOverflowDb) (userId: Guid) (pageNumber: int) (searchTerm: string) =
        task {
            let! r =
                (searchCollectedIsLatest db userId searchTerm)
                    .ToPagedListAsync(pageNumber, 15)
            return {
                Results = r |> Seq.map (fun x -> Card.load (x.Tags |> Set.ofSeq) x true)
                Details = {
                    CurrentPage = r.PageNumber
                    PageCount = r.PageCount
                }
            }
        }
    let GetQuizBatch (db: CardOverflowDb) userId query =
        let tomorrow = DateTimeX.UtcNow + Duration.FromDays 1
        task {
            let! cards =
                (searchCollected db userId query)
                    .Where(fun x -> x.Due < tomorrow && x.CardState = CardState.toDb Normal)
                    .Include(fun x -> x.CardSetting)
                    .OrderBy(fun x -> x.Due)
                    .Take(5)
                    .ToListAsync()
            return
                cards |> Seq.map QuizCard.load |> toResizeArray
        }
    let GetQuizBatchDeck (db: CardOverflowDb) deckId =
        let tomorrow = DateTimeX.UtcNow + Duration.FromDays 1
        task {
            let! cards =
                (collectedByDeck db deckId)
                    .Where(fun x -> x.Due < tomorrow && x.CardState = CardState.toDb Normal)
                    .Include(fun x -> x.CardSetting)
                    .OrderBy(fun x -> x.Due)
                    .Take(5)
                    .ToListAsync()
            return
                cards |> Seq.map QuizCard.load |> toResizeArray
        }
    let GetDueCount (db: CardOverflowDb) userId query =
        let tomorrow = DateTimeX.UtcNow + Duration.FromDays 1
        (searchCollected db userId query)
            .Where(fun x -> x.Due < tomorrow && x.CardState = CardState.toDb Normal)
            .Count()
    let private searchExplore userId (pageNumber: int) (filteredRevisions: RevisionEntity IOrderedQueryable)=
        task {
            let! r =
                filteredRevisions.Select(fun x ->
                    x,
                    x.Cards.Any(fun x -> x.UserId = userId),
                    x.TemplateRevision, // .Include fails for some reason, so we have to manually select
                    x.Concept,
                    x.Concept.Author
                ).ToPagedListAsync(pageNumber, 15)
            let squashed =
                r |> List.ofSeq |> List.map (fun (c, isCollected, template, concept, author) ->
                    c.Concept <- concept
                    c.Concept.Author <- author
                    c.TemplateRevision <- template
                    c, isCollected
                )
            return {
                Results =
                    squashed |> List.map (fun (c, isCollected) ->
                        {   ExploreConceptSummary.Id = c.ConceptId
                            Author = c.Concept.Author.DisplayName
                            AuthorId = c.Concept.AuthorId
                            Users = c.Concept.Users
                            Revision = RevisionMeta.load isCollected true c
                        }
                    )
                Details = {
                    CurrentPage = r.PageNumber
                    PageCount = r.PageCount
                }
            }
        }
    let search (db: CardOverflowDb) userId (pageNumber: int) order (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.LatestDefaultRevision.Search(searchTerm, plain, wildcard, order)
        |> searchExplore userId pageNumber
    let searchDeck (db: CardOverflowDb) userId (pageNumber: int) order (searchTerm: string) deckId =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.Deck
            .Where(fun x -> x.Id = deckId && (x.IsPublic || x.UserId = userId))
            .SelectMany(fun x -> x.Cards.Select(fun x -> x.Revision))
            .Search(searchTerm, plain, wildcard, order)
        |> searchExplore userId pageNumber

module UpdateRepository =
    let concept (db: CardOverflowDb) userId (command: EditConceptCommand) =
        let exampleNameCheck exampleId name =
            db.Example.AnyAsync(fun b -> b.Id = exampleId && b.Concept.Examples.Any(fun b -> b.Name = name && b.Id <> exampleId)) // veryLowTODO make case insensitive
            |> Task.map (Result.requireFalse <| sprintf "The concept with Example #%A already has a Example named '%s'." exampleId name)
        let exampleNameCheckConceptId conceptId name =
            db.Concept.AnyAsync(fun s -> s.Id = conceptId && s.Examples.Any(fun b -> b.Name = name)) // veryLowTODO make case insensitive
            |> Task.map (Result.requireFalse <| sprintf "Concept #%A already has a Example named '%s'." conceptId name)
        //let templateRevisionId = FSharp.UMX.UMX.untag command.TemplateRevisionId
        let templateRevisionId = FSharp.UMX.UMX.untag (Guid.NewGuid())
        taskResult {
            let! template = db.TemplateRevision.SingleAsync(fun x -> x.Id = templateRevisionId)
            let template = TemplateRevision.load template
            let! (example: ExampleEntity) =
                match command.Kind with
                    | NewRevision_Title name ->
                        exampleNameCheck command.Ids.ExampleId name
                        |> TaskResult.bind (fun () ->
                            db.Example.Include(fun x -> x.Concept).SingleOrDefaultAsync(fun x -> x.Id = command.Ids.ExampleId && x.AuthorId = userId)
                            |> Task.map (Result.requireNotNull <| sprintf "Either Example #%A doesn't exist or you aren't its author" command.Ids.ExampleId)
                        )
                    | NewCopy_SourceRevisionId_TagIds (revisionId, _) ->
                        ExampleEntity(
                            Id = command.Ids.ExampleId,
                            AuthorId = userId,
                            Concept =
                                ConceptEntity(
                                    Id = command.Ids.ConceptId,
                                    AuthorId = userId,
                                    CopySourceId = Nullable revisionId
                                )) |> Ok |> Task.FromResult
                    | NewExample_Title name -> taskResult {
                        do! db.Concept.AnyAsync(fun x -> x.Id = command.Ids.ConceptId)
                            |>% Result.requireTrue (sprintf "Concept %A not found" command.Ids.ConceptId)
                        do! exampleNameCheckConceptId command.Ids.ConceptId name
                        let example =
                            ExampleEntity(
                                Id = command.Ids.ExampleId,
                                AuthorId = userId,
                                Name = name,
                                ConceptId = command.Ids.ConceptId)
                        db.Entry(example).State <- EntityState.Added
                        return example
                        }
                    | NewOriginal_TagIds _ ->
                        ExampleEntity(
                            Id = command.Ids.ExampleId,
                            AuthorId = userId,
                            Concept =
                                ConceptEntity(
                                    Id = command.Ids.ConceptId,
                                    AuthorId = userId
                                )) |> Ok |> Task.FromResult
            return (command.CardView template).CopyFieldsToNewRevision example command.EditSummary command.Ids.RevisionId
        }

module NotificationRepository =
    let get (db: CardOverflowDb) userId (pageNumber: int) = task {
        let! ns =
            db.ReceivedNotification
                .Where(fun x -> x.ReceiverId = userId)
                .Select(fun x ->
                    x.Notification,
                    x.Notification.Sender.DisplayName,
                    x.Notification.Concept.Cards.Where(fun x -> x.UserId = userId).ToList(),
                    x.Notification.Deck.Name,
                    x.Notification.Deck.DerivedDecks.SingleOrDefault(fun x -> x.UserId = userId),
                    x.Notification.Revision.MaxIndexInclusive
                ).ToPagedListAsync(pageNumber, 30)
        return {
            Results = ns |> Seq.map Notification.load
            Details = {
                CurrentPage = ns.PageNumber
                PageCount = ns.PageCount
            }
        }
    }
    let remove (db: CardOverflowDb) (userId: Guid) (notificationId: Guid) =
        FormattableStringFactory.Create("""SELECT public.fn_delete_received_notification({0},{1});""", notificationId, userId)
        |> db.Database.ExecuteSqlInterpolatedAsync
        |>% ignore

module CardSettingsRepository =
    //let defaultAnkiCardSettings =
    //    { Id = 0
    //      Name = "Default Anki Options"
    //      NewCardsSteps = [ TimeSpan.FromMinutes 1.; TimeSpan.FromMinutes 10. ]
    //      NewCardsMaxPerDay = int16 20
    //      NewCardsGraduatingInterval = TimeSpan.FromDays 1.
    //      NewCardsEasyInterval = TimeSpan.FromDays 4.
    //      NewCardsStartingEaseFactor = 2.5
    //      NewCardsBuryRelated = false
    //      MatureCardsMaxPerDay = int16 200
    //      MatureCardsEaseFactorEasyBonusFactor = 1.3
    //      MatureCardsIntervalFactor = 1.
    //      MatureCardsMaximumInterval = 36500. |> TimeSpanInt16.fromDays
    //      MatureCardsHardInterval = 1.2
    //      MatureCardsBuryRelated = false
    //      LapsedCardsSteps = [ TimeSpan.FromMinutes 10. ]
    //      LapsedCardsNewIntervalFactor = 0.
    //      LapsedCardsMinimumInterval = TimeSpan.FromDays 1.
    //      LapsedCardsLeechThreshold = byte 8
    //      ShowAnswerTimer = false
    //      AutomaticallyPlayAudio = false
    //      ReplayQuestionAudioOnAnswer = false }
    let getAll (db: CardOverflowDb) userId = task {
            let! user = db.User.SingleAsync(fun x -> x.Id = userId)
            let! r = db.CardSetting.Where(fun x -> x.UserId = userId).ToListAsync()
            return r |> Seq.map (fun o -> CardSetting.load (o.Id = user.DefaultCardSettingId) o)
        }

type Profile = {
    DisplayName: string
}

module UserRepository =
    let theCollectiveId = Guid.Parse("00000000-0000-0000-0000-000000000002")
    let defaultCloze = "Cloze"
    let create (db: CardOverflowDb) id displayName = task {
        let defaultSetting = (Guid.Empty |> CardSetting.newUserCardSettings).CopyToNew Guid.Empty
        defaultSetting |> db.CardSetting.AddI
        DeckEntity(Name = "Default Deck") |> db.Deck.AddI
        
        let! (nonClozeIds: Guid list) =
            db.LatestTemplateRevision
                .Where(fun x -> x.Name <> defaultCloze && x.Template.AuthorId = theCollectiveId)
                .Select(fun x -> x.Id)
                .ToListAsync() |> Task.map List.ofSeq
        let! oldestClozeId =
            db.TemplateRevision
                .Where(fun x -> x.Name = defaultCloze && x.Template.AuthorId = theCollectiveId)
                .OrderBy(fun x -> x.Created)
                .Select(fun x -> x.Id)
                .FirstAsync()
        UserEntity(
            Id = id,
            DisplayName = displayName,
            //Filters = [ FilterEntity ( Name = "All", Query = "" )].ToList(),
            User_TemplateRevisions =
                (oldestClozeId :: nonClozeIds)
                .Select(fun id -> User_TemplateRevisionEntity (TemplateRevisionId = id, DefaultCardSetting = defaultSetting ))
                .ToList()) |> db.User.AddI
        return! db.SaveChangesAsyncI () }
    let profile (db: CardOverflowDb) userId =
        db.User
            .Where(fun x -> x.Id = userId)
            .Select(fun x -> x.DisplayName)
            .SingleOrDefaultAsync()
        |>% Result.requireNotNull (sprintf "User %A doesn't exist" userId)
        |>%% fun x -> { DisplayName = x }
    let getSettings (db: CardOverflowDb) userId =
        db.User.SingleOrDefaultAsync(fun x -> x.Id = userId)
        |>% (fun x ->
            {   UserId = x.Id
                DisplayName = x.DisplayName
                DefaultCardSettingId = x.DefaultCardSettingId
                DefaultDeckId = x.DefaultDeckId
                ShowNextReviewTime = x.ShowNextReviewTime
                ShowRemainingCardCount = x.ShowRemainingCardCount
                StudyOrder = x.StudyOrder
                NextDayStartsAt = x.NextDayStartsAt |> LocalTime.toDuration
                LearnAheadLimit = x.LearnAheadLimit |> LocalTime.toDuration
                TimeboxTimeLimit = x.TimeboxTimeLimit |> LocalTime.toDuration
                IsNightMode = x.IsNightMode
                Created = x.Created
                Timezone = x.Timezone
            }
        )
    let setSettings (db: CardOverflowDb) userId (command: SetUserSetting._command) = taskResult {
        let! (deck: DeckEntity) = db.Deck.SingleOrDefaultAsync(fun x -> x.Id = command.DefaultDeckId) |>% Result.requireNotNull (sprintf "Deck %A doesn't exist" command.DefaultDeckId)
        do! Result.requireEqual deck.UserId userId (sprintf "Deck %A doesn't belong to User %A" command.DefaultDeckId userId)
        let! (cardSetting: CardSettingEntity) = db.CardSetting.SingleOrDefaultAsync(fun x -> x.Id = command.DefaultCardSettingId) |>% Result.requireNotNull (sprintf "Card setting %A doesn't exist" command.DefaultCardSettingId)
        do! Result.requireEqual cardSetting.UserId userId (sprintf "Card setting %A doesn't belong to User %A" command.DefaultCardSettingId userId)
        let! (user: UserEntity) = db.User.SingleAsync(fun x -> x.Id = userId)
        user.DisplayName <- command.DisplayName
        user.DefaultCardSettingId <- command.DefaultCardSettingId
        user.DefaultDeckId <- command.DefaultDeckId
        user.ShowNextReviewTime <- command.ShowNextReviewTime
        user.ShowRemainingCardCount <- command.ShowRemainingCardCount
        user.StudyOrder <- command.StudyOrder
        user.NextDayStartsAt <- command.NextDayStartsAt |> Duration.toLocalTime
        user.LearnAheadLimit <- command.LearnAheadLimit |> Duration.toLocalTime
        user.TimeboxTimeLimit <- command.TimeboxTimeLimit |> Duration.toLocalTime
        user.IsNightMode <- command.IsNightMode
        user.Timezone <- command.Timezone
        return! db.SaveChangesAsyncI()
    }

type TreeTag = {
    Id: string
    ParentId: string
    Name: string
    IsExpanded: bool
    HasChildren: bool
}

module TagRepository =
    let delimiter = '/'
    let (+/+) a b =
        match String.IsNullOrWhiteSpace a, String.IsNullOrWhiteSpace b with
        | true, true -> ""
        | false, true -> a
        | true, false -> b
        | false, false -> sprintf "%s%c%s" a delimiter b
    let splitRawtag (rawTag: string) = // returns parent, name
        let i = rawTag.LastIndexOf delimiter
        if i = -1 then
            "", rawTag
        else
            rawTag.Substring(0, i),
            rawTag.Substring(i + 1)
    let unfold rawTag =
        (rawTag, false) |> List.unfold (fun (rawTag, hasChildren) ->
            let parent, name = splitRawtag rawTag
            match name with
            | "" -> None
            | _ ->
                ((rawTag, hasChildren), (parent, true))
                |> Some
          )
    let parse rawTags =
        rawTags
        |> Seq.toList
        |> List.collect unfold
        |> List.groupBy fst
        |> ListPair.map2 (List.exists snd)
        |> List.sortBy fst
        |> List.map(fun (rawTag, hasChildren) ->
            let parent, name = splitRawtag rawTag
            {   Id = rawTag
                ParentId = parent
                Name = name
                IsExpanded = false
                HasChildren = hasChildren
            })
    let getAll (db: CardOverflowDb) userId =
        db.User
            .SingleAsync(fun x -> x.Id = userId)
        |> Task.map (fun x -> x.CardTags |> parse)

[<CLIMutable>]
type DeckWithFollowMeta =
    {   Id: Guid
        Name: string
        AuthorId: Guid
        AuthorName: string
        IsFollowed: bool
        FollowCount: int
        TsvRank: double }
type Follower = {
    Id: Guid
    DisplayName: string
}
module DeckRepository =
    let searchMany (db: CardOverflowDb) userId (input: string list) =
        let input = input |> List.map (fun x -> x.ToLower())
        db.Deck.Where(fun t -> input.Contains(t.Name.ToLower()) && t.UserId = userId)
    let getFollowers (db: CardOverflowDb) deckId =
        db.DeckFollower.Where(fun x -> x.DeckId = deckId && x.Deck.IsPublic).Select(fun x ->
            x.FollowerId,
            x.Follower.DisplayName
        ).ToListAsync()
        |>% Seq.map (fun (followerId, displayName) -> {
            Id = followerId
            DisplayName = displayName
        })
        |>% toResizeArray
    let getPublic (db: CardOverflowDb) userId authorId = task {
        let! r =
            db.Deck
                .Where(fun x -> x.IsPublic && x.UserId = authorId)
                .Select(fun x ->
                    x,
                    x.DeckFollowers.Any(fun x -> x.FollowerId = userId),
                    x.Followers,
                    x.UserId,
                    x.User.DisplayName
                )
                .ToListAsync()
        return
            r.Select(fun (deck, isFollowed, count, authorId, authorName) -> {
                Id = deck.Id
                Name = deck.Name
                AuthorId = authorId
                AuthorName = authorName
                IsFollowed = isFollowed
                FollowCount = count
                TsvRank = 0.
            }).ToList()
    }

module FilterRepository =
    let Create (db: CardOverflowDb) userId name query =
        FilterEntity(
            UserId = userId,
            Name = name,
            Query = query
        ) |> db.Filter.AddI
        db.SaveChangesAsyncI ()

    let Get (db: CardOverflowDb) userId =
        db.Filter.Where(fun d -> d.UserId = userId).ToListAsync()
        
    let Delete (db: CardOverflowDb) deck =
        db.Filter.RemoveI deck
        db.SaveChangesAsyncI ()

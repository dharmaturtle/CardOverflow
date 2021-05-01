namespace CardOverflow.Sanitation

open System
open CardOverflow.Pure
open System.Threading.Tasks
open LoadersAndCopiers
open FSharp.Control.Tasks
open System.Collections.Generic
open Microsoft.EntityFrameworkCore
open FsToolkit.ErrorHandling
open Microsoft.FSharp.Core.Operators.Checked
open System.Linq
open Helpers
open System
open CardOverflow.Debug
open CardOverflow.Pure
open CardOverflow.Api
open CardOverflow.Entity
open System.ComponentModel.DataAnnotations
open System.Text.RegularExpressions
open System.Runtime.InteropServices
open NodaTime
open Npgsql
open Dapper
open Domain.Infrastructure
open FSharp.UMX

[<CLIMutable>]
type ViewFilter = {
    Id: Guid
    UserId: Guid
    [<StringLength(128, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 128 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Name: string
    [<StringLength(256, ErrorMessage = "Query must be less than 256 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Query: string
} with
    member this.copyTo (e: FilterEntity) =
        e.UserId <- this.UserId
        e.Name <- this.Name
        e.Query <- this.Query
    member this.copyToNew =
        let e = FilterEntity()
        this.copyTo e
        e

module ViewFilter =
    let load (e: FilterEntity) = {
        Id = e.Id
        UserId = e.UserId
        Name = e.Name
        Query = e.Query
    }

type ViewFilterWithDue = {
    Id: Guid
    UserId: Guid
    [<StringLength(128, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 128 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Name: string
    [<StringLength(256, ErrorMessage = "Query must be less than 256 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Query: string
    Due: int
}

module ViewFilterWithDue =
    let load (db: CardOverflowDb) (e: FilterEntity) = {
        Id = e.Id
        UserId = e.UserId
        Name = e.Name
        Query = e.Query
        Due = ConceptRepository.GetDueCount db e.UserId e.Query
    }

module SanitizeFilterRepository =
    let UpsertAsync (db: CardOverflowDb) (deck: ViewFilter) = task {
        let! d = db.Filter.SingleOrDefaultAsync(fun x -> x.Id = deck.Id)
        let deck =
            match d with
            | null ->
                let d = deck.copyToNew
                db.Filter.AddI d
                d
            | d ->
                deck.copyTo d
                d
        do! db.SaveChangesAsyncI ()
        return deck.Id
        }
    let Delete (db: CardOverflowDb) userId (deck: ViewFilter) = task {
        let! d = db.Filter.SingleAsync(fun x -> x.Id = deck.Id)
        return!
            if d.UserId = userId
            then task{
                do! FilterRepository.Delete db d
                return Ok ()
            }
            else Error "That isn't your deck" |> Task.FromResult
    }
    let Get (db: CardOverflowDb) userId = task {
        let! r = FilterRepository.Get db userId
        return r |> Seq.map ViewFilter.load |> toResizeArray
    }
    let GetWithDue (db: CardOverflowDb) userId = task {
        let! r = FilterRepository.Get db userId
        return r |> Seq.map (ViewFilterWithDue.load db) |> toResizeArray
    }
        
[<CLIMutable>]
type CommentText = {
    [<StringLength(500, MinimumLength = 15, ErrorMessage = "Comment must be 15 - 500 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Text: string
}

module SanitizeCommentRepository =
    let AddAndSaveAsync (db: CardOverflowDb) (comment: string) conceptId userId = taskResult { // lowTODO add idempotency key
        let text = comment |> MappingTools.standardizeWhitespace
        do! if text.Length >= 15 then Ok () else Error "Comment must be 15 or more characters."
        return!
            CommentConceptEntity(
                ConceptId = conceptId,
                UserId = userId,
                Text = text
            ) |> CommentRepository.addAndSaveAsync db
        }

[<CLIMutable>]
type TagText = {
    [<StringLength(250, ErrorMessage = "Tag must be less than 250 characters.")>] // medTODO 250 needs to be tied to the DB max somehow
    Text: string
}

module SanitizeTagRepository =
    let max = 300
    let sanitize (tag: string) =
        Array.FindAll(tag.ToCharArray(), fun c ->
            Char.IsLetterOrDigit c
            || Char.IsWhiteSpace c
            || Char.IsPunctuation c
        ) |> String
        |> String.split TagRepository.delimiter
        |> Array.map (MappingTools.standardizeWhitespace >> MappingTools.toTitleCase)
        |> String.concat (string TagRepository.delimiter)
        |> String.truncate max
    let private getCollected (db: CardOverflowDb) userId conceptId =
        db.Card.Where(fun x -> x.UserId = userId && x.ConceptId = conceptId).ToListAsync()
        |>% Result.requireNotEmptyX (sprintf "User #%A doesn't have Concept #%A." userId conceptId)
    let AddTo (db: CardOverflowDb) userId (newTag: string) conceptId = taskResult {
        let newTag = sanitize newTag
        let! (ccs: CardEntity ResizeArray) = getCollected db userId conceptId
        for cc in ccs do
            do! cc.Tags.Select(fun x -> x.ToLower()).Contains(newTag.ToLower()) |> (Result.requireFalse <| sprintf "Concept #%A for User #%A already has tag \"%s\"" conceptId userId newTag)
            cc.Tags <- cc.Tags.Append(newTag).ToArray()
        return! db.SaveChangesAsyncI ()
    }
    let DeleteFrom db userId tag conceptId = taskResult {
        let tag = MappingTools.toTitleCase tag
        let! (ccs: CardEntity ResizeArray) = getCollected db userId conceptId
        for cc in ccs do
            do! cc.Tags.Contains tag |> (Result.requireTrue <| sprintf "Concept #%A for User #%A doesn't have the tag \"%s\"" conceptId userId tag)
            cc.Tags <- cc.Tags.Where(fun x -> x <> tag).ToArray()
        return! db.SaveChangesAsyncI()
    }

module SanitizeDeckRepository =
    let private deckBelongsTo (db: CardOverflowDb) userId deckId =
        db.Deck.AnyAsync(fun x -> x.Id = deckId && x.UserId = userId)
        |>% (Result.requireTrue <| sprintf "Either Deck #%A doesn't belong to you or it doesn't exist" deckId)
    let private tryGet (db: CardOverflowDb) userId deckId =
        db.Deck.SingleOrDefaultAsync(fun x -> x.Id = deckId && x.UserId = userId)
        |>% (Result.requireNotNull <| sprintf "Either Deck #%A doesn't belong to you or it doesn't exist" deckId)
    let private requireIsPublic (db: CardOverflowDb) deckId =
        db.Deck.AnyAsync(fun x -> x.Id = deckId && x.IsPublic)
        |>% Result.requireTrue (sprintf "Either Deck #%A doesn't exist or it isn't public." deckId)
    let private verifyVisible (db: CardOverflowDb) userId deckId =
        db.Deck.AnyAsync(fun x -> x.Id = deckId && (x.IsPublic || x.UserId = userId))
        |>% Result.requireTrue (sprintf "Either Deck #%A doesn't exist, or it isn't public, or you don't own it." deckId)
    let private validateName (db: CardOverflowDb) userId (deckName: string) = taskResult {
        let! deckName = deckName |> Result.requireNotNull "Deck name can't be empty"
        let deckName = deckName.Trim()
        do! if deckName.Length > 250 then Error <| sprintf "Deck name '%s' is too long. It must be less than 250 characters." deckName else Ok ()
        do! if deckName.Length < 1 then Error <| sprintf "Deck name '%s' is too short. It must be at least 1 character long." deckName else Ok ()
        do! db.Deck.AnyAsync(fun x -> x.Name = deckName && x.UserId = userId)
            |>% (Result.requireFalse <| sprintf "User #%A already has a Deck named '%s'" userId deckName)
    }
    let create (db: CardOverflowDb) userId (newDeck: string) deckId = taskResult {
        do! validateName db userId newDeck
        let deck = DeckEntity(Id = deckId, Name = newDeck, UserId = userId)
        db.Deck.AddI deck
        do! db.SaveChangesAsyncI()
    }
    let setSource (db: CardOverflowDb) userId deckId sourceDeckId = taskResult {
        match sourceDeckId with
        | Some sourceDeckId ->
            do! requireIsPublic db sourceDeckId
        | None -> ()
        let! (deck: DeckEntity) = tryGet db userId deckId
        deck.SourceId <- sourceDeckId |> Option.toNullable
        return! db.SaveChangesAsyncI()
    }
    let delete (db: CardOverflowDb) userId deckId = taskResult {
        do! deckBelongsTo db userId deckId
        let! defaultDeckId = db.User.Where(fun x -> x.Id = userId).Select(fun x -> x.DefaultDeckId).SingleAsync()
        do! deckId <> defaultDeckId |> Result.requireTrue "You can't delete your default deck. Make another deck default first."
        do! db.Card.Where(fun x -> x.DeckId = deckId).ToListAsync()
            |> TaskSeq.iter(fun cc -> cc.DeckId <- defaultDeckId)
        db.Remove<DeckEntity> deckId
        return! db.SaveChangesAsyncI()
    }
    let setDefault (db: CardOverflowDb) userId deckId = taskResult {
        do! deckBelongsTo db userId deckId
        let! (user: UserEntity) = db.User.SingleAsync(fun x -> x.Id = userId)
        user.DefaultDeckId <- deckId
        return! db.SaveChangesAsyncI()
    }
    let setIsPublic (db: CardOverflowDb) userId deckId isPublic = taskResult {
        let! (deck: DeckEntity) = tryGet db userId deckId
        deck.IsPublic <- isPublic
        return! db.SaveChangesAsyncI()
    }
    let rename (db: CardOverflowDb) userId deckId newName = taskResult {
        let! (deck: DeckEntity) = tryGet db userId deckId
        do! validateName db userId newName
        deck.Name <- newName
        return! db.SaveChangesAsyncI()
    }
    let switch (db: CardOverflowDb) userId deckId cardId = taskResult {
        do! deckBelongsTo db userId deckId
        let! (cc: CardEntity) =
            db.Card.SingleOrDefaultAsync(fun x -> x.Id = cardId && x.UserId = userId)
            |>% (Result.requireNotNull <| sprintf "Either Card #%A doesn't belong to you or it doesn't exist" cardId)
        cc.DeckId <- deckId
        return! db.SaveChangesAsyncI ()
    }
    let switchByIds (db: CardOverflowDb) userId deckId revisionId index = taskResult {
        do! deckBelongsTo db userId deckId
        let! (cc: CardEntity) =
            db.Card.SingleOrDefaultAsync(fun x -> x.RevisionId = revisionId && x.Index = index && x.UserId = userId)
            |>% (Result.requireNotNull <| sprintf "Either Revision #%A with Index #%i doesn't belong to you or it doesn't exist" revisionId index)
        cc.DeckId <- deckId
        return! db.SaveChangesAsyncI ()
    }
    let getQuizBatch (db: CardOverflowDb) userId deckId = taskResult {
        do! deckBelongsTo db userId deckId
        return! ConceptRepository.GetQuizBatchDeck db deckId
    }
    let get (db: CardOverflowDb) userId currentTime =
        db.User
            .Where(fun x -> x.Id = userId)
            .Select(fun x ->
                x.DefaultDeckId,
                x.Decks.Select(fun x ->
                    x,
                    x.Cards.Where(fun x ->
                        x.UserId = userId &&
                        x.Due < currentTime
                    ).Count(),
                    x.Cards.Count,
                    x.Source)
            ).SingleAsync()
        |>% (fun (defaultDeckId, deckCounts) ->
            deckCounts |> Seq.map(fun (deck, dueCount, allCount, source) -> {
                Id = deck.Id
                Name = deck.Name
                IsPublic = deck.IsPublic
                IsDefault = defaultDeckId = deck.Id
                AllCount = allCount
                DueCount = dueCount
                SourceDeck =
                    source |> Option.ofObj |> Option.map (fun s ->
                    {   Id = s.Id
                        Name = s.Name
                    })
            })  |> List.ofSeq)
    let getSimple (db: CardOverflowDb) userId =
        db.User
            .Where(fun x -> x.Id = userId)
            .Select(fun x ->
                x.DefaultDeckId,
                x.Decks
            ).SingleAsync()
        |>% (fun (defaultDeckId, decks) ->
            decks |> Seq.map(fun deck -> {
                Id = deck.Id
                IsDefault = defaultDeckId = deck.Id
                Name = deck.Name
            })  |> toResizeArray)
    let getDeckWithFollowMeta (db: CardOverflowDb) userId deckId =
        db.Deck
            .Where(fun x -> x.Id = deckId && (x.IsPublic || x.UserId = userId))
            .Select(fun x ->
                x,
                x.DeckFollowers.Any(fun x -> x.FollowerId = userId),
                x.Followers,
                x.UserId,
                x.User.DisplayName
            )
            .SingleOrDefaultAsync()
        |>% Result.ofNullable (sprintf "Either Deck #%A doesn't exist or it isn't public." deckId)
        |>%% fun (deck, isFollowed, count, authorId, authorName) -> {
                Id = deck.Id
                Name = deck.Name
                AuthorId = authorId
                AuthorName = authorName
                IsFollowed = isFollowed
                FollowCount = count
                TsvRank = 0.
            }
    type FollowDeckType =
        | NewDeck of Guid * string
        | OldDeck of Guid
        | NoDeck
    type FollowError =
        | RealError of string
        | EditExistingIsNull_RevisionIdsByDeckId of (Guid * ResizeArray<Guid>) ResizeArray
        with
            member this.TryRealError([<Out>] out: _ byref) =
                match this with
                | RealError x -> out <- x; true
                | _ -> false
            member this.GetRealError =
                match this with
                | RealError x -> x
                | _ -> failwith "Not a RealError"
            member this.TryEditExistingIsNull_RevisionIdsByDeckId([<Out>] out: _ byref) =
                match this with
                | EditExistingIsNull_RevisionIdsByDeckId x -> out <- x; true
                | _ -> false
    type ConceptRevisionIndex = {
        ConceptId: Guid
        ExampleId: Guid
        RevisionId: Guid
        Index: int16
    }
    let follow (db: CardOverflowDb) userId deckId followType notifyOfAnyNewChanges editExisting = taskResult {
        do! requireIsPublic db deckId |>% Result.mapError RealError
        if notifyOfAnyNewChanges then
            do! db.DeckFollower.AnyAsync(fun df -> df.DeckId = deckId && df.FollowerId = userId)
                |>% Result.requireFalse (sprintf "You're already following Deck #%A" deckId |> RealError)
            DeckFollowerEntity(DeckId = deckId, FollowerId = userId) |> db.DeckFollower.AddI
        match followType with
            | NoDeck -> ()
            | NewDeck _ | OldDeck _ ->
                let! (theirs: ResizeArray<ConceptRevisionIndex>) =
                    db.Card
                        .Where(fun cc -> cc.DeckId = deckId)
                        .Select(fun x -> {
                            ConceptId = x.ConceptId
                            ExampleId = x.ExampleId
                            RevisionId = x.RevisionId
                            Index = x.Index
                        })
                        .ToListAsync()
                let theirConceptIds = theirs.Select(fun x -> x.ConceptId).Distinct().ToList()
                let! (mine: CardEntity ResizeArray) =
                    db.Card
                        .Where(fun cc -> cc.UserId = userId && theirConceptIds.Contains cc.ConceptId)
                        .ToListAsync()
                let! theirs =
                    match mine.Any(), editExisting with
                    | false, _
                    | true, Some true -> Ok theirs
                    | true, None ->
                        mine.Select(fun x -> x.DeckId, x.RevisionId)
                            .GroupBy(fun (deckId, _) -> deckId)
                            .Select(fun x -> x.Key, x.Select(fun (_, revisionId) -> revisionId).Distinct().ToList())
                            .Where(fun (deckId, _) ->
                                match followType with
                                | OldDeck oldDeckId -> oldDeckId <> deckId
                                | _ -> true
                            )
                        |> Seq.toList
                        |> function
                        | [] -> Ok theirs
                        | grps ->
                            grps.ToList()
                            |> EditExistingIsNull_RevisionIdsByDeckId
                            |> Error
                    | true, Some false ->
                        theirs
                            .Where(fun t -> not <| mine.Any(fun mine -> mine.ConceptId = t.ConceptId && mine.Index = t.Index))
                            .ToList()
                        |> Ok
                let! defaultCardSettingId = db.User.Where(fun x -> x.Id = userId).Select(fun x -> x.DefaultCardSettingId).SingleAsync()
                let! newDeckId =
                    match followType with
                    | NewDeck (newDeckId, name) -> (taskResult {
                            do! create db userId name newDeckId
                            do! setSource db userId newDeckId (Some deckId)
                            return newDeckId
                        } |>% Result.mapError RealError)
                    | OldDeck id -> taskResult {
                        do! db.Deck.AnyAsync(fun d -> d.Id = id && d.UserId = userId)
                            |>% Result.requireTrue (sprintf "Either Deck #%A doesn't exist or it doesn't belong to you." id |> RealError)
                        return id
                        }
                    | _ -> failwith "impossible"
                let cardSansIndex index =
                    Card.initialize
                        Ulid.create
                        userId
                        defaultCardSettingId
                        newDeckId
                        []
                    |> fun x -> x.copyToNew [||] index
                List.zipOn
                    (theirs |> Seq.toList)
                    (mine |> Seq.toList)
                    (fun theirs mine -> mine.Index = theirs.Index && mine.ConceptId = theirs.ConceptId)
                |> List.iter
                    (function
                    | Some theirs, Some mine ->
                        mine.ConceptId <- theirs.ConceptId
                        mine.ExampleId <- theirs.ExampleId
                        mine.RevisionId <- theirs.RevisionId
                        mine.Index <- theirs.Index
                        mine.DeckId <- newDeckId
                    | Some theirs, None ->
                        let mine = cardSansIndex theirs.Index
                        mine.ConceptId <- theirs.ConceptId
                        mine.ExampleId <- theirs.ExampleId
                        mine.RevisionId <- theirs.RevisionId
                        mine.Index <- theirs.Index
                        db.Card.AddI mine
                    | None, Some _ -> () // occurs when `editExisting = false`. `their` card has been filtered out, but `mine` still exists.
                    | _ -> failwith "Should be impossible.")
        return! db.SaveChangesAsyncI()
    }
    let unfollow (db: CardOverflowDb) userId deckId = taskResult {
        do! db.DeckFollower.AnyAsync(fun df ->
                df.DeckId = deckId
                && df.FollowerId = userId)
            |>% Result.requireTrue (sprintf "Either the deck doesn't exist or you are not following it.")
        DeckFollowerEntity(DeckId = deckId, FollowerId = userId) |> db.DeckFollower.RemoveI
        do! db.SaveChangesAsyncI()
    }
    let diff (db: CardOverflowDb) userId theirDeckId myDeckId = taskResult {
        do! verifyVisible db userId theirDeckId
        do! verifyVisible db userId myDeckId
        let get deckId =
            db.Card
                .Where(fun x -> x.DeckId = deckId)
                .Select(fun x -> x.ConceptId, x.ExampleId, x.RevisionId, x.Index, x.DeckId, Guid.Empty)
                .ToListAsync()
            |>% Seq.map ConceptRevisionIndex.fromTuple
            |>% List.ofSeq
        let! theirs = get theirDeckId
        let! mine   = get myDeckId
        let diffs = Diff.ids theirs mine |> Diff.toSummary
        let addedConceptIds = diffs.AddedConcept.Select(fun x -> x.ConceptId).ToList()
        let! (inOtherDeckIds: (Guid * Guid * Guid * int16 * Guid * Guid) ResizeArray) =
            db.Card
                .Where(fun x -> x.UserId = userId && addedConceptIds.Contains x.ConceptId)
                .Select(fun x -> x.ConceptId, x.ExampleId, x.RevisionId, x.Index, x.DeckId, x.Id)
                .ToListAsync() // using Task.map over ConceptRevisionIndex.fromTuple doesn't work here for some reason
        let added =
            List.zipOn
                diffs.AddedConcept
                (inOtherDeckIds |> Seq.toList |> List.map ConceptRevisionIndex.fromTuple)
                (fun x y -> x.ConceptId = y.ConceptId && x.Index = y.Index)
            |> List.map (
                function
                | Some _, Some y -> Some y
                | Some x, _      -> Some x
                | None  , _      -> None
            ) |> List.choose id
        let added, revisionChanged, exampleChanged =
            List.zipOn
                (diffs.AddedConcept) // theirs
                added              // mine, sometimes
                (fun x y -> x.ConceptId = y.ConceptId && x.Index = y.Index)
            |> List.map(function
                | Some x, Some y ->
                    if x.RevisionId = y.RevisionId then
                        Some y, None, None
                    elif x.ExampleId = y.ExampleId then
                        None, Some (x, y), None
                    else
                        None, None, Some (x, y)
                | _      -> failwith "Impossible"
            ) |> List.unzip3
            |> (fun (a, bi, b) ->
                a  |> List.choose id,
                bi |> List.choose id,
                b  |> List.choose id
            )
        let conceptIds =
            added
                @ (revisionChanged   |> List.map snd)
                @ (exampleChanged |> List.map snd)
            |> List.map (fun x -> x.ConceptId)
        return
            { diffs with
                AddedConcept    = added         @ (diffs.AddedConcept    |> List.filterOut (fun  x     -> conceptIds.Contains x.ConceptId))
                ExampleChanged = exampleChanged @ (diffs.ExampleChanged |> List.filterOut (fun (x, _) -> conceptIds.Contains x.ConceptId))
                RevisionChanged   = revisionChanged   @ (diffs.RevisionChanged   |> List.filterOut (fun (x, _) -> conceptIds.Contains x.ConceptId))
            }
    }
    type SearchParams =
        | Relevance  of (Guid * float) Option // id, rank
        | Popularity of (Guid * int)   Option // id, followers
    let searchLimit = 20
    let search (conn: NpgsqlConnection) userId searchString order =
        let additionalWhere, order =
            if searchString |> String.IsNullOrWhiteSpace then
                "",
                match order with
                | Popularity x -> Popularity x
                | Relevance  _ -> Popularity None
            else
                "AND (   qweb @@ d.tsv
                      OR qsim @@ u.tsv )",
                order
        let rank = "ts_rank_cd(dtsv, qweb)::float8 + ts_rank_cd(utsv, qsim)::float8"
        let keyset =
            match order with
            | Relevance  (Some (id, rankValue)) -> // https://dba.stackexchange.com/questions/209272/ and https://dba.stackexchange.com/questions/273544 and https://dba.stackexchange.com/questions/209272/
                sprintf "WHERE ((%s, id) < (%s, '%A'))" rank (rankValue.ToString()) id // the ToString is because %f rounds and I can't figure out how to make it stop
            | Popularity (Some (id, followers)) ->
                sprintf "AND ((d.followers, d.id) < (%i, '%A'))" followers id
            | _ -> ""
        let baseQuery additionalSelect =
            sprintf """
            SELECT
            	d.id
            	, d.name
            	, d.user_id AS AuthorId
            	, u.display_name AS AuthorName
            	, EXISTS (
                    SELECT 1
                    FROM   public.deck_follower df
                    WHERE  df.deck_id = d.id
                    AND    df.follower_id = @userid
                ) AS IsFollowed
                , d.followers AS FollowCount
            	%s
            FROM public.deck d
            JOIN public.padawan u ON u.id = d.user_id
            , websearch_to_tsquery(@searchString) qweb
            , plainto_tsquery('simple', @searchString) qsim
            """ additionalSelect
        let query =
            match order with
            | Relevance  _ -> // https://dba.stackexchange.com/a/53851
                sprintf """
                SELECT *, %s AS TsvRank FROM (
                    %s
                    WHERE ((d.is_public OR d.user_id = @userid) %s)
                ) _
                %s
                ORDER BY TsvRank DESC, id DESC
                LIMIT %i;""" rank (baseQuery ", d.tsv as dtsv, u.tsv as utsv, qweb, qsim") additionalWhere keyset searchLimit
            | Popularity _ ->
                sprintf """
                %s
                WHERE ((d.is_public OR d.user_id = @userid) %s %s)
                ORDER BY FollowCount DESC, d.id DESC
                LIMIT %i;""" (baseQuery "") additionalWhere keyset searchLimit
        conn.QueryAsync<DeckWithFollowMeta>(query, {| searchString = searchString; userid = userId |})
        |>% Seq.toList

module SanitizeHistoryRepository =
    let AddAndSaveAsync (db: CardOverflowDb) cardId score timestamp interval easeFactor (timeFromSeeingQuestionToScore: Duration) intervalOrSteps: Task<unit> = task {
        let! card = db.Card.SingleAsync(fun x -> x.Id = cardId)
        let history =
            HistoryEntity(
                Score = Score.toDb score,
                Created = timestamp,
                IntervalWithUnusedStepsIndex = (interval |> IntervalXX |> IntervalOrStepsIndex.intervalToDb),
                EaseFactorInPermille = (easeFactor * 1000. |> Math.Round |> int16),
                TimeFromSeeingQuestionToScoreInSecondsPlus32768 = (timeFromSeeingQuestionToScore.TotalSeconds + float Int16.MinValue |> int16),
                RevisionId = Nullable card.RevisionId,
                UserId = card.UserId,
                Index = card.Index
            )
        card.Histories.Add history
        db.Entry(history).State <- EntityState.Added
        card.IntervalOrStepsIndex <- intervalOrSteps |> IntervalOrStepsIndex.intervalToDb
        card.Due <- DateTimeX.UtcNow + interval
        card.EaseFactorInPermille <- easeFactor * 1000. |> Math.Round |> int16
        card.IsLapsed <-
            match intervalOrSteps with
            | LapsedStepsIndex _ -> true
            | _ -> false
        do! db.SaveChangesAsyncI ()
        }

[<CLIMutable>]
type SearchCommand = {
    [<StringLength(250, ErrorMessage = "Query must be less than 250 characters.")>]
    Query: string
    Order: SearchOrder
}

[<CLIMutable>]
type EditCardCommand = {
    CardState: CardState
    CardSettingId: Guid
    DeckId: Guid
    [<StringLength(2000, ErrorMessage = "The Front Personal Field must be less than 2000 characters")>]
    FrontPersonalField: string
    [<StringLength(2000, ErrorMessage = "The Back Personal Field must be less than 2000 characters")>]
    BackPersonalField: string
} with
    static member init = {
        CardState = Normal
        CardSettingId = Guid.Empty
        DeckId = Guid.Empty
        FrontPersonalField = ""
        BackPersonalField = ""
    }

[<CLIMutable>]
type ViewEditConceptCommand = {
    [<Required>]
    [<StringLength(200, ErrorMessage = "The summary must be less than 200 characters")>]
    EditSummary: string
    FieldValues: EditFieldAndValue ResizeArray
    TemplateRevision: ViewTemplateRevision
    Kind: UpsertKind
    Title: string // needed cause Blazor can't bind against the immutable FSharpOption or the DU in UpsertKind
    Ids: UpsertIds
} with
    member this.Backs = 
        let valueByFieldName = this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> List.ofSeq // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls
        match this.TemplateRevision.CardTemplates with
        | Cloze t ->
             result {
                let! max = ClozeLogic.maxClozeIndexInclusive "Something's wrong with your cloze indexes." (valueByFieldName |> Map.ofSeq) t.Front
                return [0s .. max] |> List.map (fun clozeIndex ->
                    CardHtml.generate
                        <| valueByFieldName
                        <| t.Front
                        <| t.Back
                        <| this.TemplateRevision.Css
                        <| CardHtml.Cloze clozeIndex
                    |> fun (_, back, _, _) -> back
                    ) |> toResizeArray
            }
        | Standard ts ->
            ts |> List.map (fun t ->
                CardHtml.generate
                    <| (this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> Seq.toList)
                    <| t.Front
                    <| t.Back
                    <| this.TemplateRevision.Css
                    <| CardHtml.Standard
                |> fun (_, back, _, _) -> back
            ) |> toResizeArray
            |> Ok
    member this.load =
        let kind =
            match this.Title with
            | null -> this.Kind
            | title ->
                match this.Kind with
                | NewOriginal_TagIds _
                | NewCopy_SourceRevisionId_TagIds _ -> this.Kind
                | NewExample_Title _ -> NewExample_Title title
                | NewRevision_Title _ -> NewRevision_Title title
        {   EditConceptCommand.EditSummary = this.EditSummary
            FieldValues = this.FieldValues
            TemplateRevisionId = (% this.TemplateRevision.Id), 0<templateRevisionOrdinal> //this.TemplateRevision.Id
            Kind = kind
            Ids = this.Ids
        }

type UpsertCardSource =
    | VNewOriginal_UserId of Guid
    | VNewCopySource_RevisionId of Guid
    | VNewExample_SourceConceptId of Guid
    | VUpdate_ExampleId of Guid

module SanitizeCardRepository =
    let validateCommands (db: CardOverflowDb) userId (commands: EditCardCommand ResizeArray) = taskResult {
        let! defaultDeckId, defaultCardSettingId, areValidDeckIds, areValidCardSettingIds =
            let deckIds    = commands.Select(fun x -> x.DeckId       ).Distinct().Where(fun x -> x <> Guid.Empty).ToList()
            let settingIds = commands.Select(fun x -> x.CardSettingId).Distinct().Where(fun x -> x <> Guid.Empty).ToList()
            db.User
                .Where(fun x -> x.Id = userId)
                .Select(fun x ->
                    x.DefaultDeckId,
                    x.DefaultCardSettingId,
                    x.Decks.Where(fun x -> deckIds.Contains(x.Id)).Count() = deckIds.Count,
                    x.CardSettings.Where(fun x -> settingIds.Contains(x.Id)).Count() = settingIds.Count
                ).SingleAsync()
        do! areValidDeckIds        |> Result.requireTrue "You provided an invalid or unauthorized deck id."         |> Task.FromResult
        do! areValidCardSettingIds |> Result.requireTrue "You provided an invalid or unauthorized card setting id." |> Task.FromResult
        return
            commands.Select(fun c ->
                {   c with
                        DeckId =
                            if c.DeckId = Guid.Empty then
                                defaultDeckId
                            else
                                c.DeckId
                        CardSettingId =
                            if c.CardSettingId = Guid.Empty then
                                defaultCardSettingId
                            else
                                c.CardSettingId
                }
            ).ToList()
    }
    let update (db: CardOverflowDb) userId cardId (command: EditCardCommand) = taskResult {
        let! command = command |> ResizeArray.singleton |> validateCommands db userId |>%% Seq.exactlyOne
        let! (cc: CardEntity) =
            db.Card.SingleOrDefaultAsync(fun x -> x.Id = cardId && x.UserId = userId)
            |>% Result.requireNotNull (sprintf "Card #%A doesn't belong to you." cardId)
        cc.DeckId <- command.DeckId
        cc.CardSettingId <- command.CardSettingId
        return! db.SaveChangesAsyncI()
    }

module SanitizeConceptRepository =
    let getUpsert (db: CardOverflowDb) userId source ids = // medTODO this system of querying for a ViewEditConceptCommand is stupid
        let toCommand kind (revision: RevisionEntity) = taskResult {
            let! (cardIds: Guid ResizeArray) = db.Card.Where(fun x -> x.UserId = userId && x.ConceptId = revision.ConceptId).OrderBy(fun x -> x.Index).Select(fun x -> x.Id).ToListAsync()
            return
                {   EditSummary = ""
                    FieldValues =
                        EditFieldAndValue.load
                            <| Fields.fromString revision.TemplateRevision.Fields
                            <| revision.FieldValues
                    TemplateRevision = revision.TemplateRevision |> TemplateRevision.load |> ViewTemplateRevision.load
                    Kind = kind
                    Title =
                        match kind with
                        | NewOriginal_TagIds _
                        | NewCopy_SourceRevisionId_TagIds _ -> null
                        | NewExample_Title title
                        | NewRevision_Title title -> title
                    Ids =
                        { ids with
                            ConceptId = if ids.ConceptId = Guid.Empty then revision.ConceptId else ids.ConceptId
                            ExampleId = if ids.ExampleId = Guid.Empty then revision.ExampleId else ids.ExampleId
                            RevisionId = if ids.RevisionId = Guid.Empty then revision.Id else ids.RevisionId
                            CardIds = cardIds |> Seq.toList
                        }
                }
            }
        match source with
        | VNewOriginal_UserId userId ->
            db.User_TemplateRevision.Include(fun x -> x.TemplateRevision).FirstOrDefaultAsync(fun x -> x.UserId = userId)
            |>% (Result.requireNotNull (sprintf "User #%A doesn't have any templates" userId))
            |>%% (fun j ->
                {   EditSummary = ""
                    FieldValues =
                        EditFieldAndValue.load
                            <| Fields.fromString j.TemplateRevision.Fields
                            <| ""
                    TemplateRevision = j.TemplateRevision |> TemplateRevision.load |> ViewTemplateRevision.load
                    Kind = NewOriginal_TagIds Set.empty
                    Title = null
                    Ids = ids
                }
            )
        | VNewExample_SourceConceptId conceptId ->
            db.Concept.Include(fun x -> x.DefaultExample.Latest.TemplateRevision).SingleOrDefaultAsync(fun x -> x.Id = conceptId)
            |>% Result.requireNotNull (sprintf "Concept #%A not found." conceptId)
            |> TaskResult.bind(fun concept -> toCommand (NewExample_Title "New Example") concept.DefaultExample.Latest)
        | VNewCopySource_RevisionId revisionId ->
            db.Revision.Include(fun x -> x.TemplateRevision).SingleOrDefaultAsync(fun x -> x.Id = revisionId)
            |>% Result.requireNotNull (sprintf "Example Revision #%A not found." revisionId)
            |> TaskResult.bind(toCommand (NewCopy_SourceRevisionId_TagIds (revisionId, Set.empty)))
        | VUpdate_ExampleId exampleId ->
            db.Example.Include(fun x -> x.Latest.TemplateRevision).SingleOrDefaultAsync(fun x -> x.Id = exampleId)
            |>% Result.requireNotNull (sprintf "Example #%A not found." exampleId)
            |> TaskResult.bind(fun example -> toCommand (NewRevision_Title example.Name) example.Latest)
    let Update (db: CardOverflowDb) userId (acCommands: EditCardCommand list) (conceptCommand: ViewEditConceptCommand) = taskResult {
        let! (revision: RevisionEntity) = UpdateRepository.concept db userId conceptCommand.load
        let! (ccs: CardEntity list) =
            let cardIds = conceptCommand.Ids.CardIds
            match conceptCommand.Kind with
            | NewRevision_Title _ ->
                db.Revision.AddI revision
                ConceptRepository.collectConceptNoSave db userId revision false cardIds
            | NewExample_Title _ ->
                db.Revision.AddI revision
                ConceptRepository.collectConceptNoSave db userId revision true cardIds
            | NewOriginal_TagIds tags
            | NewCopy_SourceRevisionId_TagIds (_, tags) -> taskResult {
                let! (ccs: CardEntity list) = ConceptRepository.collectConceptNoSave db userId revision true cardIds
                for tag in tags do
                    for cc in ccs do
                        cc.Tags <- cc.Tags.Append(tag).ToArray()
                return ccs
                }
            |>%% List.sortBy (fun x -> x.Index)
        do! acCommands.ToList()
            |> SanitizeCardRepository.validateCommands db userId
            |>%% Seq.iteri (fun i command ->
                let cc = ccs.[i]
                cc.CardState <- command.CardState |> CardState.toDb
                cc.CardSettingId <- command.CardSettingId
                cc.DeckId <- command.DeckId
                cc.FrontPersonalField <- command.FrontPersonalField
                cc.BackPersonalField <- command.BackPersonalField
            )
        do! db.SaveChangesAsyncI()
        return revision.ExampleId
        }
    let search (db: CardOverflowDb) userId pageNumber searchCommand =
        ConceptRepository.search db userId pageNumber searchCommand.Order searchCommand.Query
    let searchDeck (db: CardOverflowDb) userId pageNumber searchCommand deckId =
        ConceptRepository.searchDeck db userId pageNumber searchCommand.Order searchCommand.Query deckId
    let GetCollectedPages (db: CardOverflowDb) userId pageNumber searchCommand =
        ConceptRepository.GetCollectedPages db userId pageNumber searchCommand.Query

[<CLIMutable>]
type PotentialSignupCommand = {
    [<Required>]
    [<EmailAddress>]
    Email: string
    [<StringLength(1000, ErrorMessage = "Message must be less than 1000 characters.")>]
    Message: string
    OneIsAlpha2Beta3Ga: int16
}

module SanitizeLandingPage =
    let SignUp (db: CardOverflowDb) signUpForm =
        PotentialSignupsEntity(
            Email = signUpForm.Email,
            Message = signUpForm.Message,
            OneIsAlpha2Beta3Ga = signUpForm.OneIsAlpha2Beta3Ga
        ) |> db.PotentialSignups.AddI
        db.SaveChangesAsyncI()

[<CLIMutable>]
type Feedback = {
    [<Required>]
    [<StringLength(50, ErrorMessage = "Title must be less than 50 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Title: string
    [<Required>]
    [<StringLength(1000, ErrorMessage = "Description must be less than 1000 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Description: string
    [<Required>]
    Priority: string
}
module SanitizeFeedback =
    let addAndSaveAsync (db: CardOverflowDb) userId feedback =
        FeedbackRepository.addAndSaveAsync db userId feedback.Title feedback.Description (feedback.Priority |> int16 |> Nullable)

module Minutes =
    let private intString (f: float) = f |> Convert.ToInt32 |> string
    let toString (timespan: Duration) =
        intString timespan.TotalMinutes
    let fromString raw =
        raw |> int |> float |> Duration.FromMinutes
    let toStringList (timespans: Duration list) =
        timespans |> List.map toString |> fun x -> String.Join(' ', x)
    let fromStringList (raw: string) =
        raw.Split ' ' |> Seq.map fromString |> List.ofSeq

module Convert =
    let toPercent (x: float) =
        x * 100. |> Math.Round |> int
    let fromPercent (x: int) =
        (float  x) / 100.

[<CLIMutable>]
type ViewCardSetting = {
    Id: Guid
    Name: string
    IsDefault: bool
    [<RegularExpression(@"[\d ]+", ErrorMessage = "Steps must be digits separated by spaces")>]
    NewCardsSteps: string
    NewCardsMaxPerDay: int
    NewCardsGraduatingInterval: int
    NewCardsEasyInterval: int
    NewCardsStartingEaseFactor: int
    NewCardsBuryRelated: bool
    MatureCardsMaxPerDay: int
    MatureCardsEaseFactorEasyBonusFactor: int
    MatureCardsIntervalFactor: int
    MatureCardsMaximumInterval: int
    MatureCardsHardInterval: int
    MatureCardsBuryRelated: bool
    [<RegularExpression(@"[\d ]+", ErrorMessage = "Steps must be digits separated by spaces")>]
    LapsedCardsSteps: string
    LapsedCardsNewIntervalFactor: int
    LapsedCardsMinimumInterval: int
    LapsedCardsLeechThreshold: int
    ShowAnswerTimer: bool
    AutomaticallyPlayAudio: bool
    ReplayQuestionAudioOnAnswer: bool
} with
    static member load (bznz: CardSetting) = {
        Id = bznz.Id
        Name = bznz.Name
        IsDefault = bznz.IsDefault
        NewCardsSteps = bznz.NewCardsSteps |> Minutes.toStringList
        NewCardsMaxPerDay = bznz.NewCardsMaxPerDay
        NewCardsGraduatingInterval = bznz.NewCardsGraduatingInterval.TotalDays |> Convert.ToInt32
        NewCardsEasyInterval = bznz.NewCardsEasyInterval.TotalDays |> Convert.ToInt32
        NewCardsStartingEaseFactor = bznz.NewCardsStartingEaseFactor |> Convert.toPercent
        NewCardsBuryRelated = bznz.NewCardsBuryRelated
        MatureCardsMaxPerDay = bznz.MatureCardsMaxPerDay
        MatureCardsEaseFactorEasyBonusFactor = bznz.MatureCardsEaseFactorEasyBonusFactor |> Convert.toPercent
        MatureCardsIntervalFactor = bznz.MatureCardsIntervalFactor |> Convert.toPercent
        MatureCardsMaximumInterval = bznz.MatureCardsMaximumInterval.TotalDays |> Math.Round |> int
        MatureCardsHardInterval = bznz.MatureCardsHardIntervalFactor |> Convert.toPercent
        MatureCardsBuryRelated = bznz.MatureCardsBuryRelated
        LapsedCardsSteps = bznz.LapsedCardsSteps |> Minutes.toStringList
        LapsedCardsNewIntervalFactor = bznz.LapsedCardsNewIntervalFactor |> Convert.toPercent
        LapsedCardsMinimumInterval = bznz.LapsedCardsMinimumInterval.TotalDays |> Convert.ToInt32
        LapsedCardsLeechThreshold = bznz.LapsedCardsLeechThreshold
        ShowAnswerTimer = bznz.ShowAnswerTimer
        AutomaticallyPlayAudio = bznz.AutomaticallyPlayAudio
        ReplayQuestionAudioOnAnswer = bznz.ReplayQuestionAudioOnAnswer
    }
    member this.copyTo: CardSetting = {
        Id = this.Id
        Name = this.Name
        IsDefault = this.IsDefault
        NewCardsSteps = this.NewCardsSteps |> Minutes.fromStringList
        NewCardsMaxPerDay = this.NewCardsMaxPerDay
        NewCardsGraduatingInterval = this.NewCardsGraduatingInterval |> float |> Duration.FromDays
        NewCardsEasyInterval = this.NewCardsEasyInterval |> float |> Duration.FromDays
        NewCardsStartingEaseFactor = this.NewCardsStartingEaseFactor |> Convert.fromPercent
        NewCardsBuryRelated = this.NewCardsBuryRelated
        MatureCardsMaxPerDay = this.MatureCardsMaxPerDay
        MatureCardsEaseFactorEasyBonusFactor = this.MatureCardsEaseFactorEasyBonusFactor |> Convert.fromPercent
        MatureCardsIntervalFactor = this.MatureCardsIntervalFactor |> Convert.fromPercent
        MatureCardsMaximumInterval = this.MatureCardsMaximumInterval |> float |> Duration.FromDays
        MatureCardsHardIntervalFactor = this.MatureCardsHardInterval |> Convert.fromPercent
        MatureCardsBuryRelated = this.MatureCardsBuryRelated
        LapsedCardsSteps = this.LapsedCardsSteps |> Minutes.fromStringList
        LapsedCardsNewIntervalFactor = this.LapsedCardsNewIntervalFactor |> Convert.fromPercent
        LapsedCardsMinimumInterval = this.LapsedCardsMinimumInterval |> float |> Duration.FromDays
        LapsedCardsLeechThreshold = this.LapsedCardsLeechThreshold
        ShowAnswerTimer = this.ShowAnswerTimer
        AutomaticallyPlayAudio = this.AutomaticallyPlayAudio
        ReplayQuestionAudioOnAnswer = this.ReplayQuestionAudioOnAnswer
    }

module SanitizeCardSettingRepository =
    let setCard (db: CardOverflowDb) userId cardId newCardSettingId = task {
        let! option = db.CardSetting.SingleOrDefaultAsync(fun x -> x.Id = newCardSettingId && x.UserId = userId)
        let! card = db.Card.SingleOrDefaultAsync(fun x -> x.Id = cardId && x.UserId = userId)
        return!
            match option, card with
            | null, _
            | _, null -> Error "Something's null" |> Task.FromResult
            | option, card -> task {
                card.CardSettingId <- option.Id
                do! db.SaveChangesAsyncI()
                return Ok ()
            }
    }
    let getAll (db: CardOverflowDb) userId = task {
        let! x = CardSettingsRepository.getAll db userId
        return x |> Seq.map ViewCardSetting.load |> toResizeArray }
    let upsertMany (db: CardOverflowDb) userId (options: ViewCardSetting ResizeArray) = task {
        let options = options.Select(fun x -> x.copyTo) |> List.ofSeq
        let oldOptionIds = options.Select(fun x -> x.Id).Where(fun x -> x <> Guid.Empty).ToList()
        let! oldOptions = db.CardSetting.Where(fun x -> oldOptionIds.Contains x.Id && x.UserId = userId).ToListAsync()
        let! user = db.User.SingleAsync(fun x -> x.Id = userId)
        return!
            options |> List.map (fun option ->
                let maybeSetDefault e =
                    if option.IsDefault then
                        user.DefaultCardSetting <- e
                if option.Id = Guid.Empty then
                    let e = option.CopyToNew userId
                    db.CardSetting.AddI e
                    maybeSetDefault e
                    Ok e
                else
                    match oldOptions.SingleOrDefault(fun x -> x.Id = option.Id) with
                    | null -> Error "Card setting not found (or doesn't belong to you.)"
                    | e ->
                        option.CopyTo e
                        maybeSetDefault e
                        Ok e
            ) |> Result.consolidate
            |> function
            | Ok e ->
                task {
                    do! db.SaveChangesAsyncI()
                    return e |> List.ofSeq |> List.map (fun x -> x.Id) |> Ok
                }
            | Error e ->
                Error e |> Task.FromResult
    }

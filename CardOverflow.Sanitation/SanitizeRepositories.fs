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
open Domain
open FSharp.UMX

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
    let setSource (db: CardOverflowDb) userId deckId sourceDeckId = taskResult {
        match sourceDeckId with
        | Some sourceDeckId ->
            do! requireIsPublic db sourceDeckId
        | None -> ()
        let! (deck: DeckEntity) = tryGet db userId deckId
        deck.SourceId <- sourceDeckId |> Option.toNullable
        return! db.SaveChangesAsyncI()
    }
    let setDefault (db: CardOverflowDb) userId deckId = taskResult {
        do! deckBelongsTo db userId deckId
        let! (user: UserEntity) = db.User.SingleAsync(fun x -> x.Id = userId)
        user.DefaultDeckId <- deckId
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
                            //do! create db userId name newDeckId // creates the deck if it doesn't exist
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
    CardSettingId: CardSettingId
    DeckIds: DeckId ResizeArray
    [<StringLength(2000, ErrorMessage = "The Front Personal Field must be less than 2000 characters")>]
    FrontPersonalField: string
    [<StringLength(2000, ErrorMessage = "The Back Personal Field must be less than 2000 characters")>]
    BackPersonalField: string
} with
    static member init = {
        CardState = Normal
        CardSettingId = % Guid.Empty
        DeckIds = ResizeArray.empty
        FrontPersonalField = ""
        BackPersonalField = ""
    }

type Upsert =
    | Insert
    | Update

[<CLIMutable>]
type ViewEditConceptCommand = {
    [<Required>]
    [<StringLength(200, ErrorMessage = "The summary must be less than 200 characters")>]
    EditSummary: string
    FieldValues: EditFieldAndValue ResizeArray
    TemplateInstance: Projection.TemplateInstance
    Title: string // needed cause Blazor can't bind against the immutable FSharpOption or the DU in UpsertKind
    Upsert: Upsert
    SourceExampleId: ExampleId Option
    ExampleRevisionId: ExampleRevisionId
    StackId: StackId
} with
    member this.Backs = 
        let valueByFieldName = this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> List.ofSeq // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls
        match this.TemplateInstance.CardTemplates with
        | Cloze t ->
             result {
                let! max = ClozeLogic.maxClozeIndexInclusive "Something's wrong with your cloze indexes." (valueByFieldName |> Map.ofSeq) t.Front
                return [0s .. max] |> List.map (fun clozeIndex ->
                    CardHtml.generate
                        <| valueByFieldName
                        <| t.Front
                        <| t.Back
                        <| this.TemplateInstance.Css
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
                    <| this.TemplateInstance.Css
                    <| CardHtml.Standard
                |> fun (_, back, _, _) -> back
            ) |> toResizeArray
            |> Ok
    static member create templateInstance =
        {   EditSummary = "Initial creation"
            TemplateInstance = templateInstance
            FieldValues =
                templateInstance.Fields
                |> List.map (fun f -> { EditField = f; Value = "" })
                |> toResizeArray
            Title = ""
            Upsert = Insert
            SourceExampleId = None
            ExampleRevisionId = % Guid.NewGuid(), Example.Fold.initialExampleRevisionOrdinal
            StackId = % Guid.NewGuid()
        }
    static member edit templateInstance (example: Projection.ExampleInstance) stackId =
        {   EditSummary = ""
            TemplateInstance = templateInstance
            FieldValues = example.FieldValues.ToList()
            Title = example.Title
            SourceExampleId = None // highTODO add Source to Example's Summary then use it here
            ExampleRevisionId = example.ExampleId, example.Ordinal + 1<exampleRevisionOrdinal>
            Upsert = Update
            StackId = stackId
        }
    static member fork templateInstance (example: Projection.ExampleInstance) =
        { ViewEditConceptCommand.edit templateInstance example (% Guid.NewGuid()) with
            SourceExampleId = Some example.ExampleId }
    member this.toEvent meta (cardCommands: EditCardCommand list) defaultEase =
        let fieldValues = this.FieldValues |> Seq.toList
        let template = this.TemplateInstance |> Projection.toTemplateRevision
        let pointers = Template.getCardTemplatePointers template fieldValues |> Result.getOk
        if pointers.Length <> cardCommands.Length then failwith "CardTemplatePointers and CardCommands do not have matching lengths"
        match this.Upsert with
        | Insert ->
            let exampleId = % Guid.NewGuid()
            let exampleEvent: Example.Events.Created =
                { Meta = meta
                  Id = exampleId
                  ParentId = this.SourceExampleId
                  AnkiNoteId = None
                  Visibility = Public
                  Title = this.Title
                  TemplateRevisionId = this.TemplateInstance.Id
                  FieldValues = fieldValues
                  EditSummary = this.EditSummary }
            let stackEvent =
                let stackId = % Guid.NewGuid()
                List.zip pointers cardCommands
                |> List.map (fun (pointer, cardCommand) -> Stack.initCard meta.ClientCreatedAt cardCommand.CardSettingId defaultEase (cardCommand.DeckIds |> Set.ofSeq) pointer)
                |> Stack.init stackId meta exampleId
            exampleEvent |> Example.Events.Event.Created,
            stackEvent   |> Stack.Events.Event.Created
        | Update ->
            let exampleEvent: Example.Events.Edited =
                { Meta = meta
                  Ordinal = snd this.ExampleRevisionId
                  Title = this.Title
                  TemplateRevisionId = this.TemplateInstance.Id
                  FieldValues = fieldValues
                  EditSummary = this.EditSummary }
            let stackEvent: Stack.Events.Edited =
                { Meta = meta
                  ExampleRevisionId  = this.ExampleRevisionId
                  FrontPersonalField = ""
                  BackPersonalField  = ""
                  Tags = Set.empty
                  CardEdits =
                    List.zip pointers cardCommands
                    |> List.map (fun (pointer, cardCommand) ->
                        ({ Pointer       = pointer
                           CardSettingId = cardCommand.CardSettingId
                           DeckIds       = cardCommand.DeckIds |> Set.ofSeq
                           State         = cardCommand.CardState }: Stack.Events.CardEdited)) }
            exampleEvent |> Example.Events.Event.Edited,
            stackEvent   |> Stack.Events.Event.Edited

module SanitizeConceptRepository =
    let search (db: CardOverflowDb) userId pageNumber searchCommand =
        ConceptRepository.search db userId pageNumber searchCommand.Order searchCommand.Query
    let searchDeck (db: CardOverflowDb) userId pageNumber searchCommand deckId =
        ConceptRepository.searchDeck db userId pageNumber searchCommand.Order searchCommand.Query deckId

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

namespace CardOverflow.Sanitation

open System
open CardOverflow.Pure.Extensions
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

[<CLIMutable>]
type ViewFilter = {
    Id: int
    UserId: int
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
    Id: int
    UserId: int
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
        Due = StackRepository.GetDueCount db e.UserId e.Query
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
    let AddAndSaveAsync (db: CardOverflowDb) (time: TimeProvider) (comment: string) stackId userId = taskResult { // lowTODO add idempotency key
        let text = comment |> MappingTools.standardizeWhitespace
        do! if text.Length >= 15 then Ok () else Error "Comment must be 15 or more characters."
        return!
            CommentStackEntity(
                StackId = stackId,
                UserId = userId,
                Text = text,
                Created = time.utcNow
            ) |> CommentRepository.addAndSaveAsync db
        }

[<CLIMutable>]
type TagText = {
    [<StringLength(250, ErrorMessage = "Tag must be less than 250 characters.")>] // medTODO 250 needs to be tied to the DB max somehow
    Text: string
}

module SanitizeTagRepository =
    let max = 250
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
    let upsertNoSave (db: CardOverflowDb) (newTag: string) = taskResult {
        let newTag = sanitize newTag
        let! (tag: TagEntity option) =
            db.Tag.SingleOrDefaultAsync(fun x -> EF.Functions.ILike(x.Name, newTag))
            |>% Option.ofObj
        return
            tag |> Option.defaultWith(fun () ->
                let tag = TagEntity(Name = newTag)
                db.Tag.AddI tag
                tag
            )
        }
    let upsert (db: CardOverflowDb) (newTag: string) = taskResult {
        let! (e: TagEntity) = upsertNoSave db newTag
        do! db.SaveChangesAsyncI()
        return e.Id
        }
    let private getFirstAcquired (db: CardOverflowDb) userId stackId =
        db.AcquiredCard.FirstOrDefaultAsync(fun x -> x.UserId = userId && x.StackId = stackId)
        |>% (Result.requireNotNull <| sprintf "User #%i doesn't have Stack #%i." userId stackId)
    let AddTo (db: CardOverflowDb) userId (newTag: string) stackId = taskResult {
        let newTag = sanitize newTag
        let! (ac: AcquiredCardEntity) = getFirstAcquired db userId stackId
        let! (tag: TagEntity) = upsertNoSave db newTag
        do! db.Tag_AcquiredCard
                .AnyAsync(fun x -> x.StackId = stackId && x.UserId = userId && x.TagId = tag.Id)
            |>% (Result.requireFalse <| sprintf "Stack #%i for User #%i already has tag \"%s\"" stackId userId newTag)
        Tag_AcquiredCardEntity(AcquiredCard = ac, Tag = tag)
        |> db.Tag_AcquiredCard.AddI
        return! db.SaveChangesAsyncI ()
    }
    let DeleteFrom db userId tag stackId = taskResult {
        let tag = MappingTools.toTitleCase tag
        let! (ac: AcquiredCardEntity) = getFirstAcquired db userId stackId
        let! join =
            db.Tag_AcquiredCard
                .SingleOrDefaultAsync(fun x -> x.AcquiredCardId = ac.Id && x.Tag.Name = tag)
            |>% (Result.requireNotNull <| sprintf "Stack #%i for User #%i doesn't have the tag \"%s\"" stackId userId tag)
        db.Tag_AcquiredCard.RemoveI join
        return! db.SaveChangesAsyncI()
    }

module SanitizeDeckRepository =
    let private deckBelongsTo (db: CardOverflowDb) userId deckId =
        db.Deck.AnyAsync(fun x -> x.Id = deckId && x.UserId = userId)
        |>% (Result.requireTrue <| sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" deckId)
    let private tryGet (db: CardOverflowDb) userId deckId =
        db.Deck.SingleOrDefaultAsync(fun x -> x.Id = deckId && x.UserId = userId)
        |>% (Result.requireNotNull <| sprintf "Either Deck #%i doesn't belong to you or it doesn't exist" deckId)
    let private validateName (db: CardOverflowDb) userId (deckName: string) = taskResult {
        do! if deckName.Length > 250 then Error <| sprintf "Deck name '%s' is too long. It must be less than 250 characters." deckName else Ok ()
        do! db.Deck.AnyAsync(fun x -> x.Name = deckName && x.UserId = userId)
            |>% (Result.requireFalse <| sprintf "User #%i already has a Deck named '%s'" userId deckName)
    }
    let create (db: CardOverflowDb) userId (newDeck: string) = taskResult {
        do! validateName db userId newDeck
        let deck = DeckEntity(Name = newDeck, UserId = userId)
        db.Deck.AddI deck
        do! db.SaveChangesAsyncI()
        return deck.Id
    }
    let delete (db: CardOverflowDb) userId deckId = taskResult {
        do! deckBelongsTo db userId deckId
        let! defaultDeckId = db.User.Where(fun x -> x.Id = userId).Select(fun x -> x.DefaultDeckId).SingleAsync()
        do! deckId <> defaultDeckId |> Result.requireTrue "You can't delete your default deck. Make another deck default first."
        do! db.AcquiredCard.Where(fun x -> x.DeckId = deckId).ToListAsync()
            |> TaskSeq.iter(fun ac -> ac.DeckId <- defaultDeckId)
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
    let switch (db: CardOverflowDb) userId deckId acquiredCardId = taskResult {
        do! deckBelongsTo db userId deckId
        let! (ac: AcquiredCardEntity) =
            db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.Id = acquiredCardId && x.UserId = userId)
            |>% (Result.requireNotNull <| sprintf "Either AcquiredCard #%i doesn't belong to you or it doesn't exist" acquiredCardId)
        ac.DeckId <- deckId
        return! db.SaveChangesAsyncI ()
    }
    let getQuizBatch (db: CardOverflowDb) userId deckId = taskResult {
        do! deckBelongsTo db userId deckId
        return! StackRepository.GetQuizBatchDeck db deckId
    }
    let get (db: CardOverflowDb) userId currentTime =
        db.User
            .Where(fun x -> x.Id = userId)
            .Select(fun x ->
                x.DefaultDeckId,
                x.Decks.Select(fun x ->
                    x,
                    x.AcquiredCards.Where(fun x ->
                        x.UserId = userId &&
                        x.Due < currentTime
                    ).Count(),
                    x.AcquiredCards.Count)
            ).SingleAsync()
        |>% (fun (defaultDeckId, deckCounts) ->
            deckCounts |> Seq.map(fun (deck, dueCount, allCount) -> {
                Id = deck.Id
                Name = deck.Name
                IsPublic = deck.IsPublic
                IsDefault = defaultDeckId = deck.Id
                AllCount = allCount
                DueCount = dueCount
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
    let getPublic (db: CardOverflowDb) userId deckId =
        db.Deck
            .Where(fun x -> x.IsPublic && x.Id = deckId)
            .Select(fun x ->
                x,
                x.DeckFollowers.Any(fun x -> x.FollowerId = userId),
                x.DeckFollowers.Count,
                x.UserId,
                x.User.DisplayName
            )
            .SingleOrDefaultAsync()
        |>% Result.ofNullable (sprintf "Either Deck #%i doesn't exist or it isn't public." deckId)
        |>%% fun (deck, isFollowed, count, authorId, authorName) -> {
                Id = deck.Id
                Name = deck.Name
                AuthorId = authorId
                AuthorName = authorName
                IsFollowed = isFollowed
                FollowCount = count
            }
    let follow (db: CardOverflowDb) userId deckId = taskResult {
        let! isValid =
            db.Deck.AnyAsync(fun d ->
                d.Id = deckId
                && not (d.DeckFollowers.Any(fun df -> df.FollowerId = userId))
            )
        do! isValid |> Result.requireTrue (sprintf "Either the deck doesn't exist or you are already following it.")
        DeckFollowersEntity(DeckId = deckId, FollowerId = userId) |> db.DeckFollowers.AddI
        do! db.SaveChangesAsyncI()
    }
    let unfollow (db: CardOverflowDb) userId deckId = taskResult {
        do! db.DeckFollowers.AnyAsync(fun df ->
                df.DeckId = deckId
                && df.FollowerId = userId)
            |>% Result.requireTrue (sprintf "Either the deck doesn't exist or you are not following it.")
        DeckFollowersEntity(DeckId = deckId, FollowerId = userId) |> db.DeckFollowers.RemoveI
        do! db.SaveChangesAsyncI()
    }
    let diff (db: CardOverflowDb) userId theirDeckId myDeckId = task {
        let theirs = db.Deck.Where(fun x -> x.Id = theirDeckId && x.IsPublic)
        let mine   = db.Deck.Where(fun x -> x.Id = myDeckId    && x.UserId = userId)
        let parse (x: DeckEntity IQueryable) =
            x.Select(fun x -> x.AcquiredCards.Select(fun x -> x.StackId, x.BranchId, x.BranchInstanceId))
                .ToListAsync()
            |>% Seq.collect id
            |>% Seq.map StackBranchInstanceIds.fromTuple
            |>% List.ofSeq
        let! theirs = parse theirs
        let! mine   = parse mine
        return Diff.ids theirs mine
    }

module SanitizeHistoryRepository =
    let AddAndSaveAsync (db: CardOverflowDb) acquiredCardId score timestamp interval easeFactor (timeFromSeeingQuestionToScore: TimeSpan) intervalOrSteps: Task<unit> = task {
        let! card = db.AcquiredCard.SingleAsync(fun x -> x.Id = acquiredCardId)
        card.Histories.Add
        <|  HistoryEntity(
                Score = Score.toDb score,
                Timestamp = timestamp,
                IntervalWithUnusedStepsIndex = (interval |> Interval |> IntervalOrStepsIndex.intervalToDb),
                EaseFactorInPermille = (easeFactor * 1000. |> Math.Round |> int16),
                TimeFromSeeingQuestionToScoreInSecondsPlus32768 = (timeFromSeeingQuestionToScore.TotalSeconds + float Int16.MinValue |> int16),
                BranchInstanceId = Nullable card.BranchInstanceId,
                UserId = card.UserId,
                Index = card.Index
            )
        card.IntervalOrStepsIndex <- intervalOrSteps |> IntervalOrStepsIndex.intervalToDb
        card.Due <- DateTime.UtcNow + interval
        card.EaseFactorInPermille <- easeFactor * 1000. |> Math.Round |> int16
        card.IsLapsed <-
            match intervalOrSteps with
            | LapsedStepsIndex _ -> true
            | _ -> false
        do! db.SaveChangesAsyncI ()
        }

[<CLIMutable>]
type AddRelationshipCommand = {
    [<Required>]
    [<StringLength(250, ErrorMessage = "Name must be less than 250 characters.")>]
    Name: string
    [<Required>]
    SourceStackId: int
    [<Required>]
    TargetStackLink: string
}
module SanitizeRelationshipRepository =
    type StackIdRegex = FSharp.Text.RegexProvider.Regex< """(?<stackId>\d+)$""" >
    let stackIdRegex =
        Regex.compiledIgnoreCase |> StackIdRegex
    let GetStackId input =
        let x = stackIdRegex.TypedMatch input // lowTODO make this a custom `ValidationAttribute` on TargetLink
        if x.Success 
        then Ok <| int x.Value
        else Error <| sprintf "Couldn't find the Stack Id in '%s'" input
    let Add (db: CardOverflowDb) userId command = taskResult {
        let! targetStackId = GetStackId command.TargetStackLink
        do! if targetStackId = command.SourceStackId then Error "A stack can't be related to itself" else Ok ()
        let! (acs: AcquiredCardEntity ResizeArray) = db.AcquiredCard.Include(fun x -> x.BranchInstance).Where(fun x -> x.UserId = userId && (x.StackId = targetStackId || x.StackId = command.SourceStackId)).ToListAsync()
        let! t = acs.FirstOrDefault(fun x -> x.StackId = targetStackId) |> Result.ofNullable (sprintf "You haven't acquired the linked stack (Stack #%i)." targetStackId)
        let! s = acs.FirstOrDefault(fun x -> x.StackId = command.SourceStackId) |> Result.ofNullable (sprintf "You haven't acquired the source stack (Stack #%i)." command.SourceStackId)
        let! r = db.Relationship.SingleOrDefaultAsync(fun x -> x.Name = command.Name)
        let r = r |> Option.ofObj |> Option.defaultValue (RelationshipEntity(Name = command.Name))
        let sid, tid, sStackId, tStackId =
            if Relationship.isDirectional command.Name then
                s.Id, t.Id, s.StackId, t.StackId
            else // if non-directional, alter the source/target ids so they're grouped properly in the DB. EG: Source=1,Target=2,Name=X and Source=2,Target=1,Name=X are seen as distinct, so this makes them the same
                if s.BranchInstanceId < t.BranchInstanceId then
                    s.Id, t.Id, s.StackId, t.StackId
                else
                    t.Id, s.Id, t.StackId, s.StackId
        return!
            Relationship_AcquiredCardEntity(
                Relationship = r,
                UserId = userId,
                SourceStackId = sStackId,
                TargetStackId = tStackId,
                SourceAcquiredCardId = sid,
                TargetAcquiredCardId = tid
            ) |> RelationshipRepository.addAndSaveAsync db
        }
    let Remove db sourceInstanceId targetStackId userId name =
        RelationshipRepository.removeAndSaveAsync db sourceInstanceId targetStackId userId name // don't eta reduce - consumed by C#

[<CLIMutable>]
type SearchCommand = {
    [<StringLength(250, ErrorMessage = "Query must be less than 250 characters.")>]
    Query: string
    Order: SearchOrder
}

[<CLIMutable>]
type EditAcquiredCardCommand = {
    CardState: CardState
    CardSettingId: int
    DeckId: int
    [<StringLength(2000, ErrorMessage = "The Front Personal Field must be less than 2000 characters")>]
    FrontPersonalField: string
    [<StringLength(2000, ErrorMessage = "The Back Personal Field must be less than 2000 characters")>]
    BackPersonalField: string
} with
    static member init = {
        CardState = Normal
        CardSettingId = 0
        DeckId = 0
        FrontPersonalField = ""
        BackPersonalField = ""
    }

[<CLIMutable>]
type ViewEditStackCommand = {
    [<Required>]
    [<StringLength(200, ErrorMessage = "The summary must be less than 200 characters")>]
    EditSummary: string
    FieldValues: EditFieldAndValue ResizeArray
    CollateInstance: ViewCollateInstance
    Kind: UpsertKind
    Title: string // needed cause Blazor can't bind against the immutable FSharpOption or the DU in UpsertKind
} with
    member this.Backs = 
        let valueByFieldName = this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> List.ofSeq // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls
        match this.CollateInstance.Templates with
        | Cloze t ->
             result {
                let! max = ClozeLogic.maxClozeIndexInclusive "Something's wrong with your cloze indexes." (valueByFieldName |> Map.ofSeq) t.Front
                return [0s .. max] |> List.map (fun clozeIndex ->
                    CardHtml.generate
                        <| valueByFieldName
                        <| t.Front
                        <| t.Back
                        <| this.CollateInstance.Css
                        <| CardHtml.Cloze clozeIndex
                    |> fun (_, back, _, _) -> back
                    ) |> toResizeArray
            }
        | Standard ts ->
            ts |> List.map (fun t ->
                CardHtml.generate
                    <| this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "").ToFList()
                    <| t.Front
                    <| t.Back
                    <| this.CollateInstance.Css
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
                | NewCopy_SourceInstanceId_TagIds _ -> this.Kind
                | NewBranch_SourceStackId_Title (id, _) -> NewBranch_SourceStackId_Title (id, title)
                | Update_BranchId_Title (id, _) -> Update_BranchId_Title(id, title)
        {   EditStackCommand.EditSummary = this.EditSummary
            FieldValues = this.FieldValues
            CollateInstance = this.CollateInstance |> ViewCollateInstance.copyTo
            Kind = kind
        }

type UpsertCardSource =
    | VNewOriginalUserId of int
    | VNewCopySourceInstanceId of int
    | VNewBranchSourceStackId of int
    | VUpdateBranchId of int

module SanitizeAcquiredCardRepository =
    let update (db: CardOverflowDb) userId acquiredCardId (command: EditAcquiredCardCommand) = taskResult {
        let! cardSettingId =
            match command.CardSettingId with 
            | 0 ->
                db.User
                    .Where(fun x -> x.Id = userId)
                    .Select(fun x -> x.DefaultCardSettingId)
                    .SingleAsync()
                |>% Ok
            | id ->
                db.CardSetting.AnyAsync(fun x -> x.Id = id && x.UserId = userId)
                |>% (Result.requireTrue <| sprintf "Card Setting #%i doesn't exist or doesn't belong to User #%i" id userId)
                |>%% fun () -> id
        let! deckId =
            match command.DeckId with
            | 0 ->
                db.User
                    .Where(fun x -> x.Id = userId)
                    .Select(fun x -> x.DefaultDeckId)
                    .SingleAsync()
                |>% Ok
            | id ->
                db.Deck.AnyAsync(fun x -> x.Id = id && x.UserId = userId)
                |>% (Result.requireTrue <| sprintf "Card Setting #%i doesn't exist or doesn't belong to User #%i" id userId)
                |>%% fun () -> id
        let! (ac: AcquiredCardEntity) =
            db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.Id = acquiredCardId && x.UserId = userId)
            |>% Result.requireNotNull (sprintf "AcquiredCard #%i doesn't belong to you." acquiredCardId)
        ac.DeckId <- deckId
        ac.CardSettingId <- cardSettingId
        return! db.SaveChangesAsyncI()
    }

module SanitizeStackRepository =
    let getUpsert (db: CardOverflowDb) source =
        let toCommand kind (branch: BranchInstanceEntity) =
            {   EditSummary = ""
                FieldValues =
                    EditFieldAndValue.load
                        <| Fields.fromString branch.CollateInstance.Fields
                        <| branch.FieldValues
                CollateInstance = branch.CollateInstance |> CollateInstance.load |> ViewCollateInstance.load
                Kind = kind
                Title =
                    match kind with
                    | NewOriginal_TagIds _
                    | NewCopy_SourceInstanceId_TagIds _ -> null
                    | NewBranch_SourceStackId_Title (_, title)
                    | Update_BranchId_Title (_, title) -> title
            }
        match source with
        | VNewOriginalUserId userId ->
            db.User_CollateInstance.Include(fun x -> x.CollateInstance).FirstOrDefaultAsync(fun x -> x.UserId = userId)
            |>% (Result.requireNotNull (sprintf "User #%i doesn't have any templates" userId))
            |>%% (fun j ->
                {   EditSummary = ""
                    FieldValues =
                        EditFieldAndValue.load
                            <| Fields.fromString j.CollateInstance.Fields
                            <| ""
                    CollateInstance = j.CollateInstance |> CollateInstance.load |> ViewCollateInstance.load
                    Kind = NewOriginal_TagIds []
                    Title = null
                }
            )
        | VNewBranchSourceStackId stackId ->
            db.Stack.Include(fun x -> x.DefaultBranch.LatestInstance.CollateInstance).SingleOrDefaultAsync(fun x -> x.Id = stackId)
            |>% Result.requireNotNull (sprintf "Stack #%i not found." stackId)
            |>%% fun stack -> toCommand (NewBranch_SourceStackId_Title (stackId, "New Branch")) stack.DefaultBranch.LatestInstance
        | VNewCopySourceInstanceId branchInstanceId ->
            db.BranchInstance.Include(fun x -> x.CollateInstance).SingleOrDefaultAsync(fun x -> x.Id = branchInstanceId)
            |>% Result.requireNotNull (sprintf "Branch Instance #%i not found." branchInstanceId)
            |>%% toCommand (NewCopy_SourceInstanceId_TagIds (branchInstanceId, []))
        | VUpdateBranchId branchId ->
            db.Branch.Include(fun x -> x.LatestInstance.CollateInstance).SingleOrDefaultAsync(fun x -> x.Id = branchId)
            |>% Result.requireNotNull (sprintf "Branch #%i not found." branchId)
            |>%% fun branch -> toCommand (Update_BranchId_Title (branchId, branch.Name)) branch.LatestInstance
    let Update (db: CardOverflowDb) userId (acCommands: EditAcquiredCardCommand list) (stackCommand: ViewEditStackCommand) = taskResult {
        let! branchInstance = UpdateRepository.stack db userId stackCommand.load
        let! (acs: AcquiredCardEntity list) =
            match stackCommand.Kind with
            | Update_BranchId_Title _ ->
                db.BranchInstance.AddI branchInstance
                StackRepository.acquireCardNoSave db userId branchInstance false
            | NewBranch_SourceStackId_Title _ ->
                StackRepository.acquireCardNoSave db userId branchInstance true
            | NewOriginal_TagIds tagIds
            | NewCopy_SourceInstanceId_TagIds (_, tagIds) -> taskResult {
                let! (acs: AcquiredCardEntity list) = StackRepository.acquireCardNoSave db userId branchInstance true
                for tagId in tagIds do
                    acs.First().Tag_AcquiredCards.Add(Tag_AcquiredCardEntity(TagId = tagId))
                return acs
                }
        do! db.SaveChangesAsyncI()
        return branchInstance.BranchId
        }
    let search (db: CardOverflowDb) userId pageNumber searchCommand =
        StackRepository.search db userId pageNumber searchCommand.Order searchCommand.Query
    let searchDeck (db: CardOverflowDb) userId pageNumber searchCommand deckId =
        StackRepository.searchDeck db userId pageNumber searchCommand.Order searchCommand.Query deckId
    let GetAcquiredPages (db: CardOverflowDb) userId pageNumber searchCommand =
        StackRepository.GetAcquiredPages db userId pageNumber searchCommand.Query

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
            OneIsAlpha2Beta3Ga = signUpForm.OneIsAlpha2Beta3Ga,
            TimeStamp = DateTime.UtcNow
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
    let toString (timespan: TimeSpan) =
        intString timespan.TotalMinutes
    let fromString raw =
        raw |> int |> float |> TimeSpan.FromMinutes
    let toStringList (timespans: TimeSpan list) =
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
    Id: int
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
        NewCardsMaxPerDay = bznz.NewCardsMaxPerDay |> int
        NewCardsGraduatingInterval = bznz.NewCardsGraduatingInterval.TotalDays |> Convert.ToInt32
        NewCardsEasyInterval = bznz.NewCardsEasyInterval.TotalDays |> Convert.ToInt32
        NewCardsStartingEaseFactor = bznz.NewCardsStartingEaseFactor |> Convert.toPercent
        NewCardsBuryRelated = bznz.NewCardsBuryRelated
        MatureCardsMaxPerDay = bznz.MatureCardsMaxPerDay |> int
        MatureCardsEaseFactorEasyBonusFactor = bznz.MatureCardsEaseFactorEasyBonusFactor |> Convert.toPercent
        MatureCardsIntervalFactor = bznz.MatureCardsIntervalFactor |> Convert.toPercent
        MatureCardsMaximumInterval = (TimeSpanInt16.value bznz.MatureCardsMaximumInterval).TotalDays |> Math.Round |> int
        MatureCardsHardInterval = bznz.MatureCardsHardIntervalFactor |> Convert.toPercent
        MatureCardsBuryRelated = bznz.MatureCardsBuryRelated
        LapsedCardsSteps = bznz.LapsedCardsSteps |> Minutes.toStringList
        LapsedCardsNewIntervalFactor = bznz.LapsedCardsNewIntervalFactor |> Convert.toPercent
        LapsedCardsMinimumInterval = bznz.LapsedCardsMinimumInterval.TotalDays |> Convert.ToInt32
        LapsedCardsLeechThreshold = bznz.LapsedCardsLeechThreshold |> int
        ShowAnswerTimer = bznz.ShowAnswerTimer
        AutomaticallyPlayAudio = bznz.AutomaticallyPlayAudio
        ReplayQuestionAudioOnAnswer = bznz.ReplayQuestionAudioOnAnswer
    }
    member this.copyTo: CardSetting = {
        Id = this.Id
        Name = this.Name
        IsDefault = this.IsDefault
        NewCardsSteps = this.NewCardsSteps |> Minutes.fromStringList
        NewCardsMaxPerDay = this.NewCardsMaxPerDay |> int16
        NewCardsGraduatingInterval = this.NewCardsGraduatingInterval |> float |> TimeSpan.FromDays
        NewCardsEasyInterval = this.NewCardsEasyInterval |> float |> TimeSpan.FromDays
        NewCardsStartingEaseFactor = this.NewCardsStartingEaseFactor |> Convert.fromPercent
        NewCardsBuryRelated = this.NewCardsBuryRelated
        MatureCardsMaxPerDay = this.MatureCardsMaxPerDay |> int16
        MatureCardsEaseFactorEasyBonusFactor = this.MatureCardsEaseFactorEasyBonusFactor |> Convert.fromPercent
        MatureCardsIntervalFactor = this.MatureCardsIntervalFactor |> Convert.fromPercent
        MatureCardsMaximumInterval = this.MatureCardsMaximumInterval |> float |> TimeSpanInt16.fromDays
        MatureCardsHardIntervalFactor = this.MatureCardsHardInterval |> Convert.fromPercent
        MatureCardsBuryRelated = this.MatureCardsBuryRelated
        LapsedCardsSteps = this.LapsedCardsSteps |> Minutes.fromStringList
        LapsedCardsNewIntervalFactor = this.LapsedCardsNewIntervalFactor |> Convert.fromPercent
        LapsedCardsMinimumInterval = this.LapsedCardsMinimumInterval |> float |> TimeSpan.FromDays
        LapsedCardsLeechThreshold = this.LapsedCardsLeechThreshold |> int16
        ShowAnswerTimer = this.ShowAnswerTimer
        AutomaticallyPlayAudio = this.AutomaticallyPlayAudio
        ReplayQuestionAudioOnAnswer = this.ReplayQuestionAudioOnAnswer
    }

module SanitizeCardSettingRepository =
    let setCard (db: CardOverflowDb) userId acquiredCardId newCardSettingId = task {
        let! option = db.CardSetting.SingleOrDefaultAsync(fun x -> x.Id = newCardSettingId && x.UserId = userId)
        let! card = db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.Id = acquiredCardId && x.UserId = userId)
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
        let oldOptionIds = options.Select(fun x -> x.Id).Where(fun x -> x <> 0).ToList()
        let! oldOptions = db.CardSetting.Where(fun x -> oldOptionIds.Contains x.Id && x.UserId = userId).ToListAsync()
        let! user = db.User.SingleAsync(fun x -> x.Id = userId)
        return!
            options |> List.map (fun option ->
                let maybeSetDefault e =
                    if option.IsDefault then
                        user.DefaultCardSetting <- e
                if option.Id = 0 then
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

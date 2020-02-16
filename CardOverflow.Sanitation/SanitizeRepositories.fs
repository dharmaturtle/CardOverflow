namespace CardOverflow.Sanitation

open System
open CardOverflow.Pure.Extensions
open System.Threading.Tasks
open CardOverflow.Pure.Core
open LoadersAndCopiers
open FSharp.Control.Tasks
open System.Collections.Generic
open Microsoft.EntityFrameworkCore
open FsToolkit.ErrorHandling
open FSharp.Text.RegexProvider
open Microsoft.FSharp.Core.Operators.Checked
open System.Linq
open Helpers
open System
open CardOverflow.Debug
open CardOverflow.Pure
open CardOverflow.Api
open CardOverflow.Entity
open System.ComponentModel.DataAnnotations

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
        Due = CardRepository.GetDueCount db e.UserId e.Query
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
    let Delete (db: CardOverflowDb) userId (deck: ViewFilter) =
        let d = db.Filter.Single(fun x -> x.Id = deck.Id)
        if d.UserId = userId
        then Ok <| FilterRepository.Delete db d
        else Error <| "That isn't your deck"
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
    [<StringLength(500, ErrorMessage = "Comment must be less than 500 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Text: string
}

module SanitizeCommentRepository =
    let AddAndSaveAsync (db: CardOverflowDb) (time: TimeProvider) (comment: CommentText) cardId email = // lowTODO add idempotency key
        let userId = db.User.First(fun x -> x.Email = email).Id // lowTODO is there a way to get the userId directly from the UI?
        CommentCardEntity(
            CardId = cardId,
            UserId = userId,
            Text = comment.Text,
            Created = time.utcNow
        ) |> CommentRepository.addAndSaveAsync db

[<CLIMutable>]
type TagText = {
    [<StringLength(250, ErrorMessage = "Tag must be less than 250 characters.")>] // medTODO 250 needs to be tied to the DB max somehow
    Text: string
}

module SanitizeTagRepository =
    let validate (db: CardOverflowDb) userId cardId action = // medTODO tag length needs validation
        db.AcquiredCard.FirstOrDefault(fun x -> x.UserId = userId && x.CardInstance.CardId = cardId)
        |> function
        | null -> Error "You haven't gotten that card."
        | card -> Ok <| action card.Id
    let AddTo db tag userId cardId =
        validate db userId cardId
            <| TagRepository.AddTo db tag.Text
    let DeleteFrom db tag userId cardId =
        validate db userId cardId
            <| TagRepository.DeleteFrom db tag

module SanitizeHistoryRepository =
    let AddAndSaveAsync (db: CardOverflowDb) acquiredCardId score timestamp interval easeFactor (timeFromSeeingQuestionToScore: TimeSpan) intervalOrSteps: Task<unit> = task {
        let! card = db.AcquiredCard.SingleAsync(fun x -> x.Id = acquiredCardId)
        card.Histories.Add
        <|  HistoryEntity(
                Score = Score.toDb score,
                Timestamp = timestamp,
                IntervalWithUnusedStepsIndex = (interval |> Interval |> IntervalOrStepsIndex.intervalToDb),
                EaseFactorInPermille = (easeFactor * 1000. |> Math.Round |> int16),
                TimeFromSeeingQuestionToScoreInSecondsPlus32768 = (timeFromSeeingQuestionToScore.TotalSeconds + float Int16.MinValue |> int16)
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
    SourceInstanceId: int
    [<Required>] // Source is the InstanceId, while the Target is the CardId. The target link only contains the the CardId - we get the TargetInstanceId by finding the Instance they acquired
    TargetCardLink: string
}
type CardIdRegex = Regex< """(?<cardId>\d+)$""" >
module SanitizeRelationshipRepository =
    let GetCardId input =
        let x = CardIdRegex().TypedMatch input // lowTODO make this a custom `ValidationAttribute` on TargetLink
        if x.Success 
        then Ok <| int x.Value
        else Error "Couldn't find the card ID"
    let Add (db: CardOverflowDb) userId command = taskResult {
        let! targetCardId = GetCardId command.TargetCardLink
        let! (acs: AcquiredCardEntity ResizeArray) = db.AcquiredCard.Where(fun x -> x.UserId = userId && (x.CardInstance.CardId = targetCardId || x.CardInstanceId = command.SourceInstanceId)).ToListAsync()
        let! t = acs.SingleOrDefault(fun x -> x.CardInstanceId <> command.SourceInstanceId) |> Result.ofNullable "You haven't acquired the linked card."
        let! s = acs.SingleOrDefault(fun x -> x.CardInstanceId = command.SourceInstanceId) |> Result.ofNullable "You haven't acquired the source card."
        let! r = db.Relationship.SingleOrDefaultAsync(fun x -> x.Name = command.Name)
        let r = r |> Option.ofObj |> Option.defaultValue (RelationshipEntity(Name = command.Name))
        return!
            Relationship_AcquiredCardEntity(
                Relationship = r,
                SourceAcquiredCardId = s.Id,
                TargetAcquiredCardId = t.Id
            ) |> RelationshipRepository.addAndSaveAsync db
        }
    let Remove db sourceId targetId userId name =
        RelationshipRepository.removeAndSaveAsync db sourceId targetId userId name // don't eta reduce - consumed by C#

[<CLIMutable>]
type SearchCommand = {
    [<StringLength(250, ErrorMessage = "Query must be less than 250 characters.")>]
    Query: string
}

[<CLIMutable>]
type ViewEditCardCommand = {
    [<Required>]
    [<StringLength(200, ErrorMessage = "The summary must be less than 200 characters")>]
    EditSummary: string
    FieldValues: EditFieldAndValue ResizeArray
    TemplateInstance: ViewTemplateInstance
} with
    member this.Backs = 
        let valueByFieldName = this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> Map.ofSeq // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls
        if this.TemplateInstance.IsCloze then
            result {
                let! max = AnkiImportLogic.maxClozeIndex "Something's wrong with your cloze indexes." valueByFieldName this.TemplateInstance.QuestionTemplate
                return [1 .. max] |> List.map byte |> List.map (fun clozeIndex ->
                    let zip =
                        Seq.zip
                            <| (valueByFieldName |> Seq.map (fun (KeyValue(k, _)) -> k))
                            <| (valueByFieldName |> Seq.map (fun (KeyValue(_, v)) -> v) |> List.ofSeq |> AnkiImportLogic.multipleClozeToSingleClozeList clozeIndex)
                        |> List.ofSeq
                    CardHtml.generate
                        zip
                        this.TemplateInstance.QuestionTemplate
                        this.TemplateInstance.AnswerTemplate
                        this.TemplateInstance.Css
                    |> fun (_, back, _, _) -> back
                    ) |> toResizeArray
            }
        else
            CardHtml.generate
                <| this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "").ToFList()
                <| this.TemplateInstance.QuestionTemplate
                <| this.TemplateInstance.AnswerTemplate
                <| this.TemplateInstance.Css
            |> fun (_, back, _, _) -> [back].ToList()
            |> Ok
    member this.load =
        {   EditCardCommand.EditSummary = this.EditSummary
            FieldValues = this.FieldValues
            TemplateInstance = this.TemplateInstance |> ViewTemplateInstance.copyTo
        }
    member this.CommunalFieldValues =
        this.FieldValues.Where(fun x -> x.IsCommunal).ToList()
    member this.CommunalNonClozeFieldValues =
        this.CommunalFieldValues
            .Where(fun x -> not <| this.TemplateInstance.ClozeFields.Contains x.EditField.Name)
            .ToList()

module SanitizeCardRepository =
    let getEdit (db: CardOverflowDb) cardInstanceId = task {
        let! instance =
            db.CardInstance
                .Include(fun x -> x.TemplateInstance)
                .Include(fun x -> x.CommunalFieldInstance_CardInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_CardInstanceEntity) -> x.CommunalFieldInstance.CommunalFieldInstance_CardInstances)
                .SingleOrDefaultAsync(fun x -> x.Id = cardInstanceId)
        return
            match instance with
            | null -> Error "Card instance not found"
            | instance ->
                let communalCardInstanceIdsAndValueByField =
                    instance.CommunalFieldInstance_CardInstances
                        .Select(fun x ->
                            let ids = x.CommunalFieldInstance.CommunalFieldInstance_CardInstances.Select(fun x -> x.CardInstanceId).ToList()
                            x.CommunalFieldInstance.FieldName,
                            (   x.CommunalFieldInstance.Value,
                                if ids.Any() then
                                    {   CommunalCardInstanceIds = ids
                                        InstanceId = None
                                    } |> Some
                                else None))
                    |> Map.ofSeq
                {   EditSummary = ""
                    FieldValues =
                        EditFieldAndValue.load
                            <| Fields.fromString instance.TemplateInstance.Fields
                            <| instance.FieldValues
                            <| communalCardInstanceIdsAndValueByField
                    TemplateInstance = instance.TemplateInstance |> TemplateInstance.load |> ViewTemplateInstance.load
                } |> Ok }
    let Update (db: CardOverflowDb) authorId (acquiredCard: AcquiredCard) (command: ViewEditCardCommand) = task { // medTODO how do we know that the card id hasn't been tampered with? It could be out of sync with card instance id
        let required = command.TemplateInstance.ClozeFields |> Set.ofSeq // medTODO query db for real template instance
        let actual = command.CommunalFieldValues.Select(fun x -> x.EditField.Name) |> Set.ofSeq
        let! card = db.Card.SingleOrDefaultAsync(fun x -> x.Id = acquiredCard.CardId) // lowTODO optimize by moving into `else`
        return!
            if Set.isSubset required actual then
                let update () = CardRepository.UpdateFieldsToNewInstance db acquiredCard command.load
                match card with
                | null ->
                    update ()
                | card ->
                    if card.AuthorId = authorId
                    then update ()
                    else "You aren't that card's author." |> Error |> Task.FromResult
            else
                Set.difference required actual |> String.concat ", " |> sprintf "The following cloze fields must be communal: %s" |> Error |> Task.FromResult
        }
    let SearchAsync (db: CardOverflowDb) userId pageNumber searchCommand =
        CardRepository.SearchAsync db userId pageNumber searchCommand.Query
    let GetAcquiredPages (db: CardOverflowDb) userId pageNumber searchCommand =
        CardRepository.GetAcquiredPages db userId pageNumber searchCommand.Query

[<CLIMutable>]
type PotentialSignupCommand = {
    [<Required>]
    [<EmailAddress>]
    Email: string
    [<StringLength(1000, ErrorMessage = "Message must be less than 1000 characters.")>]
    Message: string
    OneIsAlpha2Beta3Ga: byte
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
        FeedbackRepository.addAndSaveAsync db userId feedback.Title feedback.Description (feedback.Priority |> byte |> Nullable)

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
        LapsedCardsLeechThreshold = this.LapsedCardsLeechThreshold |> byte
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
                    | null -> Error "Card option not found (or doesn't belong to you.)"
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

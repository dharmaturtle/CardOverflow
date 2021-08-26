namespace CardOverflow.Api

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

[<CLIMutable>]
type TagText = {
    [<StringLength(250, ErrorMessage = "Tag must be less than 250 characters.")>] // medTODO 250 needs to be tied to the DB max somehow
    Text: string
}

module SanitizeDeckRepository =
    let private verifyVisible (db: CardOverflowDb) userId deckId =
        db.Deck.AnyAsync(fun x -> x.Id = deckId && (x.IsPublic || x.UserId = userId))
        |>% Result.requireTrue (sprintf "Either Deck #%A doesn't exist, or it isn't public, or you don't own it." deckId)
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
    [<StringLength(2000, ErrorMessage = "The Front Personal Field must be less than 2000 characters")>]
    FrontPersonalField: string
    [<StringLength(2000, ErrorMessage = "The Back Personal Field must be less than 2000 characters")>]
    BackPersonalField: string
} with
    static member init = {
        CardState = Normal
        CardSettingId = % Guid.Empty
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
    DeckIds: DeckId ResizeArray
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
            DeckIds = ResizeArray.empty
            Upsert = Insert
            SourceExampleId = None
            ExampleRevisionId = % Guid.NewGuid(), Example.Fold.initialOrdinal
            StackId = % Guid.NewGuid()
        }
    static member edit templateInstance (example: Projection.ExampleInstance) stackId =
        {   EditSummary = ""
            TemplateInstance = templateInstance
            FieldValues = example.FieldValues.ToList()
            Title = example.Title
            DeckIds = ResizeArray.empty
            SourceExampleId = None // highTODO add Source to Example's Summary then use it here
            ExampleRevisionId = example.ExampleId, example.Ordinal + 1<exampleOrdinal>
            Upsert = Update
            StackId = stackId
        }
    static member fork templateInstance (example: Projection.ExampleInstance) =
        { ViewEditConceptCommand.edit templateInstance example (% Guid.NewGuid()) with
            SourceExampleId = Some example.ExampleId }
    member this.toEvent meta (cardCommands: EditCardCommand list) defaultEase =
        let fieldValues = this.FieldValues |> Seq.toList
        let template = this.TemplateInstance |> Projection.toTemplateRevision
        let pointers = PublicTemplate.getCardTemplatePointers template fieldValues |> Result.getOk
        if pointers.Length <> cardCommands.Length then failwith "CardTemplatePointers and CardCommands do not have matching lengths"
        match this.Upsert with
        | Insert ->
            let exampleId = % Guid.NewGuid()
            let exampleEvent: Example.Events.Created =
                { Meta = meta
                  Id = exampleId
                  ParentId = this.SourceExampleId
                  AnkiNoteId = None
                  Title = this.Title
                  TemplateRevisionId = this.TemplateInstance.Id
                  FieldValues = fieldValues
                  EditSummary = this.EditSummary }
            let stackEvent =
                let stackId = % Guid.NewGuid()
                List.zip pointers cardCommands
                |> List.map (fun (pointer, cardCommand) -> Stack.initCard meta.ClientCreatedAt cardCommand.CardSettingId defaultEase pointer)
                |> Stack.init stackId meta this.TemplateInstance.Id (this.DeckIds |> Set.ofSeq) this.Title fieldValues
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
                  DeckIds = this.DeckIds |> Set.ofSeq
                  CardEdits =
                    List.zip pointers cardCommands
                    |> List.map (fun (pointer, cardCommand) ->
                        ({ Pointer       = pointer
                           CardSettingId = cardCommand.CardSettingId
                           State         = cardCommand.CardState }: Stack.Events.CardEdited)) }
            exampleEvent |> Example.Events.Event.Edited,
            stackEvent   |> Stack.Events.Event.Edited

module SanitizeConceptRepository =
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

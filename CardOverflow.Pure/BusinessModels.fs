namespace CardOverflow.Pure

open FsToolkit.ErrorHandling
open System.Runtime.InteropServices
open CardOverflow.Pure.Extensions
open CardOverflow.Debug
open System
open System.Linq
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open NodaTime

type Score = | Again | Hard | Good | Easy
module Score =
    let create =
        function
        | 0s -> Ok Again
        | 1s -> Ok Hard
        | 2s -> Ok Good
        | 3s -> Ok Easy
        | x -> sprintf "Invalid Score in database: %A" x |> Error
    let toDb =
        function
        | Again -> 0
        | Hard -> 1
        | Good -> 2
        | Easy -> 3
        >> int16

type CardState = | Normal | SchedulerBuried | UserBuried | Suspended
module CardState =
    let create =
        function
        | 0s -> Ok Normal
        | 1s -> Ok SchedulerBuried
        | 2s -> Ok UserBuried
        | 3s -> Ok Suspended
        | x -> sprintf "Invalid CardState in database: %A" x |> Error
    let toDb =
        function
        | Normal -> 0
        | SchedulerBuried -> 1
        | UserBuried -> 2
        | Suspended -> 3
        >> int16

type CardSetting = {
    Id: Guid
    Name: string
    IsDefault: bool
    NewCardsSteps: Duration list
    NewCardsMaxPerDay: int
    NewCardsGraduatingInterval: Duration
    NewCardsEasyInterval: Duration
    NewCardsStartingEaseFactor: float
    NewCardsBuryRelated: bool
    MatureCardsMaxPerDay: int
    MatureCardsEaseFactorEasyBonusFactor: float
    MatureCardsIntervalFactor: float // medTODO unused
    MatureCardsMaximumInterval: Duration
    MatureCardsHardIntervalFactor: float
    MatureCardsBuryRelated: bool
    LapsedCardsSteps: Duration list
    LapsedCardsNewIntervalFactor: float // percent by which to multiply the current interval when a card goes has lapsed, called "new interval" in anki gui
    LapsedCardsMinimumInterval: Duration
    LapsedCardsLeechThreshold: int
    ShowAnswerTimer: bool
    AutomaticallyPlayAudio: bool
    ReplayQuestionAudioOnAnswer: bool
}

module CardSetting =
    let defaultCardSettings id name isDefault =
        { Id = id
          Name = name
          IsDefault = isDefault
          NewCardsSteps = [ Duration.FromMinutes 1.; Duration.FromMinutes 10. ]
          NewCardsMaxPerDay = 20
          NewCardsGraduatingInterval = Duration.FromDays 1.
          NewCardsEasyInterval = Duration.FromDays 4.
          NewCardsStartingEaseFactor = 2.5
          NewCardsBuryRelated = true
          MatureCardsMaxPerDay = 200
          MatureCardsEaseFactorEasyBonusFactor = 1.3
          MatureCardsIntervalFactor = 1.
          MatureCardsMaximumInterval = 36500. |> Duration.FromDays
          MatureCardsHardIntervalFactor = 1.2
          MatureCardsBuryRelated = true
          LapsedCardsSteps = [ Duration.FromMinutes 10. ]
          LapsedCardsNewIntervalFactor = 0.
          LapsedCardsMinimumInterval = Duration.FromDays 1.
          LapsedCardsLeechThreshold = 8
          ShowAnswerTimer = false
          AutomaticallyPlayAudio = false
          ReplayQuestionAudioOnAnswer = false }
    let newUserCardSettings id = defaultCardSettings id "Default" true

type Field = {
    Name: string
    IsRightToLeft: bool
    IsSticky: bool
} with
    member this.toString =
        [   this.Name
            this.IsRightToLeft |> string
            this.IsSticky |> string
        ] |> MappingTools.joinByUnitSeparator
    static member fromString x =
        let x = x |> MappingTools.splitByUnitSeparator
        {   Name = x.[0]
            IsRightToLeft = x.[1] = "True"
            IsSticky = x.[2] = "True"
        }

module Fields =
    let toString: (Field seq -> string) =
        Seq.map (fun x -> x.toString)
        >> MappingTools.joinByRecordSeparator
    let fromString =
        MappingTools.splitByRecordSeparator
        >> List.map Field.fromString

[<CLIMutable>]
type CardTemplate = {
    Name: string
    [<RegularExpression(@"^[^\x1c\x1d\x1e\x1f]*$", ErrorMessage = "Unit, record, group, and file separators are not permitted.")>]
    Front: string
    [<RegularExpression(@"^[^\x1c\x1d\x1e\x1f]*$", ErrorMessage = "Unit, record, group, and file separators are not permitted.")>]
    Back: string
    [<RegularExpression(@"^[^\x1c\x1d\x1e\x1f]*$", ErrorMessage = "Unit, record, group, and file separators are not permitted.")>]
    ShortFront: string
    [<RegularExpression(@"^[^\x1c\x1d\x1e\x1f]*$", ErrorMessage = "Unit, record, group, and file separators are not permitted.")>]
    ShortBack: string
} with
    member this.FrontBackFrontSynthBackSynth css = // medTODO split this up
        CardHtml.generate [] this.Front this.Back css CardHtml.Standard
    static member initStandard =
        {   Name = "New Card Template"
            Front = """{{Front}}"""
            Back = """{{FrontSide}}

<hr id=answer>

{{Back}}"""
            ShortFront = ""
            ShortBack = ""
        }

type TemplateType =
    | Standard of CardTemplate list
    | Cloze of CardTemplate
  with
    member this.toDb =
        match this with
        | Standard _ -> 0s
        | Cloze _ -> 1s
    static member fromDb cardTemplates =
        function
        | 0s -> Standard cardTemplates
        | 1s -> Cloze <| cardTemplates.Single()
        | x -> failwith <| sprintf "Unable to convert '%i' to a TemplateType" x
    static member initStandard =
        CardTemplate.initStandard |> List.singleton |> Standard

type TemplateRevision = {
    Id: Guid
    Name: string
    TemplateId: Guid
    Css: string
    Fields: Field list
    Created: Instant
    Modified: Instant option
    LatexPre: string
    LatexPost: string
    CardTemplates: TemplateType
    EditSummary: string
} with
    member this.JustCardTemplates =
        match this.CardTemplates with
        | Cloze t -> [t]
        | Standard ts -> ts
    member this.IsCloze =
        match this.CardTemplates with
        | Cloze _ -> true
        | _ -> false
    member this.FrontBackFrontSynthBackSynth () = // medTODO split this up
        let fieldNameValueMap = this.Fields.Select(fun x -> x.Name, x.Name + " field") |> Seq.toList
        match this.CardTemplates with
        | Standard ts -> 
            ts.Select(fun t ->
                CardHtml.generate fieldNameValueMap t.Front t.Back this.Css CardHtml.Standard
            ).ToList()
        | Cloze t ->
            CardHtml.generate fieldNameValueMap t.Front t.Back this.Css (CardHtml.Cloze 0s)
            |> List.singleton |> toResizeArray
    member this.FrontBackFrontSynthBackSynthIndexed i =
        this.FrontBackFrontSynthBackSynth ()
        |> Seq.tryItem i
        |> Result.requireSome (sprintf "Index %i out of range" i)

type CollectedTemplateRevision = {
    DefaultTags: string seq
    DefaultCardSettingId: Guid
    TemplateRevision: TemplateRevision
}

type Template = {
    Id: Guid
    AuthorId: Guid
    Latest: TemplateRevision
}

type IntervalOrStepsIndex =
    | NewStepsIndex of byte
    | LapsedStepsIndex of byte
    | IntervalXX of Duration

type QuizCard = {
    CardId: Guid
    RevisionId: Guid
    Due: Instant
    Front: string
    Back: string
    FrontSynthVoice: string
    BackSynthVoice: string
    CardState: CardState
    IsLapsed: bool
    EaseFactor: float
    IntervalOrStepsIndex: IntervalOrStepsIndex
    Settings: CardSetting
}

//type Revision = {
//    Id: Guid
//    Created: Instant
//    Modified: Instant option
//    Fields: string seq
//}

// medTODO delete?
//type CollectedDisplayCard = { // Collected cause only private tags can be on a card
//    TemplateName: string
//    Front: string
//    Back: string
//    Tags: string seq
//}

[<CLIMutable>]
type FieldAndValue = {
    Field: Field
    Value: string
}

module IntervalOrStepsIndex =
    //                 255            |             255             |            1439         | <- The value of this is 1, not 0, cause 0 days is 0 minutes
    //       |------------------------|-----------------------------|-------------------------|-------------------|
    //           New Step Indexes   n1|l0   Lapsed Step Indexes   l1|m0         Minutes       |d0      Days
    let minutesInADay = TimeSpan.FromDays(1.).TotalMinutes
    let n1 = Int16.MinValue + int16 Byte.MaxValue |> float
    let l0 = n1 + 1.
    let l1 = l0 + float Byte.MaxValue
    let m0 = l1 + 1.
    let d0 = m0 + float minutesInADay
    let intervalFromDb (x: int16) =
        let x = float x
        if x <= n1
        then x - float Int16.MinValue |> byte |> NewStepsIndex
        elif x > d0 // exclusive because we start counting at 1
        then x - d0 |> float |> Duration.FromDays |> IntervalXX
        elif l0 <= x && x <= l1
        then x - float l0 |> byte |> LapsedStepsIndex
        else x - m0 |> float |> Duration.FromMinutes |> IntervalXX
    let intervalToDb =
        function
        | NewStepsIndex x ->
            int16 x + Int16.MinValue
        | LapsedStepsIndex x ->
            int16 x + int16 l0
        | IntervalXX x ->
            if x.TotalMinutes >= minutesInADay
            then x.TotalDays + d0
            else x.TotalMinutes + m0
            |> int16

//[<CLIMutable>]
//type CollectedConcept = {
//    Id: Guid
//    // medTODO 100 needs to be tied to the DB max somehow
//    [<StringLength(100, ErrorMessage = "Name must be less than 100 characters.")>] Name: string
//    AuthorId: Guid
//    Cards: Card ResizeArray
//}

type PagedListDetails = {
    CurrentPage: int
    PageCount: int
}

type PagedList<'T> = {
    Results: 'T seq
    Details: PagedListDetails
}

[<CLIMutable>]
type CommieldValue = {
    RevisionId: Guid option
    CommunalRevisionIds: int ResizeArray
}

[<CLIMutable>]
type EditFieldAndValue = {
    EditField: Field
    [<RegularExpression(@"^[^\x1c\x1d\x1e\x1f]*$", ErrorMessage = "Unit, record, group, and file separators are not permitted.")>]
    [<StringLength(10_000)>]
    Value: string
}

module Helper =
    let maxIndexInclusive cardTemplate valueByFieldName =
        match cardTemplate with
        | Cloze t ->
            let max = AnkiImportLogic.maxClozeIndex "Something's wrong with your cloze indexes." valueByFieldName t.Front |> Result.getOk
            max - 1s
        | Standard ts ->
            (ts.Length |> int16) - 1s

type RevisionView = {
    FieldValues: FieldAndValue ResizeArray
    TemplateRevision: TemplateRevision
} with
    member this.MaxIndexInclusive =
        Helper.maxIndexInclusive
            (this.TemplateRevision.CardTemplates)
            (this.FieldValues.Select(fun x -> x.Field.Name, x.Value |?? lazy "") |> Map.ofSeq) // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls
    member this.Indexes = [0s .. this.MaxIndexInclusive]
    member this.FrontBackFrontSynthBackSynth = // medTODO split this up
        match this.TemplateRevision.CardTemplates with
        | Standard ts -> 
            ts.Select(fun t ->
                CardHtml.generate
                <| this.FieldValues.Select(fun x -> x.Field.Name, x.Value |?? lazy "").ToFList()
                <| t.Front
                <| t.Back
                <| this.TemplateRevision.Css
                <| CardHtml.Standard
            ).ToList()
        | Cloze t ->
            [0s .. this.MaxIndexInclusive] |> List.map(fun i ->
                CardHtml.generate
                <| this.FieldValues.Select(fun x -> x.Field.Name, x.Value |?? lazy "").ToFList()
                <| t.Front
                <| t.Back
                <| this.TemplateRevision.Css
                <| CardHtml.Cloze i
            ) |> toResizeArray
    member this.FrontBackFrontSynthBackSynthIndex i =
        this.FrontBackFrontSynthBackSynth
        |> Seq.tryItem i
        |> Result.requireSome (sprintf "Index %i out of range" i)
        

type Commeaf = {
    Id: Guid
    FieldName: string
    Value: string
}

[<CLIMutable>]
type SimpleDeck = {
    Id: Guid
    IsDefault: bool
    Name: string
}

[<CLIMutable>]
type ViewDeck = {
    Id: Guid
    IsPublic: bool
    IsDefault: bool
    [<StringLength(250, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 250 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Name: string
    DueCount: int
    AllCount: int
    SourceDeck: IdName option
}

[<CLIMutable>]
type ViewTag = {
    Name: string
    Count: int
    IsCollected: bool
}

[<CLIMutable>]
type ViewRelationship = {
    Name: string
    SourceConceptId: Guid
    TargetConceptId: Guid
    IsCollected: bool
    Users: int
} with
    member this.PrimaryName =
        Relationship.split this.Name |> fst
    member this.SecondaryName =
        Relationship.split this.Name |> snd

[<CLIMutable>]
type RevisionMeta = {
    Id: Guid
    ConceptId: Guid
    ExampleId: Guid
    MaxIndexInclusive: int16
    Created: Instant
    Modified: Instant option
    IsDmca: bool
    IsCollected: bool
    IsLatest: bool
    StrippedFront: string
    StrippedBack: string
    Commields: Commeaf ResizeArray
    Users: int
    EditSummary: string
} with
    member this.Indexes = [0s .. this.MaxIndexInclusive]

[<CLIMutable>]
type Card = {
    CardId: Guid
    UserId: Guid
    ConceptId: Guid
    ExampleId: Guid
    RevisionMeta: RevisionMeta
    Index: int16
    CardState: CardState
    IsLapsed: bool
    EaseFactorInPermille: int16
    IntervalOrStepsIndex: IntervalOrStepsIndex
    Due: Instant
    CardSettingId: Guid
    Tags: string list
    DeckId: Guid
}

type Comment = {
    User: string
    UserId: Guid
    Text: string
    Created: Instant
    IsDmca: bool
}

[<CLIMutable>]
type ExploreConceptSummary = {
    Id: Guid
    Users: int
    Author: string
    AuthorId: Guid
    Revision: RevisionMeta
} with
    member this.IsCollected = this.Revision.IsCollected

[<CLIMutable>]
type ExploreExampleSummary = {
    Id: Guid
    Users: int
    Author: string
    AuthorId: Guid
    Revision: RevisionMeta
} with
    member this.IsCollected = this.Revision.IsCollected

type Example = {
    Name: string
    Summary: ExploreExampleSummary
} with
    member this.Id = this.Summary.Id
    member this.Users = this.Summary.Users
    member this.Author = this.Summary.Author
    member this.AuthorId = this.Summary.AuthorId
    member this.Revision = this.Summary.Revision

type CollectedIds = UpsertIds Option

module CollectedIds =
    let revisionId =
        function
        | Some (x: UpsertIds) -> x.RevisionId
        | None -> Guid.Empty
    let exampleId =
        function
        | Some x -> x.ExampleId
        | None -> Guid.Empty
    let conceptId =
        function
        | Some x -> x.ConceptId
        | None -> Guid.Empty

[<CLIMutable>]
type ExploreConcept = {
    Id: Guid
    Users: int
    Tags: ViewTag ResizeArray
    Relationships: ViewRelationship ResizeArray
    Comments: Comment ResizeArray
    CollectedIds: CollectedIds
    Examples: Example ResizeArray
} with
    //don't add users - the UI needs it to be mutable
    member this.Default = this.Examples.Single(fun x -> x.Name = null)
    member this.Author = this.Default.Author
    member this.AuthorId = this.Default.AuthorId
    member this.IsAnyCollected =
        this.CollectedIds |> Option.isSome

type ExampleRevision = {
    Id: Guid
    Author: string
    AuthorId: Guid
    Name: string
    SortedMeta: RevisionMeta list
}

type UpsertKind =
    | NewOriginal_TagIds of string Set
    | NewCopy_SourceRevisionId_TagIds of Guid * string Set
    | NewExample_Title of string
    | NewRevision_Title of string
with
    member this.TryGetCopySourceRevisionId([<Out>] x:byref<_>) = // https://stackoverflow.com/a/17264768
        match this with
        | NewCopy_SourceRevisionId_TagIds (revisionId, _) -> x <- revisionId; true
        | _ -> false


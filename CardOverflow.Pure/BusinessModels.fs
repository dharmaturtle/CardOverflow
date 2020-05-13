namespace CardOverflow.Pure

open FsToolkit.ErrorHandling
open System.Runtime.InteropServices
open CardOverflow.Pure.Extensions
open CardOverflow.Pure.Core
open System
open System.Linq
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations

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

module TimeSpanInt16 =
    type TimeSpanInt16 = private TimeSpanInt16 of TimeSpan
    let fromDays days = min (float Int16.MaxValue) days |> TimeSpan.FromDays |> TimeSpanInt16
    let value (TimeSpanInt16 t) = t
    let totalDays t = (value t).TotalDays |> int16

type CardSetting = {
    Id: int
    Name: string
    IsDefault: bool
    NewCardsSteps: TimeSpan list
    NewCardsMaxPerDay: int16
    NewCardsGraduatingInterval: TimeSpan
    NewCardsEasyInterval: TimeSpan
    NewCardsStartingEaseFactor: float
    NewCardsBuryRelated: bool
    MatureCardsMaxPerDay: int16
    MatureCardsEaseFactorEasyBonusFactor: float
    MatureCardsIntervalFactor: float // medTODO unused
    MatureCardsMaximumInterval: TimeSpanInt16.TimeSpanInt16
    MatureCardsHardIntervalFactor: float
    MatureCardsBuryRelated: bool
    LapsedCardsSteps: TimeSpan list
    LapsedCardsNewIntervalFactor: float // percent by which to multiply the current interval when a card goes has lapsed, called "new interval" in anki gui
    LapsedCardsMinimumInterval: TimeSpan
    LapsedCardsLeechThreshold: int16
    ShowAnswerTimer: bool
    AutomaticallyPlayAudio: bool
    ReplayQuestionAudioOnAnswer: bool
}

type Field = {
    Name: string
    IsRightToLeft: bool
    Ordinal: byte
    IsSticky: bool
} with
    member this.toString =
        [   this.Name
            this.IsRightToLeft |> string
            this.Ordinal |> string
            this.IsSticky |> string
        ] |> MappingTools.joinByUnitSeparator
    static member fromString x =
        let x = x |> MappingTools.splitByUnitSeparator
        {   Name = x.[0]
            IsRightToLeft = x.[1] = "True"
            Ordinal = x.[2] |> byte
            IsSticky = x.[3] = "True"
        }

module Fields =
    let toString: (Field seq -> string) =
        Seq.map (fun x -> x.toString)
        >> MappingTools.joinByRecordSeparator
    let fromString =
        MappingTools.splitByRecordSeparator
        >> List.map Field.fromString

type Template = {
    Name: string
    Front: string
    Back: string
    ShortFront: string
    ShortBack: string
}

type CollateType =
    | Standard of Template list
    | Cloze of Template
  with
    member this.toDb =
        match this with
        | Standard -> 0s
        | Cloze -> 1s
    static member fromDb templates =
        function
        | 0s -> Standard templates
        | 1s -> Cloze <| templates.Single()
        | x -> failwith <| sprintf "Unable to convert '%i' to a CollateType" x

type CollateInstance = {
    Id: int
    Name: string
    CollateId: int
    Css: string
    Fields: Field list
    Created: DateTime
    Modified: DateTime option
    LatexPre: string
    LatexPost: string
    Templates: CollateType
    EditSummary: string
} with
    member this.FirstTemplate =
        match this.Templates with
        | Cloze t -> t
        | Standard ts -> ts.[0]
    member this.JustTemplates =
        match this.Templates with
        | Cloze t -> [t]
        | Standard ts -> ts
    member this.IsCloze =
        match this.Templates with
        | Cloze -> true
        | _ -> false
    member this.ClozeFields =
        match this.Templates with
        | Cloze x -> AnkiImportLogic.clozeFields x.Front
        | _ -> failwith "Not a cloze"
    member this.FrontBackFrontSynthBackSynth i =
        this.JustTemplates
        |> List.map (fun t -> CardHtml.generate [] t.Front t.Back this.Css)
        |> List.tryItem i
        |> Result.requireSome (sprintf "Index %i out of range" i)

type AcquiredCollateInstance = {
    DefaultTags: int seq
    DefaultCardSettingId: int
    CollateInstance: CollateInstance
}

type Collate = {
    Id: int
    AuthorId: int
    LatestInstance: CollateInstance
}

type IntervalOrStepsIndex =
    | NewStepsIndex of byte
    | LapsedStepsIndex of byte
    | Interval of TimeSpan

type QuizCard = {
    AcquiredCardId: int
    BranchInstanceId: int
    Due: DateTime
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

//type BranchInstance = {
//    Id: int
//    Created: DateTime
//    Modified: DateTime option
//    Fields: string seq
//}

// medTODO delete?
//type AcquiredDisplayCard = { // Acquired cause only private tags can be on a card
//    CollateName: string
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
        then x - d0 |> float |> TimeSpan.FromDays |> Interval
        elif l0 <= x && x <= l1
        then x - float l0 |> byte |> LapsedStepsIndex
        else x - m0 |> float |> TimeSpan.FromMinutes |> Interval
    let intervalToDb =
        function
        | NewStepsIndex x ->
            int16 x + Int16.MinValue
        | LapsedStepsIndex x ->
            int16 x + int16 l0
        | Interval x ->
            if x.TotalMinutes >= minutesInADay
            then x.TotalDays + d0
            else x.TotalMinutes + m0
            |> int16

//[<CLIMutable>]
//type AcquiredConcept = {
//    Id: int
//    // medTODO 100 needs to be tied to the DB max somehow
//    [<StringLength(100, ErrorMessage = "Name must be less than 100 characters.")>] Name: string
//    AuthorId: int
//    AcquiredCards: AcquiredCard ResizeArray
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
type CommunalFieldValue = {
    InstanceId: int option
    CommunalBranchInstanceIds: int ResizeArray
}

[<CLIMutable>]
type EditFieldAndValue = {
    EditField: Field
    [<StringLength(10000)>]
    Value: string
}

type BranchInstanceView = {
    FieldValues: FieldAndValue ResizeArray
    CollateInstance: CollateInstance
} with
    member this.FrontBackFrontSynthBackSynth = // medTODO split this up
        this.CollateInstance.JustTemplates.Select(fun t ->
            CardHtml.generate
            <| this.FieldValues.Select(fun x -> x.Field.Name, x.Value |?? lazy "").ToFList()
            <| t.Front
            <| t.Back
            <| this.CollateInstance.Css
        ).ToList()
    member this.FrontBackFrontSynthBackSynthIndex i =
        this.FrontBackFrontSynthBackSynth
        |> Seq.tryItem i
        |> Result.requireSome (sprintf "Index %i out of range" i)
        

type CommunalFieldInstance = {
    Id: int
    FieldName: string
    Value: string
}

[<CLIMutable>]
type ViewTag = {
    Name: string
    Count: int
    IsAcquired: bool
}

[<CLIMutable>]
type ViewRelationship = {
    Name: string
    SourceCardId: int
    TargetCardId: int
    IsAcquired: bool
    Users: int
} with
    member this.PrimaryName =
        Relationship.split this.Name |> fst
    member this.SecondaryName =
        Relationship.split this.Name |> snd

[<CLIMutable>]
type BranchInstanceMeta = {
    Id: int
    CardId: int
    BranchId: int
    MaxIndexInclusive: int16
    Created: DateTime
    Modified: DateTime option
    IsDmca: bool
    IsAcquired: bool
    IsLatest: bool
    StrippedFront: string
    StrippedBack: string
    CommunalFields: CommunalFieldInstance ResizeArray
    Users: int
}

[<CLIMutable>]
type AcquiredCard = {
    AcquiredCardId: int
    UserId: int
    CardId: int
    BranchId: int
    BranchInstanceMeta: BranchInstanceMeta
    CardState: CardState
    IsLapsed: bool
    EaseFactorInPermille: int16
    IntervalOrStepsIndex: IntervalOrStepsIndex
    Due: DateTime
    CardSettingId: int
    Tags: string list
}

type Comment = {
    User: string
    UserId: int
    Text: string
    Created: DateTime
    IsDmca: bool
}

[<CLIMutable>]
type ExploreBranchSummary = {
    Id: int
    Users: int
    Author: string
    AuthorId: int
    Instance: BranchInstanceMeta
} with
    member this.IsAcquired = this.Instance.IsAcquired

type ExploreCardAcquiredStatus =
    | ExactInstanceAcquired of int // card instance ids
    | OtherInstanceAcquired of int
    | LatestBranchAcquired of int
    | OtherBranchAcquired of int
    | NotAcquired
    with
        member this.InstanceId =
            match this with
            | ExactInstanceAcquired x -> Some x
            | OtherInstanceAcquired x -> Some x
            | LatestBranchAcquired x -> Some x
            | OtherBranchAcquired x -> Some x
            | NotAcquired -> None

type Branch = {
    Name: string
    Summary: ExploreBranchSummary
} with
    member this.Id = this.Summary.Id
    member this.Users = this.Summary.Users
    member this.Author = this.Summary.Author
    member this.AuthorId = this.Summary.AuthorId
    member this.Instance = this.Summary.Instance

[<CLIMutable>]
type ExploreCard = {
    Id: int
    Summary: ExploreBranchSummary
    Tags: ViewTag ResizeArray
    Relationships: ViewRelationship ResizeArray
    Comments: Comment ResizeArray
    AcquiredStatus: ExploreCardAcquiredStatus
    Branches: Branch ResizeArray
} with
    //don't add users - the UI needs it to be mutable
    member this.Author = this.Summary.Author
    member this.AuthorId = this.Summary.AuthorId
    member this.Instance = this.Summary.Instance
    member this.IsAnyAcquired =
        match this.AcquiredStatus with
        | NotAcquired -> false
        | _ -> true

type BranchRevision = {
    Id: int
    Author: string
    AuthorId: int
    SortedMeta: BranchInstanceMeta list
}

type CardSource =
    | NewOriginal
    | NewCopySourceInstanceId of int
    | NewBranchSourceCardId of int * string
    | UpdateBranchId of int * string
with
    member this.TryGetCopySourceInstanceId([<Out>] x:byref<_>) = // https://stackoverflow.com/a/17264768
        match this with
        | NewCopySourceInstanceId instanceId -> x <- instanceId; true
        | _ -> false
    member this.TryGetBranchSourceCardId([<Out>] x:byref<_>) =
        match this with
        | NewBranchSourceCardId (cardId, _) -> x <- cardId; true
        | _ -> false

module Helper =
    let maxIndexInclusive templates valueByFieldName =
        match templates with
        | Cloze t ->
            let max = AnkiImportLogic.maxClozeIndex "Something's wrong with your cloze indexes." valueByFieldName t.Front |> Result.getOk
            max - 1s
        | Standard ts ->
            (ts.Length |> int16) - 1s

type EditCardCommand = {
    EditSummary: string
    FieldValues: EditFieldAndValue ResizeArray
    CollateInstance: CollateInstance
    Source: CardSource
} with
    member this.CardView = {   
        FieldValues =
            this.FieldValues.Select(fun x ->
                {   Field = x.EditField
                    Value =  x.Value
                }).ToList()
        CollateInstance = this.CollateInstance }
    member this.MaxIndexInclusive =
        Helper.maxIndexInclusive
            (this.CollateInstance.Templates)
            (this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> Map.ofSeq) // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls

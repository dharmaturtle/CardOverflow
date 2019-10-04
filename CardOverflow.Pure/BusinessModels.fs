namespace CardOverflow.Pure

open CardOverflow.Pure.Core
open System
open System.Linq
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations

type Score = | Again | Hard | Good | Easy
module Score =
    let create =
        function
        | 0uy -> Ok Again
        | 1uy -> Ok Hard
        | 2uy -> Ok Good
        | 3uy -> Ok Easy
        | x -> sprintf "Invalid Score in database: %A" x |> Error
    let toDb =
        function
        | Again -> 0
        | Hard -> 1
        | Good -> 2
        | Easy -> 3
        >> byte

type CardState = | Normal | SchedulerBuried | UserBuried | Suspended
module CardState =
    let create =
        function
        | 0uy -> Ok Normal
        | 1uy -> Ok SchedulerBuried
        | 2uy -> Ok UserBuried
        | 3uy -> Ok Suspended
        | x -> sprintf "Invalid CardState in database: %A" x |> Error
    let toDb =
        function
        | Normal -> 0
        | SchedulerBuried -> 1
        | UserBuried -> 2
        | Suspended -> 3
        >> byte

module TimeSpanInt16 =
    type TimeSpanInt16 = private TimeSpanInt16 of TimeSpan
    let fromDays days = min (float Int16.MaxValue) days |> TimeSpan.FromDays |> TimeSpanInt16
    let value (TimeSpanInt16 t) = t
    let totalDays t = (value t).TotalDays |> int16

type CardOption = {
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
    MatureCardsHardInterval: float
    MatureCardsBuryRelated: bool
    LapsedCardsSteps: TimeSpan list
    LapsedCardsNewIntervalFactor: float // percent by which to multiply the current interval when a card goes has lapsed, called "new interval" in anki gui
    LapsedCardsMinimumInterval: TimeSpan
    LapsedCardsLeechThreshold: byte
    ShowAnswerTimer: bool
    AutomaticallyPlayAudio: bool
    ReplayQuestionAudioOnAnswer: bool
}

type Field = {
    Name: string
    Font: string
    FontSize: byte
    IsRightToLeft: bool
    Ordinal: byte
    IsSticky: bool
} with
    member this.toString =
        [   this.Name
            this.Font
            this.FontSize |> string
            this.IsRightToLeft |> string
            this.Ordinal |> string
            this.IsSticky |> string
        ] |> MappingTools.joinByUnitSeparator
    static member fromString x =
        let x = x |> MappingTools.splitByUnitSeparator
        {   Name = x.[0]
            Font = x.[1]
            FontSize = x.[2] |> byte
            IsRightToLeft = x.[3] = "True"
            Ordinal = x.[4] |> byte
            IsSticky = x.[5] = "True"
        }

module Fields =
    let toString: (Field seq -> string) =
        Seq.map (fun x -> x.toString)
        >> MappingTools.joinByRecordSeparator
    let fromString =
        MappingTools.splitByRecordSeparator
        >> List.map Field.fromString

type CardTemplateInstance = {
    Id: int
    Css: string
    Fields: Field seq
    Created: DateTime
    Modified: DateTime option
    LatexPre: string
    LatexPost: string
    AcquireHash: byte[]
    QuestionTemplate: string
    AnswerTemplate: string
    ShortQuestionTemplate: string
    ShortAnswerTemplate: string
}

type AcquiredCardTemplateInstance = {
    DefaultTags: int seq
    DefaultCardOptionId: int
    CardTemplateInstance: CardTemplateInstance
}

type CardTemplate = {
    Id: int
    MaintainerId: int
    Name: string
    Instances: CardTemplateInstance seq
}

type IntervalOrStepsIndex =
    | NewStepsIndex of byte
    | LapsedStepsIndex of byte
    | Interval of TimeSpan

type QuizCard = {
    AcquiredCardId: int
    CardInstanceId: int
    Due: DateTime
    Front: string
    Back: string
    FrontSynthVoice: string
    BackSynthVoice: string
    CardState: CardState
    IsLapsed: bool
    EaseFactor: float
    IntervalOrStepsIndex: IntervalOrStepsIndex
    Options: CardOption
}

//type CardInstance = {
//    Id: int
//    Created: DateTime
//    Modified: DateTime option
//    Fields: string seq
//}

// medTODO delete?
//type AcquiredDisplayCard = { // Acquired cause only private tags can be on a card
//    CardTemplateName: string
//    Front: string
//    Back: string
//    Tags: string seq
//}

[<CLIMutable>]
type FieldAndValue = {
    Field: Field
    Value: string
}

//[<CLIMutable>]
//type AcquiredCard = {
//    CardInstanceId: int
//    CardTemplateInstanceId: int
//    MaintainerId: int
//    Description: string
//    CardId: int
//    CardCreated: DateTime
//    CardModified: DateTime option
//    CardFields: FieldAndValue ResizeArray
//    Cards: AcquiredDisplayCard seq
//}

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
//    MaintainerId: int
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
type CardInstance = {
    Id: int
    Created: DateTime
    Modified: DateTime option
    IsDmca: bool
    FieldValues: FieldAndValue ResizeArray
    TemplateInstance: CardTemplateInstance
    IsAcquired: bool
} with
    member this.FrontBackFrontSynthBackSynth =
        CardHtml.generate   
            <| this.FieldValues.Select(fun x -> x.Field.Name, x.Value |?? lazy "")
            <| this.TemplateInstance.QuestionTemplate
            <| this.TemplateInstance.AnswerTemplate
            <| this.TemplateInstance.Css

type AcquiredCard = {
    CardId: int
    AcquiredCardId: int
    UserId: int
    CardInstance: CardInstance
    CardTemplateInstance: CardTemplateInstance
    CardState: CardState
    IsLapsed: bool
    EaseFactorInPermille: int16
    IntervalOrStepsIndex: IntervalOrStepsIndex
    Due: DateTime
    CardOptionId: int
    Tags: string seq
}

type Comment = {
    User: string
    UserId: int
    Text: string
    Created: DateTime
    IsDmca: bool
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
    CardId: int
    IsAcquired: bool
    Users: int
} with
    member this.PrimaryName =
        Relationship.split this.Name |> fst
    member this.SecondaryName =
        Relationship.split this.Name |> snd

[<CLIMutable>]
type ExploreCard = {
    Id: int
    Users: int
    Author: string
    AuthorId: int
    Description: string
    LatestInstance: CardInstance
    Tags: ViewTag seq
    Relationships: ViewRelationship seq
    Comments: Comment seq
}

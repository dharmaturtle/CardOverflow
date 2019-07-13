namespace CardOverflow.Pure

open System

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

type MemorizationState = | New | Learning | Mature | Lapsed
module MemorizationState =
    let create =
        function
        | 0uy -> Ok New 
        | 1uy -> Ok Learning
        | 2uy -> Ok Mature
        | 3uy -> Ok Lapsed
        | x -> sprintf "Invalid MemorizationState in database: %A" x |> Error
    let toDb =
        function
        | New -> 0
        | Learning -> 1
        | Mature -> 2
        | Lapsed -> 3
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
    NewCardsSteps: list<TimeSpan>
    NewCardsMaxPerDay: int16
    NewCardsGraduatingInterval: TimeSpan
    NewCardsEasyInterval: TimeSpan
    NewCardsStartingEaseFactor: float
    NewCardsBuryRelated: bool
    MatureCardsMaxPerDay: int16
    MatureCardsEaseFactorEasyBonusFactor: float
    MatureCardsIntervalFactor: float
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
}

type CardTemplate = {
    Name: string
    QuestionTemplate: string
    AnswerTemplate: string
    ShortQuestionTemplate: string
    ShortAnswerTemplate: string
    Ordinal: byte
}

type ConceptTemplate = {
    Id: int
    MaintainerId: int
    Name: string
    Css: string
    Fields: Field list
    CardTemplates: CardTemplate list
    Modified: DateTime
    IsCloze: bool
    DefaultPublicTags: int list
    DefaultPrivateTags: int list
    DefaultCardOptionId: int
    LatexPre: string
    LatexPost: string
}

type QuizCard = {
    Id: int
    Due: DateTime
    Question: string
    Answer: string
    MemorizationState: MemorizationState
    CardState: CardState
    LapseCount: byte
    EaseFactor: float
    Interval: TimeSpan
    StepsIndex: option<byte>
    Options: CardOption
}

type Concept = {
    Id: int
    Title: string
    Description: string
    ConceptTemplate: ConceptTemplate
    Fields: string list
    Modified: DateTime
    MaintainerId: int
    IsPublic: bool
}

type AcquiredCard = {
    Id: int
    UserId: int
    ConceptId: int
    MemorizationState: MemorizationState
    CardState: CardState
    LapseCount: byte
    EaseFactorInPermille: int16
    IntervalNegativeIsMinutesPositiveIsDays: int16
    StepsIndex: option<byte>
    Due: DateTime
    TemplateIndex: byte
    CardOptionId: int
}

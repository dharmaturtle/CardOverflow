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
    Id: int
    Name: string
    Font: string
    FontSize: byte
    IsRightToLeft: bool
    Ordinal: byte
    IsSticky: bool
}

type CardTemplate = {
    Id: int
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
}

type ConceptTemplateInstance = {
    Id: int
    ConceptTemplate: ConceptTemplate
    Css: string
    Fields: Field seq
    CardTemplates: CardTemplate seq
    Created: DateTime
    Modified: DateTime option
    IsCloze: bool
    DefaultPublicTags: int seq
    DefaultPrivateTags: int seq
    DefaultCardOptionId: int
    LatexPre: string
    LatexPost: string
    AcquireHash: byte[]
}

type QuizCard = {
    Due: DateTime
    Question: string
    Answer: string
    MemorizationState: MemorizationState
    CardState: CardState
    LapseCount: byte
    EaseFactor: float
    Interval: TimeSpan
    StepsIndex: byte option
    Options: CardOption
}

type Concept = {
    Id: int
    MaintainerId: int
    Name: string
}

type ConceptInstance = {
    Id: int
    Created: DateTime
    Modified: DateTime option
    Concept: Concept
    Fields: string seq
}

type AcquiredCard = {
    UserId: int
    ConceptInstance: ConceptInstance
    CardTemplate: CardTemplate
    MemorizationState: MemorizationState
    CardState: CardState
    LapseCount: byte
    EaseFactorInPermille: int16
    IntervalNegativeIsMinutesPositiveIsDays: int16
    StepsIndex: byte option
    Due: DateTime
    CardOptionId: int
}

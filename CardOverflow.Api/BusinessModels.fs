namespace CardOverflow.Api

open CardOverflow.Entity
open System

type MemorizationState = | New | Learning | Mature
    with
    static member Load enum =
        match enum with
        | MemorizationStateAndCardStateEnum.NewNormal -> New
        | MemorizationStateAndCardStateEnum.NewSchedulerBuried -> New
        | MemorizationStateAndCardStateEnum.NewUserBuried -> New
        | MemorizationStateAndCardStateEnum.NewSuspended -> New
        | MemorizationStateAndCardStateEnum.LearningNormal -> Learning
        | MemorizationStateAndCardStateEnum.LearningSchedulerBuried -> Learning
        | MemorizationStateAndCardStateEnum.LearningUserBuried -> Learning
        | MemorizationStateAndCardStateEnum.LearningSuspended -> Learning
        | MemorizationStateAndCardStateEnum.MatureNormal -> Mature
        | MemorizationStateAndCardStateEnum.MatureSchedulerBuried -> Mature
        | MemorizationStateAndCardStateEnum.MatureUserBuried -> Mature
        | MemorizationStateAndCardStateEnum.MatureSuspended -> Mature
        | _ -> "Unknown MemorizationStateAndCardStateEnum value: " + enum.ToString() |> failwith

type CardState = | Normal | SchedulerBuried | UserBuried | Suspended
    with
    static member Load enum =
        match enum with
        | MemorizationStateAndCardStateEnum.NewNormal -> Normal
        | MemorizationStateAndCardStateEnum.NewSchedulerBuried -> SchedulerBuried
        | MemorizationStateAndCardStateEnum.NewUserBuried -> UserBuried
        | MemorizationStateAndCardStateEnum.NewSuspended -> Suspended
        | MemorizationStateAndCardStateEnum.LearningNormal -> Normal
        | MemorizationStateAndCardStateEnum.LearningSchedulerBuried -> SchedulerBuried
        | MemorizationStateAndCardStateEnum.LearningUserBuried -> UserBuried
        | MemorizationStateAndCardStateEnum.LearningSuspended -> Suspended
        | MemorizationStateAndCardStateEnum.MatureNormal -> Normal
        | MemorizationStateAndCardStateEnum.MatureSchedulerBuried -> SchedulerBuried
        | MemorizationStateAndCardStateEnum.MatureUserBuried -> UserBuried
        | MemorizationStateAndCardStateEnum.MatureSuspended -> Suspended
        | _ -> "Unknown MemorizationStateAndCardStateEnum value: " + enum.ToString() |> failwith

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
    MatureCardsMaximumInterval: TimeSpan
    MatureCardsHardInterval: float
    MatureCardsBuryRelated: bool
    LapsedCardsSteps: list<TimeSpan>
    LapsedCardsNewInterval: float
    LapsedCardsMinimumInterval: TimeSpan
    LapsedCardsLeechThreshold: byte
    ShowAnswerTimer: bool
    AutomaticallyPlayAudio: bool
    ReplayQuestionAnswerAudioOnAnswer: bool
} with
    static member Load(entity: CardOptionEntity) = {
        Id = entity.Id
        Name = entity.Name
        NewCardsSteps = MappingTools.stringOfMinutesToTimeSpanList entity.NewCardsStepsInMinutes
        NewCardsMaxPerDay = entity.NewCardsMaxPerDay
        NewCardsGraduatingInterval = entity.NewCardsGraduatingIntervalInDays |> float |> TimeSpan.FromDays
        NewCardsEasyInterval = entity.NewCardsEasyIntervalInDays |> float |> TimeSpan.FromDays
        NewCardsStartingEaseFactor = float entity.NewCardsStartingEaseFactorInPermille / 1000.0
        NewCardsBuryRelated = entity.NewCardsBuryRelated
        MatureCardsMaxPerDay = entity.MatureCardsMaxPerDay
        MatureCardsEaseFactorEasyBonusFactor = float entity.MatureCardsEaseFactorEasyBonusFactorInPermille / 1000.0
        MatureCardsIntervalFactor = float entity.MatureCardsIntervalFactorInPermille / 1000.0
        MatureCardsMaximumInterval = entity.MatureCardsMaximumIntervalInDays |> float |> TimeSpan.FromDays
        MatureCardsHardInterval = float entity.MatureCardsHardIntervalFactorInPermille / 1000.0
        MatureCardsBuryRelated = entity.MatureCardsBuryRelated
        LapsedCardsSteps = MappingTools.stringOfMinutesToTimeSpanList entity.LapsedCardsStepsInMinutes
        LapsedCardsNewInterval = float entity.LapsedCardsNewIntervalInPermille / 1000.0
        LapsedCardsMinimumInterval = entity.LapsedCardsMinimumIntervalInDays |> float |> TimeSpan.FromDays
        LapsedCardsLeechThreshold = entity.LapsedCardsLeechThreshold
        ShowAnswerTimer = entity.ShowAnswerTimer
        AutomaticallyPlayAudio = entity.AutomaticallyPlayAudio
        ReplayQuestionAnswerAudioOnAnswer = entity.ReplayQuestionAudioOnAnswer }
    member this.CopyTo(entity: CardOptionEntity) =
        entity.Name <- this.Name
        entity.NewCardsStepsInMinutes <- this.NewCardsSteps |> MappingTools.timeSpanListToStringOfMinutes
        entity.NewCardsMaxPerDay <- this.NewCardsMaxPerDay
        entity.NewCardsGraduatingIntervalInDays <- this.NewCardsGraduatingInterval.TotalDays |> Math.Round |> byte
        entity.NewCardsEasyIntervalInDays <- this.NewCardsEasyInterval.TotalDays |> Math.Round |> byte
        entity.NewCardsStartingEaseFactorInPermille <- this.NewCardsStartingEaseFactor * 1000.0 |> Math.Round |> int16
        entity.NewCardsBuryRelated <- this.NewCardsBuryRelated
        entity.MatureCardsMaxPerDay <- this.MatureCardsMaxPerDay
        entity.MatureCardsEaseFactorEasyBonusFactorInPermille <- this.MatureCardsEaseFactorEasyBonusFactor * 1000.0 |> Math.Round |> int16
        entity.MatureCardsIntervalFactorInPermille <- this.MatureCardsIntervalFactor * 1000.0 |> Math.Round |> int16
        entity.MatureCardsMaximumIntervalInDays <- this.MatureCardsMaximumInterval.TotalDays |> Math.Round |> int16
        entity.MatureCardsHardIntervalFactorInPermille <- this.MatureCardsHardInterval * 1000.0 |> Math.Round |> int16
        entity.MatureCardsBuryRelated <- this.MatureCardsBuryRelated
        entity.LapsedCardsStepsInMinutes <- this.LapsedCardsSteps |> MappingTools.timeSpanListToStringOfMinutes
        entity.LapsedCardsNewIntervalInPermille <- this.LapsedCardsNewInterval * 1000.0 |> Math.Round |> int16
        entity.LapsedCardsMinimumIntervalInDays <- this.LapsedCardsMinimumInterval.TotalDays |> Math.Round |> byte
        entity.LapsedCardsLeechThreshold <- this.LapsedCardsLeechThreshold
        entity.ShowAnswerTimer <- this.ShowAnswerTimer
        entity.AutomaticallyPlayAudio <- this.AutomaticallyPlayAudio
        entity.ReplayQuestionAudioOnAnswer <- this.ReplayQuestionAnswerAudioOnAnswer
    member this.CopyToNew(user: UserEntity) =
        let entity = CardOptionEntity()
        this.CopyTo entity
        entity.User <- user
        entity

type Field = {
    Name: string
    Font: string
    FontSize: byte
    IsRightToLeft: bool
    Ordinal: byte
    IsSticky: bool
} with
    static member Load =
        MappingTools.splitByUnitSeparator >> fun parsed -> {
            Name = parsed.[0]
            Font = parsed.[1]
            FontSize = Byte.Parse parsed.[2]
            IsRightToLeft = MappingTools.stringIntToBool parsed.[3]
            Ordinal = Byte.Parse parsed.[4]
            IsSticky = MappingTools.stringIntToBool parsed.[5] }
    static member GetName =
        MappingTools.splitByUnitSeparator >> List.item 0
    static member LoadMany =
        MappingTools.splitByRecordSeparator >> List.map Field.Load
    static member GetNames =
        MappingTools.splitByRecordSeparator >> List.map Field.GetName
    member this.ToEntityString =
        [   this.Name
            this.Font
            this.FontSize |> string
            this.IsRightToLeft |> MappingTools.boolToString
            this.Ordinal |> string
            this.IsSticky |> MappingTools.boolToString
        ] |> MappingTools.joinByUnitSeparator
    static member ManyToEntityString =
        List.map (fun (x: Field) -> x.ToEntityString) >> MappingTools.joinByRecordSeparator

type CardTemplate = {
    Name: string
    QuestionTemplate: string
    AnswerTemplate: string
    ShortQuestionTemplate: string
    ShortAnswerTemplate: string
    Ordinal: byte
    DefaultCardTemplateId: int
} with
    static member Load =
        MappingTools.splitByUnitSeparator >> fun parsed -> {
            Name = parsed.[0]
            QuestionTemplate = parsed.[1]
            AnswerTemplate = parsed.[2]
            ShortQuestionTemplate = parsed.[3]
            ShortAnswerTemplate = parsed.[4]
            Ordinal = Byte.Parse parsed.[5]
            DefaultCardTemplateId = parsed.[6] |> int }
    static member LoadMany =
        MappingTools.splitByRecordSeparator >> List.map CardTemplate.Load
    member this.ToEntityString =
        [   this.Name
            this.QuestionTemplate
            this.AnswerTemplate
            this.ShortQuestionTemplate
            this.ShortAnswerTemplate
            this.Ordinal |> string
            this.DefaultCardTemplateId |> string
        ] |> MappingTools.joinByUnitSeparator
    static member ManyToEntityString =
        List.map (fun (x: CardTemplate) -> x.ToEntityString) >> MappingTools.joinByRecordSeparator

type ConceptTemplate = {
    Id: int
    Name: string
    Css: string
    Fields: Field list
    CardTemplates: CardTemplate list
    Modified: DateTime
    IsCloze: bool
    DefaultTags: int list
    LatexPre: string
    LatexPost: string
} with
    static member Load(entity: ConceptTemplateEntity) = {
        Id = entity.Id
        Name = entity.Name
        Css = entity.Css
        Fields = entity.Fields |> Field.LoadMany
        CardTemplates = entity.CardTemplates |> CardTemplate.LoadMany
        Modified = entity.Modified
        IsCloze = entity.IsCloze
        DefaultTags = entity.DefaultTags |> MappingTools.stringOfIntsToIntList
        LatexPre = entity.LatexPre
        LatexPost = entity.LatexPost }
    member this.CopyTo(entity: ConceptTemplateEntity) =
        entity.Id <- this.Id
        entity.Name <- this.Name
        entity.Css <- this.Css
        entity.Fields <- this.Fields |> Field.ManyToEntityString
        entity.CardTemplates <- this.CardTemplates |> CardTemplate.ManyToEntityString
        entity.Modified <- this.Modified
        entity.IsCloze <- this.IsCloze
        entity.DefaultTags <- this.DefaultTags |> MappingTools.intsListToStringOfInts
        entity.LatexPre <- this.LatexPre
        entity.LatexPost <- this.LatexPost
    member this.CopyToNew =
        let entity = ConceptTemplateEntity()
        this.CopyTo entity
        entity

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
} with
    static member Load(entity: CardEntity) =
        let fieldNameValueMap =
            Seq.zip
                (entity.Concept.ConceptTemplate.Fields |> Field.GetNames)
                (entity.Concept.Fields |> MappingTools.splitByUnitSeparator)
        let replaceFields template =
            fieldNameValueMap |> Seq.fold(fun (aggregate: string) (key, value) -> aggregate.Replace("{{" + key + "}}", value)) template
        let cardTemplate =
            entity.Concept.ConceptTemplate.CardTemplates
            |> MappingTools.splitByRecordSeparator
            |> List.item (int entity.TemplateIndex)
            |> CardTemplate.Load
        {   Id = entity.Id
            Due = entity.Due
            Question = replaceFields cardTemplate.QuestionTemplate
            Answer = replaceFields cardTemplate.AnswerTemplate
            MemorizationState = MemorizationState.Load entity.MemorizationStateAndCardState
            CardState = CardState.Load entity.MemorizationStateAndCardState
            LapseCount = entity.LapseCount
            EaseFactor = float entity.EaseFactorInPermille / 1000.0
            Interval =
                if int32 entity.IntervalNegativeIsMinutesPositiveIsDays < 0
                then int16 -1 * entity.IntervalNegativeIsMinutesPositiveIsDays |> float |> TimeSpan.FromMinutes
                else entity.IntervalNegativeIsMinutesPositiveIsDays |> float |> TimeSpan.FromDays
            StepsIndex =
                if entity.StepsIndex.HasValue
                then Some entity.StepsIndex.Value
                else None
            Options = CardOption.Load entity.CardOption }

type Score = | Again | Hard | Good | Easy

type Concept = {
    Id: int
    Title: string
    Description: string
    ConceptTemplate: ConceptTemplate
    Fields: Field list
} with
    static member Load(entity: ConceptEntity) =
        { Id = entity.Id
          Title = entity.Title
          Description = entity.Description
          ConceptTemplate = ConceptTemplate.Load entity.ConceptTemplate
          Fields = Field.LoadMany entity.Fields }

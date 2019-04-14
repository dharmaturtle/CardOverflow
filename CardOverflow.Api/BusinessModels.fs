namespace CardOverflow.Api

open CardOverflow.Entity
open System

type MemorizationState = | New | Learning | Mature
  with 
  static member Create enum =
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
  static member Create enum =
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

type ConceptOption = {
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
  static member Create(entity: ConceptOptionEntity) =
    { Id = entity.Id
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
  member this.CopyTo(entity: ConceptOptionEntity) =
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
  static member Default = {
    Id = 0
    Name = "Default"
    NewCardsSteps = [ TimeSpan.FromMinutes 1.0; TimeSpan.FromMinutes 10.0 ]
    NewCardsMaxPerDay = int16 20
    NewCardsGraduatingInterval = TimeSpan.FromDays 1.0
    NewCardsEasyInterval = TimeSpan.FromDays 4.0
    NewCardsStartingEaseFactor = 2.5
    NewCardsBuryRelated = true
    MatureCardsMaxPerDay = int16 200
    MatureCardsEaseFactorEasyBonusFactor = 1.3
    MatureCardsIntervalFactor = 1.0
    MatureCardsMaximumInterval = TimeSpan.FromDays 36500.0
    MatureCardsHardInterval = 1.2
    MatureCardsBuryRelated = true
    LapsedCardsSteps = [ TimeSpan.FromMinutes 10.0 ]
    LapsedCardsNewInterval = 0.0
    LapsedCardsMinimumInterval = TimeSpan.FromDays 1.0
    LapsedCardsLeechThreshold = byte 8
    ShowAnswerTimer = false
    AutomaticallyPlayAudio = false
    ReplayQuestionAnswerAudioOnAnswer = false }

type Field = {
  Name: string
  Font: string
  FontSize: byte
  IsRightToLeft: bool
  Ordinal: byte
  IsSticky: bool
} with
  static member Create =
    MappingTools.splitByUnitSeparator >> fun parsed ->
    { Name = parsed.[0]
      Font = parsed.[1]
      FontSize = Byte.Parse parsed.[2]
      IsRightToLeft = MappingTools.stringIntToBool parsed.[3]
      Ordinal = Byte.Parse parsed.[4]
      IsSticky = MappingTools.stringIntToBool parsed.[5] }
  static member GetName =
    MappingTools.splitByUnitSeparator >> List.item 0
  static member CreateMany =
    MappingTools.splitByRecordSeparator >> List.map Field.Create
  static member GetNames = 
    MappingTools.splitByRecordSeparator >> List.map Field.GetName

type CardTemplate = {
  Name: string
  QuestionTemplate: string
  AnswerTemplate: string
  ShortQuestionTemplate: string
  ShortAnswerTemplate: string
  Ordinal: byte
} with
  static member Create = 
    MappingTools.splitByUnitSeparator >> fun parsed ->
    { Name = parsed.[0]
      QuestionTemplate = parsed.[1]
      AnswerTemplate = parsed.[2]
      ShortQuestionTemplate = parsed.[3]
      ShortAnswerTemplate = parsed.[4]
      Ordinal = Byte.Parse parsed.[5] }
  static member CreateMany = 
    MappingTools.splitByRecordSeparator >> List.map CardTemplate.Create

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
  Options: ConceptOption
} with
  static member Create(entity: CardEntity) = 
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
      |> CardTemplate.Create
    { Id = entity.Id
      Due = entity.Due
      Question = replaceFields cardTemplate.QuestionTemplate
      Answer = replaceFields cardTemplate.AnswerTemplate
      MemorizationState = MemorizationState.Create entity.MemorizationStateAndCardState
      CardState = CardState.Create entity.MemorizationStateAndCardState
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
      Options = ConceptOption.Create entity.Concept.ConceptOption }

type Score = | Again | Hard | Good | Easy

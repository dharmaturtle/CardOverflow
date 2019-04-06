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

type CardOption = {
  Id: int
  Name: string
  NewCardsSteps: list<TimeSpan>
  NewCardsMaxPerDay: int16
  NewCardsGraduatingInterval: TimeSpan
  NewCardsEasyInterval: TimeSpan
  NewCardsStartingEase: int16
  NewCardsBuryRelated: bool
  MatureCardsMaxPerDay: int16
  MatureCardsEasyBonus: float
  MatureCardsIntervalModifier: float
  MatureCardsMaximumInterval: TimeSpan
  MatureCardsBuryRelated: bool
  LapsedCardsSteps: list<TimeSpan>
  LapsedCardsNewInterval: int16
  LapsedCardsMinimumInterval: TimeSpan
  LapsedCardsLeechThreshold: byte
  ShowAnswerTimer: bool
  AutomaticallyPlayAudio: bool
  ReplayQuestionAnswerAudioOnAnswer: bool
} with 
  static member Create(entity: CardOptionEntity) =
    let parse(string: string) =
      string.Split [|' '|] |> Seq.map (Double.Parse >> TimeSpan.FromMinutes) |> Seq.toList
    { Id = entity.Id
      Name = entity.Name
      NewCardsSteps = parse entity.NewCardsSteps
      NewCardsMaxPerDay = entity.NewCardsMaxPerDay
      NewCardsGraduatingInterval = entity.NewCardsGraduatingInterval |> float |> TimeSpan.FromDays
      NewCardsEasyInterval = entity.NewCardsEasyInterval |> float |> TimeSpan.FromDays
      NewCardsStartingEase = entity.NewCardsStartingEase
      NewCardsBuryRelated = entity.NewCardsBuryRelated
      MatureCardsMaxPerDay = entity.MatureCardsMaxPerDay
      MatureCardsEasyBonus = float entity.MatureCardsEasyBonus / 100.0
      MatureCardsIntervalModifier = float entity.MatureCardsIntervalModifier / 100.0
      MatureCardsMaximumInterval = entity.MatureCardsMaximumInterval |> float |> TimeSpan.FromDays
      MatureCardsBuryRelated = entity.MatureCardsBuryRelated
      LapsedCardsSteps = parse entity.LapsedCardsSteps
      LapsedCardsNewInterval = entity.LapsedCardsNewInterval
      LapsedCardsMinimumInterval = entity.LapsedCardsMinimumInterval |> float |> TimeSpan.FromDays
      LapsedCardsLeechThreshold = entity.LapsedCardsLeechThreshold
      ShowAnswerTimer = entity.ShowAnswerTimer
      AutomaticallyPlayAudio = entity.AutomaticallyPlayAudio
      ReplayQuestionAnswerAudioOnAnswer = entity.ReplayQuestionAnswerAudioOnAnswer }

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
  static member Create(entity: CardEntity) = {
    Id = entity.Id
    Due = entity.Due
    Question = entity.Question
    Answer = entity.Answer
    MemorizationState = MemorizationState.Create entity.MemorizationStateAndCardState
    CardState = CardState.Create entity.MemorizationStateAndCardState
    LapseCount = entity.LapseCount
    EaseFactor = float entity.EaseFactor / 1000.0
    Interval = 
      if int32 entity.Interval < 0 
      then int16 -1 * entity.Interval |> float |> TimeSpan.FromMinutes 
      else entity.Interval |> float |> TimeSpan.FromDays
    StepsIndex = 
      if entity.StepsIndex.HasValue 
      then Some entity.StepsIndex.Value 
      else None
    Options = CardOption.Create entity.CardOption }

type Score = | Again | Hard | Good | Easy
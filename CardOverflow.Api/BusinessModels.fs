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
  NewCardsSteps: seq<int>
  NewCardsMaxPerDay: int16
  NewCardsGraduatingInterval: byte
  NewCardsEasyInterval: byte
  NewCardsStartingEase: int16
  NewCardsBuryRelated: bool
  MatureCardsMaxPerDay: int16
  MatureCardsEasyBonus: int16
  MatureCardsIntervalModifier: int16
  MatureCardsMaximumInterval: int16
  MatureCardsBuryRelated: bool
  LapsedCardsSteps: seq<int>
  LapsedCardsNewInterval: int16
  LapsedCardsMinimumInterval: byte
  LapsedCardsLeechThreshold: byte
  ShowAnswerTimer: bool
  AutomaticallyPlayAudio: bool
  ReplayQuestionAnswerAudioOnAnswer: bool
} with 
  static member Create(entity: CardOptionEntity) = {
    Id = entity.Id
    Name = entity.Name
    NewCardsSteps = entity.NewCardsSteps.Split [|','|] |> Seq.map Int32.Parse
    NewCardsMaxPerDay = entity.NewCardsMaxPerDay
    NewCardsGraduatingInterval = entity.NewCardsGraduatingInterval
    NewCardsEasyInterval = entity.NewCardsEasyInterval
    NewCardsStartingEase = entity.NewCardsStartingEase
    NewCardsBuryRelated = entity.NewCardsBuryRelated
    MatureCardsMaxPerDay = entity.MatureCardsMaxPerDay
    MatureCardsEasyBonus = entity.MatureCardsEasyBonus
    MatureCardsIntervalModifier = entity.MatureCardsIntervalModifier
    MatureCardsMaximumInterval = entity.MatureCardsMaximumInterval
    MatureCardsBuryRelated = entity.MatureCardsBuryRelated
    LapsedCardsSteps = entity.LapsedCardsSteps.Split [|','|] |> Seq.map Int32.Parse
    LapsedCardsNewInterval = entity.LapsedCardsNewInterval
    LapsedCardsMinimumInterval = entity.LapsedCardsMinimumInterval
    LapsedCardsLeechThreshold = entity.LapsedCardsLeechThreshold
    ShowAnswerTimer = entity.ShowAnswerTimer
    AutomaticallyPlayAudio = entity.AutomaticallyPlayAudio
    ReplayQuestionAnswerAudioOnAnswer = entity.ReplayQuestionAnswerAudioOnAnswer
  }

type QuizCard = {
  Id: int
  Question: string
  Answer: string
  MemorizationState: MemorizationState
  CardState: CardState
  LapseCount: byte
  EaseFactor: int16
  Interval: int16
  ReviewsUntilGraduation: option<byte>
  Options: CardOption
} with
  static member Create(entity: CardEntity) = {
    Id = entity.Id
    Question = entity.Question
    Answer = entity.Answer
    MemorizationState = MemorizationState.Create entity.MemorizationStateAndCardState
    CardState = CardState.Create entity.MemorizationStateAndCardState
    LapseCount = entity.LapseCount
    EaseFactor = entity.EaseFactor
    Interval = entity.Interval
    ReviewsUntilGraduation = 
      if entity.ReviewsUntilGraduation.HasValue 
      then Some entity.ReviewsUntilGraduation.Value 
      else None
    Options = CardOption.Create entity.CardOption
  }

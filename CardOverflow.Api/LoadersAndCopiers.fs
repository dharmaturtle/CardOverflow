module LoadersAndCopiers

open CardOverflow.Debug
open MappingTools
open CardOverflow.Entity
open CardOverflow.Pure
open System
open System.Linq

type MemorizationState with
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

type CardState with
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

module ScoreAndMemorizationState =
    let from score memorizationState =
        match (score, memorizationState) with
        | (Again, New) -> ScoreAndMemorizationStateEnum.AgainNew
        | (Hard, New) -> ScoreAndMemorizationStateEnum.HardNew
        | (Good, New) -> ScoreAndMemorizationStateEnum.GoodNew
        | (Easy, New) -> ScoreAndMemorizationStateEnum.EasyNew
        | (Again, Learning) -> ScoreAndMemorizationStateEnum.AgainLearning
        | (Hard, Learning) -> ScoreAndMemorizationStateEnum.HardLearning
        | (Good, Learning) -> ScoreAndMemorizationStateEnum.GoodLearning
        | (Easy, Learning) -> ScoreAndMemorizationStateEnum.EasyLearning
        | (Again, Mature) -> ScoreAndMemorizationStateEnum.AgainMature
        | (Hard, Mature) -> ScoreAndMemorizationStateEnum.HardMature
        | (Good, Mature) -> ScoreAndMemorizationStateEnum.GoodMature
        | (Easy, Mature) -> ScoreAndMemorizationStateEnum.EasyMature
        | (Again, Lapsed) -> ScoreAndMemorizationStateEnum.AgainLapsed
        | (Hard, Lapsed) -> ScoreAndMemorizationStateEnum.HardLapsed
        | (Good, Lapsed) -> ScoreAndMemorizationStateEnum.GoodLapsed
        | (Easy, Lapsed) -> ScoreAndMemorizationStateEnum.EasyLapsed

module MemorizationStateAndCardStateEnum =
    let from memorizationState cardState =
        match (memorizationState, cardState) with
        | (New, Normal) -> MemorizationStateAndCardStateEnum.NewNormal
        | (New, SchedulerBuried) -> MemorizationStateAndCardStateEnum.NewSchedulerBuried
        | (New, UserBuried) -> MemorizationStateAndCardStateEnum.NewUserBuried
        | (New, Suspended) -> MemorizationStateAndCardStateEnum.NewSuspended
        | (Learning, Normal) -> MemorizationStateAndCardStateEnum.LearningNormal
        | (Learning, SchedulerBuried) -> MemorizationStateAndCardStateEnum.LearningSchedulerBuried
        | (Learning, UserBuried) -> MemorizationStateAndCardStateEnum.LearningUserBuried
        | (Learning, Suspended) -> MemorizationStateAndCardStateEnum.LearningSuspended
        | (Mature, Normal) -> MemorizationStateAndCardStateEnum.MatureNormal
        | (Mature, SchedulerBuried) -> MemorizationStateAndCardStateEnum.MatureSchedulerBuried
        | (Mature, UserBuried) -> MemorizationStateAndCardStateEnum.MatureUserBuried
        | (Mature, Suspended) -> MemorizationStateAndCardStateEnum.MatureSuspended
        | (Lapsed, Normal) -> MemorizationStateAndCardStateEnum.LapsedNormal
        | (Lapsed, SchedulerBuried) -> MemorizationStateAndCardStateEnum.LapsedSchedulerBuried
        | (Lapsed, UserBuried) -> MemorizationStateAndCardStateEnum.LapsedUserBuried
        | (Lapsed, Suspended) -> MemorizationStateAndCardStateEnum.LapsedSuspended

type CardOption with
    member this.AcquireEquality (that: CardOption) =
        this.Name = that.Name &&
        this.NewCardsSteps = that.NewCardsSteps &&
        this.NewCardsMaxPerDay = that.NewCardsMaxPerDay &&
        this.NewCardsGraduatingInterval = that.NewCardsGraduatingInterval &&
        this.NewCardsEasyInterval = that.NewCardsEasyInterval &&
        this.NewCardsStartingEaseFactor = that.NewCardsStartingEaseFactor &&
        this.NewCardsBuryRelated = that.NewCardsBuryRelated &&
        this.MatureCardsMaxPerDay = that.MatureCardsMaxPerDay &&
        this.MatureCardsEaseFactorEasyBonusFactor = that.MatureCardsEaseFactorEasyBonusFactor &&
        this.MatureCardsIntervalFactor = that.MatureCardsIntervalFactor &&
        this.MatureCardsMaximumInterval = that.MatureCardsMaximumInterval &&
        this.MatureCardsHardInterval = that.MatureCardsHardInterval &&
        this.MatureCardsBuryRelated = that.MatureCardsBuryRelated &&
        this.LapsedCardsSteps = that.LapsedCardsSteps &&
        this.LapsedCardsNewIntervalFactor = that.LapsedCardsNewIntervalFactor &&
        this.LapsedCardsMinimumInterval = that.LapsedCardsMinimumInterval &&
        this.LapsedCardsLeechThreshold = that.LapsedCardsLeechThreshold &&
        this.ShowAnswerTimer = that.ShowAnswerTimer &&
        this.AutomaticallyPlayAudio = that.AutomaticallyPlayAudio &&
        this.ReplayQuestionAudioOnAnswer = that.ReplayQuestionAudioOnAnswer
    static member Load(entity: CardOptionEntity) =
        { Id = entity.Id
          Name = entity.Name
          NewCardsSteps = MappingTools.stringOfMinutesToTimeSpanList entity.NewCardsStepsInMinutes
          NewCardsMaxPerDay = entity.NewCardsMaxPerDay
          NewCardsGraduatingInterval = entity.NewCardsGraduatingIntervalInDays |> float |> TimeSpan.FromDays
          NewCardsEasyInterval = entity.NewCardsEasyIntervalInDays |> float |> TimeSpan.FromDays
          NewCardsStartingEaseFactor = float entity.NewCardsStartingEaseFactorInPermille / 1000.
          NewCardsBuryRelated = entity.NewCardsBuryRelated
          MatureCardsMaxPerDay = entity.MatureCardsMaxPerDay
          MatureCardsEaseFactorEasyBonusFactor = float entity.MatureCardsEaseFactorEasyBonusFactorInPermille / 1000.
          MatureCardsIntervalFactor = float entity.MatureCardsIntervalFactorInPermille / 1000.
          MatureCardsMaximumInterval = entity.MatureCardsMaximumIntervalInDays |> float |> TimeSpanInt16.fromDays
          MatureCardsHardInterval = float entity.MatureCardsHardIntervalFactorInPermille / 1000.
          MatureCardsBuryRelated = entity.MatureCardsBuryRelated
          LapsedCardsSteps = MappingTools.stringOfMinutesToTimeSpanList entity.LapsedCardsStepsInMinutes
          LapsedCardsNewIntervalFactor = float entity.LapsedCardsNewIntervalFactorInPermille / 1000.
          LapsedCardsMinimumInterval = entity.LapsedCardsMinimumIntervalInDays |> float |> TimeSpan.FromDays
          LapsedCardsLeechThreshold = entity.LapsedCardsLeechThreshold
          ShowAnswerTimer = entity.ShowAnswerTimer
          AutomaticallyPlayAudio = entity.AutomaticallyPlayAudio
          ReplayQuestionAudioOnAnswer = entity.ReplayQuestionAudioOnAnswer }
    member this.CopyTo(entity: CardOptionEntity) =
        entity.Id <- this.Id
        entity.Name <- this.Name
        entity.NewCardsStepsInMinutes <- this.NewCardsSteps |> MappingTools.timeSpanListToStringOfMinutes
        entity.NewCardsMaxPerDay <- this.NewCardsMaxPerDay
        entity.NewCardsGraduatingIntervalInDays <- this.NewCardsGraduatingInterval.TotalDays |> Math.Round |> byte
        entity.NewCardsEasyIntervalInDays <- this.NewCardsEasyInterval.TotalDays |> Math.Round |> byte
        entity.NewCardsStartingEaseFactorInPermille <- this.NewCardsStartingEaseFactor * 1000. |> Math.Round |> int16
        entity.NewCardsBuryRelated <- this.NewCardsBuryRelated
        entity.MatureCardsMaxPerDay <- this.MatureCardsMaxPerDay
        entity.MatureCardsEaseFactorEasyBonusFactorInPermille <- this.MatureCardsEaseFactorEasyBonusFactor * 1000. |> Math.Round |> int16
        entity.MatureCardsIntervalFactorInPermille <- this.MatureCardsIntervalFactor * 1000. |> Math.Round |> int16
        entity.MatureCardsMaximumIntervalInDays <- TimeSpanInt16.totalDays this.MatureCardsMaximumInterval
        entity.MatureCardsHardIntervalFactorInPermille <- this.MatureCardsHardInterval * 1000. |> Math.Round |> int16
        entity.MatureCardsBuryRelated <- this.MatureCardsBuryRelated
        entity.LapsedCardsStepsInMinutes <- this.LapsedCardsSteps |> MappingTools.timeSpanListToStringOfMinutes
        entity.LapsedCardsNewIntervalFactorInPermille <- this.LapsedCardsNewIntervalFactor * 1000. |> Math.Round |> int16
        entity.LapsedCardsMinimumIntervalInDays <- this.LapsedCardsMinimumInterval.TotalDays |> Math.Round |> byte
        entity.LapsedCardsLeechThreshold <- this.LapsedCardsLeechThreshold
        entity.ShowAnswerTimer <- this.ShowAnswerTimer
        entity.AutomaticallyPlayAudio <- this.AutomaticallyPlayAudio
        entity.ReplayQuestionAudioOnAnswer <- this.ReplayQuestionAudioOnAnswer
    member this.CopyToNew user =
        let entity = CardOptionEntity()
        this.CopyTo entity
        entity.User <- user
        entity
    member this.CopyToNew userId =
        let entity = CardOptionEntity()
        this.CopyTo entity
        entity.UserId <- userId
        entity

type Field with
   static member Load =
       MappingTools.splitByUnitSeparator >> fun parsed ->
           { Name = parsed.[0]
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
       [ this.Name
         this.Font
         this.FontSize |> string
         this.IsRightToLeft |> MappingTools.boolToString
         this.Ordinal |> string
         this.IsSticky |> MappingTools.boolToString
       ] |> MappingTools.joinByUnitSeparator
   static member ManyToEntityString =
       List.map (fun (x: Field) -> x.ToEntityString) >> MappingTools.joinByRecordSeparator

type CardTemplate with
    static member Load =
        MappingTools.splitByUnitSeparator >> fun parsed ->
            { Name = parsed.[0]
              QuestionTemplate = parsed.[1]
              AnswerTemplate = parsed.[2]
              ShortQuestionTemplate = parsed.[3]
              ShortAnswerTemplate = parsed.[4]
              Ordinal = Byte.Parse parsed.[5] }
    static member LoadMany =
        MappingTools.splitByRecordSeparator >> List.map CardTemplate.Load
    member this.ToEntityString =
        [ this.Name
          this.QuestionTemplate
          this.AnswerTemplate
          this.ShortQuestionTemplate
          this.ShortAnswerTemplate
          this.Ordinal |> string
        ] |> MappingTools.joinByUnitSeparator
    static member ManyToEntityString =
        List.map (fun (x: CardTemplate) -> x.ToEntityString) >> MappingTools.joinByRecordSeparator

type ConceptTemplate with
    member this.AcquireEquality(that: ConceptTemplate) =
        this.Name = that.Name &&
        this.Css = that.Css &&
        this.Fields = that.Fields &&
        this.CardTemplates = that.CardTemplates &&
        this.IsCloze = that.IsCloze &&
        this.LatexPre = that.LatexPre &&
        this.LatexPost = that.LatexPost
    static member Load(entity: ConceptTemplateEntity) =
        { Id = entity.Id
          MaintainerId = entity.MaintainerId
          Name = entity.Name
          Css = entity.Css
          Fields = entity.Fields |> Field.LoadMany
          CardTemplates = entity.CardTemplates |> CardTemplate.LoadMany
          Modified = entity.Modified
          IsCloze = entity.IsCloze
          DefaultPublicTags = entity.ConceptTemplateConceptTemplateDefaultUsers.Single().ConceptTemplateDefault.DefaultPublicTags |> MappingTools.stringOfIntsToIntList
          DefaultPrivateTags = entity.ConceptTemplateConceptTemplateDefaultUsers.Single().ConceptTemplateDefault.DefaultPrivateTags |> MappingTools.stringOfIntsToIntList
          DefaultCardOptionId = entity.ConceptTemplateConceptTemplateDefaultUsers.Single().ConceptTemplateDefault.DefaultCardOptionId
          LatexPre = entity.LatexPre
          LatexPost = entity.LatexPost }
    member this.CopyTo(entity: ConceptTemplateEntity) =
        entity.Id <- this.Id
        entity.MaintainerId <- this.MaintainerId
        entity.Name <- this.Name
        entity.Css <- this.Css
        entity.Fields <- this.Fields |> Field.ManyToEntityString
        entity.CardTemplates <- this.CardTemplates |> CardTemplate.ManyToEntityString
        entity.Modified <- this.Modified
        entity.IsCloze <- this.IsCloze
        entity.LatexPre <- this.LatexPre
        entity.LatexPost <- this.LatexPost
    member this.CopyToNew maintainer defaultCardOption =
        let entity = ConceptTemplateEntity()
        ConceptTemplateConceptTemplateDefaultUserEntity(
            User = maintainer,
            ConceptTemplate = entity,
            ConceptTemplateDefault = ConceptTemplateDefaultEntity(
                DefaultPublicTags = MappingTools.intsListToStringOfInts this.DefaultPublicTags,
                DefaultPrivateTags = MappingTools.intsListToStringOfInts this.DefaultPrivateTags,
                DefaultCardOption = defaultCardOption
            )
        ) |> entity.ConceptTemplateConceptTemplateDefaultUsers.Add
        this.CopyTo entity
        entity.Maintainer <- maintainer // medTODO SaveChanges after creating the users so you don't have to pass this around
        entity
    member this.CopyToNew2 defaultCardOption =
        let entity = ConceptTemplateEntity()
        ConceptTemplateConceptTemplateDefaultUserEntity(
            UserId = this.MaintainerId,
            ConceptTemplate = entity,
            ConceptTemplateDefault = ConceptTemplateDefaultEntity(
                DefaultPublicTags = MappingTools.intsListToStringOfInts this.DefaultPublicTags,
                DefaultPrivateTags = MappingTools.intsListToStringOfInts this.DefaultPrivateTags,
                DefaultCardOption = defaultCardOption
            )
        ) |> entity.ConceptTemplateConceptTemplateDefaultUsers.Add
        this.CopyTo entity
        entity

type QuizCard with
    static member Load(entity: AcquiredCardEntity) =
        let fieldNameValueMap =
            Seq.zip
                (entity.Card.Concept.ConceptTemplate.Fields |> Field.GetNames)
                (entity.Card.Concept.Fields |> MappingTools.splitByUnitSeparator)
        let replaceFields template =
            fieldNameValueMap |> Seq.fold(fun (aggregate: string) (key, value) -> aggregate.Replace("{{" + key + "}}", value)) template
        let cardTemplate =
            entity.Card.Concept.ConceptTemplate.CardTemplates
            |> MappingTools.splitByRecordSeparator
            |> List.item (int entity.Card.TemplateIndex)
            |> CardTemplate.Load
        { Id = entity.Id
          Due = entity.Due
          Question = replaceFields cardTemplate.QuestionTemplate
          Answer = replaceFields cardTemplate.AnswerTemplate
          MemorizationState = MemorizationState.Load entity.MemorizationStateAndCardState
          CardState = CardState.Load entity.MemorizationStateAndCardState
          LapseCount = entity.LapseCount
          EaseFactor = float entity.EaseFactorInPermille / 1000.
          Interval =
              if int32 entity.IntervalNegativeIsMinutesPositiveIsDays < 0
              then int16 -1 * entity.IntervalNegativeIsMinutesPositiveIsDays |> float |> TimeSpan.FromMinutes
              else entity.IntervalNegativeIsMinutesPositiveIsDays |> float |> TimeSpan.FromDays
          StepsIndex =
              if entity.StepsIndex.HasValue
              then Some entity.StepsIndex.Value
              else None
          Options = CardOption.Load entity.CardOption }

type Concept with
    static member Load(entity: ConceptEntity) =
        { Id = entity.Id
          Title = entity.Title
          Description = entity.Description
          ConceptTemplate = ConceptTemplate.Load entity.ConceptTemplate
          Fields = MappingTools.splitByUnitSeparator entity.Fields
          Modified = entity.Modified
          IsPublic = entity.IsPublic
          MaintainerId = entity.MaintainerId }
    member this.CopyTo (entity: ConceptEntity) =
        entity.Id <- this.Id
        entity.Title <- this.Title
        entity.Description <- this.Description
        entity.ConceptTemplateId <- this.ConceptTemplate.Id
        entity.Fields <- MappingTools.joinByUnitSeparator this.Fields
        entity.Modified <- this.Modified
        entity.IsPublic <- this.IsPublic
        entity.MaintainerId <- this.MaintainerId
    member this.CopyToNew =
        let entity = ConceptEntity()
        this.CopyTo entity
        entity

type AcquiredCard with
    member this.CopyTo (entity: AcquiredCardEntity) =
        entity.Id <- this.Id
        entity.UserId <- this.UserId
        entity.MemorizationStateAndCardState <- MemorizationStateAndCardStateEnum.from this.MemorizationState this.CardState
        entity.LapseCount <- this.LapseCount
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalNegativeIsMinutesPositiveIsDays <- this.IntervalNegativeIsMinutesPositiveIsDays
        entity.StepsIndex <- Option.toNullable this.StepsIndex
        entity.Due <- this.Due
        entity.CardOptionId <- this.CardOptionId
        entity.Card <- CardEntity (
            TemplateIndex = this.TemplateIndex,
            ConceptId = this.ConceptId
        )
    member this.CopyToNew concept cardOption (privateTags: PrivateTagEntity seq) =
        let entity = AcquiredCardEntity ()
        this.CopyTo entity
        entity.Card.Concept <- concept
        entity.CardOption <- cardOption
        entity.PrivateTagAcquiredCards <- privateTags.Select(fun x -> PrivateTagAcquiredCardEntity(AcquiredCard = entity, PrivateTag = x)).ToList()
        entity
    static member NewlyAcquired userId cardOptionId (card: CardEntity) =
        AcquiredCardEntity(
            Card = card,
            MemorizationStateAndCardState = MemorizationStateAndCardStateEnum.NewNormal,
            LapseCount = 0uy,
            EaseFactorInPermille = 0s,
            IntervalNegativeIsMinutesPositiveIsDays = 0s,
            StepsIndex = Nullable 0uy,
            Due = DateTime.UtcNow,
            CardOptionId = cardOptionId,
            UserId = userId
        )

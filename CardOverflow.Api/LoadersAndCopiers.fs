module LoadersAndCopiers

open CardOverflow.Debug
open MappingTools
open CardOverflow.Entity
open CardOverflow.Pure
open System
open System.Linq
open FsToolkit.ErrorHandling

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
    member this.CopyToNew userId =
        let entity = CardOptionEntity()
        this.CopyTo entity
        entity.UserId <- userId
        entity

type Field with
    static member Load (entity: FieldEntity) =
        { Name = entity.Name
          Font = entity.Font
          FontSize = entity.FontSize
          IsRightToLeft = entity.IsRightToLeft
          Ordinal = entity.Ordinal
          IsSticky = entity.IsSticky }
    member this.CopyToNew (conceptTemplateInstance: ConceptTemplateInstanceEntity) =
        let entity = FieldEntity()
        entity.Name <- this.Name
        entity.Font <- this.Font
        entity.FontSize <- this.FontSize
        entity.IsRightToLeft <- this.IsRightToLeft
        entity.Ordinal <- this.Ordinal
        entity.IsSticky <- this.IsSticky
        entity.ConceptTemplateInstance <- conceptTemplateInstance
        entity

type CardTemplate with
    static member Load (entity: CardTemplateEntity) =
        { Name = entity.Name
          QuestionTemplate = entity.QuestionTemplate
          AnswerTemplate = entity.AnswerTemplate
          ShortQuestionTemplate = entity.ShortQuestionTemplate
          ShortAnswerTemplate = entity.ShortAnswerTemplate  }
    member this.CopyToNew (conceptTemplateInstance: ConceptTemplateInstanceEntity)=
        let entity = CardTemplateEntity()
        entity.Name <- this.Name
        entity.QuestionTemplate <- this.QuestionTemplate
        entity.AnswerTemplate <- this.AnswerTemplate
        entity.ShortQuestionTemplate <- this.ShortQuestionTemplate
        entity.ShortAnswerTemplate <- this.ShortAnswerTemplate
        entity.ConceptTemplateInstance <- conceptTemplateInstance
        entity

type ConceptTemplateInstance with
    member this.AcquireEquality(that: ConceptTemplateInstance) =
        this.ConceptTemplate.Id = that.ConceptTemplate.Id &&
        this.Css = that.Css &&
        this.Fields = that.Fields &&
        this.CardTemplates = that.CardTemplates &&
        this.IsCloze = that.IsCloze &&
        this.LatexPre = that.LatexPre &&
        this.LatexPost = that.LatexPost
    static member Load(entity: ConceptTemplateInstanceEntity) =
        { Id = entity.Id
          ConceptTemplate = {
            Id = entity.ConceptTemplate.Id
            Name = entity.ConceptTemplate.Name
            MaintainerId = entity.ConceptTemplate.MaintainerId
          }
          Css = entity.Css
          Fields = entity.Fields |> Seq.map Field.Load
          CardTemplates = entity.CardTemplates |> Seq.map CardTemplate.Load
          Created = entity.Created
          Modified = entity.Modified |> Option.ofNullable
          IsCloze = entity.IsCloze
          DefaultPublicTags = entity.ConceptTemplate.ConceptTemplateDefaultConceptTemplateUsers.Single().ConceptTemplateDefault.DefaultPublicTags |> MappingTools.stringOfIntsToIntList
          DefaultPrivateTags = entity.ConceptTemplate.ConceptTemplateDefaultConceptTemplateUsers.Single().ConceptTemplateDefault.DefaultPrivateTags |> MappingTools.stringOfIntsToIntList
          DefaultCardOptionId = entity.ConceptTemplate.ConceptTemplateDefaultConceptTemplateUsers.Single().ConceptTemplateDefault.DefaultCardOptionId
          LatexPre = entity.LatexPre
          LatexPost = entity.LatexPost }
    member this.CopyTo (entity: ConceptTemplateInstanceEntity) =
        entity.ConceptTemplate.MaintainerId <- this.ConceptTemplate.MaintainerId
        entity.ConceptTemplate.Name <- this.ConceptTemplate.Name
        entity.Css <- this.Css
        entity.Fields <- this.Fields |> Seq.map (fun x -> x.CopyToNew entity) |> fun x -> x.ToList()
        entity.CardTemplates <- this.CardTemplates |> Seq.map (fun x -> x.CopyToNew entity) |> fun x -> x.ToList()
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.IsCloze <- this.IsCloze
        entity.LatexPre <- this.LatexPre
        entity.LatexPost <- this.LatexPost
    //member this.CopyToNew defaultCardOption = // medTODO this belongs on `ConceptTemplate`
    //    let entity = ConceptTemplateEntity()
    //    ConceptTemplateDefaultConceptTemplateUserEntity(
    //        UserId = this.ConceptTemplate.MaintainerId,
    //        ConceptTemplate = entity,
    //        ConceptTemplateDefault = ConceptTemplateDefaultEntity(
    //            DefaultPublicTags = MappingTools.intsListToStringOfInts this.DefaultPublicTags, // medTODO normalize this
    //            DefaultPrivateTags = MappingTools.intsListToStringOfInts this.DefaultPrivateTags,
    //            DefaultCardOption = defaultCardOption
    //        )
    //    ) |> entity.ConceptTemplateDefaultConceptTemplateUsers.Add
    //    this.CopyTo entity
    //    entity

type QuizCard with
    static member Load(entity: AcquiredCardEntity) =
        let fieldNameValueMap =
                entity.Card.ConceptInstance.FieldValues |> Seq.map (fun x -> (x.Field.Name, x.Value))
        let replaceFields template =
            fieldNameValueMap |> Seq.fold(fun (aggregate: string) (key, value) -> aggregate.Replace("{{" + key + "}}", value)) template
        let cardTemplate = CardTemplate.Load entity.Card.CardTemplate
        result {
            let! memorizationState = MemorizationState.create entity.MemorizationState
            let! cardState = CardState.create entity.CardState
            return
                { Due = entity.Due
                  Question = replaceFields cardTemplate.QuestionTemplate
                  Answer = replaceFields cardTemplate.AnswerTemplate
                  MemorizationState = memorizationState
                  CardState = cardState
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
        }

type ConceptInstance with
    static member Load(entity: ConceptInstanceEntity) =
        { Id = entity.Id
          Fields = entity.FieldValues |> Seq.map (fun x -> x.Value)
          Created = entity.Created
          Modified = entity.Modified |> Option.ofNullable
          Concept = {
            Id = entity.ConceptId
            MaintainerId = entity.Concept.MaintainerId
            Name = entity.Concept.Name
          }
          IsPublic = entity.IsPublic }
    member this.CopyTo (entity: ConceptInstanceEntity) =
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.IsPublic <- this.IsPublic
        entity.FieldValues <- this.Fields |> Seq.map (fun x -> FieldValueEntity(Value = x)) |> fun x -> x.ToList()
    member this.CopyToNew =
        let entity = ConceptInstanceEntity()
        this.CopyTo entity
        entity.Concept <- ConceptEntity()
        entity

type AcquiredCard with
    member this.CopyTo (entity: AcquiredCardEntity) =
        entity.UserId <- this.UserId
        entity.MemorizationState <- MemorizationState.toDb this.MemorizationState
        entity.CardState <- CardState.toDb this.CardState
        entity.LapseCount <- this.LapseCount
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalNegativeIsMinutesPositiveIsDays <- this.IntervalNegativeIsMinutesPositiveIsDays
        entity.StepsIndex <- Option.toNullable this.StepsIndex
        entity.Due <- this.Due
    member this.CopyToNew concept cardOption (privateTags: PrivateTagEntity seq) =
        let entity = AcquiredCardEntity ()
        this.CopyTo entity
        entity.Card <- CardEntity (
            TemplateIndex = this.TemplateIndex,
            Concept = concept
        )
        entity.CardOption <- cardOption
        entity.PrivateTagAcquiredCards <- privateTags.Select(fun x -> PrivateTagAcquiredCardEntity(AcquiredCard = entity, PrivateTag = x)).ToList()
        entity
    static member NewlyAcquired userId cardOptionId (card: CardEntity) =
        AcquiredCardEntity(
            Card = card,
            MemorizationState = MemorizationState.toDb New,
            CardState = CardState.toDb Normal,
            LapseCount = 0uy,
            EaseFactorInPermille = 0s,
            IntervalNegativeIsMinutesPositiveIsDays = 0s,
            StepsIndex = Nullable 0uy,
            Due = DateTime.UtcNow,
            CardOptionId = cardOptionId,
            UserId = userId
        )
    member this.AcquireEquality (db: CardOverflowDb) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        db.AcquiredCards.FirstOrDefault(fun c -> 
            this.UserId = c.UserId &&
            this.ConceptId = c.Card.ConceptId &&
            this.TemplateIndex = c.Card.TemplateIndex
        )

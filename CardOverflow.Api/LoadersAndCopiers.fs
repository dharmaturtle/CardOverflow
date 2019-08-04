module LoadersAndCopiers

open CardOverflow.Debug
open MappingTools
open CardOverflow.Entity
open CardOverflow.Pure
open System
open System.Linq
open FsToolkit.ErrorHandling
open System.Security.Cryptography
open System.Text
open System.Collections.Generic

module ConceptTemplateInstanceEntity =
    let acquireHash (hasher: SHA256) (e: ConceptTemplateInstanceEntity) =
        let hash (hasher: HashAlgorithm) (ct: CardTemplateEntity) =
            [ ct.QuestionTemplate; ct.AnswerTemplate ]
            |> MappingTools.joinByUnitSeparator
            |> Encoding.Unicode.GetBytes
            |> hasher.ComputeHash
        let stringBytes =
            [   e.Css
                e.LatexPre
                e.LatexPost
            ].Concat <| e.Fields.OrderBy(fun x -> x.Ordinal).Select(fun x -> x.Name)
            |> Seq.toList
            |> MappingTools.joinByUnitSeparator
            |> Encoding.Unicode.GetBytes
        e.CardTemplates
            |> Seq.sortBy (fun x -> x.Ordinal)
            |> Seq.collect (hash hasher)
            |> Seq.append stringBytes
            |> Seq.toArray
            |> hasher.ComputeHash

module ConceptInstanceEntity =
    let acquireHash (e: ConceptInstanceEntity) (conceptTemplateHash: byte[]) (hasher: SHA256) =
        e.FieldValues
        |> Seq.map (fun x -> x.Value)
        |> Seq.sort
        |> MappingTools.joinByUnitSeparator
        |> Encoding.Unicode.GetBytes
        |> Seq.append conceptTemplateHash
        |> Seq.toArray
        |> hasher.ComputeHash

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
          IsDefault = entity.IsDefault
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
        entity.IsDefault <- this.IsDefault
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
        { Id = entity.Id
          Name = entity.Name
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
        { Id = entity.Id
          Name = entity.Name
          QuestionTemplate = entity.QuestionTemplate
          AnswerTemplate = entity.AnswerTemplate
          ShortQuestionTemplate = entity.ShortQuestionTemplate
          ShortAnswerTemplate = entity.ShortAnswerTemplate
          Ordinal = entity.Ordinal }
    member this.CopyToNew (conceptTemplateInstance: ConceptTemplateInstanceEntity)=
        let entity = CardTemplateEntity()
        entity.Name <- this.Name
        entity.QuestionTemplate <- this.QuestionTemplate
        entity.AnswerTemplate <- this.AnswerTemplate
        entity.ShortQuestionTemplate <- this.ShortQuestionTemplate
        entity.ShortAnswerTemplate <- this.ShortAnswerTemplate
        entity.ConceptTemplateInstance <- conceptTemplateInstance
        entity.Ordinal <- this.Ordinal
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
          DefaultPublicTags = entity.User_ConceptTemplateInstances.Single().PublicTag_User_ConceptTemplateInstances.Select(fun x -> x.DefaultPublicTagId)
          DefaultPrivateTags = entity.User_ConceptTemplateInstances.Single().PrivateTag_User_ConceptTemplateInstances.Select(fun x -> x.DefaultPrivateTagId)
          DefaultCardOptionId = entity.User_ConceptTemplateInstances.Single().DefaultCardOptionId
          LatexPre = entity.LatexPre
          LatexPost = entity.LatexPost
          AcquireHash = entity.AcquireHash }

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
          }}
    member this.CopyTo (entity: ConceptInstanceEntity) =
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
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
    static member InitialCopyTo userId cardOptionId =
        AcquiredCardEntity(
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

type InitialFieldValue = {
    FieldId: int
    Value: string
}

type InitialConceptInstance = {
    FieldValues: InitialFieldValue seq
    MaintainerId: int
    DefaultCardOptionId: int
    Name: string
    ConceptTemplateHash: byte[]
    CardTemplateIds: int seq
} with
    member this.CopyToNew fileConceptInstances =
        let e =
            ConceptInstanceEntity(
                Created = DateTime.UtcNow,
                Concept = ConceptEntity (
                    MaintainerId = this.MaintainerId,
                    Name = this.Name),
                Cards = (
                    this.CardTemplateIds
                    |> Seq.map (fun x -> 
                        CardEntity (
                            CardTemplateId = x,
                            AcquiredCards = (
                                AcquiredCard.InitialCopyTo this.MaintainerId this.DefaultCardOptionId
                                |> Seq.singleton
                                |> fun x -> x.ToList())))
                    |> fun x -> x.ToList()),
                FieldValues = (
                    this.FieldValues
                    |> Seq.map (fun { FieldId = fi; Value = v } -> FieldValueEntity(FieldId = fi, Value = v))
                    |> fun x -> x.ToList()),
                File_ConceptInstances = fileConceptInstances
            )
        use hasher = SHA256.Create() // lowTODO pull this out
        e.AcquireHash <- ConceptInstanceEntity.acquireHash e this.ConceptTemplateHash hasher
        e

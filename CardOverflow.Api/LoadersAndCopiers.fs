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

module FacetTemplateInstanceEntity =
    let acquireHash (hasher: SHA256) (e: FacetTemplateInstanceEntity) =
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

module FacetInstanceEntity =
    let acquireHash (e: FacetInstanceEntity) (facetTemplateHash: byte[]) (hasher: SHA256) =
        e.FieldValues
        |> Seq.map (fun x -> x.Value)
        |> Seq.sort
        |> MappingTools.joinByUnitSeparator
        |> Encoding.Unicode.GetBytes
        |> Seq.append facetTemplateHash
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
    static member load(entity: CardOptionEntity) =
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
    static member load (entity: FieldEntity) =
        { Id = entity.Id
          Name = entity.Name
          Font = entity.Font
          FontSize = entity.FontSize
          IsRightToLeft = entity.IsRightToLeft
          Ordinal = entity.Ordinal
          IsSticky = entity.IsSticky }
    member this.CopyToNew (facetTemplateInstance: FacetTemplateInstanceEntity) =
        let entity = FieldEntity()
        entity.Name <- this.Name
        entity.Font <- this.Font
        entity.FontSize <- this.FontSize
        entity.IsRightToLeft <- this.IsRightToLeft
        entity.Ordinal <- this.Ordinal
        entity.IsSticky <- this.IsSticky
        entity.FacetTemplateInstance <- facetTemplateInstance
        entity

type CardTemplate with
    static member load (entity: CardTemplateEntity) =
        { Id = entity.Id
          Name = entity.Name
          QuestionTemplate = entity.QuestionTemplate
          AnswerTemplate = entity.AnswerTemplate
          ShortQuestionTemplate = entity.ShortQuestionTemplate
          ShortAnswerTemplate = entity.ShortAnswerTemplate
          Ordinal = entity.Ordinal }
    member this.CopyToNew (facetTemplateInstance: FacetTemplateInstanceEntity)=
        let entity = CardTemplateEntity()
        entity.Name <- this.Name
        entity.QuestionTemplate <- this.QuestionTemplate
        entity.AnswerTemplate <- this.AnswerTemplate
        entity.ShortQuestionTemplate <- this.ShortQuestionTemplate
        entity.ShortAnswerTemplate <- this.ShortAnswerTemplate
        entity.FacetTemplateInstance <- facetTemplateInstance
        entity.Ordinal <- this.Ordinal
        entity

type FacetTemplateInstance with
    static member load(entity: FacetTemplateInstanceEntity) =
        { Id = entity.Id
          Css = entity.Css
          Fields = entity.Fields |> Seq.map Field.load
          CardTemplates = entity.CardTemplates |> Seq.map CardTemplate.load
          Created = entity.Created
          Modified = entity.Modified |> Option.ofNullable
          IsCloze = entity.IsCloze
          LatexPre = entity.LatexPre
          LatexPost = entity.LatexPost
          AcquireHash = entity.AcquireHash }

type AcquiredFacetTemplateInstance with
    static member load(entity: FacetTemplateInstanceEntity) =
        { DefaultPublicTags = entity.User_FacetTemplateInstances.Single().PublicTag_User_FacetTemplateInstances.Select(fun x -> x.DefaultPublicTagId)
          DefaultPrivateTags = entity.User_FacetTemplateInstances.Single().PrivateTag_User_FacetTemplateInstances.Select(fun x -> x.DefaultPrivateTagId)
          DefaultCardOptionId = entity.User_FacetTemplateInstances.Single().DefaultCardOptionId
          Instance = FacetTemplateInstance.load entity }

type FacetTemplate with
    static member load(entity: FacetTemplateEntity) = {
        Id = entity.Id
        Name = entity.Name
        MaintainerId = entity.MaintainerId
        Instances = entity.FacetTemplateInstances |> Seq.map FacetTemplateInstance.load }

type QuizCard with
    static member load(entity: AcquiredCardEntity) =
        let frontSide, backSide =
            CardHtml.generate
                (entity.Card.FacetInstance.FieldValues |> Seq.map (fun x -> (x.Field.Name, x.Value)))
                entity.Card.CardTemplate.QuestionTemplate
                entity.Card.CardTemplate.AnswerTemplate
                entity.Card.CardTemplate.FacetTemplateInstance.Css
        result {
            let! cardState = CardState.create entity.CardState
            return
                { CardId = entity.CardId
                  Due = entity.Due
                  Question = frontSide
                  Answer = backSide
                  CardState = cardState
                  IsLapsed = entity.IsLapsed
                  EaseFactor = float entity.EaseFactorInPermille / 1000.
                  IntervalOrStepsIndex = AcquiredCard.intervalFromDb entity.IntervalOrStepsIndex
                  Options = CardOption.load entity.CardOption }
        }

//type FacetInstance with
//    static member load(entity: FacetInstanceEntity) = {
//        Id = entity.Id
//        Fields = entity.FieldValues |> Seq.map (fun x -> x.Value)
//        Created = entity.Created
//        Modified = entity.Modified |> Option.ofNullable
//    }
//    member this.CopyTo (entity: FacetInstanceEntity) =
//        entity.Created <- this.Created
//        entity.Modified <- this.Modified |> Option.toNullable
//        entity.FieldValues <- this.Fields |> Seq.map (fun x -> FieldValueEntity(Value = x)) |> fun x -> x.ToList()
//    member this.CopyToNew =
//        let entity = FacetInstanceEntity()
//        this.CopyTo entity
//        entity.Facet <- FacetEntity()
//        entity

type AcquiredCard with
    member this.CopyTo (entity: AcquiredCardEntity) =
        entity.UserId <- this.UserId
        entity.CardState <- CardState.toDb this.CardState
        entity.IsLapsed <- this.IsLapsed
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalOrStepsIndex <- AcquiredCard.intervalToDb this.IntervalOrStepsIndex
        entity.Due <- this.Due
    static member InitialCopyTo userId cardOptionId (privateTagIds: int seq) =
        AcquiredCardEntity(
            PrivateTag_AcquiredCards = privateTagIds.Select(fun x -> PrivateTag_AcquiredCardEntity(PrivateTagId = x, UserId = userId)).ToList(),
            CardState = CardState.toDb Normal,
            IsLapsed = false,
            EaseFactorInPermille = 0s,
            IntervalOrStepsIndex = Int16.MinValue,
            Due = DateTime.UtcNow,
            CardOptionId = cardOptionId,
            UserId = userId
        )

type InitialConceptInstance = {
    FieldValues: FieldValue seq
    MaintainerId: int
    DefaultCardOptionId: int
    Description: string
    FacetTemplateHash: byte[]
    CardTemplateIdsAndTags: (int * int seq) seq
} with
    member this.CopyToNew fileFacetInstances =
        let e =
            FacetInstanceEntity(
                Created = DateTime.UtcNow,
                Cards = (
                    this.CardTemplateIdsAndTags
                    |> Seq.map (fun (i, tags) -> 
                        CardEntity (
                            CardTemplateId = i,
                            AcquiredCards = (
                                AcquiredCard.InitialCopyTo this.MaintainerId this.DefaultCardOptionId tags
                                |> Seq.singleton
                                |> fun x -> x.ToList())))
                    |> fun x -> x.ToList()),
                FieldValues = (
                    this.FieldValues
                    |> Seq.map (fun { FieldId = fi; Value = v } -> FieldValueEntity(FieldId = fi, Value = v))
                    |> fun x -> x.ToList()),
                File_FacetInstances = fileFacetInstances
            )
        use hasher = SHA256.Create() // lowTODO pull this out
        e.AcquireHash <- FacetInstanceEntity.acquireHash e this.FacetTemplateHash hasher
        ConceptEntity(
            Name = this.FieldValues.First().Value,
            MaintainerId = this.MaintainerId,
            Facets = [
                FacetEntity (
                    MaintainerId = this.MaintainerId,
                    Description = this.Description,
                    FacetInstances = [e].ToList()
                )
            ].ToList()
        )

type AcquiredConcept with
    static member load userId (concept: ConceptEntity) =
        {   Id = concept.Id
            Name = concept.Name
            MaintainerId = concept.MaintainerId
            AcquiredFacets =
                concept.Facets.Select(fun x -> x.FacetInstances |> Seq.maxBy (fun x -> x.Created) ).Select(fun fi -> // lowTODO, optimization, there should only be one facetInstance loaded from the db
                    let cards =
                        fi.Cards.GroupBy(fun x -> x.CardTemplateId).Select(fun x ->
                            let cardTemplate = x.First().CardTemplate
                            let card =
                                x
                                    .Single(fun x -> x.CardTemplateId = cardTemplate.Id)
                                    .AcquiredCards
                                    .SingleOrDefault(fun x -> x.UserId = userId)
                            let front, back =
                                CardHtml.generate
                                    (fi.FieldValues.Select(fun x -> (x.Field.Name, x.Value)))
                                    cardTemplate.QuestionTemplate
                                    cardTemplate.AnswerTemplate
                                    cardTemplate.FacetTemplateInstance.Css
                            if isNull card
                            then None
                            else
                                {   Front = front
                                    Back = back
                                    CardTemplateName = cardTemplate.Name
                                    Tags = card.PrivateTag_AcquiredCards.Select(fun x -> x.PrivateTag.Name)
                                } |> Some
                        )
                    {   FacetInstanceId = fi.Id
                        FacetTemplateInstanceId = fi.FieldValues.First().Field.FacetTemplateInstanceId
                        MaintainerId = fi.Facet.MaintainerId
                        Description = fi.Facet.Description
                        FacetId = fi.FacetId
                        FacetCreated = fi.Created
                        FacetModified = Option.ofNullable fi.Modified
                        FacetFields = fi.FieldValues.OrderBy(fun x -> x.Field.Ordinal).Select(fun x -> (Field.load x.Field, x.Value))
                        Cards = cards |> Seq.choose id
                    }
                )
        }

type FieldValue with
    static member load (entity: FieldValueEntity) = {
        Value = entity.Value
        FieldId = entity.FieldId
    }

type Card with
    static member load userId (entity: CardEntity) =
        let front, back =
            CardHtml.generate
                (entity.FacetInstance.FieldValues |> Seq.map (fun x -> (x.Field.Name, x.Value)))
                entity.CardTemplate.QuestionTemplate
                entity.CardTemplate.AnswerTemplate
                entity.CardTemplate.FacetTemplateInstance.Css
        {   Id = entity.Id
            CardTemplateName = entity.CardTemplate.Name
            ClozeIndex = entity.ClozeIndex |> Option.ofNullable
            Front = front
            Back = back
            IsAcquired = entity.AcquiredCards.Any(fun x -> x.UserId = userId)
        }

type FacetInstance with
    static member load userId (entity: FacetInstanceEntity) = {
        Id = entity.Id
        Created = entity.Created
        Modified = entity.Modified |> Option.ofNullable
        IsDmca = entity.IsDmca
        Cards = entity.Cards |> Seq.map (Card.load userId)
        FieldValues = entity.FieldValues |> Seq.map FieldValue.load
        TemplateInstance = entity.Cards.First().CardTemplate.FacetTemplateInstance |> FacetTemplateInstance.load
    }

type Comment with
    static member load (entity: CommentFacetEntity) = {
        User = entity.User.DisplayName
        UserId = entity.UserId
        Text = entity.Text
        Created = entity.Created
        IsDmca = entity.IsDmca
    }

type Facet with
    static member load userId (entity: FacetEntity) = {
        Id = entity.Id
        Maintainer = entity.Maintainer.DisplayName
        MaintainerId = entity.MaintainerId
        Description = entity.Description
        LatestInstance = entity.FacetInstances.OrderByDescending(fun x -> x.Created).First() |> FacetInstance.load userId
        Comments = entity.CommentFacets |> Seq.map Comment.load
    }

type DetailedConcept with
    static member load userId (entity: ConceptEntity) = {
        Id = entity.Id
        Name = entity.Name
        Maintainer = entity.Maintainer.DisplayName
        MaintainerId = entity.MaintainerId
        Facets = entity.Facets |> Seq.map (Facet.load userId)
    }

type ExploreConcept with
    static member load userId (entity: ConceptEntity) = {
        Id = entity.Id
        Name = entity.Name
        Maintainer = entity.Maintainer.DisplayName
        MaintainerId = entity.MaintainerId
        Users = entity.Facets.SelectMany(fun x -> x.FacetInstances.SelectMany(fun x -> x.Cards.Select(fun x -> x.AcquiredCards.Count))).Sum()
        Facets = entity.Facets |> Seq.map (Facet.load userId)
    }

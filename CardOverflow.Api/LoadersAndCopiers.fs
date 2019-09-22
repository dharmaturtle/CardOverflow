module LoadersAndCopiers

open CardOverflow.Pure.Core
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

module CardTemplateInstanceEntity =
    let acquireHash (hasher: SHA256) (e: CardTemplateInstanceEntity) =
        [   e.Css
            e.LatexPre
            e.LatexPost
            e.QuestionTemplate
            e.AnswerTemplate
            e.ShortQuestionTemplate
            e.ShortAnswerTemplate
        ].Concat <| e.Fields.OrderBy(fun x -> x.Ordinal).Select(fun x -> x.Name)
        |> Seq.toList
        |> MappingTools.joinByUnitSeparator
        |> Encoding.Unicode.GetBytes
        |> hasher.ComputeHash

module CardInstanceEntity =
    let acquireHash (e: CardInstanceEntity) (cardTemplateHash: byte[]) (hasher: SHA256) =
        e.FieldValues
        |> Seq.map (fun x -> x.Value)
        |> Seq.sort
        |> MappingTools.joinByUnitSeparator
        |> Encoding.Unicode.GetBytes
        |> Seq.append cardTemplateHash
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
    member this.copyTo (entity: FieldEntity) (cardTemplateInstance: CardTemplateInstanceEntity) =
        entity.Name <- this.Name
        entity.Font <- this.Font
        entity.FontSize <- this.FontSize
        entity.IsRightToLeft <- this.IsRightToLeft
        entity.Ordinal <- this.Ordinal
        entity.IsSticky <- this.IsSticky
        entity.CardTemplateInstance <- cardTemplateInstance
    member this.CopyToNew (cardTemplateInstance: CardTemplateInstanceEntity) =
        let entity = FieldEntity()
        this.copyTo entity cardTemplateInstance
        entity

type FieldAndValue with
    static member load (entity: FieldValueEntity) =
        {   Field = Field.load entity.Field
            Value = entity.Value
        }
    member this.copyToValue (entity: FieldValueEntity) =
        entity.FieldId <- this.Field.Id
        entity.Value <- this.Value
    member this.copyToNewValue =
        let entity = FieldValueEntity()
        this.copyToValue entity
        entity

type CardTemplateInstance with
    static member load(entity: CardTemplateInstanceEntity) =
        { Id = entity.Id
          Css = entity.Css
          Fields = entity.Fields |> Seq.map Field.load
          Created = entity.Created
          Modified = entity.Modified |> Option.ofNullable
          LatexPre = entity.LatexPre
          LatexPost = entity.LatexPost
          AcquireHash = entity.AcquireHash
          QuestionTemplate = entity.QuestionTemplate
          AnswerTemplate = entity.AnswerTemplate
          ShortQuestionTemplate = entity.ShortQuestionTemplate
          ShortAnswerTemplate = entity.ShortAnswerTemplate }
    member this.CopyToNew (cardTemplateInstance: CardTemplateInstanceEntity)=
        let entity = CardTemplateInstanceEntity()
        entity.QuestionTemplate <- this.QuestionTemplate
        entity.AnswerTemplate <- this.AnswerTemplate
        entity.ShortQuestionTemplate <- this.ShortQuestionTemplate
        entity.ShortAnswerTemplate <- this.ShortAnswerTemplate // medTODO not done
        entity

type AcquiredCardTemplateInstance with
    static member load(entity: CardTemplateInstanceEntity) =
        { DefaultTags = entity.User_CardTemplateInstances.Single().Tag_User_CardTemplateInstances.Select(fun x -> x.DefaultTagId)
          DefaultCardOptionId = entity.User_CardTemplateInstances.Single().DefaultCardOptionId
          CardTemplateInstance = CardTemplateInstance.load entity }

type CardTemplate with
    static member load(entity: CardTemplateEntity) = {
        Id = entity.Id
        Name = entity.Name
        MaintainerId = entity.AuthorId
        Instances = entity.CardTemplateInstances |> Seq.map CardTemplateInstance.load }

type QuizCard with
    static member load(entity: AcquiredCardEntity) =
        let front, back, frontSynthVoice, backSynthVoice =
            CardHtml.generate
                (entity.CardInstance.FieldValues |> Seq.map (fun x -> (x.Field.Name, x.Value)))
                (entity.CardInstance.FieldValues.First().Field.CardTemplateInstance.QuestionTemplate)
                (entity.CardInstance.FieldValues.First().Field.CardTemplateInstance.AnswerTemplate)
                (entity.CardInstance.FieldValues.First().Field.CardTemplateInstance.Css)
        result {
            let! cardState = CardState.create entity.CardState
            return {
                AcquiredCardId = entity.Id
                CardInstanceId = entity.CardInstanceId
                Due = entity.Due
                Front = front
                Back = back
                FrontSynthVoice = frontSynthVoice
                BackSynthVoice = backSynthVoice
                CardState = cardState
                IsLapsed = entity.IsLapsed
                EaseFactor = float entity.EaseFactorInPermille / 1000.
                IntervalOrStepsIndex = IntervalOrStepsIndex.intervalFromDb entity.IntervalOrStepsIndex
                Options = CardOption.load entity.CardOption }
        }

type CardInstance with
    static member load userId (entity: CardInstanceEntity) = {
        Id = entity.Id
        Created = entity.Created
        Modified = entity.Modified |> Option.ofNullable
        IsDmca = entity.IsDmca
        FieldValues = entity.FieldValues |> Seq.map FieldAndValue.load
        TemplateInstance = entity.FieldValues.First().Field.CardTemplateInstance |> CardTemplateInstance.load
        IsAcquired = entity.AcquiredCards.Any(fun x -> x.UserId = userId)
    }
    member this.CopyTo (entity: CardInstanceEntity) =
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.FieldValues <- this.FieldValues |> Seq.map (fun x -> FieldValueEntity(FieldId = x.Field.Id, Value = x.Value)) |> fun x -> x.ToList()
        use hasher = SHA256.Create()
        entity.AcquireHash <- CardInstanceEntity.acquireHash entity this.TemplateInstance.AcquireHash hasher
    member this.CopyToNew =
        let entity = CardInstanceEntity()
        this.CopyTo entity
        entity
    member this.CopyFieldsToNewInstance cardId =
        let e = this.CopyToNew
        e.CardId <- cardId
        e

type AcquiredCard with
    member this.CopyTo (entity: AcquiredCardEntity) =
        entity.UserId <- this.UserId
        entity.CardState <- CardState.toDb this.CardState
        entity.IsLapsed <- this.IsLapsed
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalOrStepsIndex <- IntervalOrStepsIndex.intervalToDb this.IntervalOrStepsIndex
        entity.Due <- this.Due
    static member InitialCopyTo userId cardOptionId (tagIds: int seq) =
        AcquiredCardEntity(
            Tag_AcquiredCards = tagIds.Select(fun x -> Tag_AcquiredCardEntity(TagId = x)).ToList(),
            CardState = CardState.toDb Normal,
            IsLapsed = false,
            EaseFactorInPermille = 0s,
            IntervalOrStepsIndex = Int16.MinValue,
            Due = DateTime.UtcNow,
            CardOptionId = cardOptionId,
            UserId = userId
        )

type InitialCardInstance = {
    FieldValues: FieldAndValue seq
    AuthorId: int
    DefaultCardOptionId: int
    Description: string
    CardTemplateHash: byte[]
    CardTemplateIdsAndTags: int * int seq
} with
    member this.CopyToNew fileCardInstances =
        let _, tags = this.CardTemplateIdsAndTags // medTODO drop the _
        let e =
            CardInstanceEntity(
                Created = DateTime.UtcNow,
                Card =
                    CardEntity(
                        AuthorId = this.AuthorId,
                        Description = this.Description
                    ),
                FieldValues =
                    this.FieldValues
                        .Select(fun x -> x.copyToNewValue)
                        .ToList(),
                File_CardInstances = fileCardInstances,
                AcquiredCards = [
                    AcquiredCard.InitialCopyTo this.AuthorId this.DefaultCardOptionId tags
                ].ToList()
            )
        use hasher = SHA256.Create() // lowTODO pull this out
        e.AcquireHash <- CardInstanceEntity.acquireHash e this.CardTemplateHash hasher
        e

type AcquiredCard with
    static member load (entity: AcquiredCardEntity) = result {
        let! cardState = entity.CardState |> CardState.create
        return
            {   CardId = entity.CardInstance.CardId
                AcquiredCardId = entity.Id
                UserId = entity.UserId
                CardTemplateInstance = entity.CardInstance.FieldValues.First().Field.CardTemplateInstance |> CardTemplateInstance.load
                CardState = cardState
                IsLapsed = entity.IsLapsed
                EaseFactorInPermille = entity.EaseFactorInPermille
                IntervalOrStepsIndex = entity.IntervalOrStepsIndex |> IntervalOrStepsIndex.intervalFromDb
                Due = entity.Due
                CardOptionId = entity.CardOptionId
                CardInstance = CardInstance.load entity.UserId entity.CardInstance
                Tags = entity.Tag_AcquiredCards.Select(fun x -> x.Tag.Name)
                //Id = concept.Id
                //Name = concept.Name
                //MaintainerId = concept.MaintainerId
                //AcquiredCards =
                //    concept.Cards.Select(fun x -> x.CardInstances |> Seq.maxBy (fun x -> x.Modified |?? lazy x.Created) ).Select(fun fi -> // lowTODO, optimization, there should only be one cardInstance loaded from the db
                //        let cards =
                //            fi.Cards.GroupBy(fun x -> x.CardTemplateId).Select(fun x ->
                //                let cardTemplate = x.First().CardTemplate
                //                let card =
                //                    x
                //                        .Single(fun x -> x.CardTemplateId = cardTemplate.Id)
                //                        .AcquiredCards
                //                        .SingleOrDefault(fun x -> x.UserId = userId)
                //                let front, back, _, _ =
                //                    CardHtml.generate
                //                        (fi.FieldValues.Select(fun x -> (x.Field.Name, x.Value)))
                //                        cardTemplate.QuestionTemplate
                //                        cardTemplate.AnswerTemplate
                //                        cardTemplate.CardTemplateInstance.Css
                //                if isNull card
                //                then None
                //                else
                //                    {   Front = front
                //                        Back = back
                //                        CardTemplateName = cardTemplate.Name
                //                        Tags = card.PrivateTag_AcquiredCards.Select(fun x -> x.PrivateTag.Name)
                //                    } |> Some
                //            )
                //        {   CardInstanceId = fi.Id
                //            CardTemplateInstanceId = fi.FieldValues.First().Field.CardTemplateInstanceId
                //            MaintainerId = fi.Card.MaintainerId
                //            Description = fi.Card.Description
                //            CardId = fi.CardId
                //            CardCreated = fi.Created
                //            CardModified = Option.ofNullable fi.Modified
                //            CardFields =
                //                fi.FieldValues
                //                    .OrderBy(fun x -> x.Field.Ordinal)
                //                    .Select(fun x -> 
                //                        {   Field = Field.load x.Field
                //                            Value = x.Value
                //                        }).ToList()
                //            Cards = cards |> Seq.choose id
                //        }
                //    ).ToList()
            }
        }

//type Card with
//    static member load userId (entity: CardEntity) =
//        let front, back, _, _ =
//            CardHtml.generate
//                (entity.CardInstance.FieldValues |> Seq.map (fun x -> (x.Field.Name, x.Value)))
//                entity.CardTemplate.QuestionTemplate
//                entity.CardTemplate.AnswerTemplate
//                entity.CardTemplate.CardTemplateInstance.Css
//        {   Id = entity.Id
//            CardTemplateName = entity.CardTemplate.Name
//            ClozeIndex = entity.ClozeIndex |> Option.ofNullable
//            Front = front
//            Back = back
//            IsAcquired = entity.AcquiredCards.Any(fun x -> x.UserId = userId)
//        }

type Comment with
    static member load (entity: CommentCardEntity) = {
        User = entity.User.DisplayName
        UserId = entity.UserId
        Text = entity.Text
        Created = entity.Created
        IsDmca = entity.IsDmca
    }

//type Card with
//    static member load userId (entity: CardEntity) = {
//        Id = entity.Id
//        Maintainer = entity.Author.DisplayName
//        MaintainerId = entity.AuthorId
//        Description = entity.Description
//        LatestInstance = entity.CardInstances.OrderByDescending(fun x -> x.Created).First() |> CardInstance.load userId
//        Comments = entity.CommentCards |> Seq.map Comment.load
//    }

type ExploreCard with
    static member load userId (entity: CardEntity) = {
        Id = entity.Id
        Author = entity.Author.DisplayName
        AuthorId = entity.AuthorId
        Users = entity.CardInstances.Select(fun x -> x.AcquiredCards.Count).Sum()
        Description = entity.Description
        LatestInstance = entity.CardInstances |> Seq.maxBy (fun x -> x.Modified |?? lazy x.Created) |> CardInstance.load userId
        Comments = entity.CommentCards |> Seq.map Comment.load
        Tags =
            entity.CardInstances
                .SelectMany(fun x -> x.AcquiredCards.SelectMany(fun x -> x.Tag_AcquiredCards.Select(fun x -> x.Tag.Name)))
                .GroupBy(fun x -> x)
                .Select(fun tags ->
                    let name = tags.First()
                    {   Name = name
                        Count = tags.Count()
                        IsAcquired = 
                            entity.CardInstances
                                .Select(fun x -> x.AcquiredCards.SingleOrDefault(fun x -> x.UserId = userId))
                                .SingleOrDefault() // medTODO revisit this when multiple card instances are a thing
                                |> Option.ofObj
                                |> function
                                | Some x -> x.Tag_AcquiredCards.Any(fun x -> x.Tag.Name = name)
                                | None -> false
                    })
        Relationships =
            let sources =
                entity.RelationshipSources.Select(fun r ->
                    let front, _, _, _ =
                        r.Target.CardInstances.First() // .First should have an orderby when we have multiple card instances
                        |> CardInstance.load userId
                        |> fun x -> x.FrontBackFrontSynthBackSynth
                    {   Name = r.Name
                        Front = front
                        CardId = r.TargetId
                        IsAcquired = r.Target.CardInstances.Any(fun x -> x.AcquiredCards.Any(fun x -> x.UserId = userId))
                    }) |> Seq.toList
            let targets =
                entity.RelationshipTargets.Select(fun r ->
                    let front, _, _, _ =
                        r.Source.CardInstances.First() // .First should have an orderby when we have multiple card instances
                        |> CardInstance.load userId
                        |> fun x -> x.FrontBackFrontSynthBackSynth
                    {   Name = r.Name
                        Front = front
                        CardId = r.SourceId
                        IsAcquired = r.Source.CardInstances.Any(fun x -> x.AcquiredCards.Any(fun x -> x.UserId = userId))
                    }) |> Seq.toList
            sources @ targets
    }

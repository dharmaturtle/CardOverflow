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
            e.Fields
        ]
        |> MappingTools.joinByUnitSeparator
        |> Encoding.Unicode.GetBytes
        |> hasher.ComputeHash

module CardInstanceEntity =
    let acquireHash (e: CardInstanceEntity) (cardTemplateHash: byte[]) (hasher: SHA256) =
        e.FieldValues
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

type FieldAndValue with
    static member load (fields: Field seq) fieldValues =
        fieldValues |> MappingTools.splitByUnitSeparator |> Seq.indexed |> Seq.map (fun (i, x) -> {
            Field = fields.Single(fun x -> int x.Ordinal = i)
            Value = x
        }) |> toResizeArray
    static member join (fields: FieldAndValue seq) =
        fields |> Seq.map (fun x -> x.Value) |> MappingTools.joinByUnitSeparator

type CardTemplateInstance with
    static member load(entity: CardTemplateInstanceEntity) = {
        Id = entity.Id
        Css = entity.Css
        Fields = Fields.fromString entity.Fields
        Created = entity.Created
        Modified = entity.Modified |> Option.ofNullable
        LatexPre = entity.LatexPre
        LatexPost = entity.LatexPost
        AcquireHash = entity.AcquireHash
        QuestionTemplate = entity.QuestionTemplate
        AnswerTemplate = entity.AnswerTemplate
        ShortQuestionTemplate = entity.ShortQuestionTemplate
        ShortAnswerTemplate = entity.ShortAnswerTemplate }
    member this.CopyTo (entity: CardTemplateInstanceEntity) =
        entity.Css <- this.Css
        entity.Fields <- Fields.toString this.Fields
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.LatexPre <- this.LatexPre
        entity.LatexPost <- this.LatexPost
        entity.QuestionTemplate <- this.QuestionTemplate
        entity.AnswerTemplate <- this.AnswerTemplate
        entity.ShortQuestionTemplate <- this.ShortQuestionTemplate
        entity.ShortAnswerTemplate <- this.ShortAnswerTemplate
        use hasher = SHA256.Create()
        entity.AcquireHash <- CardTemplateInstanceEntity.acquireHash hasher entity
    member this.CopyToNewInstance cardTemplateId =
        let e = CardTemplateInstanceEntity()
        this.CopyTo e
        e.Created <- DateTime.UtcNow
        e.Modified <- Nullable()
        e.CardTemplateId <- cardTemplateId
        e

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
        LatestInstance = entity.CardTemplateInstances |> Seq.maxBy (fun x -> x.Modified |?? lazy x.Created) |> CardTemplateInstance.load }

type CardInstance with
    static member load userId (entity: CardInstanceEntity) = {
        Id = entity.Id
        Created = entity.Created
        Modified = entity.Modified |> Option.ofNullable
        IsDmca = entity.IsDmca
        FieldValues = FieldAndValue.load (Fields.fromString entity.CardTemplateInstance.Fields) entity.FieldValues
        TemplateInstance = CardTemplateInstance.load entity.CardTemplateInstance
        IsAcquired = entity.AcquiredCards.Any(fun x -> x.UserId = userId) }
    member this.CopyTo (entity: CardInstanceEntity) =
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.FieldValues <- FieldAndValue.join this.FieldValues 
        use hasher = SHA256.Create()
        entity.AcquireHash <- CardInstanceEntity.acquireHash entity this.TemplateInstance.AcquireHash hasher
    member this.CopyToNew =
        let entity = CardInstanceEntity()
        this.CopyTo entity
        entity
    member this.CopyFieldsToNewInstance cardId cardTemplateInstanceId =
        let e = this.CopyToNew
        e.Created <- DateTime.UtcNow
        e.Modified <- Nullable()
        e.CardId <- cardId
        e.CardTemplateInstanceId <- cardTemplateInstanceId
        e

type QuizCard with
    static member load userId (entity: AcquiredCardEntity) =
        let instance = entity.CardInstance |> CardInstance.load userId
        let front, back, frontSynthVoice, backSynthVoice =
            CardHtml.generate
                (instance.FieldValues |> Seq.map (fun x -> x.Field.Name, x.Value))
                (entity.CardInstance.CardTemplateInstance.QuestionTemplate)
                (entity.CardInstance.CardTemplateInstance.AnswerTemplate)
                (entity.CardInstance.CardTemplateInstance.Css)
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
    CardTemplateInstanceIdAndTags: int * int seq
    Created: DateTime
} with
    member this.CopyToNew fileCardInstances =
        let cardTemplateInstanceId, tags = this.CardTemplateInstanceIdAndTags
        let e =
            CardInstanceEntity(
                Created = this.Created,
                CardTemplateInstanceId = cardTemplateInstanceId,
                Card =
                    CardEntity(
                        AuthorId = this.AuthorId,
                        Description = this.Description
                    ),
                FieldValues = FieldAndValue.join this.FieldValues,
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
                CardTemplateInstance = entity.CardInstance.CardTemplateInstance |> CardTemplateInstance.load
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
                                .SingleOrDefault(fun x -> not <| isNull x)
                                |> function
                                | null -> false
                                | x -> x.Tag_AcquiredCards.Any(fun x -> x.Tag.Name = name)
                    })
        Relationships =
            let sources =
                entity.RelationshipSources.GroupBy(fun x -> x.Name, x.SourceId, x.TargetId).Select(fun r ->
                    let name = r.First().Name
                    let sourceId = r.First().SourceId
                    let targetId = r.First().TargetId
                    {   Name = name
                        CardId = targetId
                        IsAcquired = r.Any(fun x -> x.SourceId = sourceId && x.TargetId = targetId && x.UserId = userId && x.Name = name)
                        Users = r.Count(fun x -> x.SourceId = sourceId && x.TargetId = targetId && x.Name = name)
                    }) |> Seq.toList
            let targets =
                entity.RelationshipTargets.GroupBy(fun x -> x.Name, x.SourceId, x.TargetId).Select(fun r ->
                    let name = r.First().Name
                    let sourceId = r.First().SourceId
                    let targetId = r.First().TargetId
                    {   Name = Relationship.flipName name
                        CardId = sourceId
                        IsAcquired = r.Any(fun x -> x.SourceId = sourceId && x.TargetId = targetId && x.UserId = userId && x.Name = name)
                        Users = r.Count(fun x -> x.SourceId = sourceId && x.TargetId = targetId && x.Name = name)
                    }) |> Seq.toList
            sources @ targets
            |> List.groupBy (fun x -> x.CardId)
            |> List.map (fun (cardId, relationships) -> 
                {   Name = relationships.First().Name
                    CardId = cardId
                    IsAcquired = relationships.Any(fun x -> x.IsAcquired)
                    Users = relationships.Sum(fun x -> x.Users)
                }
            )
    }

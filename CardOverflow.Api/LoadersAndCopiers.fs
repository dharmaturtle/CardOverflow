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
open System.Text.RegularExpressions

module CardTemplateInstanceEntity =
    let acquireHash (hasher: SHA256) (e: CardTemplateInstanceEntity) =
        [   e.Name
            e.Css
            e.LatexPre
            e.LatexPost
            e.QuestionTemplate
            e.AnswerTemplate
            e.ShortQuestionTemplate
            e.ShortAnswerTemplate
            e.Fields
        ]
        |> List.map standardizeWhitespace
        |> MappingTools.joinByUnitSeparator
        |> Encoding.Unicode.GetBytes
        |> hasher.ComputeHash
        |> Convert.ToBase64String

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
        fieldValues |> MappingTools.splitByUnitSeparator |> List.mapi (fun i x -> {
            Field = fields.Single(fun x -> int x.Ordinal = i)
            Value = x
        }) |> toResizeArray
    static member join (fields: FieldAndValue seq) =
        fields |> Seq.map (fun x -> x.Value) |> MappingTools.joinByUnitSeparator

type IdOrEntity<'a> =
    | Id of int
    | Entity of 'a

type CardTemplateInstance with
    static member load (entity: CardTemplateInstanceEntity) = {
        Id = entity.Id
        Name = entity.Name
        CardTemplateId = entity.CardTemplateId
        Css = entity.Css
        Fields = Fields.fromString entity.Fields
        Created = entity.Created
        Modified = entity.Modified |> Option.ofNullable
        LatexPre = entity.LatexPre
        LatexPost = entity.LatexPost
        QuestionTemplate = entity.QuestionTemplate
        AnswerTemplate = entity.AnswerTemplate
        ShortQuestionTemplate = entity.ShortQuestionTemplate
        ShortAnswerTemplate = entity.ShortAnswerTemplate
        EditSummary = entity.EditSummary }
    static member initialize = {
        Id = 0
        Name = "New Template"
        CardTemplateId = 0
        Css = """.card {
     font-family: arial;
     font-size: 20px;
     text-align: center;
}"""
        Fields = [
        {   Name = "Front"
            Font = "Arial"
            FontSize = 20uy
            IsRightToLeft = false
            Ordinal = 0uy
            IsSticky = false }
        {   Name = "Back"
            Font = "Arial"
            FontSize = 20uy
            IsRightToLeft = false
            Ordinal = 1uy
            IsSticky = false }
        {   Name = "Source"
            Font = "Arial"
            FontSize = 20uy
            IsRightToLeft = false
            Ordinal = 2uy
            IsSticky = true
        }]
        Created = DateTime.UtcNow
        Modified = None
        LatexPre = """\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
"""
        LatexPost = """\end{document}"""
        QuestionTemplate = """{{Front}}"""
        AnswerTemplate = """{{FrontSide}}

<hr id=answer>

{{Back}}"""
        ShortQuestionTemplate = ""
        ShortAnswerTemplate = ""
        EditSummary = "Initial creation" }
    member this.CopyTo (entity: CardTemplateInstanceEntity) =
        entity.Name <- this.Name
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
        entity.EditSummary <- this.EditSummary
    member this.CopyToNewInstance cardTemplate =
        let e = CardTemplateInstanceEntity()
        this.CopyTo e
        e.Created <- DateTime.UtcNow
        e.Modified <- Nullable()
        match cardTemplate with
        | Id id -> e.CardTemplateId <- id
        | Entity entity -> e.CardTemplate <- entity
        e

type AcquiredCardTemplateInstance with
    static member load(entity: CardTemplateInstanceEntity) =
        { DefaultTags = entity.User_CardTemplateInstances.Single().Tag_User_CardTemplateInstances.Select(fun x -> x.DefaultTagId)
          DefaultCardOptionId = entity.User_CardTemplateInstances.Single().DefaultCardOptionId
          CardTemplateInstance = CardTemplateInstance.load entity }

type CardTemplate with
    static member load (entity: CardTemplateEntity) = {
        Id = entity.Id
        AuthorId = entity.AuthorId
        LatestInstance = entity.CardTemplateInstances |> Seq.maxBy (fun x -> x.Modified |?? lazy x.Created) |> CardTemplateInstance.load }

type CardInstanceView with
    static member load (entity: CardInstanceEntity) = {
        FieldValues = FieldAndValue.load (Fields.fromString entity.CardTemplateInstance.Fields) entity.FieldValues
        TemplateInstance = CardTemplateInstance.load entity.CardTemplateInstance }
    member this.CopyTo (entity: CardInstanceEntity) =
        entity.FieldValues <- FieldAndValue.join this.FieldValues 
        entity.CardTemplateInstanceId <- this.TemplateInstance.Id
    member this.CopyToNew =
        let entity = CardInstanceEntity()
        this.CopyTo entity
        entity
    member this.CopyFieldsToNewInstance card editSummary =
        let e = this.CopyToNew
        e.Created <- DateTime.UtcNow
        e.Modified <- Nullable()
        match card with
        | Id id -> e.CardId <- id
        | Entity entity -> e.Card <- entity
        e.EditSummary <- editSummary
        e

type CardInstanceMeta with
    static member load userId (entity: CardInstanceEntity) =
        let front, back, _, _ = entity |> CardInstanceView.load |> fun x -> x.FrontBackFrontSynthBackSynth
        {   Id = entity.Id
            Created = entity.Created
            Modified = entity.Modified |> Option.ofNullable
            IsDmca = entity.IsDmca
            IsAcquired = entity.AcquiredCards.Any(fun x -> x.UserId = userId)
            StrippedFront = MappingTools.stripHtmlTags front
            StrippedBack = MappingTools.stripHtmlTags back
        }
    static member initialize =
        {   Id = 0
            Created = DateTime.UtcNow
            Modified = None
            IsDmca = false
            IsAcquired = true
            StrippedFront = ""
            StrippedBack = ""
        }
    member this.copyTo (entity: CardInstanceEntity) =
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.IsDmca <- this.IsDmca
    member this.copyToNew =
        let e = CardInstanceEntity()
        this.copyTo e
        e

type QuizCard with
    static member load (entity: AcquiredCardEntity) =
        let front, back, frontSynthVoice, backSynthVoice =
            entity.CardInstance |> CardInstanceView.load |> fun x -> x.FrontBackFrontSynthBackSynth
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
    member this.copyTo (entity: AcquiredCardEntity) (tagIds: int seq) =
        entity.UserId <- this.UserId
        entity.CardState <- CardState.toDb this.CardState
        entity.IsLapsed <- this.IsLapsed
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalOrStepsIndex <- IntervalOrStepsIndex.intervalToDb this.IntervalOrStepsIndex
        entity.CardOptionId <- this.CardOptionId
        entity.Due <- this.Due
        entity.Tag_AcquiredCards <- tagIds.Select(fun x -> Tag_AcquiredCardEntity(TagId = x)).ToList()
    member this.copyToNew tagIds =
        let e = AcquiredCardEntity()
        this.copyTo e tagIds
        e
    static member initialize userId cardOptionId tags =
        {   CardId = 0
            AcquiredCardId = 0
            UserId = userId
            CardState = CardState.Normal
            IsLapsed = false
            EaseFactorInPermille = 0s
            IntervalOrStepsIndex = NewStepsIndex 0uy
            Due = DateTime.UtcNow
            CardOptionId = cardOptionId
            CardInstanceMeta = CardInstanceMeta.initialize
            Tags = tags
        }
    static member load (entity: AcquiredCardEntity) = result {
        let! cardState = entity.CardState |> CardState.create
        return
            {   CardId = entity.CardInstance.CardId
                AcquiredCardId = entity.Id
                UserId = entity.UserId
                CardState = cardState
                IsLapsed = entity.IsLapsed
                EaseFactorInPermille = entity.EaseFactorInPermille
                IntervalOrStepsIndex = entity.IntervalOrStepsIndex |> IntervalOrStepsIndex.intervalFromDb
                Due = entity.Due
                CardOptionId = entity.CardOptionId
                CardInstanceMeta = CardInstanceMeta.load entity.UserId entity.CardInstance
                Tags = entity.Tag_AcquiredCards.Select(fun x -> x.Tag.Name)
            }
        }

type Comment with
    static member load (entity: CommentCardEntity) = {
        User = entity.User.DisplayName
        UserId = entity.UserId
        Text = entity.Text
        Created = entity.Created
        IsDmca = entity.IsDmca
    }

type ExploreCardSummary with
    static member load userId (entity: CardEntity) = {
        Id = entity.Id
        Author = entity.Author.DisplayName
        AuthorId = entity.AuthorId
        Users =
            let actual = entity.CardInstances.Select(fun x -> x.AcquiredCards.Count).Sum() // medTODO eventually remove
            if actual = entity.Users then
                actual
            else
                if actual = 0 then
                    entity.Users
                else
                    failwithf "Discrepancy between the triggered value (%i) and the actual value (%i) for CardId %i" entity.Users actual entity.Id
        LatestMeta = entity.CardInstances |> Seq.maxBy (fun x -> x.Modified |?? lazy x.Created) |> CardInstanceMeta.load userId
    }

type ExploreCard with
    static member load userId (entity: CardEntity) = {
        Summary = ExploreCardSummary.load userId entity
        Comments = entity.CommentCards |> Seq.map Comment.load |> List.ofSeq
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
                 |> List.ofSeq
        Relationships =
            let sources =
                entity.CardInstances.SelectMany(fun x -> x.RelationshipSources :> IEnumerable<_>).GroupBy(fun x -> x.Name, x.Source.CardId, x.Target.CardId).Select(fun r ->
                    let name = r.First().Name
                    let sourceId = r.First().SourceId
                    let targetId = r.First().TargetId
                    {   Name = name
                        CardId = targetId
                        IsAcquired = r.Any(fun x -> x.SourceId = sourceId && x.TargetId = targetId && x.UserId = userId && x.Name = name)
                        Users = r.Count(fun x -> x.SourceId = sourceId && x.TargetId = targetId && x.Name = name)
                    }) |> Seq.toList
            let targets =
                entity.CardInstances.SelectMany(fun x -> x.RelationshipTargets :> IEnumerable<_>).GroupBy(fun x -> x.Name, x.Source.CardId, x.Target.CardId).Select(fun r ->
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
            ) |> toResizeArray
    }

type CardRevision with
    static member load userId (e: CardEntity) = {
        Id = e.Id
        Author = e.Author.DisplayName
        AuthorId = e.AuthorId
        SortedMeta = e.CardInstances |> Seq.sortByDescending (fun x -> x.Modified |?? lazy x.Created) |> Seq.map (CardInstanceMeta.load userId) |> Seq.toList
    }

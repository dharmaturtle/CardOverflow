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
open System.Collections

module TemplateInstanceEntity =
    let byteArrayHash (hasher: SHA512) (e: TemplateInstanceEntity) =
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
    let hash h e = byteArrayHash h e |> BitArray
    let hashBase64 hasher entity = byteArrayHash hasher entity |> Convert.ToBase64String

module CardInstanceEntity =
    let bitArrayToByteArray (bitArray: BitArray) = // https://stackoverflow.com/a/45760138
        let bytes = Array.zeroCreate ((bitArray.Length - 1) / 8 + 1)
        bitArray.CopyTo(bytes, 0)
        bytes
    let hash (templateHash: BitArray) (hasher: SHA512) (e: CardInstanceEntity) =
        e.CommunalFieldInstance_CardInstances
            .Select(fun x -> x.CommunalFieldInstance.Value)
            .OrderBy(fun x -> x)
            .Append(e.FieldValues)
            .Append(e.AnkiNoteId.ToString())
            .Append(e.AnkiNoteOrd.ToString())
            .Append(e.TemplateInstance.AnkiId.ToString())
        |> Seq.toList
        |> List.map standardizeWhitespace
        |> MappingTools.joinByUnitSeparator
        |> Encoding.Unicode.GetBytes
        |> Array.append (bitArrayToByteArray templateHash)
        |> hasher.ComputeHash
        |> BitArray

type CardSetting with
    member this.AcquireEquality (that: CardSetting) =
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
        this.MatureCardsHardIntervalFactor = that.MatureCardsHardIntervalFactor &&
        this.MatureCardsBuryRelated = that.MatureCardsBuryRelated &&
        this.LapsedCardsSteps = that.LapsedCardsSteps &&
        this.LapsedCardsNewIntervalFactor = that.LapsedCardsNewIntervalFactor &&
        this.LapsedCardsMinimumInterval = that.LapsedCardsMinimumInterval &&
        this.LapsedCardsLeechThreshold = that.LapsedCardsLeechThreshold &&
        this.ShowAnswerTimer = that.ShowAnswerTimer &&
        this.AutomaticallyPlayAudio = that.AutomaticallyPlayAudio &&
        this.ReplayQuestionAudioOnAnswer = that.ReplayQuestionAudioOnAnswer
    static member load isDefault (entity: CardSettingEntity) =
        { Id = entity.Id
          Name = entity.Name
          IsDefault = isDefault
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
          MatureCardsHardIntervalFactor = float entity.MatureCardsHardIntervalFactorInPermille / 1000.
          MatureCardsBuryRelated = entity.MatureCardsBuryRelated
          LapsedCardsSteps = MappingTools.stringOfMinutesToTimeSpanList entity.LapsedCardsStepsInMinutes
          LapsedCardsNewIntervalFactor = float entity.LapsedCardsNewIntervalFactorInPermille / 1000.
          LapsedCardsMinimumInterval = entity.LapsedCardsMinimumIntervalInDays |> float |> TimeSpan.FromDays
          LapsedCardsLeechThreshold = entity.LapsedCardsLeechThreshold
          ShowAnswerTimer = entity.ShowAnswerTimer
          AutomaticallyPlayAudio = entity.AutomaticallyPlayAudio
          ReplayQuestionAudioOnAnswer = entity.ReplayQuestionAudioOnAnswer }
    member this.CopyTo(entity: CardSettingEntity) =
        entity.Name <- this.Name
        entity.NewCardsStepsInMinutes <- this.NewCardsSteps |> MappingTools.timeSpanListToStringOfMinutes
        entity.NewCardsMaxPerDay <- this.NewCardsMaxPerDay
        entity.NewCardsGraduatingIntervalInDays <- this.NewCardsGraduatingInterval.TotalDays |> Math.Round |> int16
        entity.NewCardsEasyIntervalInDays <- this.NewCardsEasyInterval.TotalDays |> Math.Round |> int16
        entity.NewCardsStartingEaseFactorInPermille <- this.NewCardsStartingEaseFactor * 1000. |> Math.Round |> int16
        entity.NewCardsBuryRelated <- this.NewCardsBuryRelated
        entity.MatureCardsMaxPerDay <- this.MatureCardsMaxPerDay
        entity.MatureCardsEaseFactorEasyBonusFactorInPermille <- this.MatureCardsEaseFactorEasyBonusFactor * 1000. |> Math.Round |> int16
        entity.MatureCardsIntervalFactorInPermille <- this.MatureCardsIntervalFactor * 1000. |> Math.Round |> int16
        entity.MatureCardsMaximumIntervalInDays <- TimeSpanInt16.totalDays this.MatureCardsMaximumInterval
        entity.MatureCardsHardIntervalFactorInPermille <- this.MatureCardsHardIntervalFactor * 1000. |> Math.Round |> int16
        entity.MatureCardsBuryRelated <- this.MatureCardsBuryRelated
        entity.LapsedCardsStepsInMinutes <- this.LapsedCardsSteps |> MappingTools.timeSpanListToStringOfMinutes
        entity.LapsedCardsNewIntervalFactorInPermille <- this.LapsedCardsNewIntervalFactor * 1000. |> Math.Round |> int16
        entity.LapsedCardsMinimumIntervalInDays <- this.LapsedCardsMinimumInterval.TotalDays |> Math.Round |> int16
        entity.LapsedCardsLeechThreshold <- this.LapsedCardsLeechThreshold
        entity.ShowAnswerTimer <- this.ShowAnswerTimer
        entity.AutomaticallyPlayAudio <- this.AutomaticallyPlayAudio
        entity.ReplayQuestionAudioOnAnswer <- this.ReplayQuestionAudioOnAnswer
    member this.CopyToNew userId =
        let entity = CardSettingEntity()
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
        fields |> Seq.sortBy (fun x -> x.Field.Ordinal) |> Seq.map (fun x -> x.Value) |> MappingTools.joinByUnitSeparator

type EditFieldAndValue with
    static member load (fields: Field list) fieldValues valuesByFieldName =
        FieldAndValue.load fields fieldValues
        |> Seq.map (fun { Field = field; Value = value } ->
            let value, communalValue =
                valuesByFieldName
                |> Map.tryFind field.Name
                |> Option.defaultValue (value, None)
            {   EditField = field
                Communal = communalValue
                Value = value }
        ) |> toResizeArray

type IdOrEntity<'a> =
    | Id of int
    | Entity of 'a

type TemplateInstance with
    static member load (entity: TemplateInstanceEntity) = {
        Id = entity.Id
        Name = entity.Name
        TemplateId = entity.TemplateId
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
        TemplateId = 0
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
    member this.CopyTo (entity: TemplateInstanceEntity) =
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
    member this.CopyToNewInstance template =
        let e = TemplateInstanceEntity()
        this.CopyTo e
        e.Created <- DateTime.UtcNow
        e.Modified <- Nullable()
        match template with
        | Id id -> e.TemplateId <- id
        | Entity entity -> e.Template <- entity
        e

type AcquiredTemplateInstance with
    static member load(entity: TemplateInstanceEntity) =
        { DefaultTags = entity.User_TemplateInstances.Single().Tag_User_TemplateInstances.Select(fun x -> x.DefaultTagId)
          DefaultCardSettingId = entity.User_TemplateInstances.Single().DefaultCardSettingId
          TemplateInstance = TemplateInstance.load entity }

type CardInstanceView with
    static member private toView (templateInstance: TemplateInstanceEntity) (fieldValues: string)=
        {   FieldValues = FieldAndValue.load (Fields.fromString templateInstance.Fields) fieldValues
            TemplateInstance = TemplateInstance.load templateInstance }
    static member load (entity: CardInstanceEntity) =
        CardInstanceView.toView
            entity.TemplateInstance
            entity.FieldValues
    member this.CopyToX (entity: CardInstanceEntity) (communalFields: CommunalFieldInstanceEntity seq) =
        entity.FieldValues <- FieldAndValue.join this.FieldValues
        entity.CommunalFieldInstance_CardInstances <-
            communalFields.Select(fun x -> CommunalFieldInstance_CardInstanceEntity(CommunalFieldInstance = x))
            |> entity.CommunalFieldInstance_CardInstances.Concat
            |> toResizeArray
        entity.TemplateInstanceId <- this.TemplateInstance.Id
    member this.CopyToNew communalFields =
        let entity = CardInstanceEntity()
        this.CopyToX entity communalFields
        entity
    member this.CopyFieldsToNewInstance card editSummary communalFields =
        let e = this.CopyToNew communalFields
        e.Created <- DateTime.UtcNow
        e.Modified <- Nullable()
        match card with
        | Id id -> e.CardId <- id
        | Entity entity -> e.Card <- entity ()
        e.EditSummary <- editSummary
        e

type CommunalFieldInstance with
    static member load (entity: CommunalFieldInstanceEntity) = {   
        Id = entity.Id
        FieldName = entity.FieldName
        Value = entity.Value }

type CardInstanceMeta with
    static member load isAcquired isLatest (entity: CardInstanceEntity) (usersTags: string Set) (tagCounts: CardTagCountEntity ResizeArray) (usersRelationships: string Set) (relationshipCounts: CardRelationshipCountEntity ResizeArray) =
        let front, back, _, _ = entity |> CardInstanceView.load |> fun x -> x.FrontBackFrontSynthBackSynth
        {   Id = entity.Id
            CardId = entity.CardId
            Created = entity.Created
            Modified = entity.Modified |> Option.ofNullable
            IsDmca = entity.IsDmca
            IsLatest = isLatest
            IsAcquired = isAcquired
            StrippedFront = MappingTools.stripHtmlTagsForDisplay front
            StrippedBack = MappingTools.stripHtmlTagsForDisplay back
            CommunalFields = entity.CommunalFieldInstance_CardInstances.Select(fun x -> CommunalFieldInstance.load x.CommunalFieldInstance).ToList()
            Relationships = relationshipCounts.Select(fun x ->
                {   Name = x.Name
                    SourceCardId = x.SourceCardId
                    TargetCardId = x.TargetCardId
                    IsAcquired = usersRelationships.Contains x.Name
                    Users = x.Count
                })  |> Seq.toList
            Tags = tagCounts.Select(fun x ->
                {   Name = x.Name
                    Count = x.Count
                    IsAcquired = usersTags.Contains x.Name
                }) |> Seq.toList
        }
    static member initialize =
        {   Id = 0
            CardId = 0
            Created = DateTime.UtcNow
            Modified = None
            IsDmca = false
            IsLatest = true
            IsAcquired = true
            StrippedFront = ""
            StrippedBack = ""
            CommunalFields = [].ToList()
            Relationships = []
            Tags = []
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
                Settings = CardSetting.load false entity.CardSetting } // lowTODO false exists to make the syntax work; it is semantically useless. Remove.
        }

type AcquiredCard with
    member this.copyTo (entity: AcquiredCardEntity) (tagIds: int seq) =
        entity.UserId <- this.UserId
        entity.CardState <- CardState.toDb this.CardState
        entity.IsLapsed <- this.IsLapsed
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalOrStepsIndex <- IntervalOrStepsIndex.intervalToDb this.IntervalOrStepsIndex
        entity.CardSettingId <- this.CardSettingId
        entity.Due <- this.Due
        entity.Tag_AcquiredCards <- tagIds.Select(fun x -> Tag_AcquiredCardEntity(TagId = x)).ToList()
    member this.copyToNew tagIds =
        let e = AcquiredCardEntity()
        this.copyTo e tagIds
        e
    static member initialize userId cardSettingId tags =
        {   CardId = 0
            AcquiredCardId = 0
            UserId = userId
            CardState = CardState.Normal
            IsLapsed = false
            EaseFactorInPermille = 0s
            IntervalOrStepsIndex = NewStepsIndex 0uy
            Due = DateTime.UtcNow
            CardSettingId = cardSettingId
            CardInstanceMeta = CardInstanceMeta.initialize
            Tags = tags
        }
    static member load (usersTags: string Set) (tagCounts: CardTagCountEntity ResizeArray) (usersRelationships: string Set) (relationshipCounts: CardRelationshipCountEntity ResizeArray) (entity: AcquiredCardIsLatestEntity) isAcquired = result {
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
                CardSettingId = entity.CardSettingId
                CardInstanceMeta = CardInstanceMeta.load isAcquired entity.IsLatest entity.CardInstance usersTags tagCounts usersRelationships relationshipCounts
                Tags = usersTags |> List.ofSeq
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
    static member load instance (entity: CardEntity) = {
        Id = entity.Id
        Author = entity.Author.DisplayName
        AuthorId = entity.AuthorId
        Users = entity.Users
        Instance = instance
    }

type ExploreCard with
    static member load (entity: CardEntity) acquiredStatus instance = {
        Summary = ExploreCardSummary.load instance entity
        Comments = entity.CommentCards |> Seq.map Comment.load |> List.ofSeq
        Tags = instance.Tags
        Relationships = instance.Relationships.ToList()
        AcquiredStatus = acquiredStatus
    }

type CardRevision with
    static member load isAcquired (e: CardEntity) = {
        Id = e.Id
        Author = e.Author.DisplayName
        AuthorId = e.AuthorId
        SortedMeta =
            e.CardInstances
            |> Seq.sortByDescending (fun x -> x.Modified |?? lazy x.Created)
            |> Seq.mapi (fun i e -> CardInstanceMeta.load isAcquired (i = 0) e Set.empty ResizeArray.empty Set.empty ResizeArray.empty)
            |> Seq.toList
    }

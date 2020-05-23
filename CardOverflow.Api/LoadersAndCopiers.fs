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

module CollateInstanceEntity =
    let byteArrayHash (hasher: SHA512) (e: CollateInstanceEntity) =
        [   e.Name
            e.Css
            e.LatexPre
            e.LatexPost
            e.Templates
            e.Fields
            e.Type |> string
        ]
        |> List.map standardizeWhitespace
        |> MappingTools.joinByUnitSeparator
        |> Encoding.Unicode.GetBytes
        |> hasher.ComputeHash
    let hash h e = byteArrayHash h e |> BitArray
    let hashBase64 hasher entity = byteArrayHash hasher entity |> Convert.ToBase64String

module BranchInstanceEntity =
    let bitArrayToByteArray (bitArray: BitArray) = // https://stackoverflow.com/a/45760138
        let bytes = Array.zeroCreate ((bitArray.Length - 1) / 8 + 1)
        bitArray.CopyTo(bytes, 0)
        bytes
    let hash (collateHash: BitArray) (hasher: SHA512) (e: BranchInstanceEntity) =
        e.CommunalFieldInstance_BranchInstances
            .Select(fun x -> x.CommunalFieldInstance.Value)
            .OrderBy(fun x -> x)
        |> Seq.toList
        |> List.append
            [   e.FieldValues
                e.AnkiNoteId.ToString()
                //e.MaxIndexInclusive |> string // Do not include! This is set from CardOverflowDbOverride, and AnkiImporter doesn't set it, leading to incorrect hashes at import-read-time. Anyway, this should be covered by collateHash and e.FieldValues
                e.CollateInstance.AnkiId.ToString()]
        |> List.map standardizeWhitespace
        |> MappingTools.joinByUnitSeparator
        |> Encoding.Unicode.GetBytes
        |> Array.append (bitArrayToByteArray collateHash)
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
    static member load (fields: Field list) fieldValues =
        fieldValues |> MappingTools.splitByUnitSeparator |> List.mapi (fun i x -> {
            Field = fields.[i]
            Value = x
        }) |> toResizeArray
    static member join (fields: FieldAndValue list) =
        fields |> List.map (fun x -> x.Value) |> MappingTools.joinByUnitSeparator

type EditFieldAndValue with
    static member load (fields: Field list) fieldValues =
        FieldAndValue.load fields fieldValues
        |> Seq.map (fun { Field = field; Value = value } ->
            {   EditField = field
                Value = value }
        ) |> toResizeArray

type IdOrEntity<'a> =
    | Id of int
    | Entity of 'a

type Template with
    static member load template =
        let x = template |> MappingTools.splitByUnitSeparator
        {   Name = x.[0]
            Front = x.[1]
            Back = x.[2]
            ShortFront = x.[3]
            ShortBack = x.[4]
        }
    static member copyTo (t: Template) =
        [t.Name; t.Front; t.Back; t.ShortFront; t.ShortBack] |> MappingTools.joinByUnitSeparator
    static member loadMany =
        MappingTools.splitByRecordSeparator
        >> List.map Template.load
    static member copyToMany =
        List.map Template.copyTo
        >> MappingTools.joinByRecordSeparator

type CollateInstance with
    static member load (entity: CollateInstanceEntity) =
        {   Id = entity.Id
            Name = entity.Name
            CollateId = entity.CollateId
            Css = entity.Css
            Fields = Fields.fromString entity.Fields
            Created = entity.Created
            Modified = entity.Modified |> Option.ofNullable
            LatexPre = entity.LatexPre
            LatexPost = entity.LatexPost
            Templates = entity.Type |> CollateType.fromDb (entity.Templates |> Template.loadMany)
            EditSummary = entity.EditSummary
        }
    static member initialize = {
        Id = 0
        Name = "New Template"
        CollateId = 0
        Css = """.card {
     font-family: arial;
     font-size: 20px;
     text-align: center;
}"""
        Fields = [
        {   Name = "Front"
            IsRightToLeft = false
            IsSticky = false }
        {   Name = "Back"
            IsRightToLeft = false
            IsSticky = false }
        {   Name = "Source"
            IsRightToLeft = false
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
        Templates = CollateType.initStandard
        EditSummary = "Initial creation" }
    member this.CopyTo (entity: CollateInstanceEntity) =
        entity.Name <- this.Name
        entity.Css <- this.Css
        entity.Fields <- Fields.toString this.Fields
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.LatexPre <- this.LatexPre
        entity.LatexPost <- this.LatexPost
        entity.Templates <- this.JustTemplates |> Template.copyToMany
        entity.EditSummary <- this.EditSummary
        entity.Type <-
            match this.Templates with
            | Standard -> 0s
            | Cloze -> 1s
    member this.CopyToNewInstance collate =
        let e = CollateInstanceEntity()
        this.CopyTo e
        e.Created <- DateTime.UtcNow
        e.Modified <- Nullable()
        match collate with
        | Id id -> e.CollateId <- id
        | Entity entity -> e.Collate <- entity
        e

type AcquiredCollateInstance with
    static member load(entity: CollateInstanceEntity) =
        { DefaultTags = entity.User_CollateInstances.Single().Tag_User_CollateInstances.Select(fun x -> x.DefaultTagId)
          DefaultCardSettingId = entity.User_CollateInstances.Single().DefaultCardSettingId
          CollateInstance = CollateInstance.load entity }

type BranchInstanceView with
    static member private toView (collateInstance: CollateInstanceEntity) (fieldValues: string)=
        {   FieldValues = FieldAndValue.load (Fields.fromString collateInstance.Fields) fieldValues
            CollateInstance = CollateInstance.load collateInstance }
    member this.MaxIndexInclusive =
        Helper.maxIndexInclusive
            (this.CollateInstance.Templates)
            (this.FieldValues.Select(fun x -> x.Field.Name, x.Value |?? lazy "") |> Map.ofSeq) // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls
    static member load (entity: BranchInstanceEntity) =
        BranchInstanceView.toView
            entity.CollateInstance
            entity.FieldValues
    member this.CopyToX (entity: BranchInstanceEntity) (communalFields: CommunalFieldInstanceEntity seq) =
        entity.FieldValues <- FieldAndValue.join (this.FieldValues |> List.ofSeq)
        entity.CommunalFieldInstance_BranchInstances <-
            communalFields.Select(fun x -> CommunalFieldInstance_BranchInstanceEntity(CommunalFieldInstance = x))
            |> entity.CommunalFieldInstance_BranchInstances.Concat
            |> toResizeArray
        entity.CollateInstanceId <- this.CollateInstance.Id
    member this.CopyToNew communalFields =
        let entity = BranchInstanceEntity()
        this.CopyToX entity communalFields
        entity
    member this.CopyFieldsToNewInstance (branch: BranchEntity) editSummary communalFields =
        let e = this.CopyToNew communalFields
        e.Created <- DateTime.UtcNow
        e.Modified <- Nullable()
        if branch.Stack = null then
            if branch.StackId = 0 then failwith "StackId is 0, you gotta .Include it"
            e.StackId <- branch.StackId
        else
            e.Stack <- branch.Stack
            e.StackId <- branch.Stack.Id
        e.Branch <- branch
        e.EditSummary <- editSummary
        e.MaxIndexInclusive <- this.MaxIndexInclusive
        e

type CommunalFieldInstance with
    static member load (entity: CommunalFieldInstanceEntity) = {   
        Id = entity.Id
        FieldName = entity.FieldName
        Value = entity.Value }

type BranchInstanceMeta with
    static member loadIndex (i: int16) isAcquired isLatest (entity: BranchInstanceEntity) =
        let front, back, _, _ = entity |> BranchInstanceView.load |> fun x -> x.FrontBackFrontSynthBackSynth.[int i]
        {   Id = entity.Id
            StackId = entity.StackId
            BranchId = entity.BranchId
            MaxIndexInclusive = entity.MaxIndexInclusive
            Created = entity.Created
            Modified = entity.Modified |> Option.ofNullable
            IsDmca = entity.IsDmca
            IsLatest = isLatest
            IsAcquired = isAcquired
            StrippedFront = MappingTools.stripHtmlTagsForDisplay front
            StrippedBack = MappingTools.stripHtmlTagsForDisplay back
            CommunalFields = entity.CommunalFieldInstance_BranchInstances.Select(fun x -> CommunalFieldInstance.load x.CommunalFieldInstance).ToList()
            Users = entity.Users
        }
    static member load = BranchInstanceMeta.loadIndex 0s
    static member loadAll isAcquired isLatest (entity: BranchInstanceEntity) =
        [0s .. entity.MaxIndexInclusive]
        |> List.map(fun i -> BranchInstanceMeta.loadIndex i isAcquired isLatest entity)
    static member initialize =
        {   Id = 0
            StackId = 0
            BranchId = 0
            MaxIndexInclusive = 0s
            Created = DateTime.UtcNow
            Modified = None
            IsDmca = false
            IsLatest = true
            IsAcquired = true
            StrippedFront = ""
            StrippedBack = ""
            CommunalFields = [].ToList()
            Users = 0
        }
    member this.copyTo (entity: BranchInstanceEntity) =
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.IsDmca <- this.IsDmca
    member this.copyToNew =
        let e = BranchInstanceEntity()
        this.copyTo e
        e

type QuizCard with
    static member load (entity: AcquiredCardEntity) =
        let front, back, frontSynthVoice, backSynthVoice =
            entity.BranchInstance |> BranchInstanceView.load |> fun x -> x.FrontBackFrontSynthBackSynth.[int entity.Index]
        result {
            let! cardState = CardState.create entity.CardState
            return {
                AcquiredCardId = entity.Id
                BranchInstanceId = entity.BranchInstanceId
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
    member this.copyTo (entity: AcquiredCardEntity) (tagIds: int seq) index =
        entity.UserId <- this.UserId
        entity.BranchId <- this.BranchId
        entity.StackId <- this.StackId
        entity.Index <- index
        entity.CardState <- CardState.toDb this.CardState
        entity.IsLapsed <- this.IsLapsed
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalOrStepsIndex <- IntervalOrStepsIndex.intervalToDb this.IntervalOrStepsIndex
        entity.CardSettingId <- this.CardSettingId
        entity.Due <- this.Due
        entity.Tag_AcquiredCards <- tagIds.Select(fun x -> Tag_AcquiredCardEntity(TagId = x)).ToList()
    member this.copyToNew tagIds i =
        let e = AcquiredCardEntity()
        this.copyTo e tagIds i
        e
    static member initialize userId cardSettingId tags =
        {   StackId = 0
            BranchId = 0
            AcquiredCardId = 0
            BranchInstanceMeta = BranchInstanceMeta.initialize
            Index = 0s
            UserId = userId
            CardState = CardState.Normal
            IsLapsed = false
            EaseFactorInPermille = 0s
            IntervalOrStepsIndex = NewStepsIndex 0uy
            Due = DateTime.UtcNow
            CardSettingId = cardSettingId
            Tags = tags
        }
    static member load (usersTags: string Set) (entity: AcquiredCardIsLatestEntity) isAcquired = result {
        let! cardState = entity.CardState |> CardState.create
        return
            {   StackId = entity.StackId
                BranchId = entity.BranchId
                AcquiredCardId = entity.Id
                BranchInstanceMeta = BranchInstanceMeta.loadIndex entity.Index isAcquired entity.IsLatest entity.BranchInstance
                Index = entity.Index
                UserId = entity.UserId
                CardState = cardState
                IsLapsed = entity.IsLapsed
                EaseFactorInPermille = entity.EaseFactorInPermille
                IntervalOrStepsIndex = entity.IntervalOrStepsIndex |> IntervalOrStepsIndex.intervalFromDb
                Due = entity.Due
                CardSettingId = entity.CardSettingId
                Tags = usersTags |> List.ofSeq
            }
        }

type Comment with
    static member load (entity: CommentStackEntity) = {
        User = entity.User.DisplayName
        UserId = entity.UserId
        Text = entity.Text
        Created = entity.Created
        IsDmca = entity.IsDmca
    }

type ExploreBranchSummary with
    static member load instance (entity: BranchEntity) = {
        Id = entity.Id
        Author = entity.Author.DisplayName
        AuthorId = entity.AuthorId
        Users = entity.Users
        Instance = instance
    }

type Branch with
    static member load (status: ExploreStackAcquiredStatus) (branch: BranchEntity) = {
        Name = branch.Name
        Summary =
            ExploreBranchSummary.load
                <| BranchInstanceMeta.load (branch.LatestInstanceId |> Some = status.BranchInstanceId) true branch.LatestInstance
                <| branch
    }

type ExploreStack with
    static member load (entity: StackEntity) acquiredStatus (usersTags: string Set) (tagCounts: StackTagCountEntity ResizeArray) (usersRelationships: string Set) (relationshipCounts: StackRelationshipCountEntity ResizeArray) instance = {
        Id = entity.Id
        Summary = ExploreBranchSummary.load instance <| entity.Branches.Single(fun x -> x.Id = entity.DefaultBranchId)
        Comments = entity.CommentStacks |> Seq.map Comment.load |> toResizeArray
        Tags =
            tagCounts.Select(fun x ->
                {   Name = x.Name
                    Count = x.Count
                    IsAcquired = usersTags.Contains x.Name
                }) |> toResizeArray
        Relationships =
            relationshipCounts.Select(fun x ->
                {   Name = x.Name
                    SourceStackId = x.SourceStackId
                    TargetStackId = x.TargetStackId
                    IsAcquired = usersRelationships.Contains x.Name
                    Users = x.Count
                })  |> toResizeArray
        Branches = entity.Branches |> Seq.map (Branch.load acquiredStatus) |> toResizeArray
        AcquiredStatus = acquiredStatus
    }

type BranchRevision with
    static member load isAcquired (e: BranchEntity) = {
        Id = e.Id
        Author = e.Author.DisplayName
        AuthorId = e.AuthorId
        SortedMeta =
            e.BranchInstances
            |> Seq.sortByDescending (fun x -> x.Modified |?? lazy x.Created)
            |> Seq.mapi (fun i e -> BranchInstanceMeta.load isAcquired (i = 0) e)
            |> Seq.toList
    }

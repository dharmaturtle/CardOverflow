module LoadersAndCopiers

open CardOverflow.Debug
open MappingTools
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Pure.Core
open System
open System.Linq
open FsToolkit.ErrorHandling
open System.Security.Cryptography
open System.Text
open System.Collections.Generic
open System.Text.RegularExpressions
open System.Collections
open NUlid

module Ulid =
    let create = Ulid.NewUlid().ToGuid()

module UpsertIds =
    let create = {
        StackId = Ulid.create
        BranchId = Ulid.create
        LeafId = Ulid.create
        CardIds = [ Ulid.create ]
    }

module GrompleafEntity =
    let byteArrayHash (hasher: SHA512) (e: GrompleafEntity) =
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

module Notification =
    let load ((n: NotificationEntity), senderName, (cc: CardEntity), deckName, (myDeck: DeckEntity)) =
        let theirDeck =
            lazy{ Id = n.DeckId.Value
                  Name = deckName }
        let myDeck =
            lazy (myDeck |> Option.ofObj |> Option.map(fun myDeck ->
                { Id = myDeck.Id
                  Name = myDeck.Name }))
        let stackLeafIds =
            lazy{ StackId = n.StackId.Value
                  BranchId = n.BranchId.Value
                  LeafId = n.LeafId.Value
                }
        let collected =
            lazy(cc |> Option.ofObj |> Option.map (fun card ->
                {   StackId = card.StackId
                    BranchId = card.BranchId
                    LeafId = card.LeafId
                }))
        let message =
            match n.Type with
            | NotificationType.DeckAddedStack ->
                {   DeckAddedStack.TheirDeck = theirDeck.Value
                    MyDeck = myDeck.Value
                    New = stackLeafIds.Value
                    Collected = collected.Value
                } |> DeckAddedStack
            | NotificationType.DeckUpdatedStack ->
                {   TheirDeck = theirDeck.Value
                    MyDeck = myDeck.Value
                    New = stackLeafIds.Value
                    Collected = collected.Value
                } |> DeckUpdatedStack
            | NotificationType.DeckDeletedStack ->
                {   TheirDeck = theirDeck.Value
                    MyDeck = myDeck.Value
                    Deleted = stackLeafIds.Value
                    Collected = collected.Value
                } |> DeckDeletedStack
            | x -> failwith <| sprintf "Invalid enum value: %A" x
        {   Id = n.Id
            SenderId = n.SenderId
            SenderDisplayName = senderName
            Created = n.Created
            Message = message
        }

module LeafEntity =
    let bitArrayToByteArray (bitArray: BitArray) = // https://stackoverflow.com/a/45760138
        let bytes = Array.zeroCreate ((bitArray.Length - 1) / 8 + 1)
        bitArray.CopyTo(bytes, 0)
        bytes
    let hash (gromplateHash: BitArray) (hasher: SHA512) (e: LeafEntity) =
        e.Commeaf_Leafs
            .Select(fun x -> x.Commeaf.Value)
            .OrderBy(fun x -> x)
        |> Seq.toList
        |> List.append
            [   e.FieldValues
                e.AnkiNoteId.ToString()
                //e.MaxIndexInclusive |> string // Do not include! This is set from CardOverflowDbOverride, and AnkiImporter doesn't set it, leading to incorrect hashes at import-read-time. Anyway, this should be covered by gromplateHash and e.FieldValues
                e.Grompleaf.AnkiId.ToString()]
        |> List.map standardizeWhitespace
        |> MappingTools.joinByUnitSeparator
        |> Encoding.Unicode.GetBytes
        |> Array.append (bitArrayToByteArray gromplateHash)
        |> hasher.ComputeHash
        |> BitArray

type CardSetting with
    member this.CollectedEquality (that: CardSetting) =
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
    | Id of Guid
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

type Grompleaf with
    static member load (entity: GrompleafEntity) =
        {   Id = entity.Id
            Name = entity.Name
            GromplateId = entity.GromplateId
            Css = entity.Css
            Fields = Fields.fromString entity.Fields
            Created = entity.Created
            Modified = entity.Modified |> Option.ofNullable
            LatexPre = entity.LatexPre
            LatexPost = entity.LatexPost
            Templates = entity.Type |> GromplateType.fromDb (entity.Templates |> Template.loadMany)
            EditSummary = entity.EditSummary
        }
    static member initialize = {
        Id = Guid.Empty
        Name = "New Template"
        GromplateId = Guid.Empty
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
        Templates = GromplateType.initStandard
        EditSummary = "Initial creation" }
    member this.CopyTo (entity: GrompleafEntity) =
        entity.Name <- this.Name
        entity.Css <- this.Css
        entity.Fields <- Fields.toString this.Fields
        entity.Modified <- this.Modified |> Option.toNullable
        entity.LatexPre <- this.LatexPre
        entity.LatexPost <- this.LatexPost
        entity.Templates <- this.JustTemplates |> Template.copyToMany
        entity.EditSummary <- this.EditSummary
        entity.Type <-
            match this.Templates with
            | Standard _ -> 0s
            | Cloze _ -> 1s
    member this.CopyToNewLeaf gromplate =
        let e = GrompleafEntity()
        this.CopyTo e
        match gromplate with
        | Id id -> e.GromplateId <- id
        | Entity entity -> e.Gromplate <- entity
        e

type CollectedGrompleaf with
    static member load(entity: GrompleafEntity) =
        { DefaultTags = entity.User_Grompleafs.Single().Tag_User_Grompleafs.Select(fun x -> x.DefaultTagId)
          DefaultCardSettingId = entity.User_Grompleafs.Single().DefaultCardSettingId
          Grompleaf = Grompleaf.load entity }

type LeafView with
    static member private toView (grompleaf: GrompleafEntity) (fieldValues: string)=
        {   FieldValues = FieldAndValue.load (Fields.fromString grompleaf.Fields) fieldValues
            Grompleaf = Grompleaf.load grompleaf }
    member this.MaxIndexInclusive =
        Helper.maxIndexInclusive
            (this.Grompleaf.Templates)
            (this.FieldValues.Select(fun x -> x.Field.Name, x.Value |?? lazy "") |> Map.ofSeq) // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls
    static member load (entity: LeafEntity) =
        LeafView.toView
            entity.Grompleaf
            entity.FieldValues
    member this.CopyToX (entity: LeafEntity) (commields: CommeafEntity seq) =
        entity.FieldValues <- FieldAndValue.join (this.FieldValues |> List.ofSeq)
        entity.Commeaf_Leafs <-
            commields.Select(fun x -> Commeaf_LeafEntity(Commeaf = x))
            |> entity.Commeaf_Leafs.Concat
            |> toResizeArray
        entity.GrompleafId <- this.Grompleaf.Id
    member this.CopyToNew commields =
        let entity = LeafEntity()
        this.CopyToX entity commields
        entity
    member this.CopyFieldsToNewLeaf (branch: BranchEntity) editSummary commields =
        let e = this.CopyToNew commields
        if branch.Stack = null then
            if branch.StackId = Guid.Empty then failwith "StackId is Guid.Empty, you gotta .Include it"
            e.StackId <- branch.StackId
        else
            e.Stack <- branch.Stack
            e.StackId <- branch.Stack.Id
        e.Branch <- branch
        e.EditSummary <- editSummary
        e.MaxIndexInclusive <- this.MaxIndexInclusive
        e

type Commeaf with
    static member load (entity: CommeafEntity) = {   
        Id = entity.Id
        FieldName = entity.FieldName
        Value = entity.Value }

type LeafMeta with
    static member loadIndex (i: int16) isCollected isLatest (entity: LeafEntity) =
        let front, back, _, _ = entity |> LeafView.load |> fun x -> x.FrontBackFrontSynthBackSynth.[int i]
        {   Id = entity.Id
            StackId = entity.StackId
            BranchId = entity.BranchId
            MaxIndexInclusive = entity.MaxIndexInclusive
            Created = entity.Created
            Modified = entity.Modified |> Option.ofNullable
            IsDmca = entity.IsDmca
            IsLatest = isLatest
            IsCollected = isCollected
            StrippedFront = MappingTools.stripHtmlTagsForDisplay front
            StrippedBack = MappingTools.stripHtmlTagsForDisplay back
            Commields = entity.Commeaf_Leafs.Select(fun x -> Commeaf.load x.Commeaf).ToList()
            Users = entity.Users
            EditSummary = entity.EditSummary
        }
    static member load = LeafMeta.loadIndex 0s
    static member loadAll isCollected isLatest (entity: LeafEntity) =
        [0s .. entity.MaxIndexInclusive]
        |> List.map(fun i -> LeafMeta.loadIndex i isCollected isLatest entity)
    static member initialize =
        {   Id = Guid.Empty
            StackId = Guid.Empty
            BranchId = Guid.Empty
            MaxIndexInclusive = 0s
            Created = DateTime.UtcNow
            Modified = None
            IsDmca = false
            IsLatest = true
            IsCollected = true
            StrippedFront = ""
            StrippedBack = ""
            Commields = [].ToList()
            Users = 0
            EditSummary = ""
        }
    member this.copyTo (entity: LeafEntity) =
        entity.Modified <- this.Modified |> Option.toNullable
        entity.IsDmca <- this.IsDmca
    member this.copyToNew =
        let e = LeafEntity()
        this.copyTo e
        e

type QuizCard with
    static member load (entity: CardEntity) =
        let front, back, frontSynthVoice, backSynthVoice =
            entity.Leaf |> LeafView.load |> fun x -> x.FrontBackFrontSynthBackSynth.[int entity.Index]
        result {
            let! cardState = CardState.create entity.CardState
            return {
                CardId = entity.Id
                LeafId = entity.LeafId
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

type Card with
    member this.copyTo (entity: CardEntity) (tagIds: Guid seq) index =
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
        entity.Tag_Cards <- tagIds.Select(fun x -> Tag_CardEntity(TagId = x)).ToList()
        entity.DeckId <- this.DeckId
    member this.copyToNew tagIds i =
        let e = CardEntity()
        this.copyTo e tagIds i
        e
    static member initialize userId cardSettingId deckId tags =
        {   StackId = Guid.Empty
            BranchId = Guid.Empty
            CardId = Guid.Empty
            LeafMeta = LeafMeta.initialize
            Index = 0s
            UserId = userId
            CardState = CardState.Normal
            IsLapsed = false
            EaseFactorInPermille = 0s
            IntervalOrStepsIndex = NewStepsIndex 0uy
            Due = DateTime.UtcNow
            CardSettingId = cardSettingId
            Tags = tags
            DeckId = deckId
        }
    static member load (usersTags: string Set) (entity: CardIsLatestEntity) isCollected = result {
        let! cardState = entity.CardState |> CardState.create
        return
            {   StackId = entity.StackId
                BranchId = entity.BranchId
                CardId = entity.Id
                LeafMeta = LeafMeta.loadIndex entity.Index isCollected entity.IsLatest entity.Leaf
                Index = entity.Index
                UserId = entity.UserId
                CardState = cardState
                IsLapsed = entity.IsLapsed
                EaseFactorInPermille = entity.EaseFactorInPermille
                IntervalOrStepsIndex = entity.IntervalOrStepsIndex |> IntervalOrStepsIndex.intervalFromDb
                Due = entity.Due
                CardSettingId = entity.CardSettingId
                Tags = usersTags |> List.ofSeq
                DeckId = entity.DeckId
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
    static member load leaf (entity: BranchEntity) = {
        Id = entity.Id
        Author = entity.Author.DisplayName
        AuthorId = entity.AuthorId
        Users = entity.Users
        Leaf = leaf
    }

type Branch with
    static member load (ids: CollectedIds) (branch: BranchEntity) = {
        Name = branch.Name
        Summary =
            ExploreBranchSummary.load
                <| LeafMeta.load (branch.LatestId = CollectedIds.leafId ids) true branch.Latest
                <| branch
    }

type ExploreStack with
    static member load (entity: StackEntity) collectedIds (usersTags: string Set) (tagCounts: StackTagCountEntity ResizeArray) (usersRelationships: string Set) (relationshipCounts: StackRelationshipCountEntity ResizeArray) = {
        Id = entity.Id
        Users = entity.Users
        Comments = entity.CommentStacks |> Seq.map Comment.load |> toResizeArray
        Tags =
            tagCounts.Select(fun x ->
                {   Name = x.Name
                    Count = x.Count
                    IsCollected = usersTags.Contains x.Name
                }) |> toResizeArray
        Relationships =
            relationshipCounts.Select(fun x ->
                {   Name = x.Name
                    SourceStackId = x.SourceStackId
                    TargetStackId = x.TargetStackId
                    IsCollected = usersRelationships.Contains x.Name
                    Users = x.Count
                })  |> toResizeArray
        Branches = entity.Branches |> Seq.map (Branch.load collectedIds) |> toResizeArray
        CollectedIds = collectedIds
    }

type BranchRevision with
    static member load leafId (e: BranchEntity) = {
        Id = e.Id
        Author = e.Author.DisplayName
        AuthorId = e.AuthorId
        Name = e.Name
        SortedMeta =
            e.Leafs
            |> Seq.sortByDescending (fun x -> x.Modified |?? lazy x.Created)
            |> Seq.mapi (fun i e -> LeafMeta.load (e.Id = leafId) (i = 0) e)
            |> Seq.toList
    }

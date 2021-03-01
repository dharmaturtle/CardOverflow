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
open NodaTime
open CardOverflow.Api

module TemplateRevisionEntity =
    let byteArrayHash (hasher: SHA512) (e: TemplateRevisionEntity) =
        [   e.Name
            e.Css
            e.LatexPre
            e.LatexPost
            e.CardTemplates
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
    let load ((n: NotificationEntity), senderName, (cc: CardEntity ResizeArray), deckName, (myDeck: DeckEntity), (newCardCount: int16)) =
        let theirDeck =
            lazy{ Id = n.DeckId.Value
                  Name = deckName }
        let myDeck =
            lazy (myDeck |> Option.ofObj |> Option.map(fun myDeck ->
                { Id = myDeck.Id
                  Name = myDeck.Name }))
        let conceptRevisionIds =
            lazy{ ConceptId = n.ConceptId.Value
                  ExampleId = n.ExampleId.Value
                  RevisionId = n.RevisionId.Value
                }
        let cardCount = newCardCount |> int |> (+) 1
        let collected =
            lazy(cc |> List.ofSeq |> function
                | [] -> None
                | card ->
                    {   ConceptId = card.First().ConceptId
                        ExampleId = card.First().ExampleId
                        RevisionId = card.First().RevisionId
                        CardIds = card.Select(fun x -> x.Id) |> Seq.toList
                    } |> Some
                )
        let message =
            match n.Type with
            | NotificationType.DeckAddedConcept ->
                {   DeckAddedConcept.TheirDeck = theirDeck.Value
                    MyDeck = myDeck.Value
                    New = conceptRevisionIds.Value
                    NewCardCount = cardCount
                    Collected = collected.Value
                } |> DeckAddedConcept
            | NotificationType.DeckUpdatedConcept ->
                {   TheirDeck = theirDeck.Value
                    MyDeck = myDeck.Value
                    New = conceptRevisionIds.Value
                    NewCardCount = cardCount
                    Collected = collected.Value
                } |> DeckUpdatedConcept
            | NotificationType.DeckDeletedConcept ->
                {   TheirDeck = theirDeck.Value
                    MyDeck = myDeck.Value
                    Deleted = conceptRevisionIds.Value
                    DeletedCardCount = cardCount
                    Collected = collected.Value
                } |> DeckDeletedConcept
            | x -> failwith <| sprintf "Invalid enum value: %A" x
        {   Id = n.Id
            SenderId = n.SenderId
            SenderDisplayName = senderName
            Created = n.Created
            Message = message
        }

module RevisionEntity =
    let bitArrayToByteArray (bitArray: BitArray) = // https://stackoverflow.com/a/45760138
        let bytes = Array.zeroCreate ((bitArray.Length - 1) / 8 + 1)
        bitArray.CopyTo(bytes, 0)
        bytes
    let hash (templateHash: BitArray) (hasher: SHA512) (e: RevisionEntity) =
        e.Commeaf_Revisions
            .Select(fun x -> x.Commeaf.Value)
            .OrderBy(fun x -> x)
        |> Seq.toList
        |> List.append
            [   e.FieldValues
                e.AnkiNoteId.ToString()
                //e.MaxIndexInclusive |> string // Do not include! This is set from CardOverflowDbOverride, and AnkiImporter doesn't set it, leading to incorrect hashes at import-read-time. Anyway, this should be covered by templateHash and e.FieldValues
                e.TemplateRevision.AnkiId.ToString()]
        |> List.map standardizeWhitespace
        |> MappingTools.joinByUnitSeparator
        |> Encoding.Unicode.GetBytes
        |> Array.append (bitArrayToByteArray templateHash)
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
          NewCardsSteps = entity.NewCardsSteps |> List.ofArray |> List.map Period.toDuration
          NewCardsMaxPerDay = entity.NewCardsMaxPerDay
          NewCardsGraduatingInterval = entity.NewCardsGraduatingInterval.ToDuration()
          NewCardsEasyInterval = entity.NewCardsEasyInterval.ToDuration()
          NewCardsStartingEaseFactor = float entity.NewCardsStartingEaseFactorInPermille / 1000.
          NewCardsBuryRelated = entity.NewCardsBuryRelated
          MatureCardsMaxPerDay = entity.MatureCardsMaxPerDay
          MatureCardsEaseFactorEasyBonusFactor = float entity.MatureCardsEaseFactorEasyBonusFactorInPermille / 1000.
          MatureCardsIntervalFactor = float entity.MatureCardsIntervalFactorInPermille / 1000.
          MatureCardsMaximumInterval = entity.MatureCardsMaximumInterval.ToDuration()
          MatureCardsHardIntervalFactor = float entity.MatureCardsHardIntervalFactorInPermille / 1000.
          MatureCardsBuryRelated = entity.MatureCardsBuryRelated
          LapsedCardsSteps = entity.LapsedCardsSteps |> List.ofArray |> List.map Period.toDuration
          LapsedCardsNewIntervalFactor = float entity.LapsedCardsNewIntervalFactorInPermille / 1000.
          LapsedCardsMinimumInterval = entity.LapsedCardsMinimumInterval.ToDuration()
          LapsedCardsLeechThreshold = entity.LapsedCardsLeechThreshold
          ShowAnswerTimer = entity.ShowAnswerTimer
          AutomaticallyPlayAudio = entity.AutomaticallyPlayAudio
          ReplayQuestionAudioOnAnswer = entity.ReplayQuestionAudioOnAnswer }
    member this.CopyTo(entity: CardSettingEntity) =
        entity.Name <- this.Name
        entity.NewCardsSteps <- this.NewCardsSteps |> List.map Duration.toPeriod |> Array.ofList
        entity.NewCardsMaxPerDay <- this.NewCardsMaxPerDay
        entity.NewCardsGraduatingInterval <- this.NewCardsGraduatingInterval |> Duration.toPeriod
        entity.NewCardsEasyInterval <- this.NewCardsEasyInterval |> Duration.toPeriod
        entity.NewCardsStartingEaseFactorInPermille <- this.NewCardsStartingEaseFactor * 1000. |> Math.Round |> int16
        entity.NewCardsBuryRelated <- this.NewCardsBuryRelated
        entity.MatureCardsMaxPerDay <- this.MatureCardsMaxPerDay
        entity.MatureCardsEaseFactorEasyBonusFactorInPermille <- this.MatureCardsEaseFactorEasyBonusFactor * 1000. |> Math.Round |> int16
        entity.MatureCardsIntervalFactorInPermille <- this.MatureCardsIntervalFactor * 1000. |> Math.Round |> int16
        entity.MatureCardsMaximumInterval <- this.MatureCardsMaximumInterval |> Duration.toPeriod
        entity.MatureCardsHardIntervalFactorInPermille <- this.MatureCardsHardIntervalFactor * 1000. |> Math.Round |> int16
        entity.MatureCardsBuryRelated <- this.MatureCardsBuryRelated
        entity.LapsedCardsSteps <- this.LapsedCardsSteps |> List.map Duration.toPeriod |> Array.ofList
        entity.LapsedCardsNewIntervalFactorInPermille <- this.LapsedCardsNewIntervalFactor * 1000. |> Math.Round |> int16
        entity.LapsedCardsMinimumInterval <- this.LapsedCardsMinimumInterval |> Duration.toPeriod
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

type CardTemplate with
    static member load cardTemplate =
        let x = cardTemplate |> MappingTools.splitByUnitSeparator
        {   Name = x.[0]
            Front = x.[1]
            Back = x.[2]
            ShortFront = x.[3]
            ShortBack = x.[4]
        }
    static member copyTo (t: CardTemplate) =
        [t.Name; t.Front; t.Back; t.ShortFront; t.ShortBack] |> MappingTools.joinByUnitSeparator
    static member loadMany =
        MappingTools.splitByRecordSeparator
        >> List.map CardTemplate.load
    static member copyToMany =
        List.map CardTemplate.copyTo
        >> MappingTools.joinByRecordSeparator

type TemplateRevision with
    static member load (entity: TemplateRevisionEntity) =
        {   Id = entity.Id
            Name = entity.Name
            TemplateId = entity.TemplateId
            Css = entity.Css
            Fields = Fields.fromString entity.Fields
            Created = entity.Created
            Modified = entity.Modified |> Option.ofNullable
            LatexPre = entity.LatexPre
            LatexPost = entity.LatexPost
            CardTemplates = entity.Type |> TemplateType.fromDb (entity.CardTemplates |> CardTemplate.loadMany)
            EditSummary = entity.EditSummary
        }
    static member initialize templateRevisionId templateId = {
        Id = templateRevisionId
        Name = "New Card Template"
        TemplateId = templateId
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
        Created = DateTimeX.UtcNow
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
        CardTemplates = TemplateType.initStandard
        EditSummary = "Initial creation" }
    member this.CopyTo (entity: TemplateRevisionEntity) =
        entity.Name <- this.Name
        entity.Css <- this.Css
        entity.Fields <- Fields.toString this.Fields
        entity.Modified <- this.Modified |> Option.toNullable
        entity.LatexPre <- this.LatexPre
        entity.LatexPost <- this.LatexPost
        entity.CardTemplates <- this.JustCardTemplates |> CardTemplate.copyToMany
        entity.EditSummary <- this.EditSummary
        entity.Type <-
            match this.CardTemplates with
            | Standard _ -> 0s
            | Cloze _ -> 1s
    member this.CopyToNewRevision =
        let e = TemplateRevisionEntity()
        this.CopyTo e
        e.Id <- this.Id
        e.TemplateId <- this.TemplateId
        e

type CollectedTemplateRevision with
    static member load(entity: TemplateRevisionEntity) =
        { DefaultTags = entity.User_TemplateRevisions.Single().DefaultTags
          DefaultCardSettingId = entity.User_TemplateRevisions.Single().DefaultCardSettingId
          TemplateRevision = TemplateRevision.load entity }

type RevisionView with
    static member private toView (templateRevision: TemplateRevisionEntity) (fieldValues: string)=
        {   FieldValues = FieldAndValue.load (Fields.fromString templateRevision.Fields) fieldValues
            TemplateRevision = TemplateRevision.load templateRevision }
    member this.MaxIndexInclusive =
        Helper.maxIndexInclusive
            (this.TemplateRevision.CardTemplates)
            (this.FieldValues.Select(fun x -> x.Field.Name, x.Value |?? lazy "") |> Map.ofSeq) // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls
    static member load (entity: RevisionEntity) =
        RevisionView.toView
            entity.TemplateRevision
            entity.FieldValues
    member this.CopyToX (entity: RevisionEntity) (commields: CommeafEntity seq) =
        entity.FieldValues <- FieldAndValue.join (this.FieldValues |> List.ofSeq)
        entity.Commeaf_Revisions <-
            commields.Select(fun x -> Commeaf_RevisionEntity(Commeaf = x))
            |> entity.Commeaf_Revisions.Concat
            |> toResizeArray
        entity.TemplateRevisionId <- this.TemplateRevision.Id
    member this.CopyToNew commields =
        let entity = RevisionEntity()
        this.CopyToX entity commields
        entity
    member this.CopyFieldsToNewRevision (example: ExampleEntity) editSummary commields revisionId =
        let e = this.CopyToNew commields
        if example.Concept = null then
            if example.ConceptId = Guid.Empty then failwith "ConceptId is Guid.Empty, you gotta .Include it"
            e.ConceptId <- example.ConceptId
        else
            e.Concept <- example.Concept
            e.ConceptId <- example.Concept.Id
        e.Id <- revisionId
        e.Example <- example
        e.ExampleId <- example.Id
        e.EditSummary <- editSummary
        e.MaxIndexInclusive <- this.MaxIndexInclusive
        e

type Commeaf with
    static member load (entity: CommeafEntity) = {   
        Id = entity.Id
        FieldName = entity.FieldName
        Value = entity.Value }

type RevisionMeta with
    static member loadIndex (i: int16) isCollected isLatest (entity: RevisionEntity) =
        let front, back, _, _ = entity |> RevisionView.load |> fun x -> x.FrontBackFrontSynthBackSynth.[int i]
        {   Id = entity.Id
            ConceptId = entity.ConceptId
            ExampleId = entity.ExampleId
            MaxIndexInclusive = entity.MaxIndexInclusive
            Created = entity.Created
            Modified = entity.Modified |> Option.ofNullable
            IsDmca = entity.IsDmca
            IsLatest = isLatest
            IsCollected = isCollected
            StrippedFront = MappingTools.stripHtmlTagsForDisplay front
            StrippedBack = MappingTools.stripHtmlTagsForDisplay back
            Commields = entity.Commeaf_Revisions.Select(fun x -> Commeaf.load x.Commeaf).ToList()
            Users = entity.Users
            EditSummary = entity.EditSummary
        }
    static member load = RevisionMeta.loadIndex 0s
    static member loadAll isCollected isLatest (entity: RevisionEntity) =
        [0s .. entity.MaxIndexInclusive]
        |> List.map(fun i -> RevisionMeta.loadIndex i isCollected isLatest entity)
    static member initialize =
        {   Id = Guid.Empty
            ConceptId = Guid.Empty
            ExampleId = Guid.Empty
            MaxIndexInclusive = 0s
            Created = DateTimeX.UtcNow
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
    member this.copyTo (entity: RevisionEntity) =
        entity.Modified <- this.Modified |> Option.toNullable
        entity.IsDmca <- this.IsDmca
    member this.copyToNew =
        let e = RevisionEntity()
        this.copyTo e
        e

type QuizCard with
    static member load (entity: CardEntity) =
        let front, back, frontSynthVoice, backSynthVoice =
            entity.Revision |> RevisionView.load |> fun x -> x.FrontBackFrontSynthBackSynth.[int entity.Index]
        result {
            let! cardState = CardState.create entity.CardState
            return {
                CardId = entity.Id
                RevisionId = entity.RevisionId
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
    member this.copyTo (entity: CardEntity) tags index =
        entity.UserId <- this.UserId
        entity.ExampleId <- this.ExampleId
        entity.ConceptId <- this.ConceptId
        entity.Index <- index
        entity.CardState <- CardState.toDb this.CardState
        entity.IsLapsed <- this.IsLapsed
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalOrStepsIndex <- IntervalOrStepsIndex.intervalToDb this.IntervalOrStepsIndex
        entity.CardSettingId <- this.CardSettingId
        entity.Due <- this.Due
        entity.Tags <- tags
        entity.DeckId <- this.DeckId
    member this.copyToNew tagIds i =
        let e = CardEntity()
        e.Id <- this.CardId
        this.copyTo e tagIds i
        e
    static member initialize cardId userId cardSettingId deckId tags =
        {   ConceptId = Guid.Empty
            ExampleId = Guid.Empty
            CardId = cardId
            RevisionMeta = RevisionMeta.initialize
            Index = 0s
            UserId = userId
            CardState = CardState.Normal
            IsLapsed = false
            EaseFactorInPermille = 0s
            IntervalOrStepsIndex = NewStepsIndex 0uy
            Due = DateTimeX.UtcNow
            CardSettingId = cardSettingId
            Tags = tags
            DeckId = deckId
        }
    static member load (usersTags: string Set) (entity: CardIsLatestEntity) isCollected = result {
        let! cardState = entity.CardState |> CardState.create
        return
            {   ConceptId = entity.ConceptId
                ExampleId = entity.ExampleId
                CardId = entity.Id
                RevisionMeta = RevisionMeta.loadIndex entity.Index isCollected entity.IsLatest entity.Revision
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
    static member load (entity: CommentConceptEntity) = {
        User = entity.User.DisplayName
        UserId = entity.UserId
        Text = entity.Text
        Created = entity.Created
        IsDmca = entity.IsDmca
    }

type ExploreExampleSummary with
    static member load revision (entity: ExampleEntity) = {
        Id = entity.Id
        Author = entity.Author.DisplayName
        AuthorId = entity.AuthorId
        Users = entity.Users
        Revision = revision
    }

type Example with
    static member load (ids: CollectedIds) (example: ExampleEntity) = {
        Name = example.Name
        Summary =
            ExploreExampleSummary.load
                <| RevisionMeta.load (example.LatestId = CollectedIds.revisionId ids) true example.Latest
                <| example
    }

type ExploreConcept with
    static member load (entity: ConceptEntity) collectedIds tags (usersRelationships: string Set) (relationshipCounts: ConceptRelationshipCountEntity ResizeArray) = {
        Id = entity.Id
        Users = entity.Users
        Comments = entity.CommentConcepts |> Seq.map Comment.load |> toResizeArray
        Tags = tags
        Relationships =
            relationshipCounts.Select(fun x ->
                {   Name = x.Name
                    SourceConceptId = x.SourceConceptId
                    TargetConceptId = x.TargetConceptId
                    IsCollected = usersRelationships.Contains x.Name
                    Users = x.Count
                })  |> toResizeArray
        Examples = entity.Examples |> Seq.map (Example.load collectedIds) |> toResizeArray
        CollectedIds = collectedIds
    }

type ExampleRevision with
    static member load revisionId (e: ExampleEntity) = {
        Id = e.Id
        Author = e.Author.DisplayName
        AuthorId = e.AuthorId
        Name = e.Name
        SortedMeta =
            e.Revisions
            |> Seq.sortByDescending (fun x -> x.Modified |?? lazy x.Created)
            |> Seq.mapi (fun i e -> RevisionMeta.load (e.Id = revisionId) (i = 0) e)
            |> Seq.toList
    }

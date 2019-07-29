namespace CardOverflow.Api

open FSharp.Text.RegexProvider
open CardOverflow.Entity.Anki
open CardOverflow.Entity
open CardOverflow.Pure
open System
open System.Linq
open LoadersAndCopiers
open Thoth.Json.Net
open Microsoft.FSharp.Core.Operators.Checked
open System.Collections.Generic
open CardOverflow.Debug
open MappingTools
open FsToolkit.ErrorHandling
open System.Security.Cryptography

type SimpleAnkiDb = {
    Cards: CardOverflow.Entity.Anki.CardEntity list
    Cols: ColEntity list
    Notes: NoteEntity list
    Revlogs: RevlogEntity list
}

type AnkiConceptTemplate = {
    ConceptTemplate: ConceptTemplateInstance
    DeckId: int
}

type AnkiConceptWrite = {
    ConceptTemplateHash: byte[]
    FieldValues: FieldValueEntity seq
    Created: DateTime
    Modified: DateTime option
    MaintainerId: int
    IsPublic: bool
} with
    member this.CopyTo(entity: ConceptInstanceEntity, conceptTemplateHash: byte[]) =
        entity.FieldValues <- this.FieldValues.ToList()
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.IsPublic <- this.IsPublic
        use hasher = SHA256.Create()
        entity.AcquireHash <- ConceptInstanceEntity.acquireHash entity conceptTemplateHash hasher
    member this.CopyToNew (files: FileEntity seq) =
        let entity = ConceptInstanceEntity()
        entity.Concept <-
            ConceptEntity(
                MaintainerId = this.MaintainerId,
                Name = "Imported from Anki"
            )
        entity.FileConceptInstances <-
            files.Select(fun x ->
                FileConceptInstanceEntity(
                    ConceptInstance = entity,
                    File = x
                )
            ).ToList()
        this.CopyTo(entity, this.ConceptTemplateHash)
        entity
    member this.AcquireEquality (db: CardOverflowDb) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        db.ConceptInstances
            .FirstOrDefault(fun c ->
                c.FieldValues.Select(fun x -> x.Value).All(fun x -> this.FieldValues.Select(fun x -> x.Value).Contains(x)) &&
                //this.ConceptTemplate.Id = c.FieldValues.First().Field.ConceptTemplateInstanceId && // medtodo fix this equality check, use hashes
                this.MaintainerId = c.Concept.MaintainerId &&
                this.IsPublic = c.IsPublic
            )

type AnkiAcquiredCard = {
    UserId: int
    ConceptInstance: ConceptInstanceEntity
    CardTemplate: CardTemplateEntity
    MemorizationState: MemorizationState
    CardState: CardState
    LapseCount: byte
    EaseFactorInPermille: int16
    IntervalNegativeIsMinutesPositiveIsDays: int16
    StepsIndex: byte option
    Due: DateTime
    CardOption: CardOptionEntity
} with
    member this.CopyTo (entity: AcquiredCardEntity) =
        entity.UserId <- this.UserId
        entity.MemorizationState <- MemorizationState.toDb this.MemorizationState
        entity.CardState <- CardState.toDb this.CardState
        entity.LapseCount <- this.LapseCount
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalNegativeIsMinutesPositiveIsDays <- this.IntervalNegativeIsMinutesPositiveIsDays
        entity.StepsIndex <- Option.toNullable this.StepsIndex
        entity.Due <- this.Due
    member this.CopyToNew (privateTags: PrivateTagEntity seq) =
        let entity = AcquiredCardEntity ()
        this.CopyTo entity
        entity.Card <- CardEntity (
            ConceptInstance = this.ConceptInstance,
            CardTemplate = this.CardTemplate
        )
        entity.CardOption <- this.CardOption
        entity.PrivateTagAcquiredCards <- privateTags.Select(fun x -> PrivateTagAcquiredCardEntity(AcquiredCard = entity, PrivateTag = x)).ToList()
        entity
    member this.AcquireEquality (db: CardOverflowDb) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        db.AcquiredCards.FirstOrDefault(fun c -> 
            this.UserId = c.UserId &&
            this.ConceptInstance.Id = c.ConceptInstanceId &&
            this.CardTemplate.Id = c.CardTemplateId
        )

type AnkiHistory = {
    AcquiredCard: AcquiredCardEntity
    Score: byte
    MemorizationState: byte
    Timestamp: DateTime
    IntervalNegativeIsMinutesPositiveIsDays: int16
    EaseFactorInPermille: int16
    TimeFromSeeingQuestionToScoreInSecondsMinus32768: int16
} with
    member this.AcquireEquality (db: CardOverflowDb) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        let roundedTimeStamp = MappingTools.round this.Timestamp <| TimeSpan.FromMinutes(1.)
        db.Histories.FirstOrDefault(fun h -> 
            this.AcquiredCard.UserId = h.UserId &&
            this.AcquiredCard.ConceptInstanceId = h.ConceptInstanceId &&
            this.AcquiredCard.CardTemplateId = h.CardTemplateId &&
            this.Score = h.Score &&
            this.MemorizationState = h.MemorizationState &&
            roundedTimeStamp = h.Timestamp &&
            this.IntervalNegativeIsMinutesPositiveIsDays = h.IntervalNegativeIsMinutesPositiveIsDays &&
            this.EaseFactorInPermille = h.EaseFactorInPermille &&
            this.TimeFromSeeingQuestionToScoreInSecondsMinus32768 = h.TimeFromSeeingQuestionToScoreInSecondsMinus32768
        )
    member this.CopyTo (entity: HistoryEntity) =
        entity.AcquiredCard <- this.AcquiredCard
        entity.Score <- this.Score
        entity.MemorizationState <- this.MemorizationState
        entity.Timestamp <- this.Timestamp
        entity.IntervalNegativeIsMinutesPositiveIsDays <- this.IntervalNegativeIsMinutesPositiveIsDays
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.TimeFromSeeingQuestionToScoreInSecondsMinus32768 <- this.TimeFromSeeingQuestionToScoreInSecondsMinus32768
    member this.CopyToNew =
        let history = HistoryEntity()
        this.CopyTo history
        history

module Anki =
    let toHistory (cardByAnkiId: Map<int64, AcquiredCardEntity>) getHistory (revLog: RevlogEntity) =
        result {
            let! score =
                match revLog.Ease with
                | 1L -> Ok Again
                | 2L -> Ok Hard
                | 3L -> Ok Good
                | 4L -> Ok Easy
                | _ -> Error <| sprintf "Unrecognized Anki revlog ease: %i" revLog.Ease
            let! memorizationState =
                match revLog.Type with
                | 0L -> Ok New
                | 1L -> Ok Learning
                | 2L -> Ok Mature
                | 3L -> Ok Mature
                | _ -> Error <| sprintf "Unrecognized Anki revlog type: %i" revLog.Type
            let history = {
                AcquiredCard = cardByAnkiId.[revLog.Cid]
                EaseFactorInPermille = int16 revLog.Factor
                IntervalNegativeIsMinutesPositiveIsDays =
                    match revLog.Ivl with
                    | p when p > 0L -> int16 p // positive is days
                    | _ -> revLog.Ivl / 60L |> int16 // In Anki, negative is seconds, and we want minutes
                Score = score |> Score.toDb
                MemorizationState =memorizationState |> MemorizationState.toDb
                TimeFromSeeingQuestionToScoreInSecondsMinus32768 =
                    revLog.Time / 1000L - 32768L |> int16
                Timestamp =
                    DateTimeOffset.FromUnixTimeMilliseconds(revLog.Id).UtcDateTime
            }
            return
                getHistory history
                |> function
                | Some x -> x
                | None -> history.CopyToNew
        }

    let ankiIntToBool =
        Decode.int
        |> Decode.andThen (fun i ->
            match i with
            | 0 -> Decode.succeed false
            | 1 -> Decode.succeed true
            | _ -> "Unexpected number when parsing Anki value: " + string i |> Decode.fail )
    let parseCardOptions =
        Decode.object(fun get ->
            { Id = 0 // medTODO this entire record needs to be validated for out of range values
              Name = get.Required.Field "name" Decode.string
              NewCardsSteps = get.Required.At ["new"; "delays"] (Decode.array Decode.float) |> Array.map TimeSpan.FromMinutes |> List.ofArray
              NewCardsMaxPerDay = get.Required.At ["new"; "perDay"] Decode.int |> int16
              NewCardsGraduatingInterval = get.Required.At ["new"; "ints"] (Decode.array Decode.float) |> Array.map TimeSpan.FromDays |> Seq.item 0
              NewCardsEasyInterval = get.Required.At ["new"; "ints"] (Decode.array Decode.float) |> Array.map TimeSpan.FromDays |> Seq.item 1
              NewCardsStartingEaseFactor = (get.Required.At ["new"; "initialFactor"] Decode.float) / 1000.
              NewCardsBuryRelated = get.Required.At ["new"; "bury"] Decode.bool
              MatureCardsMaxPerDay = get.Required.At ["rev"; "perDay"] Decode.int |> int16
              MatureCardsEaseFactorEasyBonusFactor = get.Required.At ["rev"; "ease4"] Decode.float
              MatureCardsIntervalFactor = get.Required.At ["rev"; "ivlFct"] Decode.float
              MatureCardsMaximumInterval = get.Required.At ["rev"; "maxIvl"] Decode.float |> TimeSpanInt16.fromDays
              MatureCardsHardInterval = get.Required.At ["rev"; "hardFactor"] Decode.float
              MatureCardsBuryRelated = get.Required.At ["rev"; "bury"] Decode.bool
              LapsedCardsSteps = get.Required.At ["lapse"; "delays"] (Decode.array Decode.float) |> Array.map TimeSpan.FromMinutes |> List.ofArray
              LapsedCardsNewIntervalFactor = get.Required.At ["lapse"; "mult"] Decode.float
              LapsedCardsMinimumInterval = get.Required.At ["lapse"; "minInt"] Decode.float |> TimeSpan.FromDays
              LapsedCardsLeechThreshold = get.Required.At ["lapse"; "leechFails"] Decode.int |> byte
              ShowAnswerTimer = get.Required.Field "timer" ankiIntToBool
              AutomaticallyPlayAudio = get.Required.Field "autoplay" Decode.bool
              ReplayQuestionAudioOnAnswer = get.Required.Field "replayq" Decode.bool })
        |> Decode.keyValuePairs
        |> Decode.fromString
    let parseDecks =
        Decode.object(fun get ->
            (get.Required.Field "id" Decode.int,
             get.Required.Field "name" Decode.string,
             get.Optional.Field "conf" Decode.int))
        |> Decode.keyValuePairs
        |> Decode.fromString
    let parseModels userId =
        Decode.object(fun get ->
            { DeckId = get.Required.Field "did" Decode.int
              ConceptTemplate =
                { Id = 0
                  ConceptTemplate = {
                    Id = 0
                    MaintainerId = userId
                    Name = get.Required.Field "name" Decode.string
                  }
                  Css = get.Required.Field "css" Decode.string
                  Fields =
                    get.Required.Field "flds" (Decode.object(fun get ->
                        { Id = 0
                          Name = get.Required.Field "name" Decode.string
                          Font = get.Required.Field "font" Decode.string
                          FontSize = get.Required.Field "size" Decode.int |> byte
                          IsRightToLeft = get.Required.Field "rtl" Decode.bool
                          Ordinal = get.Required.Field "ord" Decode.int |> byte
                          IsSticky = get.Required.Field "sticky" Decode.bool })
                        |> Decode.list )
                  CardTemplates =
                    get.Required.Field "tmpls" (Decode.object(fun g ->
                        { Id = 0
                          Name = g.Required.Field "name" Decode.string
                          QuestionTemplate = g.Required.Field "qfmt" Decode.string
                          AnswerTemplate = g.Required.Field "afmt" Decode.string
                          ShortQuestionTemplate = g.Required.Field "bqfmt" Decode.string
                          ShortAnswerTemplate = g.Required.Field "bafmt" Decode.string
                          Ordinal = g.Required.Field "ord" Decode.int |> byte })
                          |> Decode.list )
                  Created = get.Required.Field "id" Decode.int64 |> DateTimeOffset.FromUnixTimeMilliseconds |> fun x -> x.UtcDateTime
                  Modified = get.Required.Field "mod" Decode.int64 |> DateTimeOffset.FromUnixTimeSeconds |> fun x -> x.UtcDateTime |> Some
                  IsCloze = get.Required.Field "type" ankiIntToBool
                  DefaultPublicTags = []
                  DefaultPrivateTags = [] // lowTODO the caller should pass in these values, having done some preprocessing on the JSON string to add and retrieve the tag ids
                  DefaultCardOptionId = 0
                  LatexPre = get.Required.Field "latexPre" Decode.string
                  LatexPost = get.Required.Field "latexPost" Decode.string
                }
            })
        |> Decode.keyValuePairs
        |> Decode.fromString
    type ImgRegex = Regex< """<img src="(?<ankiFileName>.+?)">""" >
    type SoundRegex = Regex< """\[sound:(?<ankiFileName>.+?)\]""" >
    let replaceAnkiFilenames field (fileEntityByAnkiFileName: Map<string, FileEntity>) =
        (([], field, []), ImgRegex().TypedMatches field)
        ||> Seq.fold (fun (files, field, missingAnkiFileNames) m -> 
            let ankiFileName = m.ankiFileName.Value
            if fileEntityByAnkiFileName |> Map.containsKey ankiFileName
            then
                let file = fileEntityByAnkiFileName.[ankiFileName]
                ( file :: files,
                  field.Replace(ankiFileName, Convert.ToBase64String file.Sha256),
                  missingAnkiFileNames )
            else
                ( files,
                  field,
                  ankiFileName :: missingAnkiFileNames )
        )
        |> fun x -> (x, SoundRegex().TypedMatches field)
        ||> Seq.fold (fun (files, field, missingAnkiFileNames) m -> 
            let ankiFileName = m.ankiFileName.Value
            if fileEntityByAnkiFileName |> Map.containsKey ankiFileName
            then
                let file = fileEntityByAnkiFileName.[ankiFileName]
                let field = SoundRegex().Replace(field, """
<audio controls autoplay>
    <source src="${ankiFileName}" type="audio/mpeg">
    Your browser does not support the audio element.
</audio>
"""             )
                ( file :: files,
                  field.Replace(ankiFileName, Convert.ToBase64String file.Sha256),
                  missingAnkiFileNames )
            else
                ( files,
                  field,
                  ankiFileName :: missingAnkiFileNames )
        )
        |> fun (files, fields, missingAnkiFileNames) -> (files |> List.distinct, fields, missingAnkiFileNames)
    let parseNotes
        (conceptTemplatesByModelId: Map<string, ConceptTemplateInstanceEntity>)
        initialTags
        userId
        fileEntityByAnkiFileName
        getConcept = // medTODO use tail recursion
        let rec parseNotesRec tags conceptsAndTagsByNoteId missingAnkiFileNames =
            function
            | (note: NoteEntity) :: tail ->
                let notesTags = note.Tags.Split(' ') |> Array.map (fun x -> x.Trim()) |> Array.filter (not << String.IsNullOrWhiteSpace) |> Set.ofArray
                let allTags =
                    Set.difference
                        notesTags
                        (tags |> List.map (fun (x: PrivateTagEntity) -> x.Name) |> Set.ofSeq)
                    |> List.ofSeq
                    |> List.map (fun x -> PrivateTagEntity(Name = x,  UserId = userId))
                    |> List.append tags
                let files, fields, newMissingAnkiFileNames = replaceAnkiFilenames note.Flds fileEntityByAnkiFileName
                let conceptTemplate = conceptTemplatesByModelId.[string note.Mid]
                let concept =
                    let c =
                        { ConceptTemplateHash = conceptTemplate.AcquireHash
                          FieldValues =
                            Seq.zip
                                conceptTemplate.Fields
                                (MappingTools.splitByUnitSeparator fields)
                            |> Seq.map (fun (field, value) -> FieldValueEntity(Field = field, Value = value))
                          Created = DateTimeOffset.FromUnixTimeMilliseconds(note.Id).UtcDateTime
                          Modified = DateTimeOffset.FromUnixTimeSeconds(note.Mod).UtcDateTime |> Some
                          MaintainerId = userId
                          IsPublic = false }
                    getConcept c
                    |> function
                    | Some x -> x
                    | None -> c.CopyToNew files
                let relevantTags = allTags |> Seq.filter(fun x -> notesTags.Contains x.Name)
                parseNotesRec
                    allTags
                    ((note.Id, (concept, relevantTags))::conceptsAndTagsByNoteId)
                    (newMissingAnkiFileNames.Concat missingAnkiFileNames)
                    tail
            | _ ->
                if missingAnkiFileNames.Any()
                then Error <| "In Anki, click 'Tools', then 'Check Media', because your Anki notes refer to some missing files: \r\n" + String.Join(", ", missingAnkiFileNames)
                else Ok conceptsAndTagsByNoteId
        parseNotesRec initialTags [] []
    let mapCard
        (cardOptionAndDeckTagByDeckId: Map<int, CardOptionEntity * string>)
        (conceptsAndTagsByAnkiId: Map<int64, ConceptInstanceEntity * PrivateTagEntity seq>)
        (colCreateDate: DateTime)
        userId
        (usersTags: PrivateTagEntity seq)
        getCard
        (ankiCard: Anki.CardEntity) =
        let cardOption, deckTag = cardOptionAndDeckTagByDeckId.[int ankiCard.Did]
        let deckTag = usersTags.First(fun x -> x.Name = deckTag)
        let concept, tags = conceptsAndTagsByAnkiId.[ankiCard.Nid]
        match ankiCard.Type with
        | 0L -> Ok New
        | 1L -> Ok Learning
        | 2L -> Ok Mature
        | 3L -> Error "Filtered decks are not supported. Please delete the filtered decks and upload the new export."
        | _ -> Error "Unexpected card type. Please contact support and attach the file you tried to import."
        |> Result.map (fun memorizationState ->
            let entity =
                let c: AnkiAcquiredCard =
                    { UserId = userId
                      ConceptInstance = concept
                      CardTemplate = concept.FieldValues.First().Field.ConceptTemplateInstance.CardTemplates.First(fun x -> x.Ordinal = byte ankiCard.Ord)
                      MemorizationState = memorizationState
                      CardState =
                        match ankiCard.Queue with
                        | -3L -> UserBuried
                        | -2L -> SchedulerBuried
                        | -1L -> Suspended
                        | _ -> Normal
                      LapseCount = ankiCard.Lapses |> byte // lowTODO This will throw an exception from `Microsoft.FSharp.Core.Operators.Checked` if Lapses is too big; should be a Result somehow
                      EaseFactorInPermille = ankiCard.Factor |> int16
                      IntervalNegativeIsMinutesPositiveIsDays =
                        if ankiCard.Ivl > 0L
                        then ankiCard.Ivl |> int16
                        else float ankiCard.Ivl * -1. / 60. |> Math.Round |> int16
                      StepsIndex =
                        match memorizationState with
                        | New
                        | Learning ->
                            if ankiCard.Left = 0L
                            then 0
                            else cardOption.NewCardsStepsInMinutes.Count() - (int ankiCard.Left % 1000)
                            |> byte |> Some
                        | Lapsed ->
                            if ankiCard.Left = 0L
                            then 0
                            else cardOption.LapsedCardsStepsInMinutes.Count() - (int ankiCard.Left % 1000)
                            |> byte |> Some
                        | Mature -> None
                      Due =
                        match memorizationState with
                        | New -> DateTime.UtcNow.Date
                        | Learning -> DateTimeOffset.FromUnixTimeSeconds(ankiCard.Due).UtcDateTime
                        | Lapsed -> DateTimeOffset.FromUnixTimeSeconds(ankiCard.Due).UtcDateTime
                        | Mature -> colCreateDate + TimeSpan.FromDays(float ankiCard.Due)
                      CardOption = cardOption }
                getCard c
                |> function
                | Some entity ->
                    c.CopyTo entity
                    entity
                | None -> c.CopyToNew (deckTag :: List.ofSeq tags)
            (ankiCard.Id, entity)
        )

namespace CardOverflow.Api

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

type SimpleAnkiDb = {
    Cards: CardOverflow.Entity.Anki.CardEntity list
    Cols: ColEntity list
    Notes: NoteEntity list
    Revlogs: RevlogEntity list
}

type AnkiConceptTemplate = {
    ConceptTemplate: ConceptTemplate
    DeckId: int
}

type AnkiConceptWrite = {
    Title: string
    Description: string
    ConceptTemplate: ConceptTemplateEntity
    Fields: string list
    Modified: DateTime
    MaintainerId: int
    IsPublic: bool
} with
    member this.CopyTo(entity: ConceptEntity) =
        entity.Title <- this.Title
        entity.Description <- this.Description
        entity.ConceptTemplate <- this.ConceptTemplate
        entity.Fields <- this.Fields |> MappingTools.joinByUnitSeparator
        entity.Modified <- this.Modified
        entity.MaintainerId <- this.MaintainerId
        entity.IsPublic <- this.IsPublic
    member this.CopyToNew =
        let entity = ConceptEntity()
        this.CopyTo entity
        entity

module Anki =
    let toHistory (cardByAnkiId: Map<int64, CardOverflow.Entity.AcquiredCardEntity>) userId (revLog: RevlogEntity) =
        HistoryEntity(
            AcquiredCard = cardByAnkiId.[revLog.Cid],
            EaseFactorInPermille = int16 revLog.Factor,
            IntervalNegativeIsMinutesPositiveIsDays = (
                match revLog.Ivl with
                | p when p > 0L -> int16 p // positive is days
                | _ -> revLog.Ivl / 60L |> int16 // In Anki, negative is seconds, and we want minutes
            ),
            ScoreAndMemorizationState =
                (ScoreAndMemorizationState.from
                <| match revLog.Ease with
                    | 1L -> Score.Again
                    | 2L -> Score.Hard
                    | 3L -> Score.Good
                    | 4L -> Score.Easy
                    | _ -> failwith <| sprintf "Unrecognized Anki revlog ease: %i" revLog.Ease
                <| match revLog.Type with
                    |0L -> MemorizationState.New
                    | 1L -> MemorizationState.Learning
                    | 2L -> MemorizationState.Mature
                    | 3L -> MemorizationState.Mature
                    | _ -> failwith <| sprintf "Unrecognized Anki revlog type: %i" revLog.Type
                |> byte),
            TimeFromSeeingQuestionToScoreInSecondsMinus32768 =
                (revLog.Time / 1000L - 32768L |> int16),
            Timestamp =
                DateTimeOffset.FromUnixTimeMilliseconds(revLog.Id).UtcDateTime
        )

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
    let parseModels userId (cardOptionAndDeckNameByDeckId: Map<int, CardOptionEntity * string>) =
        Decode.object(fun get ->
            { DeckId = get.Required.Field "did" Decode.int
              ConceptTemplate =
                { Id = 0
                  MaintainerId = userId
                  Name = get.Required.Field "name" Decode.string
                  Css = get.Required.Field "css" Decode.string
                  Fields =
                    get.Required.Field "flds" (Decode.object(fun get ->
                        { Name = get.Required.Field "name" Decode.string
                          Font = get.Required.Field "font" Decode.string
                          FontSize = get.Required.Field "size" Decode.int |> byte
                          IsRightToLeft = get.Required.Field "rtl" Decode.bool
                          Ordinal = get.Required.Field "ord" Decode.int |> byte
                          IsSticky = get.Required.Field "sticky" Decode.bool })
                        |> Decode.list )
                  CardTemplates =
                    get.Required.Field "tmpls" (Decode.object(fun g ->
                        { Name = g.Required.Field "name" Decode.string
                          QuestionTemplate = g.Required.Field "qfmt" Decode.string
                          AnswerTemplate = g.Required.Field "afmt" Decode.string
                          ShortQuestionTemplate = g.Required.Field "bqfmt" Decode.string
                          ShortAnswerTemplate = g.Required.Field "bafmt" Decode.string
                          Ordinal = g.Required.Field "ord" Decode.int |> byte })
                          |> Decode.list )
                  Modified = get.Required.Field "mod" Decode.int64 |> DateTimeOffset.FromUnixTimeMilliseconds |> fun x -> x.UtcDateTime
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
    let rec parseNotes (conceptTemplatesByModelId: Map<string, ConceptTemplateEntity>) tags userId conceptsAndTagsByNoteId = // medTODO use tail recursion
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
            let concept =
                { Title = ""
                  Description = ""
                  ConceptTemplate = conceptTemplatesByModelId.[string note.Mid]
                  Fields = MappingTools.splitByUnitSeparator note.Flds
                  Modified = DateTimeOffset.FromUnixTimeSeconds(note.Mod).UtcDateTime
                  MaintainerId = userId
                  IsPublic = false }.CopyToNew
            parseNotes conceptTemplatesByModelId allTags userId ((note.Id, (concept, allTags.Where(fun x -> notesTags.Contains x.Name)))::conceptsAndTagsByNoteId) tail
        | _ -> conceptsAndTagsByNoteId
    let mapCard (cardOptionAndDeckTagByDeckId: Map<int, CardOptionEntity * string>) (conceptsAndTagsByAnkiId: Map<int64, ConceptEntity * PrivateTagEntity seq>) (colCreateDate: DateTime) userId (usersTags: PrivateTagEntity list) (ankiCard: Anki.CardEntity) =
        let cardOption, deckTag = cardOptionAndDeckTagByDeckId.[int ankiCard.Did]
        let deckTag = usersTags.First(fun x -> x.Name = deckTag)
        let concept, tags = conceptsAndTagsByAnkiId.[ankiCard.Nid]
        match ankiCard.Type with
        | 0L -> Ok MemorizationState.New
        | 1L -> Ok MemorizationState.Learning
        | 2L -> Ok MemorizationState.Mature
        | 3L -> Error "Filtered decks are not supported. Please delete the filtered decks and upload the new export."
        | _ -> Error "Unexpected card type. Please contact support and attach the file you tried to import."
        |> Result.map (fun memorizationState ->
            { AcquiredCard.Id = 0
              UserId = userId
              ConceptId = 0
              MemorizationState = memorizationState
              CardState =
                match ankiCard.Queue with
                | -3L -> CardState.UserBuried
                | -2L -> CardState.SchedulerBuried
                | -1L -> CardState.Suspended
                | _ -> CardState.Normal
              LapseCount = ankiCard.Lapses |> byte // lowTODO This will throw an exception from `Microsoft.FSharp.Core.Operators.Checked` if Lapses is too big; should be a Result somehow
              EaseFactorInPermille = ankiCard.Factor |> int16
              IntervalNegativeIsMinutesPositiveIsDays =
                if ankiCard.Ivl > 0L
                then ankiCard.Ivl |> int16
                else float ankiCard.Ivl * -1. / 60. |> Math.Round |> int16
              StepsIndex =
                match memorizationState with
                | MemorizationState.New
                | MemorizationState.Learning ->
                    if ankiCard.Left = 0L
                    then 0
                    else cardOption.NewCardsStepsInMinutes.Count() - (int ankiCard.Left % 1000)
                    |> byte |> Some
                | MemorizationState.Lapsed ->
                    if ankiCard.Left = 0L
                    then 0
                    else cardOption.LapsedCardsStepsInMinutes.Count() - (int ankiCard.Left % 1000)
                    |> byte |> Some
                | MemorizationState.Mature -> None
              Due =
                match memorizationState with
                | MemorizationState.New -> DateTime.UtcNow.Date
                | MemorizationState.Learning -> DateTimeOffset.FromUnixTimeSeconds(ankiCard.Due).UtcDateTime
                | MemorizationState.Lapsed -> DateTimeOffset.FromUnixTimeSeconds(ankiCard.Due).UtcDateTime
                | MemorizationState.Mature -> colCreateDate + TimeSpan.FromDays(float ankiCard.Due)
              TemplateIndex = ankiCard.Ord |> byte
              CardOptionId = 0 }.CopyToNew concept cardOption (deckTag :: List.ofSeq tags), ankiCard)

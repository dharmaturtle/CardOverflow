namespace CardOverflow.Api

open NeoSmart.Utils
open FSharp.Text.RegexProvider
open CardOverflow.Entity.Anki
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
open System
open System.Linq
open LoadersAndCopiers
open Thoth.Json.Net
open Microsoft.FSharp.Core.Operators.Checked
open System.Collections.Generic
open Helpers
open FsToolkit.ErrorHandling
open System.Security.Cryptography
open Microsoft.EntityFrameworkCore

type SimpleAnkiDb = {
    Cards: CardOverflow.Entity.Anki.CardEntity list
    Cols: ColEntity list
    Notes: NoteEntity list
    Revlogs: RevlogEntity list
}

type AnkiCardTemplateInstance = {
    MaintainerId: int
    Name: string
    Css: string
    Fields: Field seq
    Created: DateTime
    Modified: DateTime option
    DefaultTags: int list
    DefaultCardOptionId: int
    LatexPre: string
    LatexPost: string
    QuestionTemplate: string
    AnswerTemplate: string
    ShortQuestionTemplate: string
    ShortAnswerTemplate: string
    DeckId: int64
    IsCloze: bool
} with
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
    member this.CopyToNew userId defaultCardOption =
        let entity = CardTemplateInstanceEntity()
        entity.User_CardTemplateInstances <-
            [User_CardTemplateInstanceEntity(
                UserId = userId,
                Tag_User_CardTemplateInstances =
                    (this.DefaultTags
                        .Select(fun x -> Tag_User_CardTemplateInstanceEntity(UserId = userId, DefaultTagId = x))
                        .ToList()),
                DefaultCardOption = defaultCardOption)].ToList()
        entity.CardTemplate <-
            CardTemplateEntity(
                AuthorId = this.MaintainerId,
                Name = this.Name)
        this.CopyTo entity
        use hasher = SHA256.Create() // lowTODO pull this out
        entity.AcquireHash <- CardTemplateInstanceEntity.acquireHash hasher entity
        entity
    
type AnkiCardWrite = {
    CardTemplate: CardTemplateInstanceEntity
    FieldValues: string
    Created: DateTime
    Modified: DateTime option
    MaintainerId: int
} with
    member this.CopyTo(entity: CardInstanceEntity, cardTemplateHash: byte[]) =
        entity.FieldValues <- this.FieldValues
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.CardTemplateInstance <- this.CardTemplate
        use hasher = SHA256.Create()
        entity.AcquireHash <- CardInstanceEntity.acquireHash entity cardTemplateHash hasher
    member this.CopyToNew (files: FileEntity seq) =
        let entity = CardInstanceEntity()
        entity.Card <-
            CardEntity(
                AuthorId = this.MaintainerId,
                Description = "Imported from Anki"
            )
        entity.File_CardInstances <-
            files.Select(fun x ->
                File_CardInstanceEntity(
                    CardInstance = entity,
                    File = x
                )
            ).ToList()
        this.CopyTo(entity, this.CardTemplate.AcquireHash)
        entity
    member this.AcquireEquality (db: CardOverflowDb) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        db.CardInstance
            .Include(fun x -> x.Card.RelationshipSources)
            .Include(fun x -> x.Card.RelationshipTargets)
            .FirstOrDefault(fun c -> c.AcquireHash = (this.CopyToNew []).AcquireHash)

type AnkiAcquiredCard = {
    UserId: int
    CardInstance: CardInstanceEntity
    CardTemplateInstance: CardTemplateInstanceEntity
    CardState: CardState
    IsLapsed: bool
    LapseCount: byte
    EaseFactorInPermille: int16
    IntervalOrStepsIndex: IntervalOrStepsIndex
    Due: DateTime
    CardOption: CardOptionEntity
} with
    member this.CopyTo (entity: AcquiredCardEntity) =
        entity.UserId <- this.UserId
        entity.CardState <- CardState.toDb this.CardState
        entity.IsLapsed <- this.IsLapsed
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalOrStepsIndex <- IntervalOrStepsIndex.intervalToDb this.IntervalOrStepsIndex
        entity.Due <- this.Due
    member this.CopyToNew (tags: TagEntity seq) =
        let entity = AcquiredCardEntity ()
        this.CopyTo entity
        entity.CardInstance <- this.CardInstance
        entity.CardOption <- this.CardOption
        entity.Tag_AcquiredCards <- tags.Select(fun x -> Tag_AcquiredCardEntity(AcquiredCard = entity, Tag = x)).ToList()
        entity
    member this.AcquireEquality (db: CardOverflowDb) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        db.AcquiredCard
            .FirstOrDefault(fun c -> 
                this.UserId = c.UserId &&
                this.CardInstance.Id = c.CardInstanceId &&
                this.CardTemplateInstance.Id = c.CardInstance.CardTemplateInstanceId
            )

type AnkiHistory = {
    AcquiredCard: AcquiredCardEntity
    Score: byte
    Timestamp: DateTime
    IntervalWithUnusedStepsIndex: IntervalOrStepsIndex
    EaseFactorInPermille: int16
    TimeFromSeeingQuestionToScoreInSecondsMinus32768: int16
} with
    member this.AcquireEquality (db: CardOverflowDb) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        let roundedTimeStamp = MappingTools.round this.Timestamp <| TimeSpan.FromMinutes(1.)
        let interval = this.IntervalWithUnusedStepsIndex |> IntervalOrStepsIndex.intervalToDb
        db.History.FirstOrDefault(fun h -> 
            this.AcquiredCard.UserId = h.AcquiredCard.UserId &&
            this.AcquiredCard.CardInstanceId = h.AcquiredCard.CardInstanceId &&
            this.AcquiredCard.CardInstance.CardTemplateInstanceId = h.AcquiredCard.CardInstance.CardTemplateInstanceId &&
            this.Score = h.Score &&
            roundedTimeStamp = h.Timestamp &&
            interval = h.IntervalWithUnusedStepsIndex &&
            this.EaseFactorInPermille = h.EaseFactorInPermille &&
            this.TimeFromSeeingQuestionToScoreInSecondsMinus32768 = h.TimeFromSeeingQuestionToScoreInSecondsPlus32768
        )
    member this.CopyTo (entity: HistoryEntity) =
        entity.AcquiredCard <- this.AcquiredCard
        entity.Score <- this.Score
        entity.Timestamp <- this.Timestamp
        entity.IntervalWithUnusedStepsIndex <- this.IntervalWithUnusedStepsIndex |> IntervalOrStepsIndex.intervalToDb
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.TimeFromSeeingQuestionToScoreInSecondsPlus32768 <- this.TimeFromSeeingQuestionToScoreInSecondsMinus32768
    member this.CopyToNew =
        let history = HistoryEntity()
        this.CopyTo history
        history

type AnkiCardType = | New | Learning | Due // | Filtered

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
            return
                if cardByAnkiId.ContainsKey revLog.Cid // veryLowTODO report/log this, sometimes a user reviews a card then deletes it, but Anki keeps the orphaned revlog
                then
                    let history = {
                        AcquiredCard = cardByAnkiId.[revLog.Cid]
                        EaseFactorInPermille = int16 revLog.Factor
                        IntervalWithUnusedStepsIndex =
                            match revLog.Ivl with
                            | p when p > 0L -> p |> float |> TimeSpan.FromDays    // In Anki, positive is days
                            | n             -> n |> float |> TimeSpan.FromSeconds // In Anki, negative is seconds
                            |> Interval
                        Score = score |> Score.toDb
                        TimeFromSeeingQuestionToScoreInSecondsMinus32768 =
                            revLog.Time / 1000L - 32768L |> int16
                        Timestamp =
                            DateTimeOffset.FromUnixTimeMilliseconds(revLog.Id).UtcDateTime
                    }
                    getHistory history
                    |> function
                    | Some x -> x
                    | None -> history.CopyToNew
                    |> Some
                else None
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
            { Id = 0 // lowTODO this entire record needs to be validated for out of range values
              Name = get.Required.Field "name" Decode.string
              IsDefault = false
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
              MatureCardsHardInterval = get.Optional.At ["rev"; "hardFactor"] Decode.float |? 1.2
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
            (get.Required.Field "id" Decode.int64,
             get.Required.Field "name" Decode.string,
             get.Optional.Field "conf" Decode.int))
        |> Decode.keyValuePairs
        |> Decode.fromString
    let parseModels userId =
        Decode.object(fun get ->
            let cardTemplates =
                get.Required.Field "tmpls" (Decode.object(fun g ->
                            {|Name = g.Required.Field "name" Decode.string
                              QuestionTemplate = g.Required.Field "qfmt" Decode.string
                              AnswerTemplate = g.Required.Field "afmt" Decode.string
                              ShortQuestionTemplate = g.Required.Field "bqfmt" Decode.string
                              ShortAnswerTemplate = g.Required.Field "bafmt" Decode.string
                              Ordinal = g.Required.Field "ord" Decode.int |> byte|})
                              |> Decode.list )
                |> Seq.sortBy (fun x -> x.Ordinal)
            cardTemplates
            |> Seq.map(fun cardTemplate ->
                let namePostfix =
                    if cardTemplates.Count() >= 2
                    then " - " + cardTemplate.Name
                    else ""
                {   MaintainerId = userId
                    Name = get.Required.Field "name" Decode.string + namePostfix
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
                    QuestionTemplate = cardTemplate.QuestionTemplate
                    AnswerTemplate = cardTemplate.AnswerTemplate
                    ShortQuestionTemplate = cardTemplate.ShortQuestionTemplate
                    ShortAnswerTemplate = cardTemplate.ShortAnswerTemplate
                    Created = get.Required.Field "id" Decode.int64 |> DateTimeOffset.FromUnixTimeMilliseconds |> fun x -> x.UtcDateTime
                    Modified = get.Required.Field "mod" Decode.int64 |> DateTimeOffset.FromUnixTimeSeconds |> fun x -> x.UtcDateTime |> Some
                    DefaultTags = [] // lowTODO the caller should pass in these values, having done some preprocessing on the JSON string to add and retrieve the tag ids
                    DefaultCardOptionId = 0
                    LatexPre = get.Required.Field "latexPre" Decode.string
                    LatexPost = get.Required.Field "latexPost" Decode.string
                    DeckId = get.Required.Field "did" Decode.int64
                    IsCloze = get.Required.Field "type" ankiIntToBool
                }))
        |> Decode.keyValuePairs
        |> Decode.fromString
    type ImgRegex = Regex< """<img src="(?<ankiFileName>[^"]+)".*?>""" >
    type SoundRegex = Regex< """\[sound:(?<ankiFileName>.+?)\]""" >
    let replaceAnkiFilenames field (fileEntityByAnkiFileName: Map<string, FileEntity>) =
        (([], field), ImgRegex().TypedMatches field)
        ||> Seq.fold (fun (files, field) m -> 
            let ankiFileName = m.ankiFileName.Value
            if fileEntityByAnkiFileName |> Map.containsKey ankiFileName then
                let file = fileEntityByAnkiFileName.[ankiFileName]
                ( file :: files,
                  field.Replace(ankiFileName, "/image/" + UrlBase64.Encode file.Sha256))
            else
                ( files,
                  field.Replace(ankiFileName, "/missingImage.jpg")) // medTODO needs a placeholder
        )
        |> fun x -> (x, SoundRegex().TypedMatches field)
        ||> Seq.fold (fun (files, field) m ->
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
                  field.Replace(ankiFileName, Convert.ToBase64String file.Sha256))
            else
                ( files,
                  field.Replace(ankiFileName, "[MISSING SOUND]")) // medTODO uh, what does this look/sound like? Write a test lol
        )
        |> fun (files, fields) -> (files |> List.distinct, fields)
    
    let parseNotes
        (cardTemplatesByModelId: Map<string, {| Entity: CardTemplateInstanceEntity; IsCloze: bool |} list>)
        initialTags
        userId
        fileEntityByAnkiFileName
        noRelationship
        getCard = // lowTODO use tail recursion
        let rec parseNotesRec tags cardsAndTagsByNoteId =
            function
            | (note: NoteEntity) :: tail ->
                let notesTags = note.Tags.Split(' ') |> Array.map (fun x -> x.Trim()) |> Array.filter (not << String.IsNullOrWhiteSpace) |> Set.ofArray
                let allTags =
                    Set.difference
                        notesTags
                        (tags |> List.map (fun (x: TagEntity) -> x.Name) |> Set.ofSeq)
                    |> List.ofSeq
                    |> List.map (fun x -> TagEntity(Name = x))
                    |> List.append tags
                    |> List.groupBy (fun x -> x.Name.ToLower())
                    |> List.map (fun (_, x) -> x.First())
                let files, fields = replaceAnkiFilenames note.Flds fileEntityByAnkiFileName
                let cardTemplates = cardTemplatesByModelId.[string note.Mid]
                let cards =
                    let toCard fields (cardTemplate: CardTemplateInstanceEntity) =
                        let c =
                            { CardTemplate = cardTemplate
                              FieldValues = fields
                              Created = DateTimeOffset.FromUnixTimeMilliseconds(note.Id).UtcDateTime
                              Modified = DateTimeOffset.FromUnixTimeSeconds(note.Mod).UtcDateTime |> Some
                              MaintainerId = userId }
                        defaultArg
                            <| getCard c
                            <| c.CopyToNew files
                    if cardTemplates.First().IsCloze then
                        let cardTemplate = cardTemplates |> Seq.exactlyOne
                        let instances =
                            [1 .. AnkiImportLogic.maxClozeIndex fields] |> List.map (fun i ->
                                toCard
                                    <| AnkiImportLogic.multipleClozeToSingleCloze fields i
                                    <| cardTemplate.Entity
                            )
                        Core.combination 2 instances
                        |> List.iter (fun instancePair ->
                                if  instancePair.[0].Card.Id = 0 &&
                                    instancePair.[1].Card.Id = 0 &&
                                    noRelationship instancePair.[0].Card.Id instancePair.[1].Card.Id userId "Cloze"
                                then
                                    let r = RelationshipEntity(Name = "Cloze", UserId = userId)
                                    instancePair.[0].Card.RelationshipSources.Add r
                                    instancePair.[1].Card.RelationshipTargets.Add r
                            )
                        instances
                    else
                        let instances = cardTemplates |> List.map (fun x -> toCard fields x.Entity)
                        Core.combination 2 instances
                        |> List.iter (fun instancePair ->
                                if  instancePair.[0].Card.Id = 0 &&
                                    instancePair.[1].Card.Id = 0 &&
                                    noRelationship instancePair.[0].Card.Id instancePair.[1].Card.Id userId "Linked"
                                then
                                    let r = RelationshipEntity(Name = "Linked", UserId = userId)
                                    instancePair.[0].Card.RelationshipSources.Add r
                                    instancePair.[1].Card.RelationshipTargets.Add r
                            )
                        instances
                let relevantTags = allTags |> List.filter(fun x -> notesTags.Contains x.Name)
                parseNotesRec
                    allTags
                    (Seq.append
                        [note.Id, (cards, relevantTags)]
                        cardsAndTagsByNoteId)
                    tail
            | _ ->
                cardsAndTagsByNoteId
        parseNotesRec initialTags []
    let mapCard
        (cardOptionAndDeckTagByDeckId: Map<int64, CardOptionEntity * string>)
        (cardsAndTagsByNoteId: Map<int64, CardInstanceEntity list * TagEntity list>)
        (colCreateDate: DateTime)
        userId
        (usersTags: TagEntity list)
        getCard
        (ankiCard: Anki.CardEntity) =
        let cardOption, deckTag = cardOptionAndDeckTagByDeckId.[ankiCard.Did]
        let deckTag = usersTags.First(fun x -> x.Name = deckTag)
        let cards, tags = cardsAndTagsByNoteId.[ankiCard.Nid]
        let card = cards.ElementAt(ankiCard.Ord |> int)
        let cti = card.CardTemplateInstance
        match ankiCard.Type with
        | 0L -> Ok New
        | 1L -> Ok Learning
        | 2L -> Ok Due
        | 3L -> Error "Filtered decks are not supported. Please delete the filtered decks and upload the new export."
        | _ -> Error "Unexpected card type. Please contact support and attach the file you tried to import."
        |> Result.map (fun cardType ->
            let entity =
                let c: AnkiAcquiredCard =
                    { UserId = userId
                      CardInstance = card
                      CardTemplateInstance = cti
                      CardState =
                        match ankiCard.Queue with
                        | -3L -> UserBuried
                        | -2L -> SchedulerBuried
                        | -1L -> Suspended
                        | _ -> Normal
                      LapseCount = ankiCard.Lapses |> byte // lowTODO This will throw an exception from `Microsoft.FSharp.Core.Operators.Checked` if Lapses is too big; should be a Result somehow
                      EaseFactorInPermille = ankiCard.Factor |> int16
                      IsLapsed = false // lowTODO not the real value, need to find a way to get it from Anki, but it doesn't look like its in the ankiDb, and looking at sched.py is just infuriating
                      IntervalOrStepsIndex =
                        match cardType with
                        | New
                        | Learning ->
                            if ankiCard.Left = 0L
                            then 0
                            else cardOption.NewCardsStepsInMinutes.Count() - (int ankiCard.Left % 1000)
                            |> byte |> NewStepsIndex
                        | Due ->
                            if ankiCard.Ivl > 0L
                            then ankiCard.Ivl |> float |> TimeSpan.FromDays
                            else float ankiCard.Ivl * -1. / 60. |> Math.Round |> float |> TimeSpan.FromMinutes
                            |> Interval
                      Due =
                        match cardType with
                        | New -> DateTime.UtcNow.Date
                        | Learning -> DateTimeOffset.FromUnixTimeSeconds(ankiCard.Due).UtcDateTime
                        | Due ->
                            if ankiCard.Odue = 0L
                            then ankiCard.Due
                            else ankiCard.Odue
                            |> float
                            |> TimeSpan.FromDays
                            |> (+) colCreateDate
                      CardOption = cardOption }
                getCard c
                |> function
                | Some entity ->
                    c.CopyTo entity
                    entity
                | None -> c.CopyToNew <| Seq.append [deckTag] tags
            ankiCard.Id, entity
        )

namespace CardOverflow.Api

open NeoSmart.Utils
open FSharp.Text.RegexProvider
open CardOverflow.Entity.Anki
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Pure.Core
open MappingTools
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

type AnkiCollateInstance = {
    AnkiId: int64
    AuthorId: int
    Name: string
    Css: string
    Fields: Field list
    Created: DateTime
    Modified: DateTime option
    DefaultTags: int list
    DefaultCardSettingId: int
    LatexPre: string
    LatexPost: string
    Templates: Template list
    DeckId: int64
    IsCloze: bool
} with
    member this.CopyTo (entity: CollateInstanceEntity) =
        entity.Name <- this.Name
        entity.Css <- this.Css
        entity.Fields <- Fields.toString this.Fields
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.LatexPre <- this.LatexPre
        entity.LatexPost <- this.LatexPost
        entity.Templates <- this.Templates |> Template.copyToMany
        entity.EditSummary <- "Imported from Anki"
        entity.Type <- if this.IsCloze then 1s else 0s
    member this.CopyToNewWithCollate userId collate defaultCardSetting =
        let entity = CollateInstanceEntity()
        entity.User_CollateInstances <-
            [User_CollateInstanceEntity(
                UserId = userId,
                Tag_User_CollateInstances =
                    (this.DefaultTags
                        .Select(fun x -> Tag_User_CollateInstanceEntity(UserId = userId, DefaultTagId = x))
                        .ToList()),
                DefaultCardSetting = defaultCardSetting)].ToList()
        entity.Collate <- collate
        this.CopyTo entity
        entity.AnkiId <- Nullable this.AnkiId
        entity
    member this.CopyToNew userId defaultCardSetting =
        this.CopyToNewWithCollate userId (CollateEntity(AuthorId = this.AuthorId)) defaultCardSetting
    
type AnkiCardWrite = {
    AnkiNoteId: int64
    AnkiNoteOrd: int16
    CommunalFields: CommunalFieldInstanceEntity list
    Collate: CollateInstanceEntity
    FieldValues: string
    Created: DateTime
    Modified: DateTime option
    AuthorId: int
} with
    member this.CopyTo (entity: BranchInstanceEntity) =
        entity.FieldValues <- this.FieldValues
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.CollateInstance <- this.Collate
        entity.AnkiNoteId <- Nullable this.AnkiNoteId
        entity.AnkiNoteOrd <- Nullable this.AnkiNoteOrd
        entity.CommunalFieldInstance_BranchInstances <-
            this.CommunalFields
            |> List.map (fun cf -> CommunalFieldInstance_BranchInstanceEntity(BranchInstance = entity, CommunalFieldInstance = cf))
            |> toResizeArray
    member this.CopyToNew (files: FileEntity seq) = // lowTODO add a tag indicating that it was imported from Anki
        let entity = BranchInstanceEntity()
        entity.EditSummary <- "Imported from Anki"
        let card = CardEntity(AuthorId = this.AuthorId)
        entity.Card <- card
        entity.Branch <-
            BranchEntity(
                Card = card,
                AuthorId = this.AuthorId
            )
        entity.File_BranchInstances <-
            files.Select(fun x ->
                File_BranchInstanceEntity(
                    BranchInstance = entity,
                    File = x
                )
            ).ToList()
        this.CopyTo entity
        entity
    member this.AcquireEquality (db: CardOverflowDb) (hasher: SHA512) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        let collateHash = this.Collate |> CollateInstanceEntity.hash hasher
        let hash = this.CopyToNew [] |> BranchInstanceEntity.hash collateHash hasher
        db.BranchInstance
            .Include(fun x -> x.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance)
            .OrderBy(fun x -> x.Created)
            .FirstOrDefault(fun c -> c.Hash = hash)
        |> Option.ofObj

type AnkiAcquiredCard = {
    UserId: int
    BranchInstance: BranchInstanceEntity
    CollateInstance: CollateInstanceEntity
    CardState: CardState
    IsLapsed: bool
    LapseCount: byte
    EaseFactorInPermille: int16
    IntervalOrStepsIndex: IntervalOrStepsIndex
    Due: DateTime
    CardSetting: CardSettingEntity
} with
    member this.CopyTo (entity: AcquiredCardEntity) =
        entity.UserId <- this.UserId
        entity.CardState <- CardState.toDb this.CardState
        entity.IsLapsed <- this.IsLapsed
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalOrStepsIndex <- IntervalOrStepsIndex.intervalToDb this.IntervalOrStepsIndex
        entity.Due <- this.Due
    member this.CopyToNew (tags: TagEntity seq) =
        let entity = AcquiredCardEntity()
        this.CopyTo entity
        entity.Card <- this.BranchInstance.Branch.Card
        entity.Branch <- this.BranchInstance.Branch
        entity.BranchInstance <- this.BranchInstance
        entity.CardSetting <- this.CardSetting
        entity.Tag_AcquiredCards <- tags.Select(fun x -> Tag_AcquiredCardEntity(AcquiredCard = entity, Tag = x)).ToList()
        entity
    member this.AcquireEquality (db: CardOverflowDb) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        db.AcquiredCard
            .FirstOrDefault(fun c -> 
                this.UserId = c.UserId &&
                this.BranchInstance.Id = c.BranchInstanceId &&
                this.CollateInstance.Id = c.BranchInstance.CollateInstanceId
            )

type AnkiHistory = {
    AcquiredCard: AcquiredCardEntity
    Score: int16
    Timestamp: DateTime
    IntervalWithUnusedStepsIndex: IntervalOrStepsIndex
    EaseFactorInPermille: int16
    TimeFromSeeingQuestionToScoreInSecondsMinus32768: int16
} with
    member this.AcquireEquality (db: CardOverflowDb) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        let interval = this.IntervalWithUnusedStepsIndex |> IntervalOrStepsIndex.intervalToDb
        db.History.FirstOrDefault(fun h -> 
            this.AcquiredCard.UserId = h.AcquiredCard.UserId &&
            this.AcquiredCard.BranchInstanceId = h.AcquiredCard.BranchInstanceId &&
            this.AcquiredCard.BranchInstance.CollateInstanceId = h.AcquiredCard.BranchInstance.CollateInstanceId &&
            this.Score = h.Score &&
            this.Timestamp = h.Timestamp &&
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
    let parseCardSettings =
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
              MatureCardsHardIntervalFactor = get.Optional.At ["rev"; "hardFactor"] Decode.float |? 1.2
              MatureCardsBuryRelated = get.Required.At ["rev"; "bury"] Decode.bool
              LapsedCardsSteps = get.Required.At ["lapse"; "delays"] (Decode.array Decode.float) |> Array.map TimeSpan.FromMinutes |> List.ofArray
              LapsedCardsNewIntervalFactor = get.Required.At ["lapse"; "mult"] Decode.float
              LapsedCardsMinimumInterval = get.Required.At ["lapse"; "minInt"] Decode.float |> TimeSpan.FromDays
              LapsedCardsLeechThreshold = get.Required.At ["lapse"; "leechFails"] Decode.int |> int16
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
    type BraceRegex = Regex< """{{(?<fieldName>.*?)}}""" >
    let parseModels userId =
        Decode.object(fun get ->
            let collates =
                get.Required.Field "tmpls" (Decode.object(fun g ->
                            { Name = g.Required.Field "name" Decode.string
                              Front = g.Required.Field "qfmt" Decode.string
                              Back = g.Required.Field "afmt" Decode.string
                              ShortFront = g.Required.Field "bqfmt" Decode.string
                              ShortBack = g.Required.Field "bafmt" Decode.string
                            }, g.Required.Field "ord" Decode.int |> int16) |> Decode.list )
                |> List.sortBy (fun (_, ord) -> ord)
                |> List.map fst
            {   AnkiId = get.Required.Field "id" Decode.int64
                AuthorId = userId
                Name = get.Required.Field "name" Decode.string
                Css = get.Required.Field "css" Decode.string
                Fields =
                    get.Required.Field "flds" (Decode.object(fun get ->
                        { Name = get.Required.Field "name" Decode.string
                          IsRightToLeft = get.Required.Field "rtl" Decode.bool
                          Ordinal = get.Required.Field "ord" Decode.int |> byte
                          IsSticky = get.Required.Field "sticky" Decode.bool })
                        |> Decode.list)
                Templates = collates
                Created = get.Required.Field "id" Decode.int64 |> DateTimeOffset.FromUnixTimeMilliseconds |> fun x -> x.UtcDateTime
                Modified = get.Required.Field "mod" Decode.int64 |> DateTimeOffset.FromUnixTimeSeconds |> fun x -> x.UtcDateTime |> Some
                DefaultTags = [] // lowTODO the caller should pass in these values, having done some preprocessing on the JSON string to add and retrieve the tag ids
                DefaultCardSettingId = 0
                LatexPre = get.Required.Field "latexPre" Decode.string
                LatexPost = get.Required.Field "latexPost" Decode.string
                DeckId = get.Required.Field "did" Decode.int64
                IsCloze = get.Required.Field "type" ankiIntToBool
            })
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
    
    let fieldInheritPrefix = "x/Inherit:"
    let parseNotes
        (collateByModelId: Map<string, {| Entity: CollateInstanceEntity; Collate: AnkiCollateInstance |}>)
        initialTags
        userId
        fileEntityByAnkiFileName
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
                let files, fieldValues = replaceAnkiFilenames note.Flds fileEntityByAnkiFileName
                let fieldValues = fieldValues |> MappingTools.splitByUnitSeparator
                let collate = collateByModelId.[string note.Mid]
                let toCard fields collate noteOrd =
                    let c = {
                        AnkiNoteId = note.Id
                        AnkiNoteOrd = noteOrd
                        Collate = collate
                        CommunalFields = []
                        FieldValues = fields |> MappingTools.joinByUnitSeparator
                        Created = DateTimeOffset.FromUnixTimeMilliseconds(note.Id).UtcDateTime
                        Modified = DateTimeOffset.FromUnixTimeSeconds(note.Mod).UtcDateTime |> Some
                        AuthorId = userId }
                    defaultArg
                        <| getCard c
                        <| c.CopyToNew files
                let noteIdCardsAndTags =
                    result {
                        let! cards =
                            if collate.Collate.IsCloze then result {
                                let valueByFieldName =
                                    Seq.zip
                                        <| collate.Collate.Fields.OrderBy(fun x -> x.Ordinal).Select(fun f -> f.Name)
                                        <| fieldValues
                                    |> Map.ofSeq
                                let! max =
                                    AnkiImportLogic.maxClozeIndex
                                        <| sprintf "Anki Note Id #%s is malformed. It claims to be a cloze deletion but doesn't have the syntax of one. Its fields are: %s" (string note.Id) (String.Join(',', fieldValues))
                                        <| valueByFieldName
                                        <| collate.Collate.Templates.[0].Front
                                return [1s .. max] |> List.map (fun clozeIndex ->
                                    toCard
                                        <| fieldValues
                                        <| collate.Entity
                                        <| clozeIndex - 1s // ankidb's cards' ord column is 0 indexed for cloze deletions
                                )}
                            else
                                collate.Collate.Templates
                                |> List.mapi (fun i _ -> toCard fieldValues collate.Entity (i |> int16))
                                |> Ok
                        let relevantTags = allTags |> List.filter(fun x -> notesTags.Contains x.Name)
                        return (note.Id, (cards, relevantTags))
                    }
                parseNotesRec
                    allTags
                    (Seq.append
                        [noteIdCardsAndTags]
                        cardsAndTagsByNoteId)
                    tail
            | _ ->
                cardsAndTagsByNoteId
        parseNotesRec initialTags []
    let mapCard
        (cardSettingAndDeckTagByDeckId: Map<int64, CardSettingEntity * string>)
        (cardsAndTagsByNoteId: Map<int64, BranchInstanceEntity list * TagEntity list>)
        (colCreateDate: DateTime)
        userId
        (usersTags: TagEntity list)
        getCard
        (ankiCard: Anki.CardEntity) =
        let cardSetting, deckTag = cardSettingAndDeckTagByDeckId.[ankiCard.Did]
        let deckTag = usersTags.First(fun x -> x.Name = deckTag)
        let cards, tags = cardsAndTagsByNoteId.[ankiCard.Nid]
        let card = cards.Single(fun x -> x.AnkiNoteOrd = (ankiCard.Ord |> int16 |> Nullable))
        let cti = card.CollateInstance
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
                      BranchInstance = card
                      CollateInstance = cti
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
                            else cardSetting.NewCardsStepsInMinutes.Count() - (int ankiCard.Left % 1000)
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
                      CardSetting = cardSetting }
                getCard c
                |> function
                | Some entity ->
                    c.CopyTo entity
                    entity
                | None -> c.CopyToNew <| Seq.append [deckTag] tags
            ankiCard.Id, entity
        )

namespace CardOverflow.Api

open NeoSmart.Utils
open System.Text.RegularExpressions
open CardOverflow.Entity.Anki
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
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
open NodaTime

type SimpleAnkiDb = {
    Cards: CardOverflow.Entity.Anki.CardEntity list
    Cols: ColEntity list
    Notes: NoteEntity list
    Revlogs: RevlogEntity list
}

type AnkiTemplateRevision = {
    AnkiId: int64
    AuthorId: Guid
    Name: string
    Css: string
    Fields: Field list
    Created: Instant
    Modified: Instant option
    DefaultTags: string list
    DefaultCardSettingId: Guid
    LatexPre: string
    LatexPost: string
    CardTemplates: CardTemplate list
    DeckId: int64
    IsCloze: bool
} with
    member this.CopyTo (entity: TemplateRevisionEntity) =
        entity.Name <- this.Name
        entity.Css <- this.Css
        entity.Fields <- Fields.toString this.Fields
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.LatexPre <- this.LatexPre
        entity.LatexPost <- this.LatexPost
        entity.CardTemplates <- this.CardTemplates |> CardTemplate.copyToMany
        entity.EditSummary <- "Imported from Anki"
        entity.Type <- if this.IsCloze then 1s else 0s
    member this.CopyToNewWithTemplate userId template defaultCardSetting =
        let entity = TemplateRevisionEntity()
        entity.User_TemplateRevisions <-
            [User_TemplateRevisionEntity(
                UserId = userId,
                DefaultTags = this.DefaultTags.ToArray(),
                DefaultCardSetting = defaultCardSetting)].ToList()
        entity.Template <- template
        this.CopyTo entity
        entity.AnkiId <- Nullable this.AnkiId
        entity
    member this.CopyToNew userId defaultCardSetting =
        this.CopyToNewWithTemplate userId (TemplateEntity(AuthorId = this.AuthorId)) defaultCardSetting
    
type AnkiCardWrite = {
    AnkiNoteId: int64
    Commields: CommeafEntity list
    Template: TemplateRevisionEntity
    FieldValues: string
    Created: Instant
    Modified: Instant option
    AuthorId: Guid
} with
    member this.CopyTo (entity: RevisionEntity) =
        entity.FieldValues <- this.FieldValues
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.TemplateRevision <- this.Template
        entity.AnkiNoteId <- Nullable this.AnkiNoteId
        entity.Commeaf_Revisions <-
            this.Commields
            |> List.map (fun cf -> Commeaf_RevisionEntity(Revision = entity, Commeaf = cf))
            |> toResizeArray
    member this.CopyToNew (files: FileEntity seq) = // lowTODO add a tag indicating that it was imported from Anki
        let entity = RevisionEntity()
        entity.EditSummary <- "Imported from Anki"
        let concept = ConceptEntity(AuthorId = this.AuthorId)
        entity.Concept <- concept
        entity.Example <-
            ExampleEntity(
                Concept = concept,
                AuthorId = this.AuthorId
            )
        entity.File_Revisions <-
            files.Select(fun x ->
                File_RevisionEntity(
                    Revision = entity,
                    File = x
                )
            ).ToList()
        this.CopyTo entity
        entity
    member this.CollectedEquality (db: CardOverflowDb) (hasher: SHA512) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        let templateHash = this.Template |> TemplateRevisionEntity.hash hasher
        let hash = this.CopyToNew [] |> RevisionEntity.hash templateHash hasher
        db.Revision
            .Include(fun x -> x.Commeaf_Revisions :> IEnumerable<_>)
                .ThenInclude(fun (x: Commeaf_RevisionEntity) -> x.Commeaf)
            .OrderBy(fun x -> x.Created)
            .FirstOrDefault(fun c -> c.Hash = hash)
        |> Option.ofObj

type AnkiCard = {
    UserId: Guid
    Revision: RevisionEntity
    TemplateRevision: TemplateRevisionEntity
    Index: int16
    CardState: CardState
    IsLapsed: bool
    LapseCount: byte
    EaseFactorInPermille: int16
    IntervalOrStepsIndex: IntervalOrStepsIndex
    Due: Instant
    Deck: DeckEntity
    CardSetting: CardSettingEntity
} with
    member this.CopyToX (entity: CardEntity) i =
        entity.Deck <- this.Deck
        entity.UserId <- this.UserId
        entity.Index <- i
        entity.CardState <- CardState.toDb this.CardState
        entity.IsLapsed <- this.IsLapsed
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.IntervalOrStepsIndex <- IntervalOrStepsIndex.intervalToDb this.IntervalOrStepsIndex
        entity.Due <- this.Due
    member this.CopyToNew i =
        let entity = CardEntity()
        this.CopyToX entity i
        entity.Concept <- this.Revision.Example.Concept
        entity.Example <- this.Revision.Example
        entity.Revision <- this.Revision
        entity.CardSetting <- this.CardSetting
        entity
    member this.CollectedEquality (db: CardOverflowDb) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        db.Card
            .SingleOrDefault(fun c ->
                this.UserId = c.UserId &&
                this.Index = c.Index &&
                this.Revision.Id = c.RevisionId
            )

type AnkiHistory = {
    UserId: Guid
    Card: CardEntity option
    Score: int16
    Timestamp: Instant
    IntervalWithUnusedStepsIndex: IntervalOrStepsIndex
    EaseFactorInPermille: int16
    TimeFromSeeingQuestionToScoreInSecondsMinus32768: int16
} with
    member this.CollectedEquality (db: CardOverflowDb) = // lowTODO ideally this method only does the equality check, but I can't figure out how to get F# quotations/expressions working
        let interval = this.IntervalWithUnusedStepsIndex |> IntervalOrStepsIndex.intervalToDb
        match this.Card with
        | Some cc ->
            db.History.FirstOrDefault(fun h -> 
                this.UserId = h.UserId &&
                Nullable cc.RevisionId = h.RevisionId &&
                this.Score = h.Score &&
                this.Timestamp = h.Created &&
                interval = h.IntervalWithUnusedStepsIndex &&
                this.EaseFactorInPermille = h.EaseFactorInPermille &&
                this.TimeFromSeeingQuestionToScoreInSecondsMinus32768 = h.TimeFromSeeingQuestionToScoreInSecondsPlus32768
            )
        | None ->
            db.History.FirstOrDefault(fun h ->
                this.UserId = h.UserId &&
                this.Score = h.Score &&
                this.Timestamp = h.Created &&
                interval = h.IntervalWithUnusedStepsIndex &&
                this.EaseFactorInPermille = h.EaseFactorInPermille &&
                this.TimeFromSeeingQuestionToScoreInSecondsMinus32768 = h.TimeFromSeeingQuestionToScoreInSecondsPlus32768
            )
    member this.CopyTo (entity: HistoryEntity) =
        entity.Card <- this.Card |> Option.toObj
        entity.Score <- this.Score
        entity.Created <- this.Timestamp
        entity.IntervalWithUnusedStepsIndex <- this.IntervalWithUnusedStepsIndex |> IntervalOrStepsIndex.intervalToDb
        entity.EaseFactorInPermille <- this.EaseFactorInPermille
        entity.TimeFromSeeingQuestionToScoreInSecondsPlus32768 <- this.TimeFromSeeingQuestionToScoreInSecondsMinus32768
        entity.Revision <- this.Card |> Option.map (fun x -> x.Revision) |> Option.toObj
        entity.UserId <- this.UserId
        entity.Index <- this.Card |> Option.map (fun x -> x.Index) |> Option.defaultValue 0s
    member this.CopyToNew =
        let history = HistoryEntity()
        history.Id <- Guid.Empty
        this.CopyTo history
        history

type AnkiCardType = | New | Learning | Due // | Filtered

module Anki =
    let toHistory userId (cardByAnkiId: Map<int64, CardEntity>) getHistory (revLog: RevlogEntity) =
        result {
            let! score =
                match revLog.Ease with
                | 1L -> Ok Again
                | 2L -> Ok Hard
                | 3L -> Ok Good
                | 4L -> Ok Easy
                | _ -> Error <| sprintf "Unrecognized Anki revlog ease: %i" revLog.Ease
            let history = {
                UserId = userId
                Card =
                    cardByAnkiId
                    |> Map.tryFind revLog.Cid
                EaseFactorInPermille = int16 revLog.Factor
                IntervalWithUnusedStepsIndex =
                    match revLog.Ivl with
                    | p when p > 0L -> p |> float |> Duration.FromDays    // In Anki, positive is days
                    | n             -> n |> float |> Duration.FromSeconds // In Anki, negative is seconds
                    |> IntervalXX
                Score = score |> Score.toDb
                TimeFromSeeingQuestionToScoreInSecondsMinus32768 =
                    revLog.Time / 1000L - 32768L |> int16
                Timestamp =
                    Instant.FromUnixTimeMilliseconds revLog.Id
            }
            return
                getHistory history
                |> Option.defaultWith (fun () -> history.CopyToNew)
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
            { Id = Guid.Empty // lowTODO this entire record needs to be validated for out of range values
              Name = get.Required.Field "name" Decode.string
              IsDefault = false
              NewCardsSteps = get.Required.At ["new"; "delays"] (Decode.array Decode.float) |> Array.map Duration.FromMinutes |> List.ofArray
              NewCardsMaxPerDay = get.Required.At ["new"; "perDay"] Decode.int |> int
              NewCardsGraduatingInterval = get.Required.At ["new"; "ints"] (Decode.array Decode.float) |> Array.map Duration.FromDays |> Seq.item 0
              NewCardsEasyInterval = get.Required.At ["new"; "ints"] (Decode.array Decode.float) |> Array.map Duration.FromDays |> Seq.item 1
              NewCardsStartingEaseFactor = (get.Required.At ["new"; "initialFactor"] Decode.float) / 1000.
              NewCardsBuryRelated = get.Required.At ["new"; "bury"] Decode.bool
              MatureCardsMaxPerDay = get.Required.At ["rev"; "perDay"] Decode.int |> int
              MatureCardsEaseFactorEasyBonusFactor = get.Required.At ["rev"; "ease4"] Decode.float
              MatureCardsIntervalFactor = get.Required.At ["rev"; "ivlFct"] Decode.float
              MatureCardsMaximumInterval = get.Required.At ["rev"; "maxIvl"] Decode.float |> Duration.FromDays
              MatureCardsHardIntervalFactor = get.Optional.At ["rev"; "hardFactor"] Decode.float |? 1.2
              MatureCardsBuryRelated = get.Required.At ["rev"; "bury"] Decode.bool
              LapsedCardsSteps = get.Required.At ["lapse"; "delays"] (Decode.array Decode.float) |> Array.map Duration.FromMinutes |> List.ofArray
              LapsedCardsNewIntervalFactor = get.Required.At ["lapse"; "mult"] Decode.float
              LapsedCardsMinimumInterval = get.Required.At ["lapse"; "minInt"] Decode.float |> Duration.FromDays
              LapsedCardsLeechThreshold = get.Required.At ["lapse"; "leechFails"] Decode.int |> int
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
            let templates =
                get.Required.Field "tmpls" (Decode.object(fun g ->
                            { Id = Guid.NewGuid()
                              Name = g.Required.Field "name" Decode.string
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
                          IsSticky = get.Required.Field "sticky" Decode.bool },
                        get.Required.Field "ord" Decode.int)
                        |> Decode.list)
                        |> List.sortBy snd
                        |> List.map fst
                CardTemplates = templates
                Created = get.Required.Field "id" Decode.int64 |> Instant.FromUnixTimeMilliseconds
                Modified = get.Required.Field "mod" Decode.int64 |> Instant.FromUnixTimeSeconds |> Some
                DefaultTags = [] // lowTODO the caller should pass in these values, having done some preprocessing on the JSON string to add and retrieve the tag ids
                DefaultCardSettingId = Guid.Empty
                LatexPre = get.Required.Field "latexPre" Decode.string
                LatexPost = get.Required.Field "latexPost" Decode.string
                DeckId = get.Required.Field "did" Decode.int64
                IsCloze = get.Required.Field "type" ankiIntToBool
            })
        |> Decode.keyValuePairs
        |> Decode.fromString
    type ImgRegex = FSharp.Text.RegexProvider.Regex< """<img src="(?<ankiFileName>[^"]+)".*?>""" >
    type SoundRegex = FSharp.Text.RegexProvider.Regex< """\[sound:(?<ankiFileName>.+?)\]""" >
    let imgRegex =
        Regex.compiledIgnoreCase |> ImgRegex
    let soundRegex =
        Regex.compiledIgnoreCase |> SoundRegex
    let replaceAnkiFilenames field (fileEntityByAnkiFileName: Map<string, FileEntity>) =
        (([], field), imgRegex.TypedMatches field)
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
        |> fun x -> (x, soundRegex.TypedMatches field)
        ||> Seq.fold (fun (files, field) m ->
            let ankiFileName = m.ankiFileName.Value
            if fileEntityByAnkiFileName |> Map.containsKey ankiFileName
            then
                let file = fileEntityByAnkiFileName.[ankiFileName]
                let field = soundRegex.Replace(field, """
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
        (templateByModelId: Map<string, {| Entity: TemplateRevisionEntity; Template: AnkiTemplateRevision |}>)
        initialTags
        userId
        fileEntityByAnkiFileName
        getCard = // lowTODO use tail recursion
        let rec parseNotesRec (tags: string list) cardsAndTagsByNoteId =
            function
            | (note: NoteEntity) :: tail ->
                let notesTags =
                    note.Tags.Split ' '
                    |> Array.map String.trim
                    |> Array.filter String.hasContent
                    |> Set.ofArray
                let allTags =
                    Set.difference
                        notesTags
                        (tags |> Set.ofList)
                    |> List.ofSeq
                    |> List.append tags
                    |> List.groupBy (fun x -> x.ToLower())
                    |> List.map (fun (_, x) -> x.First())
                let files, fieldValues = replaceAnkiFilenames note.Flds fileEntityByAnkiFileName
                let fieldValues = fieldValues |> MappingTools.splitByUnitSeparator
                let template = templateByModelId.[string note.Mid]
                let toCard fields template =
                    let c = {
                        AnkiNoteId = note.Id
                        Template = template
                        Commields = []
                        FieldValues = fields |> MappingTools.joinByUnitSeparator
                        Created = Instant.FromUnixTimeMilliseconds note.Id
                        Modified = Instant.FromUnixTimeSeconds note.Mod |> Some
                        AuthorId = userId }
                    defaultArg
                        <| getCard c
                        <| c.CopyToNew files
                let noteIdCardsAndTags =
                    let cards =
                        if template.Template.IsCloze then
                            toCard
                                <| fieldValues
                                <| template.Entity
                        else
                            toCard fieldValues template.Entity
                    let relevantTags = allTags |> List.filter notesTags.Contains
                    note.Id, (cards, relevantTags)
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
        (cardSettingAndDeckByDeckId: Map<int64, CardSettingEntity * DeckEntity>)
        (cardAndTagsByNoteId: Map<int64, RevisionEntity * (string list)>)
        (colCreateDate: Instant)
        userId
        getCard
        (ankiCard: Anki.CardEntity) =
        let cardSetting, deck = cardSettingAndDeckByDeckId.[ankiCard.Did]
        let card, _ = cardAndTagsByNoteId.[ankiCard.Nid]
        let cti = card.TemplateRevision
        match ankiCard.Type with
        | 0L -> Ok New
        | 1L -> Ok Learning
        | 2L -> Ok Due
        | 3L -> Error "Filtered decks are not supported. Please delete the filtered decks and upload the new export."
        | _ -> Error "Unexpected card type. Please contact support and attach the file you tried to import."
        |> Result.map (fun cardType ->
            let entity =
                let c: AnkiCard =
                    { UserId = userId
                      Revision = card
                      TemplateRevision = cti
                      Index = ankiCard.Ord |> int16
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
                            let diff = cardSetting.NewCardsSteps.Count() - (int ankiCard.Left % 1000)
                            if ankiCard.Left = 0L || diff < 0
                            then 0
                            else diff
                            |> byte |> NewStepsIndex
                        | Due ->
                            if ankiCard.Ivl > 0L
                            then ankiCard.Ivl |> float |> Duration.FromDays
                            else float ankiCard.Ivl * -1. / 60. |> Math.Round |> float |> Duration.FromMinutes
                            |> IntervalXX
                      Due =
                        match cardType with
                        | New -> DateTimeX.UtcNow
                        | Learning -> Instant.FromUnixTimeSeconds ankiCard.Due
                        | Due ->
                            if ankiCard.Odue = 0L
                            then ankiCard.Due
                            else ankiCard.Odue
                            |> float
                            |> Duration.FromDays
                            |> (+) colCreateDate
                      Deck = deck
                      CardSetting = cardSetting }
                getCard c
                |> function
                | Some entity ->
                    c.CopyToX entity (int16 ankiCard.Ord)
                    entity
                | None -> c.CopyToNew (int16 ankiCard.Ord)
            ankiCard.Id, entity
        )

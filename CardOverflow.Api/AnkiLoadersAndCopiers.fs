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
    QuestionXemplate: string
    AnswerXemplate: string
    ShortQuestionXemplate: string
    ShortAnswerXemplate: string
    DeckId: int64
    IsCloze: bool
    Ordinal: int16
    NewByOldField: Map<Field, Field>
    FieldsByOrdinal: Map<int16, Field list>
    CommunalFields: Field list
} with
    member this.CopyTo (entity: CollateInstanceEntity) =
        entity.Name <- this.Name
        entity.Css <- this.Css
        entity.Fields <- Fields.toString this.Fields
        entity.Created <- this.Created
        entity.Modified <- this.Modified |> Option.toNullable
        entity.LatexPre <- this.LatexPre
        entity.LatexPost <- this.LatexPost
        entity.QuestionXemplate <- this.QuestionXemplate
        entity.AnswerXemplate <- this.AnswerXemplate
        entity.ShortQuestionXemplate <- this.ShortQuestionXemplate
        entity.ShortAnswerXemplate <- this.ShortAnswerXemplate
        entity.EditSummary <- "Imported from Anki"
    member this.CopyToNew userId defaultCardSetting =
        let entity = CollateInstanceEntity()
        entity.User_CollateInstances <-
            [User_CollateInstanceEntity(
                UserId = userId,
                Tag_User_CollateInstances =
                    (this.DefaultTags
                        .Select(fun x -> Tag_User_CollateInstanceEntity(UserId = userId, DefaultTagId = x))
                        .ToList()),
                DefaultCardSetting = defaultCardSetting)].ToList()
        entity.Collate <-
            CollateEntity(
                AuthorId = this.AuthorId)
        this.CopyTo entity
        entity.AnkiId <- Nullable this.AnkiId
        entity
    
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
    type TempCollate = {
        Collate: AnkiCollateInstance
        NewByOldField: (Field * Field) list
        ReducedFields: Field list
        FieldsByOrdinal: (int16 * Field list) list
    } with
        member this.QuestionXemplate = this.Collate.QuestionXemplate
        member this.AnswerXemplate = this.Collate.AnswerXemplate
        member this.Ordinal = this.Collate.Ordinal
    let parseModels userId =
        let reduceCollate (collates: AnkiCollateInstance list) =
            let collates = collates |> List.map (fun x -> { Collate = x; NewByOldField = []; ReducedFields = x.Fields; FieldsByOrdinal = [] })
            let contains questionXemplate answerXemplate fieldName =
                let qa = MappingTools.joinByUnitSeparator [questionXemplate; answerXemplate]
                qa.Contains("{{" + fieldName + "}}", StringComparison.OrdinalIgnoreCase) ||
                qa.Contains("{{type:" + fieldName + "}}", StringComparison.OrdinalIgnoreCase)
            let communalFields =
                collates.Head.ReducedFields
                |> List.filter (fun field ->
                    collates.Count(fun t -> contains t.QuestionXemplate t.AnswerXemplate field.Name)
                    |> function
                    | 1 -> false
                    | _ -> true
                    || collates.Any(fun t -> t.Collate.IsCloze))
            let communalFieldNames = communalFields |> List.map (fun x -> x.Name)
            let rec reduceLoop collates =
                let combine (newT: TempCollate) (oldT: TempCollate) =
                    let newQA = [newT.QuestionXemplate; newT.AnswerXemplate] |> joinByUnitSeparator
                    let oldQA = [oldT.QuestionXemplate; oldT.AnswerXemplate] |> joinByUnitSeparator
                    if  standardizeWhitespace (BraceRegex().Replace(newQA, "")) =
                        standardizeWhitespace (BraceRegex().Replace(oldQA, "")) then
                        let getFields x =
                            BraceRegex().TypedMatches(x)
                                |> List.ofSeq
                                |> List.map(fun x -> x.fieldName.Value)
                                |> List.filter(fun x ->
                                    if x.[0] = '^' then failwith "^ currently not supported, yell at me to fix this medTODO"
                                    x.[0] <> '#' &&
                                    x.[0] <> '/' &&
                                    not <| String.Equals(x, "FrontSide", StringComparison.OrdinalIgnoreCase) &&
                                    not <| x.StartsWith("hint:", StringComparison.OrdinalIgnoreCase))
                                |> List.distinct
                        let fieldByName = oldT.ReducedFields |> List.map (fun x -> x.Name, x) |> Map.ofList
                        let newFields = getFields newQA |> List.map (fun m -> fieldByName.[m])
                        let oldFields = getFields oldQA |> List.map (fun m -> fieldByName.[m])
                        if newFields.Length <> oldFields.Length then failwith "Field counts should be the same"
                        { newT with
                            ReducedFields =
                                oldT.ReducedFields
                                |> List.filter (fun f ->
                                    not <| oldFields.Any(fun x -> x.Name = f.Name) ||
                                    communalFieldNames.Contains f.Name)
                            NewByOldField =
                                newT.NewByOldField @
                                oldT.NewByOldField @
                                List.zip communalFields communalFields @
                                List.zip oldFields newFields
                            FieldsByOrdinal =
                                newT.FieldsByOrdinal @
                                oldT.FieldsByOrdinal @
                                [newT.Ordinal, newFields @ communalFields |> List.distinct] @
                                [oldT.Ordinal, oldFields @ communalFields |> List.distinct]
                        } |> Some
                    else
                        None
                combination 2 collates
                |> Seq.tryPick (fun x ->
                    let a = x.[0]
                    let b = x.[1]
                    combine a b
                    |> Option.map (fun x -> x :: collates |> List.filter(fun x -> x <> a && x <> b)))
                |> function
                | Some x -> reduceLoop x
                | None -> collates
            let collates = reduceLoop collates
            let collateSpecificFieldsByCollate =
                let usedFieldsByCollate =
                    collates |> List.map(fun t ->
                        let qa = [t.QuestionXemplate; t.AnswerXemplate] |> joinByUnitSeparator
                        t, BraceRegex().TypedMatches(qa).Select(fun x -> x.fieldName.Value) |> List.ofSeq
                    ) |> Map.ofList
                collates |> List.map(fun t ->
                    let otherCollatesUsedFields = usedFieldsByCollate |> Map.toList |> List.filter (fun (x, _) -> x <> t) |> List.collect snd
                    t, usedFieldsByCollate.[t] |> List.filter (fun x -> not <| otherCollatesUsedFields.Contains x))
            let oldFields = collates |> List.collect (fun x -> x.NewByOldField) |> List.map fst
            collates |> List.map (fun t ->
                let otherCollateSpecificFields = collateSpecificFieldsByCollate |> List.filter (fun (x, _) -> x <> t) |> List.collect snd
                let fields =
                    t.ReducedFields
                    |> List.filter (fun x ->
                        (not <| otherCollateSpecificFields.Contains x.Name && not <| oldFields.Contains x) ||
                        communalFieldNames.Contains x.Name
                    )
                    |> List.sortBy (fun x -> x.Ordinal)
                    |> List.mapi (fun i x -> { x with Ordinal = byte i})
                { t.Collate with
                    NewByOldField =
                        if List.isEmpty t.NewByOldField then
                            List.zip t.Collate.Fields t.Collate.Fields
                        else
                            t.NewByOldField
                        |> Map.ofList
                    FieldsByOrdinal =
                        if List.isEmpty t.FieldsByOrdinal then
                            [(t.Ordinal, fields)]
                        else
                            t.FieldsByOrdinal
                        |> Map.ofList
                    Fields = fields
                    CommunalFields = communalFields})
        Decode.object(fun get ->
            let collates =
                get.Required.Field "tmpls" (Decode.object(fun g ->
                            {|Name = g.Required.Field "name" Decode.string
                              QuestionXemplate = g.Required.Field "qfmt" Decode.string
                              AnswerXemplate = g.Required.Field "afmt" Decode.string
                              ShortQuestionXemplate = g.Required.Field "bqfmt" Decode.string
                              ShortAnswerXemplate = g.Required.Field "bafmt" Decode.string
                              Ordinal = g.Required.Field "ord" Decode.int |> int16 |})
                              |> Decode.list )
                |> List.sortBy (fun x -> x.Ordinal)
            collates
            |> List.map(fun collate ->
                let namePostfix =
                    if collates.Count() >= 2
                    then " - " + collate.Name
                    else ""
                {   AnkiId = get.Required.Field "id" Decode.int64
                    Ordinal = collate.Ordinal
                    AuthorId = userId
                    Name = get.Required.Field "name" Decode.string + namePostfix
                    Css = get.Required.Field "css" Decode.string
                    Fields =
                        get.Required.Field "flds" (Decode.object(fun get ->
                            { Name = get.Required.Field "name" Decode.string
                              IsRightToLeft = get.Required.Field "rtl" Decode.bool
                              Ordinal = get.Required.Field "ord" Decode.int |> byte
                              IsSticky = get.Required.Field "sticky" Decode.bool })
                            |> Decode.list)
                    QuestionXemplate = collate.QuestionXemplate
                    AnswerXemplate = collate.AnswerXemplate
                    ShortQuestionXemplate = collate.ShortQuestionXemplate
                    ShortAnswerXemplate = collate.ShortAnswerXemplate
                    Created = get.Required.Field "id" Decode.int64 |> DateTimeOffset.FromUnixTimeMilliseconds |> fun x -> x.UtcDateTime
                    Modified = get.Required.Field "mod" Decode.int64 |> DateTimeOffset.FromUnixTimeSeconds |> fun x -> x.UtcDateTime |> Some
                    DefaultTags = [] // lowTODO the caller should pass in these values, having done some preprocessing on the JSON string to add and retrieve the tag ids
                    DefaultCardSettingId = 0
                    LatexPre = get.Required.Field "latexPre" Decode.string
                    LatexPost = get.Required.Field "latexPost" Decode.string
                    DeckId = get.Required.Field "did" Decode.int64
                    IsCloze = get.Required.Field "type" ankiIntToBool
                    NewByOldField = Map.empty
                    FieldsByOrdinal = Map.empty
                    CommunalFields = []
                }) |> reduceCollate)
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
        (collatesByModelId: Map<string, {| Entity: CollateInstanceEntity; Collate: AnkiCollateInstance |} list>)
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
                let collates = collatesByModelId.[string note.Mid]
                let communalFields =
                    collates
                    |> List.collect(fun x -> x.Collate.CommunalFields)
                    |> List.distinct
                    |> List.map (fun field ->
                        CommunalFieldInstanceEntity(
                            FieldName = field.Name,
                            Value = fieldValues.[int field.Ordinal],
                            EditSummary = "Imported from Anki",
                            CommunalField = CommunalFieldEntity(AuthorId = userId),
                            Created = DateTime.UtcNow,
                            CommunalFieldInstance_BranchInstances = [].ToList()))
                let toCard fields collate noteOrd =
                    let c = {
                        AnkiNoteId = note.Id
                        AnkiNoteOrd = noteOrd
                        Collate = collate
                        CommunalFields = communalFields
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
                            if collates.First().Collate.IsCloze then result {
                                let collate = collates |> Seq.exactlyOne
                                let valueByFieldName =
                                    Seq.zip
                                        <| collate.Collate.Fields.OrderBy(fun x -> x.Ordinal).Select(fun f -> f.Name)
                                        <| fieldValues
                                    |> Map.ofSeq
                                let! max =
                                    AnkiImportLogic.maxClozeIndex
                                        <| sprintf "Anki Note Id #%s is malformed. It claims to be a cloze deletion but doesn't have the syntax of one. Its fields are: %s" (string note.Id) (String.Join(',', fieldValues))
                                        <| valueByFieldName
                                        <| collate.Collate.QuestionXemplate
                                return [1 .. max] |> List.map int16 |> List.map (fun clozeIndex ->
                                    toCard
                                        <| AnkiImportLogic.multipleClozeToSingleClozeList clozeIndex fieldValues
                                        <| collate.Entity
                                        <| clozeIndex - 1s // ankidb's cards' ord column is 0 indexed for cloze deletions
                                )}
                            else
                                let instances = collates |> List.collect (fun anon ->
                                    let collate = anon.Collate
                                    let newByOldField = collate.NewByOldField
                                    let oldFields = newByOldField |> Map.toList |> List.map fst
                                    let newFields = newByOldField |> Map.toList |> List.map snd
                                    collate.FieldsByOrdinal
                                    |> Map.toList
                                    |> List.map (fun (collateOrdinal, fields) ->
                                        let fields =
                                            fields |> List.map (fun f ->
                                                oldFields
                                                |> Seq.filter (fun x -> x.Name = f.Name)
                                                |> Seq.tryExactlyOne
                                                |> function
                                                | None ->
                                                    let field =
                                                        newFields
                                                        |> Seq.filter (fun x -> x.Name = f.Name)
                                                        |> Seq.tryHead
                                                        |> Option.defaultValue f
                                                    field.Ordinal, field.Ordinal
                                                | Some field ->
                                                    newByOldField.[field].Ordinal, field.Ordinal
                                            ) |> List.sortBy (fun (newOrdinal, _) -> newOrdinal)
                                            |> List.map (fun (_, oldOrdinal) -> fieldValues.[int oldOrdinal])
                                        toCard fields anon.Entity collateOrdinal
                                    ))
                                instances |> Ok
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

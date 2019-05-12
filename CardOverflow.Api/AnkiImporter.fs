namespace CardOverflow.Api

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Entity.Anki
open System
open System.Linq
open Thoth.Json.Net

type SimpleAnkiDb = {
    Cards: CardEntity list
    Cols: ColEntity list
    Notes: NoteEntity list
    Revlogs: RevlogEntity list
}

module AnkiImporter =
    let getSimpleAnkiDb (ankiDbService: AnkiDbService) =
        { Cards = ankiDbService.Query(fun db -> db.Cards.ToList() ) |> List.ofSeq
          Cols = ankiDbService.Query(fun db -> db.Cols.ToList() ) |> List.ofSeq
          Notes = ankiDbService.Query(fun db -> db.Notes.ToList() ) |> List.ofSeq
          Revlogs = ankiDbService.Query(fun db -> db.Revlogs.ToList() ) |> List.ofSeq }

type AnkiConceptWrite = {
    Title: string
    Description: string
    ConceptTemplate: ConceptTemplateEntity
    Fields: string list
    Modified: DateTime
} with
    member this.CopyTo(entity: ConceptEntity) =
        entity.Title <- this.Title
        entity.Description <- this.Description
        entity.ConceptTemplate <- this.ConceptTemplate
        entity.Fields <- this.Fields |> MappingTools.joinByUnitSeparator
        entity.Modified <- this.Modified
    member this.CopyToNew (privateTagConcepts: seq<PrivateTagEntity>) =
        let entity = ConceptEntity()
        this.CopyTo entity
        entity.PrivateTagConcepts <- privateTagConcepts.Select(fun x -> PrivateTagConceptEntity(Concept = entity, PrivateTag = x)).ToList()
        entity

type AnkiImporter(ankiDb: SimpleAnkiDb, dbService: IDbService, userId: int) =
    let ankiIntToBool =
        Decode.int
        |> Decode.andThen (fun i ->
            match i with
            | 0 -> Decode.succeed false
            | 1 -> Decode.succeed true
            | _ -> "Unexpected number when parsing Anki value: " + string i |> Decode.fail )
    let parseDconf =
        Decode.object(fun get -> // medTODO input validation
            { Id = 0
              Name = get.Required.Field "name" Decode.string
              NewCardsSteps = get.Required.At ["new"; "delays"] (Decode.array Decode.float) |> Array.map TimeSpan.FromMinutes |> List.ofArray
              NewCardsMaxPerDay = get.Required.At ["new"; "perDay"] Decode.int |> int16
              NewCardsGraduatingInterval = get.Required.At ["new"; "ints"] (Decode.array Decode.float) |> Array.map TimeSpan.FromDays |> Seq.item 0
              NewCardsEasyInterval = get.Required.At ["new"; "ints"] (Decode.array Decode.float) |> Array.map TimeSpan.FromDays |> Seq.item 1
              NewCardsStartingEaseFactor = (get.Required.At ["new"; "initialFactor"] Decode.float) / 1000.0
              NewCardsBuryRelated = get.Required.At ["new"; "bury"] Decode.bool
              MatureCardsMaxPerDay = get.Required.At ["rev"; "perDay"] Decode.int |> int16
              MatureCardsEaseFactorEasyBonusFactor = get.Required.At ["rev"; "ease4"] Decode.float
              MatureCardsIntervalFactor = get.Required.At ["rev"; "ivlFct"] Decode.float
              MatureCardsMaximumInterval = get.Required.At ["rev"; "maxIvl"] Decode.float |> TimeSpan.FromDays
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
    let parseModels =
        Decode.object(fun get ->
            { Id = 0
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
                      Ordinal = g.Required.Field "ord" Decode.int |> byte
                      DefaultCardOptionId = get.Required.Field "did" Decode.int * -1 }) // temp value that will be overwritten later highTODO actually do this
                      |> Decode.list )
              Modified = get.Required.Field "mod" Decode.int64 |> DateTimeOffset.FromUnixTimeMilliseconds |> fun x -> x.UtcDateTime
              IsCloze = get.Required.Field "type" ankiIntToBool
              DefaultPublicTags = []
              DefaultPrivateTags = [] // lowTODO the caller should pass in these values, having done some preprocessing on the JSON string to add and retrieve the tag ids
              LatexPre = get.Required.Field "latexPre" Decode.string
              LatexPost = get.Required.Field "latexPost" Decode.string })
        |> Decode.keyValuePairs
        |> Decode.fromString
    let rec parseNotes (conceptTemplatesByModelId: Map<string, ConceptTemplateEntity>) tags conceptsByNoteId =
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
                  Modified = DateTimeOffset.FromUnixTimeSeconds(note.Mod).UtcDateTime }.CopyToNew
                  (allTags.Where(fun x -> notesTags.Contains x.Name))
            parseNotes conceptTemplatesByModelId allTags ((note.Id, concept)::conceptsByNoteId) tail
        | _ -> conceptsByNoteId

    let mapCard (getCardOption: int -> CardOption * CardOptionEntity) (conceptsByAnkiId: Map<int64, ConceptEntity>) (colCreateDate: DateTime) (ankiCard: Anki.CardEntity) =
        let cardOption, cardOptionEntity = int ankiCard.Did |> getCardOption
        match ankiCard.Type with
        | 0L -> Ok MemorizationState.New
        | 1L -> Ok MemorizationState.Learning
        | 2L -> Ok MemorizationState.Mature
        | 3L -> Error "Filtered decks are not supported. Please delete the filtered decks and upload the new export."
        | _ -> Error "Unexpected card type. Please contact support and attach the file you tried to import."
        |> Result.map (fun memorizationState ->
            { Card.Id = 0
              ConceptId = conceptsByAnkiId.[ankiCard.Nid].Id
              MemorizationState = memorizationState
              CardState =
                match ankiCard.Queue with
                | -3L -> CardState.UserBuried
                | -2L -> CardState.SchedulerBuried
                | -1L -> CardState.Suspended
                | _ -> CardState.Normal
              LapseCount = ankiCard.Lapses |> byte // medTODO validate these, eg 9999 will yield 15
              EaseFactorInPermille = ankiCard.Factor |> int16
              IntervalNegativeIsMinutesPositiveIsDays =
                if ankiCard.Ivl > 0L
                then ankiCard.Ivl |> int16
                else float ankiCard.Ivl * -1.0 / 60.0 |> Math.Round |> int16
              StepsIndex =
                match memorizationState with
                | MemorizationState.New
                | MemorizationState.Learning ->
                    if ankiCard.Left = 0L
                    then 0
                    else cardOption.NewCardsSteps.Count() - (int ankiCard.Left % 1000)
                    |> byte |> Some // medTODO handle importing of lapsed cards, this assumes it's a new card
                | MemorizationState.Mature -> None
              Due =
                match memorizationState with
                | MemorizationState.New -> DateTime.UtcNow.Date
                | MemorizationState.Learning -> DateTimeOffset.FromUnixTimeSeconds(ankiCard.Due).UtcDateTime
                | MemorizationState.Mature -> colCreateDate + TimeSpan.FromDays(float ankiCard.Due)
              TemplateIndex = ankiCard.Ord |> byte
              CardOptionId = cardOptionEntity.Id }.CopyToNew)

    member __.run() = // medTODO it should be possible to present to the user import errors *before* importing anything.
        let col = ankiDb.Cols.Single()
        ResultBuilder() {
            let! cardOptionByDeckConfigurationId =
                parseDconf col.Dconf
                |> Result.bind (List.map (fun (deckConfigurationId, cardOption) -> (deckConfigurationId, (cardOption, cardOption.CopyToNew userId))) >> Map.ofList >> Ok )
            let! nameAndDeckConfigurationIdByDeckId =
                parseDecks col.Decks
                |> Result.bind (fun tuples ->
                    let names = tuples |> List.map(fun (_, (_, name, _)) -> name)
                    if names |> List.distinct |> List.length = names.Count()
                    then tuples |> List.map snd |> Ok
                    else Error "Cannot import decks with the same name. Please give your decks distinct names." ) // lowTODO list the decks with the same names
                |> Result.bind (fun tuples ->
                    let filtered = tuples |> List.filter (fun (_, _, i) -> i.IsSome)
                    if filtered |> List.length = tuples.Count()
                    then filtered |> List.map (fun (id, name, conf) -> (id, (name, conf.Value))) |> Map.ofList |> Ok
                    else Error "Cannot import filtered decks. Please delete all filtered decks - they're temporary https://apps.ankiweb.net/docs/am-manual.html#filtered-decks" ) // lowTODO name the filtered decks
            let getCardOption deckId =
                let (_, deckConfigurationId) = nameAndDeckConfigurationIdByDeckId.[deckId] // medTODO tag imported cards with the name of the deck they're in
                cardOptionByDeckConfigurationId.[string deckConfigurationId]
            let! conceptTemplatesByModelId =
                parseModels col.Models
                |> Result.map (Seq.map(fun (key, value) -> (key, value.CopyToNew userId)) >> Map.ofSeq )
            let conceptsByAnkiId =
                parseNotes
                    conceptTemplatesByModelId
                    (dbService.Query(fun db -> db.PrivateTags.Where(fun pt -> pt.UserId = userId).ToList()) |> Seq.toList)
                    []
                    (ankiDb.Notes)
                |> Map.ofList
            let collectionCreationTimeStamp = DateTimeOffset.FromUnixTimeSeconds(col.Crt).UtcDateTime
            let getCardEntities() =
                ankiDb.Cards
                |> List.map (mapCard getCardOption conceptsByAnkiId collectionCreationTimeStamp)
                |> Result.consolidate
            let! _ = getCardEntities() // checking if there are any errors

            dbService.Command(fun db -> conceptsByAnkiId |> Map.toSeq |> Seq.map snd |> db.Concepts.AddRange)
            dbService.Command(fun db -> cardOptionByDeckConfigurationId |> Map.toSeq |> Seq.map snd |> Seq.map snd |> db.CardOptions.AddRange) // EF updates the options' Ids
            let! cardEntities = getCardEntities() // called again to update the Card's Option Id (from the line above)
            dbService.Command(fun db -> cardEntities |> db.Cards.AddRange)
            return ()
        }

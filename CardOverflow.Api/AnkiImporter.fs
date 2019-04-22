namespace CardOverflow.Api

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Entity.Anki
open System
open System.Linq
open Thoth.Json.Net

type AnkiConceptWrite = {
    Id: int
    Title: string
    Description: string
    ConceptTemplateId: int
    Fields: string list
    Modified: DateTime
} with
    member this.CopyTo(entity: ConceptEntity) =
        entity.Title <- this.Title
        entity.Description <- this.Description
        entity.ConceptTemplateId <- this.ConceptTemplateId
        entity.Fields <- this.Fields |> MappingTools.joinByUnitSeparator
        entity.Modified <- this.Modified
    member this.CopyToNew (privateTagConcepts: seq<PrivateTagEntity>) =
        let entity = ConceptEntity()
        this.CopyTo entity
        entity.PrivateTagConcepts <- privateTagConcepts.Select(fun x -> PrivateTagConceptEntity(Concept = entity, PrivateTag = x)).ToList()
        entity

type AnkiImporter(ankiDbService: AnkiDbService, dbService: DbService, userId: int) =
    let ankiIntToBool =
        Decode.int
        |> Decode.andThen (fun i ->
            match i with
            | 0 -> Decode.succeed false
            | 1 -> Decode.succeed true
            | _ -> "Unexpected number when parsing Anki value: " + string i |> Decode.fail )
    member __.run() =
        let col = ankiDbService.Query(fun x -> x.Cols.Single())
        ResultBuilder() {
            let! cardOptionByDeckConfigurationId =
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
                <| col.Dconf
                |> Result.bind(fun cardOptions ->
                    let entitiesByDeckConfigurationId = cardOptions |> List.map (fun (deckConfigurationId, cardOption) -> (deckConfigurationId, cardOption.CopyToNew userId))
                    dbService.Command(fun db -> List.map snd entitiesByDeckConfigurationId |> db.CardOptions.AddRange)
                    entitiesByDeckConfigurationId |> List.map (fun (deckConfigurationId, co) -> (deckConfigurationId, CardOption.Load co)) |> Map.ofList |> Ok ) // EF updates the entities' Ids which are then loaded into the records
            let! nameAndDeckConfigurationIdByDeckId =
                Decode.object(fun get ->
                    (get.Required.Field "id" Decode.int,
                     get.Required.Field "name" Decode.string,
                     get.Optional.Field "conf" Decode.int))
                |> Decode.keyValuePairs
                |> Decode.fromString
                <| col.Decks
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
            let! conceptTemplatesByModelId = 
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
                      CardTemplates = get.Required.Field "tmpls" (Decode.object(fun g ->
                        { Name = g.Required.Field "name" Decode.string
                          QuestionTemplate = g.Required.Field "qfmt" Decode.string
                          AnswerTemplate = g.Required.Field "afmt" Decode.string
                          ShortQuestionTemplate = g.Required.Field "bqfmt" Decode.string
                          ShortAnswerTemplate = g.Required.Field "bafmt" Decode.string
                          Ordinal = g.Required.Field "ord" Decode.int |> byte
                          DefaultCardOptionId =
                            let (_, deckConfigurationId) = nameAndDeckConfigurationIdByDeckId.[get.Required.Field "did" Decode.int] // medTODO tag imported cards with the name of the deck they're in
                            cardOptionByDeckConfigurationId.[string deckConfigurationId].Id })
                        |> Decode.list )
                      Modified = get.Required.Field "mod" Decode.int64 |> DateTimeOffset.FromUnixTimeMilliseconds |> fun x -> x.UtcDateTime
                      IsCloze = get.Required.Field "type" ankiIntToBool
                      DefaultPublicTags = []
                      DefaultPrivateTags = [] // lowTODO the caller should pass in these values, having done some preprocessing on the JSON string to add and retrieve the tag ids
                      LatexPre = get.Required.Field "latexPre" Decode.string
                      LatexPost = get.Required.Field "latexPost" Decode.string })
                |> Decode.keyValuePairs
                |> Decode.fromString
                <| col.Models
                |> Result.map (Seq.map(fun (key, value) -> (key, value.CopyToNew userId)) >> Map.ofSeq )
            dbService.Command(fun db -> db.ConceptTemplates.AddRange (conceptTemplatesByModelId |> Seq.map (fun x -> x.Value))) // EF updates the entities' Ids
            let rec getConceptsToAdd tags concepts =
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
                        { Id = 0
                          Title = ""
                          Description = ""
                          ConceptTemplateId = conceptTemplatesByModelId.[string note.Mid].Id
                          Fields = MappingTools.splitByUnitSeparator note.Flds
                          Modified = DateTimeOffset.FromUnixTimeSeconds(note.Mod).UtcDateTime }.CopyToNew
                          (allTags.Where(fun x -> notesTags.Contains x.Name))
                    getConceptsToAdd allTags (concept::concepts) tail
                | _ -> concepts
            let conceptsToAdd =
                getConceptsToAdd
                    (dbService.Query(fun x -> x.PrivateTags.Where(fun pt -> pt.UserId = userId).ToList()) |> Seq.toList)
                    []
                    (ankiDbService.Query(fun x -> x.Notes.ToList()) |> Seq.toList)
            dbService.Command(fun db -> db.Concepts.AddRange conceptsToAdd)
            return ()
        }

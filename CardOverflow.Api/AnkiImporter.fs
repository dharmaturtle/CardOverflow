namespace CardOverflow.Api

open LoadersAndCopiers
open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Entity.Anki
open System
open System.Linq
open Thoth.Json.Net
open FsToolkit.ErrorHandling
open Helpers
open System.IO
open System.IO.Compression
open System.Security.Cryptography
open System.Collections.Generic
open Microsoft.EntityFrameworkCore

module AnkiImporter =
    let getSimpleAnkiDb (db: AnkiDb) =
        { Cards = db.Cards.ToList() |> List.ofSeq
          Cols = db.Cols.ToList() |> List.ofSeq
          Notes = db.Notes.ToList() |> List.ofSeq
          Revlogs = db.Revlogs.ToList() |> List.ofSeq }
    let loadFiles (getFromDb: byte[] -> FileEntity option) zipPath =
        let fileNameAndFileByHash = Dictionary<byte[], (string * FileEntity)>()
        let getFile hash =
            if fileNameAndFileByHash.Keys.Any(fun x -> x = hash) // I don't think .Contains and its friends work with hashes/byte[] for some reason
            then fileNameAndFileByHash.First(fun (KeyValue(h, _)) -> h = hash) |> fun (KeyValue (_, (_, file))) -> file |> Some
            else getFromDb hash
        let zipFile = ZipFile.Open (zipPath, ZipArchiveMode.Read)
        use mediaStream = zipFile.Entries.First(fun x -> x.Name = "media").Open ()
        use mediaReader = new StreamReader (mediaStream)
        use hasher = SHA256.Create ()
        Decode.string
        |> Decode.keyValuePairs
        |> Decode.fromString
        <| mediaReader.ReadToEnd()
        |> Result.map(
            List.iter(fun (index, fileName) ->
                use fileStream = zipFile.Entries.First(fun x -> x.Name = index).Open()
                use m = new MemoryStream()
                fileStream.CopyTo m
                let array = m.ToArray() // lowTODO investigate if there are memory issues if someone uploads gigs, we might need to persist to the DB sooner
                let sha256 = hasher.ComputeHash array
                getFile sha256
                |> function
                | Some e -> e
                | None ->
                    FileEntity (
                        FileName = fileName,
                        Data = array,
                        Sha256 = sha256
                    )
                |> fun e -> fileNameAndFileByHash.Add(sha256, (fileName, e)))
            >> fun () -> fileNameAndFileByHash |> Map.overValue id |> Map.ofSeq)
    let load
        ankiDb
        userId
        fileEntityByAnkiFileName
        (usersTags: PrivateTagEntity seq)
        (cardOptions: CardOption seq)
        getConceptTemplates
        getConcept
        getCard
        getHistory =
        let col = ankiDb.Cols.Single()
        result {
            let! cardOptionByDeckConfigurationId =
                let toEntity _ (cardOption: CardOption) =
                    cardOptions
                    |> Seq.filter (fun x -> x.AcquireEquality cardOption)
                    |> Seq.tryHead
                    |> function
                    | Some x -> x
                    | None -> cardOption
                    |> fun co -> co.CopyToNew userId
                Anki.parseCardOptions col.Dconf
                |> Result.map (Map.ofList >> Map.map toEntity)
            let! deckNameAndDeckConfigurationIdByDeckId =
                Anki.parseDecks col.Decks
                |> Result.bind (fun tuples ->
                    let names = tuples |> List.map (fun (_, (_, name, _)) -> name)
                    if names |> List.distinct |> List.length = names.Length
                    then tuples |> List.map snd |> Ok
                    else Error "Cannot import decks with the same name. Please give your decks distinct names." ) // lowTODO list the decks with the same names
                |> Result.bind (fun tuples ->
                    let filtered = tuples |> List.filter (fun (_, _, i) -> i.IsSome)
                    if filtered.Length = tuples.Length
                    then filtered |> List.map (fun (id, name, conf) -> (id, (name, conf.Value))) |> Map.ofList |> Ok
                    else Error "Cannot import filtered decks. Please delete all filtered decks - they're temporary https://apps.ankiweb.net/docs/am-manual.html#filtered-decks" ) // lowTODO name the filtered decks
            let cardOptionAndDeckNameByDeckId =
                deckNameAndDeckConfigurationIdByDeckId
                |> Map.map (fun _ (deckName, deckConfigurationId) ->
                    cardOptionByDeckConfigurationId.[string deckConfigurationId], "Deck:" + deckName)
            let! conceptTemplatesByModelId =
                let toEntity _ (x: AnkiConceptTemplateAndDeckId) =
                    let defaultCardOption = cardOptionAndDeckNameByDeckId.[x.DeckId] |> fst
                    let entity = x.ConceptTemplate.CopyToNew userId defaultCardOption
                    getConceptTemplates x.ConceptTemplate
                    |> function
                    | Some (e: ConceptTemplateInstanceEntity) ->
                        if e.UserConceptTemplateInstances.Any(fun x -> x.UserId = userId) |> not then
                            UserConceptTemplateInstanceEntity(
                                UserId = userId,
                                DefaultPublicTags = MappingTools.intsListToStringOfInts x.ConceptTemplate.DefaultPublicTags, // medTODO normalize this
                                DefaultPrivateTags = MappingTools.intsListToStringOfInts x.ConceptTemplate.DefaultPrivateTags,
                                DefaultCardOption = defaultCardOption)
                            |> e.UserConceptTemplateInstances.Add
                        e
                    | None -> entity
                Anki.parseModels userId col.Models
                |> Result.map (Map.ofSeq >> Map.map toEntity)
            let usersTags =
                deckNameAndDeckConfigurationIdByDeckId
                |> Map.overValue fst
                |> Seq.distinct
                |> Seq.map ((+) "Deck:")
                |> Seq.map (fun deckTag -> 
                    usersTags.Where(fun x -> x.Name = deckTag)
                    |> Seq.tryHead
                    |> function
                    | Some e -> e
                    | None -> PrivateTagEntity(Name = deckTag, UserId = userId))
                |> Seq.append usersTags
                |> Seq.toList
            let! conceptsAndTagsByAnkiId =
                Anki.parseNotes
                    conceptTemplatesByModelId
                    usersTags
                    userId
                    fileEntityByAnkiFileName
                    getConcept
                    ankiDb.Notes
            let conceptsAndTagsByAnkiId = conceptsAndTagsByAnkiId |> Map.ofList
            let! cardByAnkiId =
                let collectionCreationTimeStamp = DateTimeOffset.FromUnixTimeSeconds(col.Crt).UtcDateTime
                ankiDb.Cards
                |> List.map (Anki.mapCard cardOptionAndDeckNameByDeckId conceptsAndTagsByAnkiId collectionCreationTimeStamp userId usersTags getCard)
                |> Result.consolidate
                |> Result.map Map.ofSeq
            let! histories = ankiDb.Revlogs |> Seq.map (Anki.toHistory cardByAnkiId getHistory) |> Result.consolidate
            return (cardByAnkiId |> Map.overValue id, histories)
        }

    let save (db: CardOverflowDb) ankiDb userId fileEntityByAnkiFileName =
        let getConceptTemplateInstance (templateInstance: AnkiConceptTemplateInstance) =
            let ti = templateInstance.CopyToNew userId null
            db.ConceptTemplateInstances // medTODO what happens when the hash matches a private template?
                .Include(fun x -> x.Fields :> IEnumerable<_>)
                    .ThenInclude(fun (x: FieldEntity) -> x.ConceptTemplateInstance.CardTemplates)
                .Include(fun x -> x.UserConceptTemplateInstances)
                .FirstOrDefault(fun x -> x.AcquireHash = ti.AcquireHash)
                |> Option.ofObj
        let getConcept (concept: AnkiConceptWrite) =
            concept.AcquireEquality db
            |> Option.ofObj
        let getCard (card: AnkiAcquiredCard) =
            card.AcquireEquality db |> Option.ofObj
        let getHistory (history: AnkiHistory) =
            history.AcquireEquality db |> Option.ofObj
        result {
            let! acquiredCardEntities, histories =
                load
                    ankiDb
                    userId
                    fileEntityByAnkiFileName
                    <| db.PrivateTags.Where(fun pt -> pt.UserId = userId) // lowTODO loading all of a user's tags, cardoptions, and concepttemplates is heavy
                    <| db.CardOptions
                        .Where(fun x -> x.UserId = userId)
                        .Select CardOption.Load
                    <| getConceptTemplateInstance
                    <| getConcept
                    <| getCard
                    <| getHistory
            acquiredCardEntities |> Seq.iter (fun x ->
                if x.CardTemplateId = 0 && x.ConceptInstanceId = 0
                then db.AcquiredCards.AddI x
                //else db.AcquiredCards.UpdateI x // this line is superfluous as long as we're on the same dbContext https://www.mikesdotnetting.com/article/303/entity-framework-core-trackgraph-for-disconnected-data
            )
            histories |> Seq.iter (fun x ->
                if x.Id = 0
                then db.Histories.AddI x
                //else db.Histories.UpdateI x // this line is superfluous as long as we're on the same dbContext https://www.mikesdotnetting.com/article/303/entity-framework-core-trackgraph-for-disconnected-data
            )
            db.SaveChangesI ()
        }
        
// lowTODO consider just generating a temporary guid code side to serve as a lookupID for the record types, then build the entities at the very end.

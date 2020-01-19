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

module AnkiDefaults =
    let cardTemplateIdByHash = // lowTODO could make this a byte array
        [("ywoGEFssvi4t3nnwGoizNtb4mjt8XiN1PvkvwFvu/v785psdidQLKG3lN/aCPxwYs29/TxeRJRjs69qa7Ymsvg==", 1)
         ("vgZiAPZFIxuapH1N8OgD+Z4Rl3ZdyqzaRe5eACnygT8EQDiLTpnqcqMrsLoW2PheQMYUma7NZaXVzA4oWpFqjw==", 3)
         ("eGXsWXGAsQAfHgUGk4JLIQ3uaF687zrsHYD/PoPn2dzmpYb2vMdxqt1IznkD351frGc9G/1avQ+loJ1EzeSPuw==", 2)
         ("PEPG/kewldTi4S2NsbkREMcgkK2zg/0E/G1shopZHmhA1H904wEDUzOlWnoW3G5dpenk/K6BxOZOZsNKA6+qwg==", 5)
         ("HcNXVOVXUWnT4aP/gBO+CtuA6x2nvXSRAPCSeYXoSxm8gae4nyMtVbF+LcD4zh3sikh8d6+dr0stK/NjJEUzJg==", 4)] |> Map.ofSeq

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
        (getTags: string list -> TagEntity list)
        (cardOptions: CardOptionEntity ResizeArray)
        defaultCardOption
        getCardTemplates
        getCard
        noRelationship
        getAcquiredCard
        getHistory =
        let col = ankiDb.Cols.Single()
        let usersTags =
            ankiDb.Notes
                .Select(fun x ->
                    x.Tags.Split(" ").Where(not << String.IsNullOrWhiteSpace))
                .SelectMany(id)
                .Distinct()
                |> Seq.toList
                |> getTags
        result {
            let! cardOptionByDeckConfigurationId =
                let toEntity _ (cardOption: CardOption) =
                    cardOptions
                    |> Seq.map (CardOption.load false)
                    |> Seq.filter (fun x -> x.AcquireEquality cardOption)
                    |> Seq.tryHead
                    |> Option.defaultValue cardOption
                    |> fun co -> co.CopyToNew userId
                Anki.parseCardOptions col.Dconf
                |> Result.map (Map.ofList >> Map.map toEntity)
            let! deckNameAndDeckConfigurationIdByDeckId =
                Anki.parseDecks col.Decks
                |> Result.bind (fun tuples ->
                    let names = tuples |> List.map (fun (_, (_, name, _)) -> name)
                    if names |> List.distinct |> List.length = names.Length then
                        tuples |> List.map snd |> Ok
                    else Error "Cannot import decks with the same name. Please give your decks distinct names." ) // lowTODO list the decks with the same names
                |> Result.bind (fun tuples ->
                    let filtered = tuples |> List.filter (fun (_, _, i) -> i.IsSome)
                    if filtered.Length = tuples.Length then
                        filtered |> List.map (fun (id, name, conf) -> (id, (name, conf.Value))) |> Map.ofList |> Ok
                    else Error "Cannot import filtered decks. Please delete all filtered decks - they're temporary https://apps.ankiweb.net/docs/am-manual.html#filtered-decks" ) // lowTODO name the filtered decks
            let cardOptionAndDeckNameByDeckId =
                deckNameAndDeckConfigurationIdByDeckId
                |> Map.map (fun _ (deckName, deckConfigurationId) ->
                    cardOptionByDeckConfigurationId.[string deckConfigurationId], "Deck:" + deckName)
            let! cardTemplatesByModelId =
                let toEntity (cardTemplate: AnkiCardTemplateInstance) =
                    let defaultCardOption =
                        cardOptionAndDeckNameByDeckId.TryFind cardTemplate.DeckId
                        |> function
                        | Some (cardOption, _) -> cardOption
                        | None -> defaultCardOption // veryLowTODO some anki models have invalid deck ids. Perhaps log this
                    getCardTemplates cardTemplate
                    |> function
                    | Some (e: CardTemplateInstanceEntity) ->
                        if e.User_CardTemplateInstances.Any(fun x -> x.UserId = userId) |> not then
                            User_CardTemplateInstanceEntity(
                                UserId = userId,
                                Tag_User_CardTemplateInstances =
                                    (cardTemplate.DefaultTags.ToList()
                                    |> Seq.map (fun x -> Tag_User_CardTemplateInstanceEntity(UserId = userId, DefaultTagId = x))
                                    |> fun x -> x.ToList()),
                                DefaultCardOption = defaultCardOption)
                            |> e.User_CardTemplateInstances.Add
                        e
                    | None -> cardTemplate.CopyToNew userId defaultCardOption
                    |> fun x -> {| Entity = x; CardTemplate = cardTemplate |}
                Anki.parseModels userId col.Models
                |> Result.map (fun x -> x |> Map.ofList |> Map.map (fun _ x -> List.map toEntity x))
            let usersTags =
                deckNameAndDeckConfigurationIdByDeckId
                |> Map.overValue fst
                |> Seq.distinct
                |> Seq.map ((+) "Deck:")
                |> Seq.map (fun deckTag -> 
                    getTags [ deckTag ] // lowTODO only query once when you have all the deck names
                    |> function
                    | [ e ] -> e
                    | [] -> TagEntity(Name = deckTag)
                    | _ -> failwith "This should be impossible" )
                |> Seq.append usersTags
                |> Seq.toList
            let! cardsAndTagsByNoteId =
                Anki.parseNotes
                    cardTemplatesByModelId
                    usersTags
                    userId
                    fileEntityByAnkiFileName
                    noRelationship
                    getCard
                    ankiDb.Notes
                |> Result.consolidate
                |> Result.map Map.ofSeq
            let! cardByNoteId =
                let collectionCreationTimeStamp = DateTimeOffset.FromUnixTimeSeconds(col.Crt).UtcDateTime
                ankiDb.Cards
                |> List.map (Anki.mapCard cardOptionAndDeckNameByDeckId cardsAndTagsByNoteId collectionCreationTimeStamp userId usersTags getAcquiredCard)
                |> Result.consolidate
                |> Result.map Map.ofSeq
            let! histories = ankiDb.Revlogs |> Seq.map (Anki.toHistory cardByNoteId getHistory) |> Result.consolidate
            return
                cardByNoteId |> Map.overValue id,
                histories |> Seq.choose id
        }
    let save (db: CardOverflowDb) ankiDb userId fileEntityByAnkiFileName =
        use hasher = SHA512.Create()
        let getCardTemplateInstance (templateInstance: AnkiCardTemplateInstance) =
            let ti = templateInstance.CopyToNew userId null // verylowTODO options isn't used so we're passing null... make a better way to calculate the hash
            let hash = CardTemplateInstanceEntity.hashBase64 hasher ti
            AnkiDefaults.cardTemplateIdByHash.TryFind hash
            |> function
            | Some id ->
                db.CardTemplateInstance
                    .Include(fun x -> x.User_CardTemplateInstances)
                    .Single(fun x -> x.Id = id)
            | None ->
                db.CardTemplateInstance
                    .Include(fun x -> x.User_CardTemplateInstances)
                    .FirstOrDefault(fun x -> x.AnkiId = ti.AnkiId) // highTODO compare the actual values
            |> Option.ofObj
        let getCard (card: AnkiCardWrite) =
            card.AcquireEquality db hasher
        let getAcquiredCard (card: AnkiAcquiredCard) =
            card.AcquireEquality db |> Option.ofObj
        let getHistory (history: AnkiHistory) =
            history.AcquireEquality db |> Option.ofObj
        let noRelationship sourceId targetId userId name =
            not <| db.Relationship.Any(fun x -> x.SourceId = sourceId && x.TargetId = targetId && x.UserId = userId && x.Name = name)
        result {
            let! acquiredCardEntities, histories =
                load
                    ankiDb
                    userId
                    fileEntityByAnkiFileName
                    <| (fun s -> db.Tag.Where(fun t -> s.Contains t.Name) |> Seq.toList) // collation is case insensitive, and .Contains seems to generate the appropriate SQL
                    <| db.CardOption
                        .Where(fun x -> x.UserId = userId)
                        .ToList()
                    <| db.User.Include(fun x -> x.DefaultCardOption).Single(fun x -> x.Id = userId).DefaultCardOption
                    <| getCardTemplateInstance
                    <| getCard
                    <| noRelationship
                    <| getAcquiredCard
                    <| getHistory
            acquiredCardEntities |> Seq.iter (fun x ->
                if x.CardInstance <> null && x.CardInstanceId = 0
                then db.AcquiredCard.AddI x
            )
            histories |> Seq.iter (fun x ->
                if x.Id = 0
                then db.History.AddI x
            )
            db.ChangeTracker.Entries() // CardInstances may be unused if they're duplicates; this removes their orphaned Relationships
                |> Seq.filter (fun e -> e.State = EntityState.Added && e.Entity :? RelationshipEntity)
                |> Seq.iter (fun x ->
                    let e = x.Entity :?> RelationshipEntity
                    if isNull e.Source || isNull e.Target then
                        x.State <- EntityState.Detached
                )
            return db.SaveChangesAsyncI () // medTODO optimization when EFCore 3 GA lands https://github.com/borisdj/EFCore.BulkExtensions this may help if the guy isn't fast enough https://github.com/thepirat000/Audit.NET/issues/231
        }
        
// lowTODO consider just generating a temporary guid code side to serve as a lookupID for the record types, then build the entities at the very end.

namespace CardOverflow.Api

open LoadersAndCopiers
open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure.Core
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
    let collateInstanceIdByHash = // lowTODO could make this a byte array
        [("WX831/PqYECBDQaRxa7nceZWfvK27SNOudsTuAajr7tDTo25RDWsjXiaotM8OgBtFthzKcmiAgB0ihSM06e0Mw==", 1001)
         ("OfVUXbEwX3TYmYE4dp1lmVEuViCrST9in+wdGi9IM/lubv7kOUwIqS9EVQxGe6sMV7lqtoHnSC3A/P4NC4R/bQ==", 1002)
         ("q1nY+8Gro/Nx9Cjbjlqwqcl6wDxqSNMFfO8WSwVjieLBVC1lYIgGt/qH8lAn1lf9UxMjK0KsqAHdVmQMx7Wwhg==", 1003)
         ("WVgngW1UJ+W0USp34g8tYeYfeOvwXF7ZwJ6H+aOtmsW8TRnGpdQ+xpyc3xtE1ENESGIPGN9oJrXyGQDOSfn5DA==", 1004)
         ("RMAn+9uvBSUdW23Je/E0zQ4pnFneI4pbP09Tp9uG4AemtPa7BtFBNbruHCRXMNNoaCT2RfwfWxOs2GuUZFzstg==", 1005)
         ("v/2dG9mP9QWN+Ma38znwF7lsRN00INrNX956YZ469ANfqCoYy/NnUaEXTEY8PM0MH/2bXQbJULGKzzBVIjtdUQ==", 1006)
         ("k7J/5gY/caDTolz/10508Xttnw/MkS1GOzb3KDksyfvw4MfSHQ4AsegNtIYRUZmNypdJCfmrwCtNN/UT9ueXEw==", 1007)] |> Map.ofSeq

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
        (getDecks: string list -> DeckEntity list)
        (cardSettings: CardSettingEntity ResizeArray)
        defaultCardSetting
        getCollates
        getCard
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
            let! cardSettingByDeckConfigurationId =
                let toEntity _ (cardSetting: CardSetting) =
                    cardSettings
                    |> Seq.map (CardSetting.load false)
                    |> Seq.filter (fun x -> x.AcquireEquality cardSetting)
                    |> Seq.tryHead
                    |> Option.defaultValue cardSetting
                    |> fun co -> co.CopyToNew userId
                Anki.parseCardSettings col.Dconf
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
            let decks =
                deckNameAndDeckConfigurationIdByDeckId
                |> Map.overValue (fun (deck, _) -> deck)
                |> List.ofSeq
                |> getDecks
            let cardSettingAndDeckByDeckId =
                deckNameAndDeckConfigurationIdByDeckId
                |> Map.map (fun _ (deckName, deckConfigurationId) ->
                    cardSettingByDeckConfigurationId.[string deckConfigurationId],
                    decks.SingleOrDefault(fun x -> x.Name = deckName)
                    |?? lazy (DeckEntity(UserId = userId, Name = deckName))
                )
            let! collatesByModelId =
                let toEntity collateEntity (collate: AnkiCollateInstance) =
                    let defaultCardSetting =
                        cardSettingAndDeckByDeckId.TryFind collate.DeckId
                        |> function
                        | Some (cardSetting, _) -> cardSetting
                        | None -> defaultCardSetting // veryLowTODO some anki models have invalid deck ids. Perhaps log this
                    getCollates collate
                    |> function
                    | Some (e: CollateInstanceEntity) ->
                        if e.User_CollateInstances.Any(fun x -> x.UserId = userId) |> not then
                            User_CollateInstanceEntity(
                                UserId = userId,
                                Tag_User_CollateInstances =
                                    (collate.DefaultTags.ToList()
                                    |> Seq.map (fun x -> Tag_User_CollateInstanceEntity(UserId = userId, DefaultTagId = x))
                                    |> fun x -> x.ToList()),
                                DefaultCardSetting = defaultCardSetting)
                            |> e.User_CollateInstances.Add
                        e
                    | None -> collate.CopyToNewWithCollate userId collateEntity defaultCardSetting
                    |> fun x -> {| Entity = x; Collate = collate |}
                Anki.parseModels userId col.Models
                |> Result.map (Map.ofList >> Map.map (fun _ x -> x |> (toEntity <| CollateEntity(AuthorId = userId))))
            let cardsAndTagsByNoteId =
                Anki.parseNotes
                    collatesByModelId
                    usersTags
                    userId
                    fileEntityByAnkiFileName
                    getCard
                    ankiDb.Notes
                |> Map.ofSeq
            let! cardByNoteId =
                let collectionCreationTimeStamp = DateTimeOffset.FromUnixTimeSeconds(col.Crt).UtcDateTime
                ankiDb.Cards
                |> List.map (Anki.mapCard cardSettingAndDeckByDeckId cardsAndTagsByNoteId collectionCreationTimeStamp userId getAcquiredCard)
                |> Result.consolidate
                |> Result.map Map.ofSeq
            cardByNoteId |> Map.toList |> List.distinctBy (fun (_, x) -> x.BranchInstance) |> List.iter (fun (_, card) ->
                match card.BranchInstance.AnkiNoteId |> Option.ofNullable with
                | None -> ()
                | Some nid -> 
                    let _, tags = cardsAndTagsByNoteId.[nid]
                    card.Tag_AcquiredCards <- tags.Select(fun x -> Tag_AcquiredCardEntity(Tag = x)).ToList()
            )
            let! histories = ankiDb.Revlogs |> Seq.map (Anki.toHistory cardByNoteId getHistory) |> Result.consolidate
            return
                cardByNoteId |> Map.overValue id,
                histories |> Seq.choose id
        }
    let save (db: CardOverflowDb) ankiDb userId fileEntityByAnkiFileName =
        use hasher = SHA512.Create()
        let defaultCardSetting = db.User.Include(fun x -> x.DefaultCardSetting).Single(fun x -> x.Id = userId).DefaultCardSetting
        let getCollateInstance (collateInstance: AnkiCollateInstance) =
            let ti = collateInstance.CopyToNew userId defaultCardSetting
            let hash = CollateInstanceEntity.hashBase64 hasher ti
            AnkiDefaults.collateInstanceIdByHash.TryFind hash
            |> function
            | Some id ->
                db.CollateInstance
                    .Include(fun x -> x.User_CollateInstances)
                    .Single(fun x -> x.Id = id)
            | None ->
                db.CollateInstance
                    .Include(fun x -> x.User_CollateInstances)
                    .OrderBy(fun x -> x.Created)
                    .FirstOrDefault(fun x -> x.Hash = ti.Hash)
            |> Option.ofObj
        let getCard (card: AnkiCardWrite) =
            card.AcquireEquality db hasher
        let getAcquiredCard (card: AnkiAcquiredCard) =
            card.AcquireEquality db |> Option.ofObj
        let getHistory (history: AnkiHistory) =
            history.AcquireEquality db |> Option.ofObj
        taskResult {
            let! acquiredCardEntities, histories =
                load
                    ankiDb
                    userId
                    fileEntityByAnkiFileName
                    <| (TagRepository.searchMany db >> Seq.toList)
                    <| (DeckRepository.searchMany db userId >> Seq.toList)
                    <| db.CardSetting
                        .Where(fun x -> x.UserId = userId)
                        .ToList()
                    <| defaultCardSetting
                    <| getCollateInstance
                    <| getCard
                    <| getAcquiredCard
                    <| getHistory
            acquiredCardEntities |> Seq.iter (fun x ->
                if x.BranchInstance <> null && x.BranchInstanceId = 0
                then db.AcquiredCard.AddI x
            )
            histories |> Seq.iter (fun x ->
                if x.Id = 0
                then db.History.AddI x
            )
            return! db.SaveChangesAsyncI () // medTODO optimization when EFCore 3 GA lands https://github.com/borisdj/EFCore.BulkExtensions this may help if the guy isn't fast enough https://github.com/thepirat000/Audit.NET/issues/231
        }
        
// lowTODO consider just generating a temporary guid code side to serve as a lookupID for the record types, then build the entities at the very end.

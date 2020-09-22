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
open NodaTime

module AnkiDefaults =
    let grompleafIdByHash = // lowTODO could make this a byte array
        [("WX831/PqYECBDQaRxa7nceZWfvK27SNOudsTuAajr7tDTo25RDWsjXiaotM8OgBtFthzKcmiAgB0ihSM06e0Mw==", Guid.Parse("00000000-0000-0000-0000-7e3900001001"))
         ("OfVUXbEwX3TYmYE4dp1lmVEuViCrST9in+wdGi9IM/lubv7kOUwIqS9EVQxGe6sMV7lqtoHnSC3A/P4NC4R/bQ==", Guid.Parse("00000000-0000-0000-0000-7e3900001002"))
         ("q1nY+8Gro/Nx9Cjbjlqwqcl6wDxqSNMFfO8WSwVjieLBVC1lYIgGt/qH8lAn1lf9UxMjK0KsqAHdVmQMx7Wwhg==", Guid.Parse("00000000-0000-0000-0000-7e3900001003"))
         ("WVgngW1UJ+W0USp34g8tYeYfeOvwXF7ZwJ6H+aOtmsW8TRnGpdQ+xpyc3xtE1ENESGIPGN9oJrXyGQDOSfn5DA==", Guid.Parse("00000000-0000-0000-0000-7e3900001004"))
         ("RMAn+9uvBSUdW23Je/E0zQ4pnFneI4pbP09Tp9uG4AemtPa7BtFBNbruHCRXMNNoaCT2RfwfWxOs2GuUZFzstg==", Guid.Parse("00000000-0000-0000-0000-7e3900001005"))
         ("v/2dG9mP9QWN+Ma38znwF7lsRN00INrNX956YZ469ANfqCoYy/NnUaEXTEY8PM0MH/2bXQbJULGKzzBVIjtdUQ==", Guid.Parse("00000000-0000-0000-0000-7e3900001006"))
         ("k7J/5gY/caDTolz/10508Xttnw/MkS1GOzb3KDksyfvw4MfSHQ4AsegNtIYRUZmNypdJCfmrwCtNN/UT9ueXEw==", Guid.Parse("00000000-0000-0000-0000-7e3900001007"))] |> Map.ofSeq

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
        getGromplates
        getCCard
        getCard
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
                    |> Seq.filter (fun x -> x.CollectedEquality cardSetting)
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
            let! gromplatesByModelId =
                let toEntity gromplateEntity (gromplate: AnkiGrompleaf) =
                    let defaultCardSetting =
                        cardSettingAndDeckByDeckId.TryFind gromplate.DeckId
                        |> function
                        | Some (cardSetting, _) -> cardSetting
                        | None -> defaultCardSetting // veryLowTODO some anki models have invalid deck ids. Perhaps log this
                    getGromplates gromplate
                    |> function
                    | Some (e: GrompleafEntity) ->
                        if e.User_Grompleafs.Any(fun x -> x.UserId = userId) |> not then
                            User_GrompleafEntity(
                                UserId = userId,
                                Tag_User_Grompleafs =
                                    (gromplate.DefaultTags.ToList()
                                    |> Seq.map (fun x -> Tag_User_GrompleafEntity(UserId = userId, DefaultTagId = x))
                                    |> fun x -> x.ToList()),
                                DefaultCardSetting = defaultCardSetting)
                            |> e.User_Grompleafs.Add
                        e
                    | None -> gromplate.CopyToNewWithGromplate userId gromplateEntity defaultCardSetting
                    |> fun x -> {| Entity = x; Gromplate = gromplate |}
                Anki.parseModels userId col.Models
                |> Result.map (Map.ofList >> Map.map (fun _ x -> x |> (toEntity <| GromplateEntity(AuthorId = userId))))
            let cardsAndTagsByNoteId =
                Anki.parseNotes
                    gromplatesByModelId
                    usersTags
                    userId
                    fileEntityByAnkiFileName
                    getCard
                    ankiDb.Notes
                |> Map.ofSeq
            let! cardByNoteId =
                let collectionCreationTimeStamp = Instant.FromUnixTimeSeconds col.Crt
                ankiDb.Cards
                |> List.map (Anki.mapCard cardSettingAndDeckByDeckId cardsAndTagsByNoteId collectionCreationTimeStamp userId getCCard)
                |> Result.consolidate
                |> Result.map Map.ofSeq
            cardByNoteId |> Map.toList |> List.distinctBy (fun (_, x) -> x.Leaf) |> List.iter (fun (_, card) ->
                match card.Leaf.AnkiNoteId |> Option.ofNullable with
                | None -> ()
                | Some nid -> 
                    let _, tags = cardsAndTagsByNoteId.[nid]
                    card.Tag_Cards <- tags.Select(fun x -> Tag_CardEntity(Tag = x)).ToList()
            )
            let! histories = ankiDb.Revlogs |> Seq.map (Anki.toHistory userId cardByNoteId getHistory) |> Result.consolidate
            return
                cardByNoteId |> Map.overValue id,
                histories
        }
    let save (db: CardOverflowDb) ankiDb userId fileEntityByAnkiFileName =
        use hasher = SHA512.Create()
        let defaultCardSetting = db.User.Include(fun x -> x.DefaultCardSetting).Single(fun x -> x.Id = userId).DefaultCardSetting
        let getGrompleaf (grompleaf: AnkiGrompleaf) =
            let ti = grompleaf.CopyToNew userId defaultCardSetting
            let hash = GrompleafEntity.hashBase64 hasher ti
            AnkiDefaults.grompleafIdByHash.TryFind hash
            |> function
            | Some id ->
                db.Grompleaf
                    .Include(fun x -> x.User_Grompleafs)
                    .Single(fun x -> x.Id = id)
            | None ->
                db.Grompleaf
                    .Include(fun x -> x.User_Grompleafs)
                    .OrderBy(fun x -> x.Created)
                    .FirstOrDefault(fun x -> x.Hash = ti.Hash)
            |> Option.ofObj
        let getCard (card: AnkiCardWrite) =
            card.CollectedEquality db hasher
        let getCCard (card: AnkiCard) =
            card.CollectedEquality db |> Option.ofObj
        let getHistory (history: AnkiHistory) =
            history.CollectedEquality db |> Option.ofObj
        taskResult {
            let! ccs, histories =
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
                    <| getGrompleaf
                    <| getCCard
                    <| getCard
                    <| getHistory
            ccs |> Seq.iter (fun x ->
                if x.Leaf <> null && x.LeafId = Guid.Empty
                then db.Card.AddI x
            )
            histories |> Seq.iter (fun x ->
                if x.Id = Guid.Empty
                then
                    x.Id <- Ulid.create
                    db.History.AddI x
            )
            return! db.SaveChangesAsyncI () // medTODO optimization when EFCore 3 GA lands https://github.com/borisdj/EFCore.BulkExtensions this may help if the guy isn't fast enough https://github.com/thepirat000/Audit.NET/issues/231
        }
        
// lowTODO consider just generating a temporary guid code side to serve as a lookupID for the record types, then build the entities at the very end.

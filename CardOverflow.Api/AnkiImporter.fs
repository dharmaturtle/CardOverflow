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
    let collateInstanceIdByHash = // lowTODO could make this a byte array
        [("k1ZVjK9w8kHwIFEpi5yaCw0DxqUQ100nmCtR8tBXkUYrrU4PQNrWlhYjh9Ry8g6NH9b7f6PcriUk3fifj2llUQ==", 1001)
         ("y+u0/z05knPzHaV8WLRExZMYzStViG3eObOOUn4Jpb0lA21tM4V/QQ49M39LZliv88ALpWR9Fw006K1VLfOouQ==", 1002)
         ("j0XhB1IW4EnGbASd90hYUm9lptiNUzQsOhxYquycZqQnusfrkChR/9ZAQ9cYxOm859Gl4daIzAMDS2HRDHyrEw==", 1003)
         ("UZy5No9bXB8ocH/Ku09J79zMRbxsEaWdQ3gD615gy/uQ2e4Q5MwJIvHgSvP/+YNy1siPw84Ckxqemv26U2HMng==", 1004)
         ("2m7xLzE8/kKxAC+iMrss/hCKP1Yw83GHqb21Vq846AkpdIq9QTstmPAWluu33teyQ9RSXpYSxyxWGuTbYbGuXw==", 1005)
         ("HxOr/B0otwE7SUDMFci3Jr6bmBe1T069oVxx+DA5DWjW6u7CW0KShWpNgQ7qJEtv2XrMSyXlzPl0XPsnqHkklw==", 1006)
         ("Yr60c4Ns3yytoOWIp1aBHmAn2WMI6HaxLLxCcMMU4SCWRexkyO5YZlpth4qDy6EWw6ceCjlcYKj0XMtDxJKQUQ==", 1007)
         ("NcdH/yK+HVuabA9mnZNY7SG16S7Ww0Xv1Usf4wbHzTFpglDY4fpuN9ywE3vdBOsrsiwJA0Qo5X8zQYAVKCN3pQ==", 1008)
         ("XdfPSI1MkSNAIk1JAO9GI/Wo1oYdeMNIG/x7TyHOk5jOsHKFSK2g49Q1M8cJ3E3T1WOE42gCDhAdqJKwBKz71A==", 1009)] |> Map.ofSeq

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
            let cardSettingAndDeckNameByDeckId =
                deckNameAndDeckConfigurationIdByDeckId
                |> Map.map (fun _ (deckName, deckConfigurationId) ->
                    cardSettingByDeckConfigurationId.[string deckConfigurationId], "Deck:" + deckName)
            let! collatesByModelId =
                let toEntity collateEntity (collate: AnkiCollateInstance) =
                    let defaultCardSetting =
                        cardSettingAndDeckNameByDeckId.TryFind collate.DeckId
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
                |> Result.map (Map.ofList >> Map.map (fun _ x -> x |> List.map (toEntity <| CollateEntity(AuthorId = userId))))
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
                    collatesByModelId
                    usersTags
                    userId
                    fileEntityByAnkiFileName
                    getCard
                    ankiDb.Notes
                |> Result.consolidate
                |> Result.map Map.ofSeq
            let! cardByNoteId =
                let collectionCreationTimeStamp = DateTimeOffset.FromUnixTimeSeconds(col.Crt).UtcDateTime
                ankiDb.Cards
                |> List.map (Anki.mapCard cardSettingAndDeckNameByDeckId cardsAndTagsByNoteId collectionCreationTimeStamp userId usersTags getAcquiredCard)
                |> Result.consolidate
                |> Result.map Map.ofSeq
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
                    <| (fun s -> db.Tag.Where(fun t -> s.Contains t.Name) |> Seq.toList) // collation is case insensitive, and .Contains seems to generate the appropriate SQL
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

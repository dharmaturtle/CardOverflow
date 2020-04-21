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
    let templateIdByHash = // lowTODO could make this a byte array
        [("ywoGEFssvi4t3nnwGoizNtb4mjt8XiN1PvkvwFvu/v785psdidQLKG3lN/aCPxwYs29/TxeRJRjs69qa7Ymsvg==", 1)
         ("vgZiAPZFIxuapH1N8OgD+Z4Rl3ZdyqzaRe5eACnygT8EQDiLTpnqcqMrsLoW2PheQMYUma7NZaXVzA4oWpFqjw==", 3)
         ("eGXsWXGAsQAfHgUGk4JLIQ3uaF687zrsHYD/PoPn2dzmpYb2vMdxqt1IznkD351frGc9G/1avQ+loJ1EzeSPuw==", 2)
         ("PEPG/kewldTi4S2NsbkREMcgkK2zg/0E/G1shopZHmhA1H904wEDUzOlWnoW3G5dpenk/K6BxOZOZsNKA6+qwg==", 5)
         ("sg4//8QUMVcDU9zVP1jImablLPEfp8cGe+xaMsl3jlMJ44R6ze/VAOfUf52/4bV2yO+mF32S/Wr7PahZ23MOnw==", 4)] |> Map.ofSeq

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
        getTemplates
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
            let! templatesByModelId =
                let toEntity (template: AnkiTemplateInstance) =
                    let defaultCardSetting =
                        cardSettingAndDeckNameByDeckId.TryFind template.DeckId
                        |> function
                        | Some (cardSetting, _) -> cardSetting
                        | None -> defaultCardSetting // veryLowTODO some anki models have invalid deck ids. Perhaps log this
                    getTemplates template
                    |> function
                    | Some (e: TemplateInstanceEntity) ->
                        if e.User_TemplateInstances.Any(fun x -> x.UserId = userId) |> not then
                            User_TemplateInstanceEntity(
                                UserId = userId,
                                Tag_User_TemplateInstances =
                                    (template.DefaultTags.ToList()
                                    |> Seq.map (fun x -> Tag_User_TemplateInstanceEntity(UserId = userId, DefaultTagId = x))
                                    |> fun x -> x.ToList()),
                                DefaultCardSetting = defaultCardSetting)
                            |> e.User_TemplateInstances.Add
                        e
                    | None -> template.CopyToNew userId defaultCardSetting
                    |> fun x -> {| Entity = x; Template = template |}
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
                    templatesByModelId
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
        let getTemplateInstance (templateInstance: AnkiTemplateInstance) =
            let ti = templateInstance.CopyToNew userId defaultCardSetting
            let hash = TemplateInstanceEntity.hashBase64 hasher ti
            AnkiDefaults.templateIdByHash.TryFind hash
            |> function
            | Some id ->
                db.TemplateInstance
                    .Include(fun x -> x.User_TemplateInstances)
                    .Single(fun x -> x.Id = id)
            | None ->
                db.TemplateInstance
                    .Include(fun x -> x.User_TemplateInstances)
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
                    <| getTemplateInstance
                    <| getCard
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
            return! db.SaveChangesAsyncI () // medTODO optimization when EFCore 3 GA lands https://github.com/borisdj/EFCore.BulkExtensions this may help if the guy isn't fast enough https://github.com/thepirat000/Audit.NET/issues/231
        }
        
// lowTODO consider just generating a temporary guid code side to serve as a lookupID for the record types, then build the entities at the very end.

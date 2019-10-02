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
        (usersTags: TagEntity seq)
        (cardOptions: CardOptionEntity seq)
        getCardTemplates
        getCard
        noRelationship
        getAcquiredCard
        getHistory =
        let col = ankiDb.Cols.Single()
        result {
            let! cardOptionByDeckConfigurationId =
                let toEntity _ (cardOption: CardOption) =
                    cardOptions
                    |> Seq.map CardOption.load
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
                        | None -> cardOptions.First(fun x -> x.IsDefault) // veryLowTODO some anki models have invalid deck ids. Perhaps log this
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
                    |> fun x -> {| Entity = x; IsCloze = cardTemplate.IsCloze |}
                let toEntities _ =
                    Seq.map toEntity
                    >> Seq.distinctBy (fun x -> (System.Text.Encoding.UTF8.GetString x.Entity.AcquireHash))
                    >> Seq.toList
                Anki.parseModels userId col.Models
                |> Result.map (fun x -> x |> Map.ofSeq |> Map.map toEntities)
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
                    | None -> TagEntity(Name = deckTag))
                |> Seq.append usersTags
                |> Seq.toList
            let cardsAndTagsByNoteId =
                Anki.parseNotes
                    cardTemplatesByModelId
                    usersTags
                    userId
                    fileEntityByAnkiFileName
                    noRelationship
                    getCard
                    ankiDb.Notes
                |> List.ofSeq
            let cardsAndTagsByNoteId =
                let duplicates =
                    cardsAndTagsByNoteId
                    |> List.collect (fun (noteId, (cards, tags)) -> cards |> List.map (fun card -> (card, noteId, tags)))
                    |> List.groupBy(fun (card, _, _) -> card.AcquireHash) // lowTODO optimization, does this use Span? https://stackoverflow.com/a/48599119
                    |> List.filter(fun (_, x) -> x.Count() > 1)
                    |> List.collect(fun (_, x) ->
                        let tags = x |> List.collect(fun (_, _, tags) -> tags) |> List.groupBy(fun x -> x.Name) |> List.map(fun (x, y) -> y.First())
                        let card, _, _ = x.First()
                        card.Created <- x.Min(fun (card, _, _) -> card.Created)
                        card.Modified <- x.Max(fun (card, _, _) -> card.Modified)
                        x |> List.map (fun (_, noteId, _) -> noteId, (card, tags))
                    )
                    |> List.groupBy (fun (noteId, _) -> noteId)
                    |> List.map (fun (noteId, xs) ->
                        let cards = xs |> List.map (fun (_, (card, _)) -> card)
                        let tags = xs |> List.collect (fun (_, (_, tags)) -> tags) |> List.groupBy(fun x -> x.Name) |> List.map(fun (x, y) -> y.First())
                        noteId, (cards, tags)
                    )
                    |> Map.ofList
                cardsAndTagsByNoteId
                |> Seq.map (fun (noteId, tuple) ->
                    if duplicates.ContainsKey noteId
                    then noteId, duplicates.[noteId]
                    else noteId, tuple
                ) |> Map.ofSeq
            let! cardByNoteId =
                let collectionCreationTimeStamp = DateTimeOffset.FromUnixTimeSeconds(col.Crt).UtcDateTime
                ankiDb.Cards
                |> List.map (Anki.mapCard cardOptionAndDeckNameByDeckId cardsAndTagsByNoteId collectionCreationTimeStamp userId usersTags getAcquiredCard)
                |> Result.consolidate
                |> Result.map Map.ofSeq
            let! histories = ankiDb.Revlogs |> Seq.map (Anki.toHistory cardByNoteId getHistory) |> Result.consolidate
            return
                cardByNoteId |> Map.overValue id |> Seq.distinctBy (fun x -> x.CardInstance.GetHashCode()), // duplicate anki notes share a CardInstance instance
                histories |> Seq.choose id
        }
    let save (db: CardOverflowDb) ankiDb userId fileEntityByAnkiFileName =
        let getCardTemplateInstance (templateInstance: AnkiCardTemplateInstance) =
            let ti = templateInstance.CopyToNew userId null
            db.CardTemplateInstance
                .Include(fun x -> x.Fields :> IEnumerable<_>)
                    //.ThenInclude(fun (x: FieldEntity) -> x.CardTemplateInstance.CardTemplates)
                .Include(fun x -> x.User_CardTemplateInstances)
                .FirstOrDefault(fun x -> x.AcquireHash = ti.AcquireHash)
                |> Option.ofObj
        let getCard (card: AnkiCardWrite) =
            card.AcquireEquality db
            |> Option.ofObj
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
                    <| db.Tag // medTODO loading all of a user's tags, cardoptions, and cardtemplates is heavy... no actually you're loading the ENTIRE tag table
                    <| db.CardOption
                        .Where(fun x -> x.UserId = userId)
                    <| getCardTemplateInstance
                    <| getCard
                    <| noRelationship
                    <| getAcquiredCard
                    <| getHistory
            acquiredCardEntities |> Seq.iter (fun x ->
                if x.CardInstance <> null && x.CardInstanceId = 0
                then db.AcquiredCard.AddI x
                //else db.AcquiredCard.UpdateI x // this line is superfluous as long as we're on the same dbContext https://www.mikesdotnetting.com/article/303/entity-framework-core-trackgraph-for-disconnected-data
            )
            histories |> Seq.iter (fun x ->
                if x.Id = 0
                then db.History.AddI x
                //else db.Histories.UpdateI x // this line is superfluous as long as we're on the same dbContext https://www.mikesdotnetting.com/article/303/entity-framework-core-trackgraph-for-disconnected-data
            )
            db.ChangeTracker.Entries() // CardInstances may be unused if they're duplicates; this removes their orphaned Relationships
                |> Seq.filter (fun e -> e.State = EntityState.Added && e.Entity :? RelationshipEntity)
                |> Seq.iter (fun x ->
                    let e = x.Entity :?> RelationshipEntity
                    if isNull e.Source || isNull e.Target then
                        x.State <- EntityState.Detached
                )
            db.SaveChangesI () // medTODO optimization when EFCore 3 GA lands https://github.com/borisdj/EFCore.BulkExtensions this may help if the guy isn't fast enough https://github.com/thepirat000/Audit.NET/issues/231
        }
        
// lowTODO consider just generating a temporary guid code side to serve as a lookupID for the record types, then build the entities at the very end.

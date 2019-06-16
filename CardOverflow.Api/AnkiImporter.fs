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

module AnkiImporter =
    let getSimpleAnkiDb (db: AnkiDb) =
        { Cards = db.Cards.ToList() |> List.ofSeq
          Cols = db.Cols.ToList() |> List.ofSeq
          Notes = db.Notes.ToList() |> List.ofSeq
          Revlogs = db.Revlogs.ToList() |> List.ofSeq }
    let loadFiles userId (previousFiles: FileEntity seq) zipPath = // lowTODO we probably don't need the whole FileEntity, just the filename and hash
        let zipFile = ZipFile.Open (zipPath, ZipArchiveMode.Read)
        use mediaStream = zipFile.Entries.First(fun x -> x.Name = "media").Open ()
        use mediaReader = new StreamReader (mediaStream)
        use sha256 = SHA256.Create ()
        Decode.string
        |> Decode.keyValuePairs
        |> Decode.fromString
        <| mediaReader.ReadToEnd()
        |> Result.map(List.sortBy (fun (_, fileName) -> fileName) >> List.map(fun (index, fileName) ->
            if previousFiles.Any(fun x -> x.FileName = fileName)
            then sprintf "You already have a file called '%s'." fileName |> Error
            else
                use fileStream = zipFile.Entries.First(fun x -> x.Name = index).Open()
                use m = new MemoryStream()
                fileStream.CopyTo m
                let array = m.ToArray() // lowTODO investigate if there are memory issues if someone uploads gigs, we might need to persist to the DB sooner
                FileEntity(
                    UserId = userId,
                    FileName = fileName,
                    Data = array,
                    Sha256 = sha256.ComputeHash array
                ) |> Ok
            ))
        |> Result.bind Result.consolidate
    let load ankiDb (usersTags: PrivateTagEntity seq) (userId: int) (cardOptions: CardOption seq) (conceptTemplates: ConceptTemplate seq) =
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
                let toEntity _ (conceptTemplate: AnkiConceptTemplate) =
                    conceptTemplates
                    |> Seq.filter (fun x -> x.AcquireEquality conceptTemplate.ConceptTemplate)
                    |> Seq.tryHead
                    |> function
                    | Some x -> x
                    | None -> conceptTemplate.ConceptTemplate
                    |> fun co -> co.CopyToNew (cardOptionAndDeckNameByDeckId.[conceptTemplate.DeckId] |> fst)
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
            let conceptsAndTagsByAnkiId =
                Anki.parseNotes
                    conceptTemplatesByModelId
                    usersTags
                    userId
                    []
                    ankiDb.Notes
                |> Map.ofList
            let! cardEntities =
                let collectionCreationTimeStamp = DateTimeOffset.FromUnixTimeSeconds(col.Crt).UtcDateTime
                ankiDb.Cards
                |> List.map (Anki.mapCard cardOptionAndDeckNameByDeckId conceptsAndTagsByAnkiId collectionCreationTimeStamp userId usersTags)
                |> Result.consolidate
            let cardIdByAnkiId = cardEntities |> Seq.map (fun (card, anki) -> anki.Id, card) |> Map.ofSeq
            return (cardEntities |> Seq.map fst,
                    ankiDb.Revlogs |> Seq.map (Anki.toHistory cardIdByAnkiId userId)
                   )
        }

    let save (db: CardOverflowDb) ankiDb userId (files: FileEntity seq) =
        result {
            let usersTags = db.PrivateTags.Where(fun pt -> pt.UserId = userId) |> Seq.toList
            let! acquiredCardEntities, histories =
                load
                    ankiDb
                    usersTags
                    userId
                    (db.CardOptions
                        .Where(fun x -> x.UserId = userId)
                        .ToList()
                        |> Seq.map CardOption.Load)
                    (db.ConceptTemplateConceptTemplateDefaultUsers
                        .Where(fun x -> x.UserId = userId)
                        .Select(fun x -> x.ConceptTemplate)
                        .ToList()
                        |> Seq.map ConceptTemplate.Load)
            acquiredCardEntities |> db.AcquiredCards.AddRange
            histories |> db.Histories.AddRange
            files |> db.Files.AddRange
            db.SaveChangesI ()
        }

namespace CardOverflow.Api

open LoadersAndCopiers
open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Entity
open CardOverflow.Entity.Anki
open System
open System.Linq
open Thoth.Json.Net
open FsToolkit.ErrorHandling
open Helpers
open System.IO
open System.IO.Compression

module AnkiImporter =
    let getSimpleAnkiDb (db: AnkiDb) =
        { Cards = db.Cards.ToList() |> List.ofSeq
          Cols = db.Cols.ToList() |> List.ofSeq
          Notes = db.Notes.ToList() |> List.ofSeq
          Revlogs = db.Revlogs.ToList() |> List.ofSeq }
    let loadFiles zipPath =
        let zipFile = ZipFile.Open(zipPath, ZipArchiveMode.Read)
        use mediaStream = zipFile.Entries.First(fun x -> x.Name = "media").Open()
        use mediaReader = new StreamReader(mediaStream)
        Decode.string
        |> Decode.keyValuePairs
        |> Decode.fromString
        <| mediaReader.ReadToEnd()
        |> Result.map(List.map(fun (index, fileName) ->
            use fileStream = zipFile.Entries.First(fun x -> x.Name = index).Open()
            use m = new MemoryStream()
            fileStream.CopyTo m
            CardOverflow.Entity.FileEntity(
                UserId = 3,
                FileName = fileName,
                Data = m.ToArray() // lowTODO investigate if there are memory issues if someone uploads gigs, we might need to persist to the DB sooner
            )))
    let load ankiDb usersTags (userId: int) =
        let col = ankiDb.Cols.Single()
        result {
            let! cardOptionByDeckConfigurationId =
                Anki.parseDconf col.Dconf
                |> Result.bind (List.map (fun (deckConfigurationId, cardOption) -> (deckConfigurationId, cardOption.CopyToNew userId)) >> Map.ofList >> Ok )
            let! nameAndDeckConfigurationIdByDeckId =
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
                nameAndDeckConfigurationIdByDeckId
                |> Map.map (fun _ (deckName, deckConfigurationId) ->
                    cardOptionByDeckConfigurationId.[string deckConfigurationId], PrivateTagEntity(Name = "Deck:" + deckName, UserId = userId))
            let! conceptTemplatesByModelId =
                Anki.parseModels userId cardOptionAndDeckNameByDeckId col.Models
                |> Result.map Map.ofSeq
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
                |> List.map (Anki.mapCard cardOptionAndDeckNameByDeckId conceptsAndTagsByAnkiId collectionCreationTimeStamp)
                |> Result.consolidate
            let cardIdByAnkiId = cardEntities |> Seq.map (fun (card, anki) -> anki.Id, card) |> Map.ofSeq
            return (cardEntities |> Seq.map fst,
                    ankiDb.Revlogs |> Seq.map (Anki.toHistory cardIdByAnkiId userId),
                    conceptsAndTagsByAnkiId |> Map.overValue (fun (concept, _) -> ConceptUserEntity(Concept = concept, UserId = userId))
                   )
        }

    let save (db: CardOverflowDb) ankiDb userId (files: FileEntity seq) =
        result {
            let usersTags = db.PrivateTags.Where(fun pt -> pt.UserId = userId) |> Seq.toList
            let! cardEntities, histories, conceptUsers = load ankiDb usersTags userId
            conceptUsers |> db.ConceptUsers.AddRange
            cardEntities |> db.Cards.AddRange
            histories |> db.Histories.AddRange
            files |> db.Files.AddRange
            db.SaveChangesI ()
        }

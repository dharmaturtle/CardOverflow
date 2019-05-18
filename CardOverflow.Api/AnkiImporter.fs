namespace CardOverflow.Api

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Entity.Anki
open System
open System.Linq
open Thoth.Json.Net

module AnkiImporter =
    let getSimpleAnkiDb (ankiDbService: AnkiDbService) =
        { Cards = ankiDbService.Query(fun db -> db.Cards.ToList() ) |> List.ofSeq
          Cols = ankiDbService.Query(fun db -> db.Cols.ToList() ) |> List.ofSeq
          Notes = ankiDbService.Query(fun db -> db.Notes.ToList() ) |> List.ofSeq
          Revlogs = ankiDbService.Query(fun db -> db.Revlogs.ToList() ) |> List.ofSeq }

type AnkiImporter(ankiDb: SimpleAnkiDb, dbService: IDbService, userId: int) =
    member __.run() = // medTODO it should be possible to present to the user import errors *before* importing anything.
        let col = ankiDb.Cols.Single()
        ResultBuilder() {
            let! cardOptionByDeckConfigurationId =
                AnkiMap.parseDconf col.Dconf
                |> Result.bind (List.map (fun (deckConfigurationId, cardOption) -> (deckConfigurationId, (cardOption, cardOption.CopyToNew userId))) >> Map.ofList >> Ok )
            let! nameAndDeckConfigurationIdByDeckId =
                AnkiMap.parseDecks col.Decks
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
            let getCardOption deckId =
                let (_, deckConfigurationId) = nameAndDeckConfigurationIdByDeckId.[deckId] // medTODO tag imported cards with the name of the deck they're in
                cardOptionByDeckConfigurationId.[string deckConfigurationId]
            let! conceptTemplatesByModelId =
                AnkiMap.parseModels col.Models
                |> Result.map (Seq.map(fun (key, value) -> (key, value.CopyToNew userId)) >> Map.ofSeq )
            let conceptsByAnkiId =
                AnkiMap.parseNotes
                    conceptTemplatesByModelId
                    (dbService.Query(fun db -> db.PrivateTags.Where(fun pt -> pt.UserId = userId).ToList()) |> Seq.toList)
                    userId
                    []
                    (ankiDb.Notes)
                |> Map.ofList
            let collectionCreationTimeStamp = DateTimeOffset.FromUnixTimeSeconds(col.Crt).UtcDateTime
            let getCardEntities() =
                ankiDb.Cards
                |> List.map (AnkiMap.mapCard getCardOption conceptsByAnkiId collectionCreationTimeStamp)
                |> Result.consolidate
            let! _ = getCardEntities() // checking if there are any errors
            
            dbService.Command(fun db -> conceptsByAnkiId |> Map.toSeq |> Seq.map snd |> db.Concepts.AddRange)
            dbService.Command(fun db -> cardOptionByDeckConfigurationId |> Map.toSeq |> Seq.map snd |> Seq.map snd |> db.CardOptions.AddRange) // EF updates the options' Ids
            let! cardAndAnkiCards = getCardEntities() // called again to update the Card's Option Id (from the line above)
            dbService.Command(fun db -> cardAndAnkiCards |> Seq.map fst |> db.Cards.AddRange)
            let cardIdByAnkiId = cardAndAnkiCards |> Seq.map (fun (card, anki) -> anki.Id, card.Id) |> Map.ofSeq
            dbService.Command (fun x -> ankiDb.Revlogs |> Seq.map (AnkiMap.toHistory cardIdByAnkiId userId) |> x.Histories.AddRange)
            return ()
        }

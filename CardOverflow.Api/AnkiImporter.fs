namespace CardOverflow.Api

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Entity.Anki
open System
open System.Linq
open Thoth.Json.Net
open FsToolkit.ErrorHandling

module AnkiImporter =
    let getSimpleAnkiDb (ankiDbService: AnkiDbService) =
        { Cards = ankiDbService.Query(fun db -> db.Cards.ToList() ) |> List.ofSeq
          Cols = ankiDbService.Query(fun db -> db.Cols.ToList() ) |> List.ofSeq
          Notes = ankiDbService.Query(fun db -> db.Notes.ToList() ) |> List.ofSeq
          Revlogs = ankiDbService.Query(fun db -> db.Revlogs.ToList() ) |> List.ofSeq }
    let load ankiDb usersTags (userId: int) =
        let col = ankiDb.Cols.Single()
        result {
            let! cardOptionByDeckConfigurationId =
                AnkiMap.parseDconf col.Dconf
                |> Result.bind (List.map (fun (deckConfigurationId, cardOption) -> (deckConfigurationId, (cardOption, cardOption.CopyToNew userId))) >> Map.ofList >> Ok )
            let! nameAndDeckConfigurationIdByDeckId =
                AnkiMap.parseDecks col.Decks
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
            let getCardOption deckId =
                let (_, deckConfigurationId) = nameAndDeckConfigurationIdByDeckId.[deckId] // medTODO tag imported cards with the name of the deck they're in
                cardOptionByDeckConfigurationId.[string deckConfigurationId]
            let! conceptTemplatesByModelId =
                AnkiMap.parseModels col.Models
                |> Result.map (Seq.map(fun (modelId, conceptTemplate) -> (modelId, conceptTemplate.CopyToNew userId)) >> Map.ofSeq )
            let conceptsByAnkiId =
                AnkiMap.parseNotes
                    conceptTemplatesByModelId
                    usersTags
                    userId
                    []
                    ankiDb.Notes
                |> Map.ofList
            let collectionCreationTimeStamp = DateTimeOffset.FromUnixTimeSeconds(col.Crt).UtcDateTime
            let getCardEntities() =
                ankiDb.Cards
                |> List.map (AnkiMap.mapCard getCardOption conceptsByAnkiId collectionCreationTimeStamp)
                |> Result.consolidate
            let! _ = getCardEntities() // checking if there are any errors
            return (conceptsByAnkiId, cardOptionByDeckConfigurationId, getCardEntities, getCardOption)
        }

    let save (dbService: IDbService) ankiDb userId =
        result {
            let usersTags = dbService.Query(fun db -> db.PrivateTags.Where(fun pt -> pt.UserId = userId) |> Seq.toList)
            let! conceptsByAnkiId, cardOptionByDeckConfigurationId, getCardEntities, getCardOption = load ankiDb usersTags userId
            dbService.Command(fun db -> conceptsByAnkiId |> Map.toSeq |> Seq.map snd |> db.Concepts.AddRange)
            dbService.Command(fun db -> cardOptionByDeckConfigurationId |> Map.toSeq |> Seq.map snd |> Seq.map snd |> db.CardOptions.AddRange) // EF updates the options' Ids
            let updatedTemplates =
                conceptsByAnkiId
                |> Map.overValue (fun c ->
                    let conceptEntity = c.ConceptTemplate
                    conceptEntity.CardTemplates <-
                        CardTemplate.LoadMany conceptEntity.CardTemplates
                        |> List.map (fun cardTemplate ->
                            if cardTemplate.DefaultCardOptionId > 0
                            then cardTemplate
                            else { cardTemplate with
                                     DefaultCardOptionId =
                                        cardTemplate.DefaultCardOptionId * -1 |> getCardOption |> fun (_, entity) -> entity.Id }
                        ) |> CardTemplate.ManyToEntityString
                    conceptEntity
                )
            dbService.Command(fun db -> db.ConceptTemplates.UpdateRange updatedTemplates)
            let! cardAndAnkiCards = getCardEntities() // called again to update the Card's Option Id (from the line above)
            dbService.Command(fun db -> cardAndAnkiCards |> Seq.map fst |> db.Cards.AddRange)
            let cardIdByAnkiId = cardAndAnkiCards |> Seq.map (fun (card, anki) -> anki.Id, card.Id) |> Map.ofSeq
            dbService.Command (fun x -> ankiDb.Revlogs |> Seq.map (AnkiMap.toHistory cardIdByAnkiId userId) |> x.Histories.AddRange)
        }

module Projector

open Equinox
open FsCodec
open FsCodec.NewtonsoftJson
open Serilog
open TypeShape
open CardOverflow.Pure
open CardOverflow.Api
open FsToolkit.ErrorHandling
open Domain
open FSharp.UMX
open CardOverflow.Pure.AsyncOp
open System
open Domain.Projection

type ServerProjector (keyValueStore: KeyValueStore, elsea: Elsea.IClient) =
    let projectUser (userId: string) e =
        let templateCollectedOrDiscarded templateRevisionId transformUser incOrDec = async {
            let templateId = fst templateRevisionId
            let! templateSearch = elsea.GetTemplate templateId
            if templateSearch = None then failwith "Template search is None for some reason"
            let templateSearch = templateSearch.Value
            return!
                [ keyValueStore.Update transformUser userId
                  elsea.SetTemplateCollected templateId (incOrDec templateSearch.Collectors)
                ] |> Async.Parallel |>% ignore
            }
        match e with
        | User.Events.SignedUp signedUp ->
            let user = signedUp |> User.Fold.evolveSignedUp
            [ user                                      |> keyValueStore.InsertOrReplace |>% ignore
              user |> Kvs.Profile.fromSummary Set.empty |> keyValueStore.InsertOrReplace |>% ignore
            ] |> Async.Parallel |>% ignore
        | User.Events.OptionsEdited o ->
            keyValueStore.Update (User.Fold.evolveOptionsEdited o) userId
        | User.Events.TemplateCollected o -> templateCollectedOrDiscarded o.TemplateRevisionId (User.Fold.evolveTemplateCollected o) ((+) 1)
        | User.Events.TemplateDiscarded o -> templateCollectedOrDiscarded o.TemplateRevisionId (User.Fold.evolveTemplateDiscarded o) ((-) 1)
        | User.Events.CardSettingsEdited cs ->
            keyValueStore.Update (User.Fold.evolveCardSettingsEdited cs) userId
        | User.Events.Snapshotted d ->
            keyValueStore.Update (fun _ -> User.Fold.ofSnapshot d) userId
    
    let projectDeck (deckId: DeckId) e =
        match e with
        | Deck.Events.Created c -> async {
            let! author  = keyValueStore.GetUser c.Meta.UserId
            let! profile = keyValueStore.GetProfile c.Meta.UserId
            let summary = c |> Deck.Fold.evolveCreated
            let newDeck = DeckSearch.fromSummary' author.DisplayName 0 0 summary
            let search  = DeckSearch.fromSummary  author.DisplayName 0 0 summary
            let profile = { profile with Decks = profile.Decks |> Set.add newDeck }
            let summary =
                { summary with
                    Extra =
                        author.DisplayName
                        |> Projection.Kvs.DeckExtra.init
                        |> serializeToJson
                } |> Deck.Fold.Active
            return!
                [ summary |> keyValueStore.InsertOrReplace |>% ignore
                  profile |> keyValueStore.InsertOrReplace |>% ignore
                  elsea.UpsertDeck deckId search
                ] |> Async.Parallel |>% ignore
            }
        | _ -> async {
            let! kvsDeck = keyValueStore.GetDeck deckId
            let newState = Deck.Fold.evolve kvsDeck e
            let search =
                option {
                    let! summary = newState |> Deck.getActive |> Result.toOption
                    let deck = summary |> Projection.Kvs.Deck.fromSummary
                    let search = DeckSearch.fromSummary deck.Author deck.ExampleRevisionIds.Count deck.SourceOf summary
                    return elsea.UpsertDeck deckId search
                } |> Option.defaultWith (fun () -> elsea.DeleteDeck deckId)
            return!
                [ newState |> keyValueStore.InsertOrReplace |>% ignore
                  search ] |> Async.Parallel |>% ignore
            }
    
    let projectTemplate (templateId: TemplateId) e =
        let projectTemplate (template: Summary.Template) = async {
            let! author = keyValueStore.GetUser template.AuthorId
            let kvsTemplate = template |> Kvs.toKvsTemplate author.DisplayName Map.empty
            let search      = template |> TemplateSearch.fromSummary author.DisplayName
            return!
                [ keyValueStore.InsertOrReplace kvsTemplate |>% ignore
                  elsea.UpsertTemplate templateId search
                ] |> Async.Parallel |>% ignore
            }
        let deleteTemplate () =
            [ keyValueStore.Delete templateId |>% ignore
              elsea.DeleteTemplate templateId
            ] |> Async.Parallel |>% ignore
        match e with
        | Template.Events.Snapshotted s ->
            match s |> Template.Fold.ofSnapshot with
            | Template.Fold.Active x -> projectTemplate x
            | Template.Fold.Dmca   _ -> deleteTemplate ()
            | Template.Fold.Initial  -> Async.singleton ()
        | Template.Events.Created created -> created |> Template.Fold.evolveCreated |> projectTemplate
        | Template.Events.Edited e -> async {
            let! kvsTemplate = keyValueStore.GetTemplate templateId
            let kvsTemplate = kvsTemplate |> Kvs.evolveKvsTemplateEdited e
            let search = TemplateSearch.fromEdited e
            return!
                [ keyValueStore.InsertOrReplace kvsTemplate |>% ignore
                  elsea.UpsertTemplate templateId search
                ] |> Async.Parallel |>% ignore
            }

    let projectExample (exampleId: ExampleId) e =
        match e with
        | Example.Events.Snapshotted { State = state } -> failwith "not implemented"
            //match state with
            //| Example.Events.Compaction.Active e -> async {
            //    let! author = keyValueStore.GetUser e.AuthorId
            //    let! templates = e.Revisions |> List.map (fun x -> fst x.TemplateRevisionId) |> List.distinct |> keyValueStore.GetTemplates
            //    let templates = templates |> List.ofArray |> List.collect Kvs.allToTemplateInstance
            //    let example = e |> Kvs.toKvsExample author.DisplayName Map.empty templates // Map.empty will reset the count
            //    let search = templates |> Seq.filter (fun x -> x.Id = e.CurrentRevision.TemplateRevisionId) |> Seq.exactlyOne |> ExampleSearch.fromSummary e author.DisplayName
            //    return!
            //        [ keyValueStore.InsertOrReplace example |>% ignore
            //          elsea.UpsertExample exampleId search
            //        ] |> Async.Parallel |>% ignore
            //    }
        | Example.Events.Created created -> async {
            let! author = keyValueStore.GetUser created.Meta.UserId
            let! templateInstance = keyValueStore.GetTemplateInstance created.TemplateRevisionId
            let example = created |> Example.Fold.evolveCreated |> Kvs.toKvsExample author.DisplayName Map.empty [templateInstance]
            let concept = example |> Concept.FromExample []
            let search = ExampleSearch.fromSummary (Example.Fold.evolveCreated created) author.DisplayName templateInstance
            return!
                [ keyValueStore.InsertOrReplace example |>% ignore
                  keyValueStore.InsertOrReplace concept |>% ignore
                  elsea.UpsertExample exampleId search
                ] |> Async.Parallel |>% ignore
            }
        | Example.Events.Edited e -> async {
            let! (example: Kvs.Example) = keyValueStore.GetExample exampleId
            let! (concept: Concept)     = keyValueStore.GetConcept exampleId
            let! templates =
                let templates = example.Revisions |> List.map (fun x -> x.TemplateInstance)
                if templates |> Seq.exists (fun x -> x.Id = e.TemplateRevisionId) then
                    templates |> Async.singleton
                else async {
                    let! templateInstance = keyValueStore.GetTemplateInstance e.TemplateRevisionId
                    return templateInstance :: templates
                    }
            let kvsExample = example |> Kvs.evolveKvsExampleEdited e templates
            let concept = kvsExample |> Concept.FromExample concept.Children
            let search = templates |> Seq.filter (fun x -> x.Id = e.TemplateRevisionId) |> Seq.head |> ExampleSearch.fromEdited e
            return!
                [ keyValueStore.InsertOrReplace kvsExample |>% ignore
                  keyValueStore.InsertOrReplace concept    |>% ignore
                  elsea.UpsertExample exampleId search
                ] |> Async.Parallel |>% ignore
            }

    let projectStack (stackId: string) e =
        match e with
        | Stack.Events.Snapshotted { State = state } -> failwith "not implemented"
        | Stack.Events.Created created -> async {
            let exampleId, ordinal = created.ExampleRevisionId
            let! profile = keyValueStore.GetProfile created.Meta.UserId
            let! decks, profile = Kvs.handleDeckChanged created.ExampleRevisionId keyValueStore.GetDecks profile created.DeckIds (+) Set.add
            let deckSearchUpdates =
                profile.Decks
                |> Set.toList
                |> List.filter (fun x -> created.DeckIds.Contains x.Id)
                |> List.map (fun deck ->
                    elsea.SetDeckExampleCount deck.Id deck.ExampleCount // lowTODO make overload which takes a list of deckIds with their ExampleCounts
                ) |> Async.Parallel |>% ignore
            let! example =
                exampleId
                |> keyValueStore.GetExample
                |>% Kvs.incrementExample ordinal
            let! concept =
                exampleId
                |> keyValueStore.GetConcept
                |>% fun x -> { x with Collectors = x.Collectors + 1 }
            return!
                deckSearchUpdates ::
                [ created |> Stack.Fold.evolveCreated |> keyValueStore.InsertOrReplace                   |>% ignore
                  example                             |> keyValueStore.InsertOrReplace                   |>% ignore
                  concept                             |> keyValueStore.InsertOrReplace                   |>% ignore
                  profile                             |> keyValueStore.InsertOrReplace                   |>% ignore
                  decks                     |> Array.map keyValueStore.InsertOrReplace |> Async.Parallel |>% ignore
                  elsea.SetExampleCollected exampleId example.Collectors
                ] |> Async.Parallel |>% ignore
            }
        | Stack.Events.Discarded e -> async {
            let! stack   = keyValueStore.GetStack stackId
            let! profile = keyValueStore.GetProfile e.Meta.UserId
            let! decks, profile = Kvs.handleDeckChanged stack.ExampleRevisionId keyValueStore.GetDecks profile stack.DeckIds (-) Set.remove
            let deckSearchUpdates =
                profile.Decks
                |> Set.toList
                |> List.filter (fun x -> stack.DeckIds.Contains x.Id)
                |> List.map (fun deck ->
                    elsea.SetDeckExampleCount deck.Id deck.ExampleCount // lowTODO make overload which takes a list of deckIds with their ExampleCounts
                ) |> Async.Parallel |>% ignore
            let exampleId, ordinal = stack.ExampleRevisionId
            let! example =
                exampleId
                |> keyValueStore.GetExample
                |>% Kvs.decrementExample ordinal
            return! deckSearchUpdates ::
                    [elsea.SetExampleCollected exampleId example.Collectors
                     decks |> Array.map keyValueStore.InsertOrReplace |> Async.Parallel |>% ignore
                     example         |> keyValueStore.InsertOrReplace                   |>% ignore
                     profile         |> keyValueStore.InsertOrReplace                   |>% ignore
                     keyValueStore.Delete stackId ] |> Async.Parallel |> Async.map ignore
            }
        | Stack.Events.Edited e ->
            keyValueStore.Update (Stack.Fold.evolveEdited e) stackId
        | Stack.Events.TagsChanged e ->
            keyValueStore.Update (Stack.Fold.evolveTagsChanged e) stackId
        | Stack.Events.CardStateChanged e ->
            keyValueStore.Update (Stack.Fold.evolveCardStateChanged e) stackId
        | Stack.Events.CardSettingChanged e ->
            keyValueStore.Update (Stack.Fold.evolveCardSettingChanged e) stackId
        | Stack.Events.DecksChanged e -> async {
            let! profile  = keyValueStore.GetProfile e.Meta.UserId
            let! oldStack = keyValueStore.GetStack stackId
            let  newStack = oldStack |> Stack.Fold.evolveDecksChanged e
            let addedDecks   = Set.difference newStack.DeckIds oldStack.DeckIds
            let removedDecks = Set.difference oldStack.DeckIds newStack.DeckIds
            let handle = Kvs.handleDeckChanged newStack.ExampleRevisionId keyValueStore.GetDecks
            let! addedDecks  , profile = handle profile addedDecks   (+) Set.add
            let! removedDecks, profile = handle profile removedDecks (-) Set.remove
            let deckSearchUpdates =
                profile.Decks
                |> Set.toList
                |> List.filter (fun x -> e.DeckIds.Contains x.Id || oldStack.DeckIds.Contains x.Id)
                |> List.map (fun deck ->
                    elsea.SetDeckExampleCount deck.Id deck.ExampleCount // lowTODO make overload which takes a list of deckIds with their ExampleCounts
                ) |> Async.Parallel |>% ignore
            let decks = Array.append addedDecks removedDecks
            return!
                deckSearchUpdates ::
                [ profile         |> keyValueStore.InsertOrReplace                   |>% ignore
                  newStack        |> keyValueStore.InsertOrReplace                   |>% ignore
                  decks |> Array.map keyValueStore.InsertOrReplace |> Async.Parallel |>% ignore
                ] |> Async.Parallel |>% ignore
            }
        | Stack.Events.Reviewed e ->
            keyValueStore.Update (Stack.Fold.evolveReviewed e) stackId
        | Stack.Events.RevisionChanged e -> async {
            let! (stack: Summary.Stack) = keyValueStore.GetStack stackId
            let oldId, oldOrdinal = stack.ExampleRevisionId
            let newId, newOrdinal = e.RevisionId
            let! newExample = keyValueStore.GetExample newId
            let! exampleInserts =
                if oldId = newId then
                    let newExample =
                        newExample
                        |> Kvs.incrementExample newOrdinal
                        |> Kvs.decrementExample oldOrdinal
                    [ keyValueStore.InsertOrReplace newExample |>% ignore
                      elsea.SetExampleCollected newId newExample.Collectors
                    ] |> Async.singleton
                else async {
                    let! oldExample = keyValueStore.GetExample oldId
                    let oldExample = oldExample |> Kvs.decrementExample oldOrdinal
                    let newExample = newExample |> Kvs.incrementExample newOrdinal
                    let! oldConcept = oldId |> keyValueStore.GetConcept |>% fun x -> { x with Collectors = x.Collectors - 1 }
                    let! newConcept = newId |> keyValueStore.GetConcept |>% fun x -> { x with Collectors = x.Collectors + 1 }
                    return [ newExample |> keyValueStore.InsertOrReplace |>% ignore
                             oldExample |> keyValueStore.InsertOrReplace |>% ignore
                             newConcept |> keyValueStore.InsertOrReplace |>% ignore
                             oldConcept |> keyValueStore.InsertOrReplace |>% ignore
                             elsea.SetExampleCollected newId newExample.Collectors
                             elsea.SetExampleCollected oldId oldExample.Collectors ]
                    }
            let stackInsert = stack |> Stack.Fold.evolveRevisionChanged e |> keyValueStore.InsertOrReplace |>% ignore
            return! stackInsert :: exampleInserts |> Async.Parallel |>% ignore
            }

    member _.Project(streamName:StreamName, events:ITimelineEvent<byte[]> []) =
        let category, id = streamName |> StreamName.splitCategoryAndId
        match category with
        | "Example"  -> let id = % Guid.Parse id
                        events |> Array.map (Example .Events.codec.TryDecode >> Option.get >> projectExample  id)
        | "User"     -> events |> Array.map (User    .Events.codec.TryDecode >> Option.get >> projectUser     id)
        | "Deck"     -> let id = % Guid.Parse id
                        events |> Array.map (Deck    .Events.codec.TryDecode >> Option.get >> projectDeck     id)
        | "Template" -> let id = % Guid.Parse id
                        events |> Array.map (Template.Events.codec.TryDecode >> Option.get >> projectTemplate id)
        | "Stack"    -> events |> Array.map (Stack   .Events.codec.TryDecode >> Option.get >> projectStack    id)
        | _ -> failwith $"Unsupported category: {category}"
        |> Async.Parallel

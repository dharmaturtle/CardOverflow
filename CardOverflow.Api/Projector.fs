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

module Async =
    // We use Async.Sequential when debugging since Async.Parallel introduces nondeterminism.
    // To simulate nondeterministic thread scheduling we shuffle, but with a known seed so runs can be replicated.
    let parallelIgnore xs =
        #if DEBUG
            let swap (a: _[]) x y = // http://www.fssnip.net/L/title/Array-shuffle
                let tmp = a.[x]
                a.[x] <- a.[y]
                a.[y] <- tmp
            let shuffle a = Array.iteri (fun i _ -> swap a i (IdempotentTest.random.Next(i, Array.length a))) a
            let r = xs |> Seq.toArray
            shuffle r
            r |> Async.Sequential |> Async.Ignore
        #else
            xs |> Async.Parallel |> Async.Ignore
        #endif

type ServerProjector (keyValueStore: KeyValueStore, elsea: Elsea.IClient) =
    let projectUser (userId: string) e =
        let templateCollectedOrDiscarded templateRevisionId commandId transformUser (f: TemplateRevisionOrdinal -> (CommandId -> (Kvs.Template -> int) -> Kvs.Template -> Kvs.Template option * int)) = async {
            let (templateId: TemplateId), ordinal = templateRevisionId
            let! template = keyValueStore.GetTemplate templateId
            let template, collectors = f ordinal commandId (fun x -> x.Collectors) template
            return!
                [ keyValueStore.Update transformUser userId
                  keyValueStore.InsertOrReplace template
                  elsea.SetTemplateCollected templateId collectors
                ] |> Async.parallelIgnore
            }
        match e with
        | User.Events.SignedUp signedUp ->
            let user = signedUp |> User.Fold.evolveSignedUp
            [ user                                      |> keyValueStore.InsertOrReplace
              user |> Kvs.Profile.fromSummary Set.empty |> keyValueStore.InsertOrReplace
            ] |> Async.parallelIgnore
        | User.Events.OptionsEdited o ->
            keyValueStore.Update (User.Fold.evolveOptionsEdited o) userId
        | User.Events.TemplateCollected o -> templateCollectedOrDiscarded o.TemplateRevisionId o.Meta.CommandId (User.Fold.evolveTemplateCollected o) Kvs.tryIncrementTemplate
        | User.Events.TemplateDiscarded o -> templateCollectedOrDiscarded o.TemplateRevisionId o.Meta.CommandId (User.Fold.evolveTemplateDiscarded o) Kvs.tryDecrementTemplate
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
                [ summary |> keyValueStore.InsertOrReplace
                  profile |> keyValueStore.InsertOrReplace
                  elsea.UpsertDeck deckId search
                ] |> Async.parallelIgnore
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
                [ newState |> keyValueStore.InsertOrReplace
                  search ] |> Async.parallelIgnore
            }
    
    let projectTemplate (templateId: TemplateId) e =
        let projectTemplate (template: Summary.Template) = async {
            let! author = keyValueStore.GetUser template.AuthorId
            let kvsTemplate = template |> Kvs.toKvsTemplate author.DisplayName Map.empty
            let search      = template |> TemplateSearch.fromSummary author.DisplayName
            return!
                [ keyValueStore.InsertOrReplace kvsTemplate
                  elsea.UpsertTemplate templateId search
                ] |> Async.parallelIgnore
            }
        let deleteTemplate () =
            [ keyValueStore.Delete templateId
              elsea.DeleteTemplate templateId
            ] |> Async.parallelIgnore
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
                [ keyValueStore.InsertOrReplace kvsTemplate
                  elsea.UpsertTemplate templateId search
                ] |> Async.parallelIgnore
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
            //        [ keyValueStore.InsertOrReplace example
            //          elsea.UpsertExample exampleId search
            //        ] |> Async.parallelIgnore
            //    }
        | Example.Events.Created created -> async {
            let! author = keyValueStore.GetUser created.Meta.UserId
            let! templateInstance = keyValueStore.GetTemplateInstance created.TemplateRevisionId
            let example = created |> Example.Fold.evolveCreated |> Kvs.toKvsExample author.DisplayName Map.empty [templateInstance]
            let concept = example |> Concept.FromExample []
            let search = ExampleSearch.fromSummary (Example.Fold.evolveCreated created) author.DisplayName templateInstance
            return!
                [ keyValueStore.InsertOrReplace example
                  keyValueStore.InsertOrReplace concept
                  elsea.UpsertExample exampleId search
                ] |> Async.parallelIgnore
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
                [ keyValueStore.InsertOrReplace kvsExample
                  keyValueStore.InsertOrReplace concept
                  elsea.UpsertExample exampleId search
                ] |> Async.parallelIgnore
            }

    let projectStack (stackId: string) e =
        match e with
        | Stack.Events.Snapshotted { State = state } -> failwith "not implemented"
        | Stack.Events.Created created -> async {
            let exampleId, ordinal = created.ExampleRevisionId
            let! profile = keyValueStore.GetProfile created.Meta.UserId
            let! decks, profile = Kvs.incrementDeckChanged created.DeckIds created.ExampleRevisionId keyValueStore.GetDecks profile created.Meta.CommandId
            let deckSearchUpdates =
                profile.Decks
                |> Set.toList
                |> List.filter (fun x -> created.DeckIds.Contains x.Id)
                |> List.map (fun deck ->
                    elsea.SetDeckExampleCount deck.Id deck.ExampleCount // lowTODO make overload which takes a list of deckIds with their ExampleCounts
                ) |> Async.parallelIgnore
            let! example, _ =
                exampleId
                |> keyValueStore.GetExample
                |>% Kvs.tryIncrementExample ordinal created.Meta.CommandId id
            let! concept, collectors =
                exampleId
                |> keyValueStore.GetConcept
                |>% Concept.tryIncrementCollectors created.Meta.CommandId (fun x -> x.Collectors)
            return!
                deckSearchUpdates ::
                [ created |> Stack.Fold.evolveCreated |> keyValueStore.InsertOrReplace
                  example                             |> keyValueStore.InsertOrReplace
                  concept                             |> keyValueStore.InsertOrReplace
                  profile                             |> keyValueStore.InsertOrReplace
                  decks                     |> Array.map keyValueStore.InsertOrReplace |> Async.parallelIgnore
                  elsea.SetExampleCollected exampleId collectors
                ] |> Async.parallelIgnore
            }
        | Stack.Events.Discarded e -> async {
            let! stack   = keyValueStore.GetStack stackId
            let! profile = keyValueStore.GetProfile e.Meta.UserId
            let! decks, profile = Kvs.decrementDeckChanged stack.DeckIds stack.ExampleRevisionId keyValueStore.GetDecks profile e.Meta.CommandId
            let deckSearchUpdates =
                profile.Decks
                |> Set.toList
                |> List.filter (fun x -> stack.DeckIds.Contains x.Id)
                |> List.map (fun deck ->
                    elsea.SetDeckExampleCount deck.Id deck.ExampleCount // lowTODO make overload which takes a list of deckIds with their ExampleCounts
                ) |> Async.parallelIgnore
            let exampleId, ordinal = stack.ExampleRevisionId
            let! example, _ =
                exampleId
                |> keyValueStore.GetExample
                |>% Kvs.tryDecrementExample ordinal e.Meta.CommandId id
            let! concept, collectors =
                exampleId
                |> keyValueStore.GetConcept
                |>% Concept.tryDecrementCollectors e.Meta.CommandId (fun x -> x.Collectors)
            return! deckSearchUpdates ::
                    [elsea.SetExampleCollected exampleId collectors
                     decks |> Array.map keyValueStore.InsertOrReplace |> Async.parallelIgnore
                     example         |> keyValueStore.InsertOrReplace
                     profile         |> keyValueStore.InsertOrReplace
                     concept         |> keyValueStore.InsertOrReplace
                     keyValueStore.Delete stackId ] |> Async.parallelIgnore
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
            let   addedDecks = Set.difference newStack.DeckIds oldStack.DeckIds
            let removedDecks = Set.difference oldStack.DeckIds newStack.DeckIds
            let! decks, profile = Kvs.decrementIncrementDeckChanged removedDecks addedDecks newStack.ExampleRevisionId keyValueStore.GetDecks profile e.Meta.CommandId
            let deckSearchUpdates =
                profile.Decks
                |> Set.toList
                |> List.filter (fun x -> e.DeckIds.Contains x.Id || oldStack.DeckIds.Contains x.Id)
                |> List.map (fun deck ->
                    elsea.SetDeckExampleCount deck.Id deck.ExampleCount // lowTODO make overload which takes a list of deckIds with their ExampleCounts
                ) |> Async.parallelIgnore
            return!
                deckSearchUpdates ::
                [ profile         |> keyValueStore.InsertOrReplace
                  newStack        |> keyValueStore.InsertOrReplace
                  decks |> Array.map keyValueStore.InsertOrReplace |> Async.parallelIgnore
                ] |> Async.parallelIgnore
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
                    let newExample, collectors =
                        let crementedExample =
                            newExample
                            |> Kvs.decrementIncrementExample oldOrdinal newOrdinal e.Meta.CommandId
                        let newExample =
                            if newExample = crementedExample
                            then None
                            else Some crementedExample
                        newExample, crementedExample.Collectors
                    [ keyValueStore.InsertOrReplace newExample
                      elsea.SetExampleCollected newId collectors
                    ] |> Async.singleton
                else async {
                    let! oldExample = keyValueStore.GetExample oldId
                    let  oldExample, oldCollectors = oldExample |> Kvs.tryDecrementExample oldOrdinal e.Meta.CommandId (fun x -> x.Collectors)
                    let  newExample, newCollectors = newExample |> Kvs.tryIncrementExample newOrdinal e.Meta.CommandId (fun x -> x.Collectors)
                    let! oldConcept, _ = oldId |> keyValueStore.GetConcept |>% Concept.tryDecrementCollectors e.Meta.CommandId id
                    let! newConcept, _ = newId |> keyValueStore.GetConcept |>% Concept.tryIncrementCollectors e.Meta.CommandId id
                    return [ newExample |> keyValueStore.InsertOrReplace
                             oldExample |> keyValueStore.InsertOrReplace
                             newConcept |> keyValueStore.InsertOrReplace
                             oldConcept |> keyValueStore.InsertOrReplace
                             elsea.SetExampleCollected newId newCollectors
                             elsea.SetExampleCollected oldId oldCollectors ]
                    }
            let stackInsert = stack |> Stack.Fold.evolveRevisionChanged e |> keyValueStore.InsertOrReplace
            return! stackInsert :: exampleInserts |> Async.parallelIgnore
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
        |> Async.parallelIgnore

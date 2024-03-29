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
open Domain

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

// † Some projections query "old" data, update other projections with that data, then update/delete that "old" data. Therefore executing that final update/delete *after* the other projections may be necessary to maintain idempotency.
type ServerProjector (keyValueStore: KeyValueStore, elsea: Elsea.IClient) =
    let projectUser (userId: string) e =
        let templateCollectedOrDiscarded templateRevisionId commandId transformUser (f: PublicTemplateOrdinal -> (CommandId -> (Kvs.Template -> int) -> Kvs.Template -> Kvs.Template option * int)) = async {
            let (templateId: PublicTemplateId), ordinal = templateRevisionId
            let! template, templateEtag = keyValueStore.GetTemplate templateId
            let template, collectors = f ordinal commandId (fun x -> x.Collectors) template
            do! [ keyValueStore.Update transformUser userId
                  elsea.SetTemplateCollected templateId collectors
                ] |> Async.parallelIgnore
            return! keyValueStore.Replace (template, templateEtag)
            }
        match e with
        | User.Events.SignedUp signedUp ->
            let user = signedUp |> User.Fold.evolveSignedUp
            [ user                                      |> keyValueStore.Insert
              user |> Kvs.Profile.fromSummary Set.empty |> keyValueStore.Insert
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
            let! profile, profileEtag = keyValueStore.GetProfile c.Meta.UserId
            let summary = c |> Deck.Fold.evolveCreated
            let newDeck = DeckSearch.fromSummary' profile.DisplayName 0 0 summary
            let search  = DeckSearch.fromSummary  profile.DisplayName 0 0 summary
            let profile = { profile with Decks = profile.Decks |> Set.add newDeck }
            let summary =
                { summary with
                    Extra =
                        profile.DisplayName
                        |> Projection.Kvs.DeckExtra.init
                        |> serializeToJson
                } |> Deck.Fold.Active
            return!
                [ summary |> keyValueStore.Insert
                  keyValueStore.Replace (profile, profileEtag)
                  elsea.UpsertDeck deckId search
                ] |> Async.parallelIgnore
            }
        | _ -> async {
            let! kvsDeck, kvsDeckEtag = keyValueStore.GetDeck deckId
            let kvsDeck = Deck.Fold.evolve kvsDeck e
            let search =
                option {
                    let! summary = kvsDeck |> Deck.getActive |> Result.toOption
                    let deck = summary |> Projection.Kvs.Deck.fromSummary
                    let search = DeckSearch.fromSummary deck.Author deck.ExampleRevisionIds.Count deck.SourceOf summary
                    return elsea.UpsertDeck deckId search
                } |> Option.defaultWith (fun () -> elsea.DeleteDeck deckId)
            return!
                [ keyValueStore.Replace (kvsDeck, kvsDeckEtag)
                  search ] |> Async.parallelIgnore
            }
    
    let projectTemplate (templateId: PublicTemplateId) e =
        let createTemplate (template: Summary.PublicTemplate) = async {
            let! author = keyValueStore.GetUser_ template.AuthorId
            let kvsTemplate = template |> Kvs.toKvsTemplate author.DisplayName Map.empty
            let search      = template |> TemplateSearch.fromSummary author.DisplayName
            return!
                [ keyValueStore.Insert kvsTemplate
                  elsea.UpsertTemplate templateId search
                ] |> Async.parallelIgnore
            }
        let deleteTemplate () =
            [ keyValueStore.Delete templateId
              elsea.DeleteTemplate templateId
            ] |> Async.parallelIgnore
        match e with
        | PublicTemplate.Events.Snapshotted s ->
            match s |> PublicTemplate.Fold.ofSnapshot with
            | PublicTemplate.Fold.Active x -> createTemplate x // this is a bug. Fix when you get to figuring out projections and snapshots.
            | PublicTemplate.Fold.Dmca   _ -> deleteTemplate ()
            | PublicTemplate.Fold.Delete _ -> deleteTemplate ()
            | PublicTemplate.Fold.Initial  -> Async.singleton ()
        | PublicTemplate.Events.Created  e -> e |> PublicTemplate.Fold.evolveCreated |> createTemplate
        | PublicTemplate.Events.Deleted  _ -> deleteTemplate ()
        | PublicTemplate.Events.Edited   e -> async {
            let! kvsTemplate, kvsTemplateEtag = keyValueStore.GetTemplate templateId
            let kvsTemplate = kvsTemplate |> Kvs.evolveKvsTemplateEdited e
            let search = TemplateSearch.fromEdited e
            return!
                [ keyValueStore.Replace (kvsTemplate, kvsTemplateEtag)
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
            let! author           = keyValueStore.GetUser_ created.Meta.UserId
            let! templateInstance = keyValueStore.GetTemplateInstance_ created.TemplateRevisionId
            let example = created |> Example.Fold.evolveCreated |> Kvs.toKvsExample author.DisplayName Map.empty [templateInstance]
            let concept = example |> Concept.FromExample []
            let search = ExampleSearch.fromSummary (Example.Fold.evolveCreated created) author.DisplayName templateInstance
            return!
                [ keyValueStore.Insert example
                  keyValueStore.Insert concept
                  elsea.UpsertExample exampleId search
                ] |> Async.parallelIgnore
            }
        | Example.Events.Edited e -> async {
            let! example, exampleEtag = keyValueStore.GetExample exampleId
            let! concept, conceptEtag = keyValueStore.GetConcept exampleId
            let! templates =
                let templates = example.Revisions |> List.map (fun x -> x.TemplateInstance)
                if templates |> Seq.exists (fun x -> x.Id = e.TemplateRevisionId) then
                    templates |> Async.singleton
                else async {
                    let! templateInstance = keyValueStore.GetTemplateInstance_ e.TemplateRevisionId
                    return templateInstance :: templates
                    }
            let kvsExample = example |> Kvs.evolveKvsExampleEdited e templates
            let concept = kvsExample |> Concept.FromExample concept.Children
            let search = templates |> Seq.filter (fun x -> x.Id = e.TemplateRevisionId) |> Seq.head |> ExampleSearch.fromEdited e
            return!
                [ keyValueStore.Replace (kvsExample, exampleEtag)
                  keyValueStore.Replace (concept   , conceptEtag)
                  elsea.UpsertExample exampleId search
                ] |> Async.parallelIgnore
            }
        | Example.Events.CommentAdded e -> async {
            let! example, exampleEtag = keyValueStore.GetExample exampleId
            let! concept, conceptEtag = keyValueStore.GetConcept exampleId
            let kvsExample = example |> Kvs.evolveKvsExampleCommentAdded e
            let concept = kvsExample |> Concept.FromExample concept.Children
            return!
                [ keyValueStore.Replace (kvsExample, exampleEtag)
                  keyValueStore.Replace (concept   , conceptEtag)
                ] |> Async.parallelIgnore
            }

    let projectStack (stackId: string) e =
        match e with
        | Stack.Events.Snapshotted { State = state } -> failwith "not implemented"
        | Stack.Events.Created created -> async {
            let common = created |> Stack.Fold.evolveCreated |> keyValueStore.Insert
            match created.ExampleRevisionId with
            | None -> return! common
            | Some (exampleId, ordinal) ->
                let! profile, profileEtag = keyValueStore.GetProfile created.Meta.UserId
                let! decks, profile = Kvs.incrementDeckChanged created.DeckIds (exampleId, ordinal) keyValueStore.GetDecks profile created.Meta.CommandId
                let deckSearchUpdates =
                    profile.Decks
                    |> Set.toList
                    |> List.filter (fun x -> created.DeckIds.Contains x.Id)
                    |> List.map (fun deck ->
                        elsea.SetDeckExampleCount deck.Id deck.ExampleCount // lowTODO make overload which takes a list of deckIds with their ExampleCounts
                    ) |> Async.parallelIgnore
                let! (example, _), exampleEtag =
                    exampleId
                    |> keyValueStore.GetExample
                    |>% mapFst (Kvs.tryIncrementExample ordinal created.Meta.CommandId id)
                let! (concept, collectors), conceptEtag =
                    exampleId
                    |> keyValueStore.GetConcept
                    |>% mapFst (Concept.tryIncrementCollectors created.Meta.CommandId (fun x -> x.Collectors))
                return!
                    deckSearchUpdates ::
                    ( decks |> List.map keyValueStore.Replace) @
                    [ common
                      keyValueStore.Replace (example, exampleEtag)
                      keyValueStore.Replace (concept, conceptEtag)
                      keyValueStore.Replace (profile, profileEtag)
                      elsea.SetExampleCollected exampleId collectors
                    ] |> Async.parallelIgnore
            }
        | Stack.Events.Discarded e -> async {
            let! stack = keyValueStore.TryGet<Summary.Stack> stackId
            match stack with
            | None -> return ()
            | Some (stack, _) ->
                let common = keyValueStore.Delete stackId // †
                match stack.ExampleRevisionId with
                | None -> return! common
                | Some (exampleId, ordinal) ->
                    let! profile, profileEtag = keyValueStore.GetProfile e.Meta.UserId
                    let! decks, profile = Kvs.decrementDeckChanged stack.DeckIds (exampleId, ordinal) keyValueStore.GetDecks profile e.Meta.CommandId
                    let deckSearchUpdates =
                        profile.Decks
                        |> Set.toList
                        |> List.filter (fun x -> stack.DeckIds.Contains x.Id)
                        |> List.map (fun deck ->
                            elsea.SetDeckExampleCount deck.Id deck.ExampleCount // lowTODO make overload which takes a list of deckIds with their ExampleCounts
                        ) |> Async.parallelIgnore
                    let! (example, _), exampleEtag =
                        exampleId
                        |> keyValueStore.GetExample
                        |>% mapFst (Kvs.tryDecrementExample ordinal e.Meta.CommandId id)
                    let! (concept, collectors), conceptEtag =
                        exampleId
                        |> keyValueStore.GetConcept
                        |>% mapFst (Concept.tryDecrementCollectors e.Meta.CommandId (fun x -> x.Collectors))
                    do! deckSearchUpdates ::
                        ( decks |> List.map keyValueStore.Replace) @
                        [ keyValueStore.Replace (example, exampleEtag)
                          keyValueStore.Replace (profile, profileEtag)
                          keyValueStore.Replace (concept, conceptEtag)
                          elsea.SetExampleCollected exampleId collectors ] |> Async.parallelIgnore
                    return! common
            }
        | Stack.Events.Edited e ->
            keyValueStore.Update (Stack.Fold.evolveEdited e) stackId
        | Stack.Events.TagAdded e ->
            keyValueStore.Update (Stack.Fold.evolveTagAdded e) stackId
        | Stack.Events.TagRemoved e ->
            keyValueStore.Update (Stack.Fold.evolveTagRemoved e) stackId
        | Stack.Events.CardStateChanged e ->
            keyValueStore.Update (Stack.Fold.evolveCardStateChanged e) stackId
        | Stack.Events.CardSettingChanged e ->
            keyValueStore.Update (Stack.Fold.evolveCardSettingChanged e) stackId
        | Stack.Events.DecksChanged e -> async {
            let! profile , profileEtag = keyValueStore.GetProfile e.Meta.UserId
            let! oldStack,   stackEtag = keyValueStore.GetStack stackId
            let  newStack = oldStack |> Stack.Fold.evolveDecksChanged e
            let   addedDecks = Set.difference newStack.DeckIds oldStack.DeckIds
            let removedDecks = Set.difference oldStack.DeckIds newStack.DeckIds
            match newStack.ExampleRevisionId with
            | None -> ()
            | Some exampleRevisionId ->
                let! decks, profile = Kvs.decrementIncrementDeckChanged removedDecks addedDecks exampleRevisionId keyValueStore.GetDecks profile e.Meta.CommandId
                let deckSearchUpdates =
                    profile.Decks
                    |> Set.toList
                    |> List.filter (fun x -> e.DeckIds.Contains x.Id || oldStack.DeckIds.Contains x.Id)
                    |> List.map (fun deck ->
                        elsea.SetDeckExampleCount deck.Id deck.ExampleCount // lowTODO make overload which takes a list of deckIds with their ExampleCounts
                    )
                do! ( decks |> List.map keyValueStore.Replace ) @
                    [ keyValueStore.Replace (profile, profileEtag) ] @
                    deckSearchUpdates                                   |> Async.parallelIgnore
            return! keyValueStore.Replace (newStack, stackEtag)
            }
        | Stack.Events.Reviewed e ->
            keyValueStore.Update (Stack.Fold.evolveReviewed e) stackId
        | Stack.Events.RevisionChanged e -> async {
            let incDec kvsIncDec conceptIncDec (exampleId: ExampleId) exampleOrdinal = async {
                let! example, exampleEtag = keyValueStore.GetExample exampleId
                let  (example: _ Option), collectors = example |> kvsIncDec exampleOrdinal e.Meta.CommandId (fun (x: Kvs.Example) -> x.Collectors)
                let! (concept: _ Option, _), conceptEtag = exampleId |> keyValueStore.GetConcept |>% mapFst (conceptIncDec e.Meta.CommandId id)
                return [ keyValueStore.Replace (example, exampleEtag)
                         keyValueStore.Replace (concept, conceptEtag)
                         elsea.SetExampleCollected exampleId collectors ]
                }
            let increment = incDec Kvs.tryIncrementExample Concept.tryIncrementCollectors
            let decrement = incDec Kvs.tryDecrementExample Concept.tryDecrementCollectors
            let! stack, stackEtag = keyValueStore.GetStack stackId
            match stack.ExampleRevisionId, e.RevisionId with
            | Some (oldId, oldOrdinal), Some (newId, newOrdinal) ->
                if oldId = newId then
                    let! newExample, newExampleEtag = keyValueStore.GetExample newId
                    let newExample, collectors =
                        let crementedExample =
                            newExample
                            |> Kvs.decrementIncrementExample oldOrdinal newOrdinal e.Meta.CommandId
                        let newExample =
                            if newExample = crementedExample
                            then None
                            else Some crementedExample
                        newExample, crementedExample.Collectors
                    do!
                        [ keyValueStore.Replace (newExample, newExampleEtag)
                          elsea.SetExampleCollected newId collectors
                        ] |> Async.parallelIgnore
                else
                    do! [ increment newId newOrdinal
                          decrement oldId oldOrdinal ]
                        |> Async.Parallel                                      // first queries kvs for example/concept in parallel
                        |> Async.bind (Seq.collect id >> Async.parallelIgnore) // then does replace/set in parallel
            | None                    , None                     -> ()
            | Some (oldId, oldOrdinal), None                     -> do! decrement oldId oldOrdinal |> Async.bind Async.parallelIgnore
            | None                    , Some (newId, newOrdinal) -> do! increment newId newOrdinal |> Async.bind Async.parallelIgnore
            let stack = stack |> Stack.Fold.evolveRevisionChanged e
            return! keyValueStore.Replace (stack, stackEtag) // †
            }

    member _.Project(streamName:StreamName, events:ITimelineEvent<byte[]> []) =
        let category, id = streamName |> StreamName.splitCategoryAndId
        match category with
        | "Example"               -> let id = % Guid.Parse id
                                     events |> Array.map (Example       .Events.codec.TryDecode >> Option.get >> projectExample  id)
        | "User"                  -> events |> Array.map (User          .Events.codec.TryDecode >> Option.get >> projectUser     id)
        | Deck          .category -> let id = % Guid.Parse id
                                     events |> Array.map (Deck          .Events.codec.TryDecode >> Option.get >> projectDeck     id)
        | PublicTemplate.category -> let id = % Guid.Parse id
                                     events |> Array.map (PublicTemplate.Events.codec.TryDecode >> Option.get >> projectTemplate id)
        | "Stack"                 -> events |> Array.map (Stack         .Events.codec.TryDecode >> Option.get >> projectStack    id)
        | _                       -> failwith $"Unsupported category: {category}"
        |> Async.parallelIgnore

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
open Infrastructure
open FSharp.UMX
open CardOverflow.Pure.AsyncOp
open System
open Domain.Projection

type ServerProjector (keyValueStore: KeyValueStore, elsea: Elsea.IClient, elasticClient: Nest.IElasticClient) =
    let projectUser     id user     =   keyValueStore.UpsertUser'     id user
    
    let projectDeck (deckId: string) e =
        match e with
        | Deck.Events.Created c -> async {
            let! author = keyValueStore.GetUser c.Meta.UserId
            let extra: Kvs.DeckExtra =
                {  Author             = author.DisplayName
                   ExampleRevisionIds = Set.empty
                   SourceOf           = 0 }
            let kvsDeck: Kvs.Deck =
                { Deck  = c |> Deck.Fold.evolveCreated |> Deck.Fold.Active
                  Id    = c.Id
                  Extra = Some extra }
            return!
                [ kvsDeck |> keyValueStore.InsertOrReplace |>% ignore
                ] |> Async.Parallel |>% ignore
            }
        | _ -> async {
            let! kvsDeck = keyValueStore.GetDeck deckId
            let extra =
                match Deck.getActive kvsDeck.Deck, kvsDeck.Extra with
                |                               _, Some extra -> Some extra
                |                         Error _, _          -> None
                |                         Ok    _, None       -> failwith "Should be impossible - if you're seeing this, the programmer screwed up. Yell at them."
            let kvsDeck =
                { kvsDeck with
                    Extra = extra
                    Deck  = Deck.Fold.evolve kvsDeck.Deck e }
            return!
                [ kvsDeck |> keyValueStore.InsertOrReplace |>% ignore
                ] |> Async.Parallel |>% ignore
            }
    
    let projectTemplate (templateId: TemplateId) e =
        let projectTemplate (template: Summary.Template) = async {
            let! author = keyValueStore.GetUser template.AuthorId
            let kvsTemplate = template |> Kvs.toKvsTemplate author.DisplayName Map.empty
            let search      = template |> TemplateSearch.fromSummary author.DisplayName
            return!
                [ keyValueStore.InsertOrReplace kvsTemplate |>% ignore
                  Elsea.Template.UpsertSearch(elasticClient, string templateId, search) |> Async.AwaitTask |>% ignore
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
                  Elsea.Template.UpsertSearch(elasticClient, string templateId, search) |> Async.AwaitTask |>% ignore
                ] |> Async.Parallel |>% ignore
            }

    let projectExample (exampleId: string) e =
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
            //          Elsea.Example.UpsertSearch(elasticClient, exampleId, search) |> Async.AwaitTask |>% ignore
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
                  Elsea.Example.UpsertSearch(elasticClient, exampleId, search) |> Async.AwaitTask |>% ignore
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
                  Elsea.Example.UpsertSearch(elasticClient, exampleId, search) |> Async.AwaitTask |>% ignore
                ] |> Async.Parallel |>% ignore
            }

    let projectStack (stackId: string) e =
        match e with
        | Stack.Events.Snapshotted { State = state } -> failwith "not implemented"
        | Stack.Events.Created created -> async {
            let exampleId, ordinal = created.ExampleRevisionId
            let! example =
                exampleId
                |> keyValueStore.GetExample
                |>% Kvs.incrementExample ordinal
            let! concept =
                exampleId
                |> keyValueStore.GetConcept
                |>% fun x -> { x with Collectors = x.Collectors + 1 }
            return!
                [ created |> Stack.Fold.evolveCreated |> keyValueStore.InsertOrReplace |>% ignore
                  example                             |> keyValueStore.InsertOrReplace |>% ignore
                  concept                             |> keyValueStore.InsertOrReplace |>% ignore
                  Elsea.Example.SetCollected(elasticClient, string exampleId, example.Collectors) |> Async.AwaitTask
                ] |> Async.Parallel |>% ignore
            }
        | Stack.Events.Discarded _ -> async {
            let! (stack: Summary.Stack) = keyValueStore.GetStack stackId
            let exampleId, ordinal = stack.ExampleRevisionId
            let! example =
                exampleId
                |> keyValueStore.GetExample
                |>% Kvs.decrementExample ordinal
            return! [Elsea.Example.SetCollected(elasticClient, string exampleId, example.Collectors) |> Async.AwaitTask
                     example |> keyValueStore.InsertOrReplace |>% ignore
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
        | Stack.Events.DecksChanged e ->
            keyValueStore.Update (Stack.Fold.evolveDecksChanged e) stackId
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
                      Elsea.Example.SetCollected(elasticClient, string newId, newExample.Collectors) |> Async.AwaitTask
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
                             Elsea.Example.SetCollected(elasticClient, string newId, newExample.Collectors) |> Async.AwaitTask
                             Elsea.Example.SetCollected(elasticClient, string oldId, oldExample.Collectors) |> Async.AwaitTask ]
                    }
            let stackInsert = stack |> Stack.Fold.evolveRevisionChanged e |> keyValueStore.InsertOrReplace |>% ignore
            return! stackInsert :: exampleInserts |> Async.Parallel |>% ignore
            }

    member _.Project(streamName:StreamName, events:ITimelineEvent<byte[]> []) =
        let category, id = streamName |> StreamName.splitCategoryAndId
        match category with
        | "Example"  -> events |> Array.map (Example .Events.codec.TryDecode >> Option.get >> projectExample  id)
        | "User"     -> events |> Array.map (User    .Events.codec.TryDecode >> Option.get >> projectUser     id)
        | "Deck"     -> events |> Array.map (Deck    .Events.codec.TryDecode >> Option.get >> projectDeck     id)
        | "Template" -> let id = % Guid.Parse id
                        events |> Array.map (Template.Events.codec.TryDecode >> Option.get >> projectTemplate id)
        | "Stack"    -> events |> Array.map (Stack   .Events.codec.TryDecode >> Option.get >> projectStack    id)
        | _ -> failwith $"Unsupported category: {category}"
        |> Async.Parallel

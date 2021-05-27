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
    let projectDeck     id deck     =   keyValueStore.UpsertDeck'     id deck
    
    let projectTemplate (templateId: string) e =
        match e with
        | Template.Events.Created created -> async {
            let! author = keyValueStore.GetUser created.Meta.UserId
            let created = created |> Template.Fold.evolveCreated
            let kvsTemplate = created |> Kvs.toKvsTemplate author.DisplayName Map.empty
            let search      = created |> TemplateSearch.fromSummary author.DisplayName
            return!
                [ keyValueStore.InsertOrReplace kvsTemplate |>% ignore
                  Elsea.Template.UpsertSearch(elasticClient, templateId, search) |> Async.AwaitTask |>% ignore
                ] |> Async.Parallel |>% ignore
            }
        | Template.Events.Edited e -> async {
            let! kvsTemplate = keyValueStore.GetTemplate templateId
            let kvsTemplate = kvsTemplate |> Kvs.evolveKvsTemplateEdited e
            let search = TemplateSearch.fromEdited e
            return!
                [ keyValueStore.InsertOrReplace kvsTemplate |>% ignore
                  Elsea.Template.UpsertSearch(elasticClient, templateId, search) |> Async.AwaitTask |>% ignore
                ] |> Async.Parallel |>% ignore
            }

    let projectExample (exampleId: string) e =
        match e with
        | Example.Events.Created created -> async {
            let! author = keyValueStore.GetUser created.Meta.UserId
            let! templateInstance = keyValueStore.GetTemplateInstance created.TemplateRevisionId
            let example = created |> Example.Fold.evolveCreated |> Kvs.toKvsExample author.DisplayName Map.empty [templateInstance]
            let search = ExampleSearch.fromSummary (Example.Fold.evolveCreated created) author.DisplayName templateInstance
            return!
                [ keyValueStore.InsertOrReplace example |>% ignore
                  Elsea.Example.UpsertSearch(elasticClient, exampleId, search) |> Async.AwaitTask |>% ignore
                ] |> Async.Parallel |>% ignore
            }
        | Example.Events.Edited e -> async {
            let! (example: Kvs.Example) = keyValueStore.GetExample exampleId
            let! templates =
                let templates = example.Revisions |> List.map (fun x -> x.TemplateInstance)
                if templates |> Seq.exists (fun x -> x.Id = e.TemplateRevisionId) then
                    templates |> Async.singleton
                else async {
                    let! templateInstance = keyValueStore.GetTemplateInstance e.TemplateRevisionId
                    return templateInstance :: templates
                    }
            let kvsExample = example |> Kvs.evolveKvsExampleEdited e templates
            let search = templates |> Seq.filter (fun x -> x.Id = e.TemplateRevisionId) |> Seq.head |> ExampleSearch.fromEdited e
            return!
                [ keyValueStore.InsertOrReplace kvsExample |>% ignore
                  Elsea.Example.UpsertSearch(elasticClient, exampleId, search) |> Async.AwaitTask |>% ignore
                ] |> Async.Parallel |>% ignore
            }

    let projectStack (stackId: string) e =
        match e with
        | Stack.Events.Created created -> async {
            let exampleId, ordinal = created.ExampleRevisionId
            let! example =
                exampleId
                |> keyValueStore.GetExample
                |>% Kvs.incrementExample ordinal
            return!
                [ created |> Stack.Fold.evolveCreated |> keyValueStore.InsertOrReplace |>% ignore
                  example                             |> keyValueStore.InsertOrReplace |>% ignore
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
        | Stack.Events.TagsChanged e ->
            keyValueStore.Update (Stack.Fold.evolveTagsChanged e) stackId
        | Stack.Events.CardStateChanged e ->
            keyValueStore.Update (Stack.Fold.evolveCardStateChanged e) stackId
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
                    return [ newExample |> keyValueStore.InsertOrReplace |>% ignore
                             oldExample |> keyValueStore.InsertOrReplace |>% ignore
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
        | "Template" -> events |> Array.map (Template.Events.codec.TryDecode >> Option.get >> projectTemplate id)
        | "Stack"    -> events |> Array.map (Stack   .Events.codec.TryDecode >> Option.get >> projectStack    id)
        | _ -> failwith $"Unsupported category: {category}"
        |> Async.Parallel

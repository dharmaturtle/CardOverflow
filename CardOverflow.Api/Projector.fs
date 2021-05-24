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
    let projectExample  id example  = [ keyValueStore.UpsertExample'  id example
                                        elsea        .UpsertExampleSearch (% Guid.Parse id) example   |> Async.AwaitTask ] |> Async.Parallel |> Async.map ignore
    let projectUser     id user     =   keyValueStore.UpsertUser'     id user
    let projectDeck     id deck     =   keyValueStore.UpsertDeck'     id deck
    let projectStack    id stack    = [ keyValueStore.UpsertStack'    id stack
                                        elsea        .UpsertStackSearch (% Guid.Parse id) stack       |> Async.AwaitTask ] |> Async.Parallel |> Async.map ignore
    
    let projectTemplate (templateId: string) e =
        match e with
        | Template.Events.Created created -> async {
            let! author = keyValueStore.GetUser created.Meta.UserId
            let created = created |> Template.Fold.evolveCreated
            let kvsTemplate = created |> Kvs.toKvsTemplate author.DisplayName
            let search      = created |> TemplateSearch.fromSummary author.DisplayName
            return!
                [ keyValueStore.InsertOrReplace kvsTemplate |>% ignore
                  Elsea.Template.UpsertSearch(elasticClient, templateId, search) |> Async.AwaitTask |>% ignore
                ] |> Async.Parallel |>% ignore
            }
        | Template.Events.Edited e -> async {
            let! kvsTemplate = keyValueStore.GetTemplate templateId
            let kvsTemplate = kvsTemplate |> Kvs.toTemplate |> Template.Fold.evolveEdited e |> Kvs.toKvsTemplate kvsTemplate.Author // lowTODO needs fixing after multiple authors implemented
            let search = TemplateSearch.fromEdited e
            return!
                [ keyValueStore.InsertOrReplace kvsTemplate |>% ignore
                  Elsea.Template.UpsertSearch(elasticClient, templateId, search) |> Async.AwaitTask |>% ignore
                ] |> Async.Parallel |>% ignore
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

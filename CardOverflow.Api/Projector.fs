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

type Projector (keyValueStore: KeyValueStore) =
    member _.Project(streamName:StreamName, events:ITimelineEvent<byte[]> []) =
        let category, id = streamName |> StreamName.splitCategoryAndId
        match category with
        | "Example"  -> events |> Array.map (Example .Events.codec.TryDecode >> Option.get >> keyValueStore.UpsertExample'  id)
        | "User"     -> events |> Array.map (User    .Events.codec.TryDecode >> Option.get >> keyValueStore.UpsertUser'     id)
        | "Deck"     -> events |> Array.map (Deck    .Events.codec.TryDecode >> Option.get >> keyValueStore.UpsertDeck'     id)
        | "Template" -> events |> Array.map (Template.Events.codec.TryDecode >> Option.get >> keyValueStore.UpsertTemplate' id)
        | "Stack"    -> events |> Array.map (Stack   .Events.codec.TryDecode >> Option.get >> keyValueStore.UpsertStack'    id)
        | _ -> failwith $"Unsupported category: {category}"
        |> Async.Parallel

module EventTests

open Xunit
open CardOverflow.Pure
open Serilog
open System
open Domain
open Equinox.MemoryStore
open FSharp.UMX
open FsCheck.Xunit
open CardOverflow.Pure
open CardOverflow.Test
open EventAppender
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp
open Domain.Stack
open FsCodec.NewtonsoftJson
open Newtonsoft.Json.Linq
open Domain.Summary

module Assert =
    let contains expectedSubstring actualString = Assert.Contains(expectedSubstring, actualString)

let assertHasMeta e =
    let serialized   = Serdes.Serialize(e, jsonSerializerSettings)
    let metaString   = JObject.Parse(serialized).SelectToken("$.Fields[:1].Meta").ToString()
    let deserialized = Serdes.Deserialize<Meta>(metaString, jsonSerializerSettings)
    Assert.NotNull deserialized

let [<StandardProperty>] ``All User     events have Meta`` (e: User    .Events.Event) = assertHasMeta e
let [<StandardProperty>] ``All Deck     events have Meta`` (e: Deck    .Events.Event) = assertHasMeta e
let [<StandardProperty>] ``All Template events have Meta`` (e: Template.Events.Event) = assertHasMeta e
let [<StandardProperty>] ``All Example  events have Meta`` (e: Example .Events.Event) = assertHasMeta e
let [<StandardProperty>] ``All Stack    events have Meta`` (e: Stack   .Events.Event) = assertHasMeta e

let [<StandardProperty>] ``All Deck events are guarded`` (event: Deck.Events.Event) (deck: Deck) =
    let state = deck |> Deck.Fold.Active
    match event with
    | Deck.Events.Edited edited -> Deck.decideEdited edited state |> fst |> Result.getError |> Assert.contains "didn't author this deck"
    | Deck.Events.Created _ -> ()

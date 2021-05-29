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

let getCustomError x =
    match x with
    | Ok _ -> failwith "ya goofed - is in the Ok state"
    | Error x ->
        match x with
        | Custom e -> e
        | _ -> failwith "ya goofed - is not a Custom error"

let [<StandardProperty>] ``All User events are guarded`` (event: User.Events.Event) (author: User) (deck: Summary.Deck) (template: Summary.Template) =
    let deck = { deck with Visibility = Public } |> Deck.Fold.State.Active
    let template = { template with Visibility = Public } |> Template.Fold.State.Active
    match event with
    | User.Events.CardSettingsEdited       e -> User.validateCardSettingsEdited e           author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.DeckFollowed             e -> User.validateFollowDeck deck e              author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.DeckUnfollowed           e -> User.validateUnfollowDeck e                 author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.OptionsEdited            e -> User.validateOptionsEdited e deck           author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.TemplateCollected        e -> User.validateTemplateCollected e template   author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.TemplateDiscarded        e -> User.validateTemplateDiscarded e            author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.SignedUp _ -> ()

let [<StandardProperty>] ``All Deck events are guarded`` (event: Deck.Events.Event) (deck: Deck) =
    match event with
    | Deck.Events.Edited edited -> Deck.validateEdit deck edited |> getCustomError |> Assert.contains "You aren't allowed to edit this Deck."
    | Deck.Events.Created _ -> ()

let [<StandardProperty>] ``All Template events are guarded`` (event: Template.Events.Event) (template: Template) =
    match event with
    | Template.Events.Edited e -> Template.validateEdited template e |> getCustomError |> Assert.contains "You aren't allowed to edit this Template."
    | Template.Events.Created _ -> ()

let [<StandardProperty>] ``All Example events are guarded`` (event: Example.Events.Event) template (example: Example) =
    match event with
    | Example.Events.Edited e -> Example.validateEdit template example e |> getCustomError |> Assert.contains "You aren't allowed to edit this Example."
    | Example.Events.Created _ -> ()

let [<StandardProperty>] ``All Stack events are guarded`` (event: Stack.Events.Event) (stack: Stack) revision template =
    match event with
    | Stack.Events.TagsChanged      e -> Stack.validateTagsChanged      e                   stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.CardStateChanged e -> Stack.validateCardStateChanged e                   stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.Discarded        e -> Stack.validateDiscarded        e                   stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.RevisionChanged  e -> Stack.validateRevisionChanged  e revision template stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.Created _ -> ()

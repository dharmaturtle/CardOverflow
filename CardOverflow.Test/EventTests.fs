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

let [<EventProperty>] ``All User     events have Meta`` (e: User    .Events.Event) = assertHasMeta e
let [<EventProperty>] ``All Deck     events have Meta`` (e: Deck    .Events.Event) = assertHasMeta e
let [<EventProperty>] ``All Template events have Meta`` (e: Template.Events.Event) = assertHasMeta e
let [<EventProperty>] ``All Example  events have Meta`` (e: Example .Events.Event) = assertHasMeta e
let [<EventProperty>] ``All Stack    events have Meta`` (e: Stack   .Events.Event) = assertHasMeta e

let getCustomError x =
    match x with
    | Ok _ -> failwith "ya goofed - is in the Ok state"
    | Error x ->
        match x with
        | Custom e -> e
        | _ -> failwith "ya goofed - is not a Custom error"

let [<EventProperty>] ``All User events are guarded`` (event: User.Events.Event) (author: User) (deck: Summary.Deck) (template: Summary.Template) =
    let deck     = { deck     with Visibility = Public } |>     Deck.Fold.Active |>     Deck.Fold.State.Extant
    let template = { template with Visibility = Public } |> Template.Fold.Active |> Template.Fold.State.Extant
    match event with
    | User.Events.CardSettingsEdited       e -> User.validateCardSettingsEdited e           author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.DeckFollowed             e -> User.validateFollowDeck deck e              author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.DeckUnfollowed           e -> User.validateUnfollowDeck e                 author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.OptionsEdited            e -> User.validateOptionsEdited e deck           author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.TemplateCollected        e -> User.validateTemplateCollected e template   author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.TemplateDiscarded        e -> User.validateTemplateDiscarded e            author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.SignedUp _ -> ()
    | User.Events.Snapshotted _ -> failwith "impossible"

let [<EventProperty>] ``All Deck events are guarded`` (event: Deck.Events.Event) (deck: Deck) =
    match event with
    | Deck.Events.Edited       edited -> Deck.validateEdit    deck    edited |> getCustomError |> Assert.contains "You aren't allowed to edit this Deck."
    | Deck.Events.Discarded discarded -> Deck.validateDiscard deck discarded |> getCustomError |> Assert.contains "You aren't allowed to edit this Deck."
    | Deck.Events.Created _ -> ()
    | Deck.Events.Snapshotted _ -> failwith "impossible"

let [<EventProperty>] ``All Template events are guarded`` (event: Template.Events.Event) (template: Template) =
    match event with
    | Template.Events.Edited e -> Template.validateEdited template e |> getCustomError |> Assert.contains "You aren't allowed to edit this Template."
    | Template.Events.Created     _ -> ()
    | Template.Events.Snapshotted _ -> failwith "impossible"

let [<EventProperty>] ``All Example events are guarded`` (event: Example.Events.Event) template (example: Example) =
    match event with
    | Example.Events.Edited      e -> Example.validateEdit template example e |> getCustomError |> Assert.contains "You aren't allowed to edit this Example."
    | Example.Events.Created     _ -> ()
    | Example.Events.Snapshotted _ -> failwith "impossible"

let [<EventProperty>] ``All Stack events are guarded`` (event: Stack.Events.Event) (stack: Stack) revision template =
    match event with
    | Stack.Events.TagsChanged      e -> Stack.validateTagsChanged      e                   stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.CardStateChanged e -> Stack.validateCardStateChanged e                   stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.Discarded        e -> Stack.validateDiscarded        e                   stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.RevisionChanged  e -> Stack.validateRevisionChanged  e revision template stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.Created          _ -> ()
    | Stack.Events.Snapshotted      _ -> failwith "impossible"

let getIdempotentError x =
    match x with
    | Ok _ -> failwith "ya goofed - is in the Ok state"
    | Error x ->
        match x with
        | Idempotent -> ()
        | Custom x -> failwithf "ya goofed - is a Custom error: %A" x

let assertOkAndNoEvents (r, events) =
    r |> Result.getOk
    Assert.Empty events

let [<EventProperty>] ``All User events are idempotent`` (event: User.Events.Event) (user: User) =
    let user (meta: Meta) = { user with Id = meta.UserId }
    match event with
    | User.Events.CardSettingsEdited e -> e.Meta |> user |> User.Fold.evolveCardSettingsEdited e |> User.checkMeta e.Meta |> getIdempotentError
    | User.Events.DeckFollowed       e -> e.Meta |> user |> User.Fold.evolveDeckFollowed       e |> User.checkMeta e.Meta |> getIdempotentError
    | User.Events.DeckUnfollowed     e -> e.Meta |> user |> User.Fold.evolveDeckUnfollowed     e |> User.checkMeta e.Meta |> getIdempotentError
    | User.Events.OptionsEdited      e -> e.Meta |> user |> User.Fold.evolveOptionsEdited      e |> User.checkMeta e.Meta |> getIdempotentError
    | User.Events.TemplateCollected  e -> e.Meta |> user |> User.Fold.evolveTemplateCollected  e |> User.checkMeta e.Meta |> getIdempotentError
    | User.Events.TemplateDiscarded  e -> e.Meta |> user |> User.Fold.evolveTemplateDiscarded  e |> User.checkMeta e.Meta |> getIdempotentError
    | User.Events.SignedUp           e -> e |> User.Fold.evolveSignedUp |> User.Fold.Active |> User.Fold.Extant |> User.decideSignedUp e |> assertOkAndNoEvents
    | User.Events.Snapshotted        _ -> failwith "impossible"

let [<EventProperty>] ``All Deck events are idempotent`` (event: Deck.Events.Event) (deck: Deck) =
    let deck (meta: Meta) = { deck with AuthorId = meta.UserId }
    match event with
    | Deck.Events.Edited       edited ->    edited.Meta |> deck                                         |> Deck.Fold.evolveEdited       edited |> Deck.checkMeta edited.Meta     |> getIdempotentError
    | Deck.Events.Created     created -> created                |> Deck.Fold.evolveCreated |> Deck.Fold.Active |> Deck.Fold.Extant             |> Deck.decideCreate      created |> assertOkAndNoEvents
    | Deck.Events.Discarded discarded -> discarded.Meta |> deck |> Deck.Fold.Active |> Deck.Fold.Extant |> Deck.Fold.evolveDiscarded discarded |> Deck.decideDiscarded discarded |> assertOkAndNoEvents
    | Deck.Events.Snapshotted       _ -> failwith "impossible"

let [<EventProperty>] ``All Template events are idempotent`` (event: Template.Events.Event) (template: Template) =
    let template (meta: Meta) = { template with AuthorId = meta.UserId }
    match event with
    | Template.Events.Edited  edited  -> edited.Meta |> template |> Template.Fold.evolveEdited edited |> Template.checkMeta edited.Meta |> getIdempotentError
    | Template.Events.Created created -> created |> Template.Fold.evolveCreated |> Template.Fold.Active |> Template.Fold.Extant |> Template.decideCreate created |> assertOkAndNoEvents
    | Template.Events.Snapshotted _ -> failwith "impossible"

let [<EventProperty>] ``All Example events are idempotent`` (event: Example.Events.Event) (example: Example) template =
    let example (meta: Meta) = { example with AuthorId = meta.UserId }
    match event with
    | Example.Events.Edited  edited  -> edited.Meta |> example |> Example.Fold.evolveEdited edited |> Example.checkMeta edited.Meta |> getIdempotentError
    | Example.Events.Created created -> created |> Example.Fold.evolveCreated |> Example.Fold.Active |> Example.Fold.Extant |> Example.decideCreate template created |> assertOkAndNoEvents
    | Example.Events.Snapshotted   _ -> failwith "impossible"

let [<EventProperty>] ``All Stack events are idempotent`` (event: Stack.Events.Event) (stack: Stack) state template =
    let stackId = stack.Id
    let stack (meta: Meta) = { stack with AuthorId = meta.UserId }
    match event with
    | Stack.Events.TagsChanged      e -> e.Meta |> stack                                           |> Stack.Fold.evolveTagsChanged      e |> Stack.checkMeta e.Meta |> getIdempotentError
    | Stack.Events.CardStateChanged e -> e.Meta |> stack                                           |> Stack.Fold.evolveCardStateChanged e |> Stack.checkMeta e.Meta |> getIdempotentError
    | Stack.Events.RevisionChanged  e -> e.Meta |> stack                                           |> Stack.Fold.evolveRevisionChanged  e |> Stack.checkMeta e.Meta |> getIdempotentError
    | Stack.Events.Discarded        e -> e.Meta |> stack |> Stack.Fold.Active |> Stack.Fold.Extant |> Stack.Fold.evolveDiscarded        e |> Stack.decideDiscard stackId e       |> assertOkAndNoEvents
    | Stack.Events.Created          e -> e |> Stack.Fold.evolveCreated |> Stack.Fold.Active |> Stack.Fold.Extant                          |> Stack.decideCreate e template state |> assertOkAndNoEvents
    | Stack.Events.Snapshotted      _ -> failwith "impossible"

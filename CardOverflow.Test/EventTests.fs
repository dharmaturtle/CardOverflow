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

let getMeta e =
    JObject
        .Parse(serializeToJson e)
        .SelectToken("$.Fields[:1].Meta")
    |> string
    |> deserializeFromJson

let assertHasMeta e = e |> getMeta |> Assert.NotNull

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

let [<EventProperty>] ``All User events are guarded`` (event: User.Events.Event) (author: User) (template: Summary.Template) =
    let template = { template with Visibility = Public } |> Template.Fold.Active
    match event with
    | User.Events.CardSettingsEdited       e -> User.validateCardSettingsEdited e           author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.OptionsEdited            e -> User.validateOptionsEdited e                author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.TemplateCollected        e -> User.validateTemplateCollected e template   author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.TemplateDiscarded        e -> User.validateTemplateDiscarded e            author |> getCustomError |> Assert.contains "You aren't allowed to edit this user."
    | User.Events.SignedUp _ -> ()
    | User.Events.Snapshotted _ -> failwith "impossible"

let [<EventProperty>] ``All Deck events are guarded`` (event: Deck.Events.Event) (deck: Deck) =
    match event with
    | Deck.Events.Edited            e -> Deck.validateEdit             deck e      |> getCustomError |> Assert.contains "You aren't allowed to edit this Deck."
    | Deck.Events.VisibilityChanged e -> Deck.validateVisibilityChange deck e      |> getCustomError |> Assert.contains "You aren't allowed to edit this Deck."
    | Deck.Events.IsDefaultChanged  e -> Deck.validateIsDefaultChange  deck e      |> getCustomError |> Assert.contains "You aren't allowed to edit this Deck."
    | Deck.Events.SourceChanged     e -> Deck.validateSourceChange     deck e None |> getCustomError |> Assert.contains "You aren't allowed to edit this Deck."
    | Deck.Events.Discarded         e -> Deck.validateDiscard          deck e      |> getCustomError |> Assert.contains "You aren't allowed to edit this Deck."
    | Deck.Events.Created _ -> ()
    | Deck.Events.Snapshotted _ -> failwith "impossible"

let [<EventProperty>] ``All Deck events modify ServerModified`` (event: Deck.Events.Event) (deck: Deck) =
    match event with
    | Deck.Events.Edited            e -> Deck.Fold.evolveEdited            e deck |> fun x -> x.ServerModified
    | Deck.Events.VisibilityChanged e -> Deck.Fold.evolveVisibilityChanged e deck |> fun x -> x.ServerModified
    | Deck.Events.IsDefaultChanged  e -> Deck.Fold.evolveIsDefaultChanged  e deck |> fun x -> x.ServerModified
    | Deck.Events.SourceChanged     e -> Deck.Fold.evolveSourceChanged     e deck |> fun x -> x.ServerModified
    | Deck.Events.Created           e -> Deck.Fold.evolveCreated           e      |> fun x -> x.ServerModified
    | Deck.Events.Discarded   e -> e.Meta.ServerReceivedAt.Value // meh
    | Deck.Events.Snapshotted _ -> failwith "impossible"
    |> Assert.equal (getMeta event).ServerReceivedAt.Value

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

let [<EventProperty>] ``All Stack events are guarded`` (event: Stack.Events.Event) (stack: Stack) example template =
    match event with
    | Stack.Events.TagAdded           e -> Stack.validateTagAdded           e                   stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.TagRemoved         e -> Stack.validateTagRemoved         e                   stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.Edited             e -> Stack.validateEdited             e example template  stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.CardStateChanged   e -> Stack.validateCardStateChanged   e                   stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.Discarded          e -> Stack.validateDiscarded          e                   stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.DecksChanged       e -> Stack.validateDecksChanged       e [||]              stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.CardSettingChanged e -> Stack.validateCardSettingChanged e User.Fold.initial stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.Reviewed           e -> Stack.validateReviewed           e                   stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.RevisionChanged    e -> Stack.validateRevisionChanged    e example template  stack |> getCustomError |> Assert.contains "You aren't allowed to edit this Stack."
    | Stack.Events.Created            _ -> ()
    | Stack.Events.Snapshotted        _ -> failwith "impossible"

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
    | User.Events.OptionsEdited      e -> e.Meta |> user |> User.Fold.evolveOptionsEdited      e |> User.checkMeta e.Meta |> getIdempotentError
    | User.Events.TemplateCollected  e -> e.Meta |> user |> User.Fold.evolveTemplateCollected  e |> User.checkMeta e.Meta |> getIdempotentError
    | User.Events.TemplateDiscarded  e -> e.Meta |> user |> User.Fold.evolveTemplateDiscarded  e |> User.checkMeta e.Meta |> getIdempotentError
    | User.Events.SignedUp           e -> e |> User.Fold.evolveSignedUp |> User.Fold.Active |> User.decideSignedUp e |> assertOkAndNoEvents
    | User.Events.Snapshotted        _ -> failwith "impossible"

let [<EventProperty>] ``All Deck events are idempotent`` (event: Deck.Events.Event) (deck: Deck) =
    let deck (meta: Meta) = { deck with AuthorId = meta.UserId }
    match event with
    | Deck.Events.Edited            e ->         e.Meta |> deck |> Deck.Fold.evolveEdited            e |> Deck.checkMeta e.Meta |> getIdempotentError
    | Deck.Events.VisibilityChanged e ->         e.Meta |> deck |> Deck.Fold.evolveVisibilityChanged e |> Deck.checkMeta e.Meta |> getIdempotentError
    | Deck.Events.SourceChanged     e ->         e.Meta |> deck |> Deck.Fold.evolveSourceChanged     e |> Deck.checkMeta e.Meta |> getIdempotentError
    | Deck.Events.IsDefaultChanged  e ->         e.Meta |> deck |> Deck.Fold.evolveIsDefaultChanged  e |> Deck.checkMeta e.Meta |> getIdempotentError
    | Deck.Events.Created     created -> created                |> Deck.Fold.evolveCreated |> Deck.Fold.Active             |> Deck.decideCreate      created |> assertOkAndNoEvents
    | Deck.Events.Discarded discarded -> discarded.Meta |> deck |> Deck.Fold.Active |> Deck.Fold.evolveDiscarded discarded |> Deck.decideDiscarded discarded |> assertOkAndNoEvents
    | Deck.Events.Snapshotted       _ -> failwith "impossible"

let [<EventProperty>] ``All Template events are idempotent`` (event: Template.Events.Event) (template: Template) =
    let template (meta: Meta) = { template with AuthorId = meta.UserId }
    match event with
    | Template.Events.Edited  edited  -> edited.Meta |> template |> Template.Fold.evolveEdited edited |> Template.checkMeta edited.Meta |> getIdempotentError
    | Template.Events.Created created -> created |> Template.Fold.evolveCreated |> Template.Fold.Active |> Template.decideCreate created |> assertOkAndNoEvents
    | Template.Events.Snapshotted _ -> failwith "impossible"

let [<EventProperty>] ``All Example events are idempotent`` (event: Example.Events.Event) (example: Example) template =
    let example (meta: Meta) = { example with AuthorId = meta.UserId }
    match event with
    | Example.Events.Edited  edited  -> edited.Meta |> example |> Example.Fold.evolveEdited edited |> Example.checkMeta edited.Meta |> getIdempotentError
    | Example.Events.Created created -> created |> Example.Fold.evolveCreated |> Example.Fold.Active |> Example.decideCreate template created |> assertOkAndNoEvents
    | Example.Events.Snapshotted   _ -> failwith "impossible"

let [<EventProperty>] ``All Stack events are idempotent`` (event: Stack.Events.Event) (stack: Stack) state template =
    let stackId = stack.Id
    let stack (meta: Meta) = { stack with AuthorId = meta.UserId }
    match event with
    | Stack.Events.TagAdded           e -> e.Meta |> stack                                           |> Stack.Fold.evolveTagAdded           e |> Stack.checkMeta e.Meta |> getIdempotentError
    | Stack.Events.TagRemoved         e -> e.Meta |> stack                                           |> Stack.Fold.evolveTagRemoved         e |> Stack.checkMeta e.Meta |> getIdempotentError
    | Stack.Events.Edited             e -> e.Meta |> stack                                           |> Stack.Fold.evolveEdited             e |> Stack.checkMeta e.Meta |> getIdempotentError
    | Stack.Events.CardStateChanged   e -> e.Meta |> stack                                           |> Stack.Fold.evolveCardStateChanged   e |> Stack.checkMeta e.Meta |> getIdempotentError
    | Stack.Events.RevisionChanged    e -> e.Meta |> stack                                           |> Stack.Fold.evolveRevisionChanged    e |> Stack.checkMeta e.Meta |> getIdempotentError
    | Stack.Events.DecksChanged       e -> e.Meta |> stack                                           |> Stack.Fold.evolveDecksChanged       e |> Stack.checkMeta e.Meta |> getIdempotentError
    | Stack.Events.Reviewed           e -> e.Meta |> stack                                           |> Stack.Fold.evolveReviewed           e |> Stack.checkMeta e.Meta |> getIdempotentError
    | Stack.Events.CardSettingChanged e -> e.Meta |> stack                                           |> Stack.Fold.evolveCardSettingChanged e |> Stack.checkMeta e.Meta |> getIdempotentError
    | Stack.Events.Discarded          e -> e.Meta |> stack |> Stack.Fold.Active |> Stack.Fold.evolveDiscarded          e |> Stack.decideDiscard stackId e       |> assertOkAndNoEvents
    | Stack.Events.Created            e -> e |> Stack.Fold.evolveCreated |> Stack.Fold.Active                            |> Stack.decideCreate e template state |> assertOkAndNoEvents
    | Stack.Events.Snapshotted        _ -> failwith "impossible"

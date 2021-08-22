module EventAppender

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
open NodaTime
open Domain.Summary
open Domain.Projection

//
// Appenders should only read from event streams to maintian consistency https://www.youtube.com/watch?v=DDefPUCB9ao&t=350s
//
// The `decide` functions are intentionally passed *.Fold.State, which makes it impossible for the event streams to have circular dependencies
// since the validation functions in the Pure project can only reference event streams' *.Fold.State declared "above" them.
// E.g. the user stream shouldn't query the deck stream for validation while the deck stream also queries the user stream for validation.
// The benefit is that there's no need for global event ordering.
// When syncing client generated events, first sync the stream with 0 dependencies, then the stream that depends on that, etc.
// (Currently Deck, Template, User, Example, and finally Stack.)
//

module Example =
    open Example
    
    type Appender internal (resolveExample, resolveTemplate) =
        let resolveExample    exampleId     : Decider<_                          , _> = resolveExample   exampleId
        let resolveTemplate (templateId, _) : Decider<PublicTemplate.Events.Event, _> = resolveTemplate templateId

        member _.Create(created: Events.Created) = asyncResult {
            let stream = resolveExample created.Id
            let! template = (resolveTemplate created.TemplateRevisionId).Query id
            return! stream.Transact(decideCreate template created)
            }
        member _.Edit (edited: Events.Edited) exampleId = async {
            let stream = resolveExample exampleId
            let! template = (resolveTemplate edited.TemplateRevisionId).Query id
            return! stream.Transact(decideEdit template edited exampleId)
            }
        member _.AddComment commentAdded exampleId =
            let stream = resolveExample exampleId
            stream.Transact(decideAddComment commentAdded exampleId)

        member this.Sync (clientEvents: ClientEvent<Events.Event> seq) = asyncResult {
            for { StreamId = streamId; Event = event } in clientEvents do
                let streamId = % streamId
                do! match event with
                    | Events.Created      e -> this.Create     e
                    | Events.Edited       e -> this.Edit       e streamId
                    | Events.CommentAdded e -> this.AddComment e streamId
                    | Events.Snapshotted  _ -> $"Illegal event: {nameof(Events.Snapshotted)}" |> Error |> Async.singleton
            }

    let create resolveExample resolveTemplate =
        let resolveExample  id = Decider(Log.ForContext<Appender>(), resolveExample  (               streamName id), maxAttempts=3)
        let resolveTemplate id = Decider(Log.ForContext<Appender>(), resolveTemplate (PublicTemplate.streamName id), maxAttempts=3)
        Appender(resolveExample, resolveTemplate)

module User =
    open User

    type Appender internal (resolveUser, resolveDeck, resolveTemplate) =
        let resolveUser         userId : Decider<_                          , _>               = resolveUser         userId
        let resolveDeck         deckId : Decider<          PrivateDeck.Events.Event, PrivateDeck.Fold.State> = resolveDeck         deckId
        let resolveTemplate templateId : Decider<PublicTemplate.Events.Event, _>               = resolveTemplate templateId

        member _.OptionsEdited (o: Events.OptionsEdited) = asyncResult {
            let stream = resolveUser o.Meta.UserId
            return! stream.Transact(decideOptionsEdited o)
            }
        member _.CardSettingsEdited (cardSettingsEdited: Events.CardSettingsEdited) =
            let stream = resolveUser cardSettingsEdited.Meta.UserId
            stream.Transact(decideCardSettingsEdited cardSettingsEdited)
        member _.TemplateCollected (templateCollected: Events.TemplateCollected) = asyncResult {
            let stream = resolveUser templateCollected.Meta.UserId
            let! template = (resolveTemplate (fst templateCollected.TemplateRevisionId)).Query id
            return! stream.Transact(decideTemplateCollected templateCollected template)
            }
        member _.TemplateDiscarded (templateDiscarded: Events.TemplateDiscarded) =
            let stream = resolveUser templateDiscarded.Meta.UserId
            stream.Transact(decideTemplateDiscarded templateDiscarded)

        member this.Sync (clientEvents: ClientEvent<Events.Event> seq) = asyncResult {
            for { Event = event } in clientEvents do
                do! match event with
                    | Events.TemplateCollected        x -> this.TemplateCollected x
                    | Events.TemplateDiscarded        x -> this.TemplateDiscarded x
                    | Events.CardSettingsEdited       x -> this.CardSettingsEdited x
                    | Events.OptionsEdited            x -> this.OptionsEdited x
                    | Events.SignedUp                 _ -> $"Illegal event: {nameof(Events.SignedUp   )}" |> Error |> Async.singleton
                    | Events.Snapshotted              _ -> $"Illegal event: {nameof(Events.Snapshotted)}" |> Error |> Async.singleton
            }

    let create resolveUser resolveDeck resolveTemplate =
        let resolveUser     id = Decider(Log.ForContext<Appender>(), resolveUser     (               streamName id), maxAttempts=3)
        let resolveDeck     id = Decider(Log.ForContext<Appender>(), resolveDeck     (          PrivateDeck.streamName id), maxAttempts=3)
        let resolveTemplate id = Decider(Log.ForContext<Appender>(), resolveTemplate (PublicTemplate.streamName id), maxAttempts=3)
        Appender(resolveUser, resolveDeck, resolveTemplate)

module Deck =
    open PrivateDeck

    type Appender internal (resolve) =
        let resolve deckId : Decider<_, _> = resolve deckId

        member _.Create (created: Events.Created) = async {
            let stream = resolve created.Id
            return! stream.Transact(decideCreate created)
            }
        member _.Edit (edited: Events.Edited) deckId = async {
            let stream = resolve deckId
            return! stream.Transact(decideEdited edited)
            }
        member _.ChangeSource (sourceChanged: Events.SourceChanged) deckId = async {
            let! sourceState =
                sourceChanged.SourceId
                |> OptionAsync.traverse (fun sourceId -> (resolve sourceId).Query id)
            let stream = resolve deckId
            return! stream.Transact(decideSourceChanged sourceChanged sourceState)
            }
        member _.ChangeIsDefault (isDefaultChanged: Events.IsDefaultChanged) deckId = async {
            let stream = resolve deckId
            return! stream.Transact(decideIsDefaultChanged isDefaultChanged)
            }
        member _.ChangeVisibility (visibilityChanged: Events.VisibilityChanged) deckId = async {
            let stream = resolve deckId
            return! stream.Transact(decideVisibilityChanged visibilityChanged)
            }
        member _.Discard (discarded: Events.Discarded) deckId = async {
            let stream = resolve deckId
            return! stream.Transact(decideDiscarded discarded)
            }

        member this.Sync (clientEvents: ClientEvent<Events.Event> seq) = asyncResult {
            for { StreamId = streamId; Event = event } in clientEvents do
                let streamId = % streamId
                do! match event with
                    | Events.Created           c -> this.Create           c
                    | Events.Edited            e -> this.Edit             e streamId
                    | Events.SourceChanged     e -> this.ChangeSource     e streamId
                    | Events.IsDefaultChanged  e -> this.ChangeIsDefault  e streamId
                    | Events.VisibilityChanged e -> this.ChangeVisibility e streamId
                    | Events.Discarded         e -> this.Discard          e streamId
                    | Events.Snapshotted       _ -> $"Illegal event: {nameof(Events.Snapshotted)}" |> Error |> Async.singleton
            }

    let create resolve =
        let resolve id = Decider(Log.ForContext<Appender>(), resolve (streamName id), maxAttempts=3)
        Appender(resolve)

module PublicTemplate =
    open PublicTemplate

    type Appender internal (resolveTemplate) =
        let resolveTemplate templateId : Decider<_, _> = resolveTemplate templateId

        member _.Create (created: Events.Created) =
            let templateStream = resolveTemplate created.Id
            templateStream.Transact(decideCreate created)
        member _.Edit (edited: Events.Edited) (templateId: TemplateId) =
            let templateStream = resolveTemplate templateId
            templateStream.Transact(decideEdit edited templateId)

        member this.Sync (clientEvents: ClientEvent<Events.Event> seq) = asyncResult {
            for { StreamId = streamId; Event = event } in clientEvents do
                let streamId = % streamId
                do! match event with
                    | Events.Created     c -> this.Create c
                    | Events.Edited      e -> this.Edit e streamId
                    | Events.Snapshotted _ -> $"Illegal event: {nameof(Events.Snapshotted)}" |> Error |> Async.singleton
            }

    let create resolveTemplate =
        let resolveTemplate id = Decider(Log.ForContext<Appender>(), resolveTemplate (     streamName id), maxAttempts=3)
        Appender(resolveTemplate)

module Stack =
    open Stack

    type Appender internal (resolveStack, resolveTemplate, resolveExample, resolveDeck, resolveUser) =
        let resolveStack            stackId : Decider<_                          , _> = resolveStack       stackId
        let resolveTemplate (templateId, _) : Decider<PublicTemplate.Events.Event, _> = resolveTemplate templateId
        let resolveExample  ( exampleId, _) : Decider<       Example.Events.Event, _> = resolveExample   exampleId
        let resolveDeck     (       deckId) : Decider<   PrivateDeck.Events.Event, _> = resolveDeck         deckId
        let resolveUser     (       userId) : Decider<          User.Events.Event, _> = resolveUser         userId

        member _.Create (created: Events.Created) = asyncResult {
            let stream = resolveStack created.Id
            let! template = (resolveTemplate created.TemplateRevisionId).Query id
            return! stream.Transact(decideCreate created template)
            }
        member _.Discard discarded stackId =
            let stream = resolveStack stackId
            stream.Transact(decideDiscard stackId discarded)
        member _.AddTag (tagAdded: Events.TagAdded) stackId =
            let stream = resolveStack stackId
            stream.Transact(decideAddTag tagAdded)
        member _.RemoveTag (tagRemoved: Events.TagRemoved) stackId =
            let stream = resolveStack stackId
            stream.Transact(decideRemoveTag tagRemoved)
        member _.Edited (edited: Events.Edited) stackId = asyncResult {
            let stream = resolveStack stackId
            let! example = (resolveExample edited.ExampleRevisionId).Query id
            let! exampleRevision = example |> Example.getRevision edited.ExampleRevisionId
            let! template = (resolveTemplate exampleRevision.TemplateRevisionId).Query id
            return! stream.Transact(decideEdited edited template)
            }
        member _.ChangeCardState (cardStateChanged: Events.CardStateChanged) stackId =
            let stream = resolveStack stackId
            stream.Transact(decideChangeCardState cardStateChanged)
        member _.ChangeDecks (decksChanged: Events.DecksChanged) stackId = async {
            let stream = resolveStack stackId
            let! decks = decksChanged.DeckIds |> Set.toList |> List.map (fun deckId -> (resolveDeck deckId).Query id) |> Async.Parallel
            return! stream.Transact(decideChangeDecks decksChanged decks)
            }
        member _.ChangeCardSetting (cardSettingChanged: Events.CardSettingChanged) stackId = async {
            let stream = resolveStack stackId
            let! user = (resolveUser cardSettingChanged.Meta.UserId).Query id
            return! stream.Transact(decideChangeCardSetting cardSettingChanged user)
            }
        member _.Review (reviewed: Events.Reviewed) stackId =
            let stream = resolveStack stackId
            stream.Transact(decideReview reviewed)
        member _.ChangeRevision (revisionChanged: Events.RevisionChanged) stackId = asyncResult {
            let stream = resolveStack stackId
            let! example = revisionChanged.RevisionId |> Option.map (fun eri -> (resolveExample eri).Query id) |> OptionAsync.sequence
            return! stream.Transact(decideChangeRevision revisionChanged example)
            }

        member this.Sync (clientEvents: ClientEvent<Events.Event> seq) = asyncResult {
            for { StreamId = streamId; Event = event } in clientEvents do
                let streamId = % streamId
                do! match event with
                    | Events.Created            e -> this.Create            e
                    | Events.Discarded          e -> this.Discard           e streamId
                    | Events.TagAdded           e -> this.AddTag            e streamId
                    | Events.TagRemoved         e -> this.RemoveTag         e streamId
                    | Events.Edited             e -> this.Edited            e streamId
                    | Events.RevisionChanged    e -> this.ChangeRevision    e streamId
                    | Events.CardStateChanged   e -> this.ChangeCardState   e streamId
                    | Events.DecksChanged       e -> this.ChangeDecks       e streamId
                    | Events.CardSettingChanged e -> this.ChangeCardSetting e streamId
                    | Events.Reviewed           e -> this.Review            e streamId
                    | Events.Snapshotted      _ -> $"Illegal event: {nameof(Events.Snapshotted)}" |> Error |> Async.singleton
            }

    let create resolveStack resolveTemplate resolveExample resolveDeck resolveUser =
        let resolveStack    id = Decider(Log.ForContext<Appender>(), resolveStack    (               streamName id), maxAttempts=3)
        let resolveTemplate id = Decider(Log.ForContext<Appender>(), resolveTemplate (PublicTemplate.streamName id), maxAttempts=3)
        let resolveExample  id = Decider(Log.ForContext<Appender>(), resolveExample  (       Example.streamName id), maxAttempts=3)
        let resolveDeck     id = Decider(Log.ForContext<Appender>(), resolveDeck     (   PrivateDeck.streamName id), maxAttempts=3)
        let resolveUser     id = Decider(Log.ForContext<Appender>(), resolveUser     (          User.streamName id), maxAttempts=3)
        Appender(resolveStack, resolveTemplate, resolveExample, resolveDeck, resolveUser)

module UserSaga = // medTODO turn into a real saga
    open User

    type Appender internal (resolve, deckAppender: Deck.Appender) =
        let resolve userId : Decider<_, _> = resolve userId

        member _.BuildSignedUp meta displayName =
            let cardSettingsId = Guid.NewGuid()
            User.init meta displayName cardSettingsId

        member _.Create (signedUp: Events.SignedUp) = asyncResult {
            let stream = resolve signedUp.Meta.UserId
            let deckId = % Guid.NewGuid()
            do! stream.Transact(decideSignedUp signedUp)
            return! PrivateDeck.defaultDeck signedUp.Meta deckId |> deckAppender.Create
            }

    let create deckAppender resolve =
        let resolve id = Decider(Log.ForContext<Appender>(), resolve (streamName id), maxAttempts=3)
        Appender(resolve, deckAppender)

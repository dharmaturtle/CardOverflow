module EventWriter

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

module Example =
    open Example
    
    type Writer internal (resolve) =
        let resolve exampleId : Stream<_, _> = resolve exampleId

        member _.Create(state: Events.Summary) =
            let stream = resolve state.Id
            stream.Transact(decideCreate state)
        member _.Edit(state, exampleId, callerId) =
            let stream = resolve exampleId
            stream.Transact(decideEdit state callerId)

    let create resolve =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve)

module User =
    open User

    type Writer internal (resolve, keyValueStore: KeyValueStore) =
        let resolve userId : Stream<_, _> = resolve userId

        member _.OptionsEdited userId (o: Events.OptionsEdited) = async {
            let stream = resolve userId
            let! deck = keyValueStore.GetDeck o.DefaultDeckId
            return! stream.Transact(decideOptionsEdited o deck.AuthorId)
            }
        member _.CardSettingsEdited userId cardSettingsEdited =
            let stream = resolve userId
            stream.Transact(decideCardSettingsEdited cardSettingsEdited)
        member _.DeckFollowed userId deckId = async {
            let stream = resolve userId
            let! doesDeckExist = keyValueStore.Exists deckId
            return! stream.Transact(decideFollowDeck deckId doesDeckExist)
            }
        member _.DeckUnfollowed userId deckId =
            let stream = resolve userId
            stream.Transact(decideUnfollowDeck deckId)

    let create resolve keyValueStore =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve, keyValueStore)

module Deck =
    open Deck

    type Writer internal (resolve, keyValueStore: KeyValueStore) =
        let resolve deckId : Stream<_, _> = resolve deckId

        let doesSourceExist (source: DeckId option) =
            match source with
            | None -> Async.singleton true
            | Some x -> keyValueStore.Exists x

        member _.Create (summary: Events.Summary) = async {
            let stream = resolve summary.Id
            let! doesSourceExist = doesSourceExist summary.SourceId
            return! stream.Transact(decideCreate summary doesSourceExist)
            }
        member _.Edit (edited: Events.Edited) callerId deckId = async {
            let stream = resolve deckId
            let! doesSourceExist = doesSourceExist edited.SourceId
            return! stream.Transact(decideEdited edited callerId doesSourceExist)
            }

    let create resolve keyValueStore =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve, keyValueStore)

module Template =
    open Template

    type Writer internal (resolve, keyValueStore: KeyValueStore) =
        let resolve templateId : Stream<_, _> = resolve templateId

        member _.Create (summary: Events.Summary) =
            summary.RevisionIds |> Seq.tryExactlyOne |> function
            | Some revisionId -> async {
                let stream = resolve summary.Id
                let! doesRevisionExist = keyValueStore.Exists revisionId
                return! stream.Transact(decideCreate summary doesRevisionExist)
                }
            | None -> $"There are {summary.RevisionIds.Length} RevisionIds, but there must be exactly 1." |> Error |> Async.singleton
        member _.Edit (edited: Events.Edited) callerId templateId = async {
            let stream = resolve templateId
            let! doesRevisionExist = keyValueStore.Exists edited.RevisionId
            return! stream.Transact(decideEdit edited callerId doesRevisionExist)
            }

    let create resolve keyValueStore =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve, keyValueStore)

module Stack =
    open Stack

    type Writer internal (resolve, keyValueStore: KeyValueStore) =
        let resolve templateId : Stream<_, _> = resolve templateId

        member _.Create (summary: Events.Summary) = async {
            let stream = resolve summary.Id
            let! revision = keyValueStore.GetExampleRevision summary.ExampleRevisionId
            return! stream.Transact(decideCreate summary revision)
            }
        member _.ChangeTags (tagsChanged: Events.TagsChanged) callerId stackId =
            let stream = resolve stackId
            stream.Transact(decideChangeTags tagsChanged callerId)
        member _.ChangeRevision (revisionChanged: Events.RevisionChanged) callerId stackId = async {
            let stream = resolve stackId
            let! revision = keyValueStore.GetExampleRevision revisionChanged.RevisionId
            return! stream.Transact(decideChangeRevision callerId revision)
            }

    let create resolve keyValueStore =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve, keyValueStore)

module UserSaga = // medTODO turn into a real saga
    open User

    type Writer internal (resolve, deckWriter: Deck.Writer) =
        let resolve userId : Stream<_, _> = resolve userId

        member _.Create (summary: Events.Summary) = asyncResult {
            let stream = resolve summary.Id
            do! Deck.Events.defaultSummary summary.Id summary.DefaultDeckId |> deckWriter.Create
            return! stream.Transact(decideCreate summary)
            }

    let create deckWriter resolve =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve, deckWriter)

module ExampleSaga = // medTODO turn into a real saga
    open Example
    open AsyncOp
    
    type Writer internal (exampleResolve, stackResolve, keyValueStore: KeyValueStore, clock: IClock) =
        let exampleResolve exampleId : Stream<_, _> = exampleResolve exampleId
        let   stackResolve   stackId : Stream<_, _> =   stackResolve   stackId
        let buildStack templateRevision (example: Example.Events.Summary) stackId cardSettingId newCardsStartingEaseFactor deckId = result {
            // not validating cardSettingId, newCardsStartingEaseFactor, or deckId cause there's a default to fall back on if it's missing or doesn't belong to them
            let! cardTemplates = Template.getSubtemplateNames templateRevision example.FieldValues
            let! revisionId = example.RevisionIds |> Seq.tryExactlyOne |> Result.requireSome "Only one RevisionId is permitted."
            return
                clock.GetCurrentInstant()
                |> Stack.init stackId example.AuthorId revisionId cardSettingId newCardsStartingEaseFactor deckId cardTemplates
            }

        member _.Create(example: Events.Summary) stackId cardSettingId newCardsStartingEaseFactor deckId = asyncResult {
            do! keyValueStore.Exists stackId |>% Result.requireFalse $"The id '{stackId}' is already used."
            let! templateRevision = keyValueStore.GetTemplateRevision example.TemplateRevisionId
            let! stack = buildStack templateRevision example stackId cardSettingId newCardsStartingEaseFactor deckId
            let revision = example |> Example.toRevisionSummary templateRevision
            
            do! Example.validateSummary example
            do! Stack  .validateSummary stack revision
            
            let exampleStream = exampleResolve example.Id
            let   stackStream =   stackResolve   stack.Id
            do!   exampleStream.Transact(Example.decideCreate example)
            return! stackStream.Transact(Stack  .decideCreate stack revision)
            }
        
    let create exampleResolve stackResolve keyValueStore clock =
        let exampleResolve id = Stream(Log.ForContext<Writer>(), exampleResolve (      streamName id), maxAttempts=3)
        let   stackResolve id = Stream(Log.ForContext<Writer>(),   stackResolve (Stack.streamName id), maxAttempts=3)
        Writer(exampleResolve, stackResolve, keyValueStore, clock)

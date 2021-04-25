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

module Example =
    open Example
    
    type Appender internal (resolve, keyValueStore: KeyValueStore) =
        let resolve exampleId : Stream<_, _> = resolve exampleId

        member _.Create(state: Events.Summary) = asyncResult {
            let stream = resolve state.Id
            return! stream.Transact(decideCreate state)
            }
        member _.Edit (state: Events.Edited) exampleId callerId = async {
            let stream = resolve exampleId
            return! stream.Transact(decideEdit state callerId exampleId)
            }

    let create resolve keyValueStore =
        let resolve id = Stream(Log.ForContext<Appender>(), resolve (streamName id), maxAttempts=3)
        Appender(resolve, keyValueStore)

module User =
    open User

    type Appender internal (resolve, keyValueStore: KeyValueStore) =
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
        let resolve id = Stream(Log.ForContext<Appender>(), resolve (streamName id), maxAttempts=3)
        Appender(resolve, keyValueStore)

module Deck =
    open Deck

    type Appender internal (resolve, keyValueStore: KeyValueStore) =
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
        let resolve id = Stream(Log.ForContext<Appender>(), resolve (streamName id), maxAttempts=3)
        Appender(resolve, keyValueStore)

module TemplateCombo =
    open Template

    type Appender internal (templateResolve, userResolve, keyValueStore: KeyValueStore) =
        let templateResolve templateId : Stream<_, _> = templateResolve templateId
        let     userResolve     userId : Stream<_, _> =     userResolve     userId

        member _.Create (template: Events.Summary) = asyncResult {
            let! author = keyValueStore.GetUser template.AuthorId
            let editedTemplates : User.Events.CollectedTemplatesEdited =
                { TemplateRevisionIds =
                    User.upgradeRevision author.CollectedTemplates template.CurrentRevisionId template.CurrentRevisionId }
            
            do! User    .validateCollectedTemplatesEdited editedTemplates []
            do! Template.validateCreate template
            
            let templateStream = templateResolve template.Id
            let userStream     = userResolve     template.AuthorId

            do! templateStream.Transact(decideCreate template)
            return!
                [] // passing [] because we just created the new templateRevision above, so we know it exists
                |> User.decideCollectedTemplatesEdited editedTemplates template.AuthorId
                |> userStream.Transact
            }
        member _.Edit (edited: Events.Edited) callerId (templateId: TemplateId) = asyncResult {
            let! template = keyValueStore.GetTemplate templateId
            let! author = keyValueStore.GetUser template.AuthorId
            let editedTemplates : User.Events.CollectedTemplatesEdited =
                { TemplateRevisionIds =
                    User.upgradeRevision author.CollectedTemplates template.CurrentRevisionId (template.Id, edited.Revision) }

            do! User    .validateCollectedTemplatesEdited editedTemplates []
            do! Template.validateEdited template callerId edited
            
            let templateStream = templateResolve templateId
            let userStream     = userResolve template.AuthorId

            do! templateStream.Transact(decideEdit edited callerId templateId)
            return!
                [] // passing [] because we just created the new templateRevision above, so we know it exists
                |> User.decideCollectedTemplatesEdited editedTemplates callerId
                |> userStream.Transact
            }

    let create templateResolve userResolve keyValueStore =
        let templateResolve id = Stream(Log.ForContext<Appender>(), templateResolve (     streamName id), maxAttempts=3)
        let userResolve     id = Stream(Log.ForContext<Appender>(), userResolve     (User.streamName id), maxAttempts=3)
        Appender(templateResolve, userResolve, keyValueStore)

module Stack =
    open Stack

    type Appender internal (resolve, keyValueStore: KeyValueStore) =
        let resolve templateId : Stream<_, _> = resolve templateId

        member _.Create (summary: Events.Summary) = async {
            let stream = resolve summary.Id
            let! revision = keyValueStore.GetExampleRevision summary.ExampleRevisionId
            return! stream.Transact(decideCreate summary revision)
            }
        member _.Discard stackId =
            let stream = resolve stackId
            stream.Transact(decideDiscard stackId)
        member _.ChangeTags (tagsChanged: Events.TagsChanged) callerId stackId =
            let stream = resolve stackId
            stream.Transact(decideChangeTags tagsChanged callerId)
        member _.ChangeRevision (revisionChanged: Events.RevisionChanged) callerId stackId = async {
            let stream = resolve stackId
            let! revision = keyValueStore.GetExampleRevision revisionChanged.RevisionId
            return! stream.Transact(decideChangeRevision callerId revision)
            }

    let create resolve keyValueStore =
        let resolve id = Stream(Log.ForContext<Appender>(), resolve (streamName id), maxAttempts=3)
        Appender(resolve, keyValueStore)

module UserSaga = // medTODO turn into a real saga
    open User

    type Appender internal (resolve, deckAppender: Deck.Appender, clock: IClock) =
        let resolve userId : Stream<_, _> = resolve userId

        member _.BuildSummary (id: string) displayName =
            let id = % (Guid.Parse id)
            let cardSettingsId = Guid.NewGuid()
            let defaultDeckId = % Guid.NewGuid()
            let now = clock.GetCurrentInstant()
            User.init id displayName defaultDeckId now cardSettingsId

        member _.Create (summary: Events.Summary) = asyncResult {
            let stream = resolve summary.Id
            do! Deck.Events.defaultSummary summary.Id summary.DefaultDeckId |> deckAppender.Create
            return! stream.Transact(decideCreate summary)
            }

    let create deckAppender resolve clock =
        let resolve id = Stream(Log.ForContext<Appender>(), resolve (streamName id), maxAttempts=3)
        Appender(resolve, deckAppender, clock)

module ExampleCombo =
    open Example
    open AsyncOp
    
    type Appender internal (exampleResolve, stackResolve, keyValueStore: KeyValueStore, clock: IClock) =
        let exampleResolve exampleId : Stream<_, _> = exampleResolve exampleId
        let   stackResolve   stackId : Stream<_, _> =   stackResolve   stackId
        let buildStack templateRevision (example: Example.Events.Summary) stackId cardSettingId newCardsStartingEaseFactor deckId = result {
            // not validating cardSettingId, newCardsStartingEaseFactor, or deckId cause there's a default to fall back on if it's missing or doesn't belong to them
            let! pointers = Template.getCardTemplatePointers templateRevision example.FieldValues
            return
                clock.GetCurrentInstant()
                |> Stack.init stackId example.AuthorId example.CurrentRevisionId cardSettingId newCardsStartingEaseFactor deckId pointers
            }

        member _.Create(example: Events.Summary) stackId cardSettingId newCardsStartingEaseFactor deckId = asyncResult {
            do! keyValueStore.Exists stackId |>% Result.requireFalse $"The id '{stackId}' is already used."
            let! templateRevision = keyValueStore.GetTemplateRevision example.TemplateRevisionId
            let! stack = buildStack templateRevision example stackId cardSettingId newCardsStartingEaseFactor deckId
            let revision = example |> Example.toRevisionSummary templateRevision
            
            do! Example.validateCreate example
            do! Stack  .validateSummary stack revision
            
            let exampleStream = exampleResolve example.Id
            let   stackStream =   stackResolve   stack.Id

            do!   exampleStream.Transact(Example.decideCreate example)
            return! stackStream.Transact(Stack  .decideCreate stack revision)
            }

        member _.Edit (edited: Events.Edited) (exampleId: ExampleId) (stackId: StackId) callerId = asyncResult {
            let! stack            = keyValueStore.GetStack stackId
            let! example          = keyValueStore.GetExample exampleId
            let! templateRevision = keyValueStore.GetTemplateRevision edited.TemplateRevisionId
            let revision = example |> Example.Fold.evolveEdited edited |> Example.toRevisionSummary templateRevision
            
            do! Example.validateEdit callerId example edited
            do! Stack  .validateRevisionChanged stack callerId revision
            
            let exampleStream = exampleResolve example.Id
            let   stackStream =   stackResolve   stack.Id

            do!   exampleStream.Transact(Example.decideEdit edited callerId example.Id)
            return! stackStream.Transact(Stack  .decideChangeRevision callerId revision)
            }

    let create exampleResolve stackResolve keyValueStore clock =
        let exampleResolve id = Stream(Log.ForContext<Appender>(), exampleResolve (      streamName id), maxAttempts=3)
        let   stackResolve id = Stream(Log.ForContext<Appender>(),   stackResolve (Stack.streamName id), maxAttempts=3)
        Appender(exampleResolve, stackResolve, keyValueStore, clock)

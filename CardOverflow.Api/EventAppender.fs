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
// Appenders should only read from the event stream to maintian consistency https://www.youtube.com/watch?v=DDefPUCB9ao&t=350s
//

module Example =
    open Example
    
    type Appender internal (resolve) =
        let resolve exampleId : Stream<_, _> = resolve exampleId

        member _.Create(created: Events.Created) = asyncResult {
            let stream = resolve created.Id
            return! stream.Transact(decideCreate created)
            }
        member _.Edit (state: Events.Edited) exampleId = async {
            let stream = resolve exampleId
            return! stream.Transact(decideEdit state exampleId)
            }

    let create resolve =
        let resolve id = Stream(Log.ForContext<Appender>(), resolve (streamName id), maxAttempts=3)
        Appender(resolve)

module User =
    open User

    type Appender internal (resolveUser, resolveDeck) =
        let resolveUser userId : Stream<_                , _> = resolveUser userId
        let resolveDeck deckId : Stream<Deck.Events.Event, _> = resolveDeck deckId

        member _.OptionsEdited userId (o: Events.OptionsEdited) = asyncResult {
            let stream = resolveUser userId
            let! (deck: Summary.Deck) = (resolveDeck o.DefaultDeckId).Query Deck.getActive
            return! stream.Transact(decideOptionsEdited o deck.AuthorId)
            }
        member _.CardSettingsEdited userId cardSettingsEdited =
            let stream = resolveUser userId
            stream.Transact(decideCardSettingsEdited cardSettingsEdited)
        member _.DeckFollowed (deckFollowed: Events.DeckFollowed) = asyncResult {
            let stream = resolveUser deckFollowed.Meta.UserId
            let! deck = (resolveDeck deckFollowed.DeckId).Query Deck.getActive
            return! stream.Transact(decideFollowDeck deck deckFollowed)
            }
        member _.DeckUnfollowed (deckUnfollowed: Events.DeckUnfollowed) =
            let stream = resolveUser deckUnfollowed.Meta.UserId
            stream.Transact(decideUnfollowDeck deckUnfollowed)

    let create resolveUser resolveDeck =
        let resolveUser id = Stream(Log.ForContext<Appender>(), resolveUser (     streamName id), maxAttempts=3)
        let resolveDeck id = Stream(Log.ForContext<Appender>(), resolveDeck (Deck.streamName id), maxAttempts=3)
        Appender(resolveUser, resolveDeck)

module Deck =
    open Deck

    type Appender internal (resolve) =
        let resolve deckId : Stream<_, _> = resolve deckId

        member _.Create (created: Events.Created) = async {
            let stream = resolve created.Id
            return! stream.Transact(decideCreate created)
            }
        member _.Edit (edited: Events.Edited) deckId = async {
            let stream = resolve deckId
            return! stream.Transact(decideEdited edited)
            }

        member this.Sync (clientEvents: ClientEvent<Events.Event> seq) = asyncResult {
            for { StreamId = streamId; Event = event } in clientEvents do
                let streamId = % streamId
                do! match event with
                    | Events.Created c -> this.Create c
                    | Events.Edited  e -> this.Edit e streamId
            }

    let create resolve =
        let resolve id = Stream(Log.ForContext<Appender>(), resolve (streamName id), maxAttempts=3)
        Appender(resolve)

module TemplateCombo =
    open Template

    type Appender internal (resolveTemplate, resolveUser) =
        let resolveTemplate templateId : Stream<_, _> = resolveTemplate templateId
        let resolveUser         userId : Stream<_, _> = resolveUser         userId

        member _.Create (created: Events.Created) = asyncResult {
            let! author = (resolveUser created.Meta.UserId).Query User.getActive
            let editedTemplates : User.Events.CollectedTemplatesEdited =
                { Meta = created.Meta
                  TemplateRevisionIds = User.appendRevision author.CollectedTemplates (created.Id, Fold.initialTemplateRevisionOrdinal) }
            
            do! User    .validateCollectedTemplatesEdited editedTemplates [] author
            do! Template.validateCreate created
            
            let templateStream = resolveTemplate created.Id
            let userStream     = resolveUser     created.Meta.UserId

            do! templateStream.Transact(decideCreate created)
            return!
                [] // passing [] because we just created the new templateRevision above, so we know it exists
                |> User.decideCollectedTemplatesEdited editedTemplates
                |> userStream.Transact
            }
        member _.Edit (edited: Events.Edited) (templateId: TemplateId) = asyncResult {
            let! (template: Summary.Template) = (resolveTemplate templateId).Query Template.getActive
            let! author = (resolveUser template.AuthorId).Query User.getActive
            let editedTemplates : User.Events.CollectedTemplatesEdited =
                { Meta = edited.Meta
                  TemplateRevisionIds = User.upgradeRevision author.CollectedTemplates template.CurrentRevisionId (template.Id, edited.Ordinal) }

            do! User    .validateCollectedTemplatesEdited editedTemplates [] author
            do! Template.validateEdited template edited
            
            let templateStream = resolveTemplate templateId
            let userStream     = resolveUser template.AuthorId

            do! templateStream.Transact(decideEdit edited templateId)
            return!
                [] // passing [] because we just created the new templateRevision above, so we know it exists
                |> User.decideCollectedTemplatesEdited editedTemplates
                |> userStream.Transact
            }

    let create resolveTemplate resolveUser =
        let resolveTemplate id = Stream(Log.ForContext<Appender>(), resolveTemplate (     streamName id), maxAttempts=3)
        let resolveUser     id = Stream(Log.ForContext<Appender>(), resolveUser     (User.streamName id), maxAttempts=3)
        Appender(resolveTemplate, resolveUser)

module Stack =
    open Stack

    type Appender internal (resolveStack, resolveTemplate, resolveExample) =
        let resolveStack       stackId : Stream<_                    , _> = resolveStack       stackId
        let resolveTemplate templateId : Stream<Template.Events.Event, _> = resolveTemplate templateId
        let resolveExample   exampleId : Stream<Example.Events.Event , _> = resolveExample   exampleId

        let getExampleRevision (exampleId, ordinal) =
            (resolveExample exampleId).Query Example.getActive
            |> AsyncResult.map (fun x ->
                x.Revisions
                |> List.filter (fun x -> x.Ordinal = ordinal)
                |> List.exactlyOne
            )
        let getTemplateRevision (templateId, ordinal) =
            (resolveTemplate templateId).Query Template.getActive
            |> AsyncResult.map (fun x ->
                x.Revisions
                |> List.filter (fun x -> x.Ordinal = ordinal)
                |> List.exactlyOne
            )

        member _.Create (created: Events.Created) = asyncResult {
            let stream = resolveStack created.Id
            let! exampleRevision  = getExampleRevision created.ExampleRevisionId
            let! templateRevision = getTemplateRevision exampleRevision.TemplateRevisionId
            return! stream.Transact(decideCreate created templateRevision exampleRevision)
            }
        member _.Discard discarded stackId =
            let stream = resolveStack stackId
            stream.Transact(decideDiscard stackId discarded)
        member _.ChangeTags (tagsChanged: Events.TagsChanged) stackId =
            let stream = resolveStack stackId
            stream.Transact(decideChangeTags tagsChanged)
        member _.ChangeRevision (revisionChanged: Events.RevisionChanged) stackId = asyncResult {
            let stream = resolveStack stackId
            let! exampleRevision  = getExampleRevision revisionChanged.RevisionId
            let! templateRevision = getTemplateRevision exampleRevision.TemplateRevisionId
            return! stream.Transact(decideChangeRevision revisionChanged templateRevision exampleRevision)
            }

    let create resolveStack resolveTemplate resolveExample =
        let resolveStack    id = Stream(Log.ForContext<Appender>(), resolveStack    (         streamName id), maxAttempts=3)
        let resolveTemplate id = Stream(Log.ForContext<Appender>(), resolveTemplate (Template.streamName id), maxAttempts=3)
        let resolveExample  id = Stream(Log.ForContext<Appender>(), resolveExample  (Example .streamName id), maxAttempts=3)
        Appender(resolveStack, resolveTemplate, resolveExample)

module UserSaga = // medTODO turn into a real saga
    open User

    type Appender internal (resolve, deckAppender: Deck.Appender) =
        let resolve userId : Stream<_, _> = resolve userId

        member _.BuildSignedUp meta displayName =
            let cardSettingsId = Guid.NewGuid()
            let defaultDeckId = % Guid.NewGuid()
            User.init meta displayName defaultDeckId cardSettingsId

        member _.Create (signedUp: Events.SignedUp) = asyncResult {
            let stream = resolve signedUp.Meta.UserId
            do! Deck.defaultDeck signedUp.Meta signedUp.DefaultDeckId |> deckAppender.Create
            return! stream.Transact(decideSignedUp signedUp)
            }

    let create deckAppender resolve =
        let resolve id = Stream(Log.ForContext<Appender>(), resolve (streamName id), maxAttempts=3)
        Appender(resolve, deckAppender)

module ExampleCombo =
    open Example
    open AsyncOp
    
    type Appender internal (resolveExample, resolveStack, resolveTemplate, clock: IClock) =
        let resolveExample   exampleId : Stream<_                    , _> = resolveExample   exampleId
        let resolveStack       stackId : Stream<_                    , _> = resolveStack       stackId
        let resolveTemplate templateId : Stream<Template.Events.Event, _> = resolveTemplate templateId
        let buildStack meta templateRevision (example: Example) stackId cardSettingId newCardsStartingEaseFactor deckId = result {
            // not validating cardSettingId, newCardsStartingEaseFactor, or deckId cause there's a default to fall back on if it's missing or doesn't belong to them
            let! pointers = Template.getCardTemplatePointers templateRevision example.CurrentRevision.FieldValues
            return
                clock.GetCurrentInstant()
                |> Stack.init stackId meta example.CurrentRevisionId cardSettingId newCardsStartingEaseFactor deckId pointers
            }

        let getRevision (templateId, ordinal) =
            (resolveTemplate templateId).Query Template.getActive
            |> AsyncResult.map (fun x ->
                x.Revisions
                |> List.filter (fun x -> x.Ordinal = ordinal)
                |> List.exactlyOne
            )

        member _.Create(exampleCreated: Events.Created) stackId cardSettingId newCardsStartingEaseFactor deckId = asyncResult {
            let! templateRevision = getRevision exampleCreated.TemplateRevisionId
            let example = Example.Fold.evolveCreated exampleCreated
            let! stack = buildStack exampleCreated.Meta templateRevision example stackId cardSettingId newCardsStartingEaseFactor deckId
            let exampleRevision = example.CurrentRevision
            
            do! Example.validateCreate exampleCreated
            do! Stack  .validateCreated stack templateRevision exampleRevision
            
            let exampleStream = resolveExample exampleCreated.Id
            let   stackStream = resolveStack   stack.Id

            do!   exampleStream.Transact(Example.decideCreate exampleCreated)
            return! stackStream.Transact(Stack  .decideCreate stack templateRevision exampleRevision)
            }

        member _.Edit (edited: Events.Edited) (exampleId: ExampleId) (stackId: StackId) = asyncResult {
            let! stack            = (resolveStack     stackId).Query   Stack.getActive
            let! example          = (resolveExample exampleId).Query Example.getActive
            let! templateRevision = getRevision edited.TemplateRevisionId
            let newExample = example |> Example.Fold.evolveEdited edited
            let revisionChanged : Stack.Events.RevisionChanged = { Meta = edited.Meta; RevisionId = newExample.CurrentRevisionId }
            
            do! Example.validateEdit example edited
            do! Stack  .validateRevisionChanged revisionChanged newExample.CurrentRevision templateRevision stack
            
            let exampleStream = resolveExample example.Id
            let   stackStream = resolveStack     stack.Id

            do!   exampleStream.Transact(Example.decideEdit edited example.Id)
            return! stackStream.Transact(Stack  .decideChangeRevision revisionChanged templateRevision newExample.CurrentRevision)
            }

    let create resolveExample resolveStack resolveTemplate clock =
        let resolveExample  id = Stream(Log.ForContext<Appender>(), resolveExample  (         streamName id), maxAttempts=3)
        let resolveStack    id = Stream(Log.ForContext<Appender>(), resolveStack    (Stack   .streamName id), maxAttempts=3)
        let resolveTemplate id = Stream(Log.ForContext<Appender>(), resolveTemplate (Template.streamName id), maxAttempts=3)
        Appender(resolveExample, resolveStack, resolveTemplate, clock)

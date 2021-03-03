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

module Concept =
    open Concept

    type Writer internal (resolve, tableClient: TableClient) =
        let resolve conceptId : Stream<_, _> = resolve conceptId

        member internal _.Create(state: Events.Summary) =
            let stream = resolve state.Id
            stream.Transact(decideCreate state)
        member _.ChangeDefaultExample conceptId (newDefaultExampleId: ExampleId) callerId = asyncResult {
            let stream = resolve conceptId
            let! b, _ = tableClient.GetExample newDefaultExampleId
            return! decideDefaultExampleChanged b.Id b.ConceptId callerId |> stream.Transact
        }

    let create resolve tableClient =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve, tableClient)

module ConceptExample =

    let example authorId command title : Example.Events.Summary =
        { Id = % command.Ids.ExampleId
          RevisionIds = [ % command.Ids.RevisionId ]
          Title = title
          ConceptId = % command.Ids.ConceptId
          AuthorId = authorId
          TemplateRevisionId = % command.TemplateRevisionId
          AnkiNoteId = None
          FieldValues = command.FieldValues |> Seq.map (fun x -> x.EditField.Name, x.Value) |> Map.ofSeq
          EditSummary = command.EditSummary }
    let concept authorId command sourceRevisionId : Concept.Events.Summary =
        { Id = % command.Ids.ConceptId
          DefaultExampleId = % command.Ids.ExampleId
          AuthorId = authorId
          CopySourceRevisionId = sourceRevisionId }
    let conceptExample authorId command sourceRevisionId title =
        (concept  authorId command sourceRevisionId),
        (example authorId command title)

    type Writer
        (   conceptWriter : Concept.Writer,
            exampleWriter : Example.Writer) =
    
        let create (conceptSummary, exampleSummary) = asyncResult { // medTODO turn into a real saga
            do!     conceptWriter.Create conceptSummary
            return! exampleWriter.Create exampleSummary
        }

        member _.Upsert (authorId, command) =
            match command.Kind with
            | NewOriginal_TagIds tags -> // highTODO create card (with tag)
                conceptExample authorId command None                     "Default" |> create
            | NewCopy_SourceRevisionId_TagIds (sourceRevisionId, tags) -> // highTODO create card (with tag)
                conceptExample authorId command (% sourceRevisionId |> Some) "Default" |> create
            | NewExample_Title title ->
                exampleWriter.Create(example authorId command title)
            | NewRevision_Title title ->
                let example        = example authorId command title
                let edited : Example.Events.Edited =
                    { RevisionId             = example.RevisionIds.Head
                      Title              = example.Title
                      TemplateRevisionId = example.TemplateRevisionId
                      FieldValues        = example.FieldValues
                      EditSummary        = example.EditSummary }
                exampleWriter.Edit(edited, example.Id, authorId)

module User =
    open User

    type Writer internal (resolve, tableClient: TableClient) =
        let resolve userId : Stream<_, _> = resolve userId

        member _.OptionsEdited userId (o: Events.OptionsEdited) = async {
            let stream = resolve userId
            let! deck, _ = tableClient.GetDeck o.DefaultDeckId
            return! stream.Transact(decideOptionsEdited o deck.AuthorId)
            }
        member _.CardSettingsEdited userId cardSettingsEdited =
            let stream = resolve userId
            stream.Transact(decideCardSettingsEdited cardSettingsEdited)
        member _.DeckFollowed userId deckId = async {
            let stream = resolve userId
            let! doesDeckExist = tableClient.Exists deckId
            return! stream.Transact(decideFollowDeck deckId doesDeckExist)
            }
        member _.DeckUnfollowed userId deckId =
            let stream = resolve userId
            stream.Transact(decideUnfollowDeck deckId)

    let create resolve tableClient =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve, tableClient)

module Deck =
    open Deck

    type Writer internal (resolve, tableClient: TableClient) =
        let resolve deckId : Stream<_, _> = resolve deckId

        let doesSourceExist (source: DeckId option) =
            match source with
            | None -> Async.singleton true
            | Some x -> tableClient.Exists x

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

    let create resolve tableClient =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve, tableClient)

module Template =
    open Template

    type Writer internal (resolve, tableClient: TableClient) =
        let resolve templateId : Stream<_, _> = resolve templateId

        member _.Create (summary: Events.Summary) =
            summary.RevisionIds |> Seq.tryExactlyOne |> function
            | Some revisionId -> async {
                let stream = resolve summary.Id
                let! doesRevisionExist = tableClient.Exists revisionId
                return! stream.Transact(decideCreate summary doesRevisionExist)
                }
            | None -> $"There are {summary.RevisionIds.Length} RevisionIds, but there must be exactly 1." |> Error |> Async.singleton
        member _.Edit (edited: Events.Edited) callerId templateId = async {
            let stream = resolve templateId
            let! doesRevisionExist = tableClient.Exists edited.RevisionId
            return! stream.Transact(decideEdit edited callerId doesRevisionExist)
            }

    let create resolve tableClient =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve, tableClient)

module Stack =
    open Stack

    type Writer internal (resolve, tableClient: TableClient) =
        let resolve templateId : Stream<_, _> = resolve templateId

        member _.Create (summary: Events.Summary) = async {
            let stream = resolve summary.Id
            let! doesRevisionExist = tableClient.Exists summary.ExampleRevisionId
            return! stream.Transact(decideCreate summary doesRevisionExist)
            }
        member _.ChangeTags (tagsChanged: Events.TagsChanged) callerId stackId =
            let stream = resolve stackId
            stream.Transact(decideChangeTags tagsChanged callerId)

    let create resolve tableClient =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve, tableClient)

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

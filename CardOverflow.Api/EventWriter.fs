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

module Branch =
    open Branch
    
    type Writer internal (resolve) =
        let resolve branchId : Stream<_, _> = resolve branchId

        member _.Create(state: Events.Summary) =
            let stream = resolve state.Id
            stream.Transact(decideCreate state)
        member _.Edit(state, branchId, callerId) =
            let stream = resolve branchId
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
        member _.ChangeDefaultBranch conceptId (newDefaultBranchId: BranchId) callerId = asyncResult {
            let stream = resolve conceptId
            let! b, _ = tableClient.GetBranch newDefaultBranchId
            return! decideDefaultBranchChanged b.Id b.ConceptId callerId |> stream.Transact
        }

    let create resolve tableClient =
        let resolve id = Stream(Log.ForContext<Writer>(), resolve (streamName id), maxAttempts=3)
        Writer(resolve, tableClient)

module ConceptBranch =

    let branch authorId command title : Branch.Events.Summary =
        { Id = % command.Ids.BranchId
          LeafIds = [ % command.Ids.LeafId ]
          Title = title
          ConceptId = % command.Ids.ConceptId
          AuthorId = authorId
          TemplateRevisionId = % command.Grompleaf.Id
          AnkiNoteId = None
          FieldValues = command.FieldValues |> Seq.map (fun x -> x.EditField.Name, x.Value) |> Map.ofSeq
          EditSummary = command.EditSummary }
    let concept authorId command sourceLeafId : Concept.Events.Summary =
        { Id = % command.Ids.ConceptId
          DefaultBranchId = % command.Ids.BranchId
          AuthorId = authorId
          CopySourceLeafId = sourceLeafId }
    let conceptBranch authorId command sourceLeafId title =
        (concept  authorId command sourceLeafId),
        (branch authorId command title)

    type Writer
        (   conceptWriter  : Concept.Writer,
            branchWriter : Branch.Writer) =
    
        let create (conceptSummary, branchSummary) = asyncResult { // medTODO turn into a real saga
            do!     conceptWriter .Create conceptSummary
            return! branchWriter.Create branchSummary
        }

        member _.Upsert (authorId, command) =
            match command.Kind with
            | NewOriginal_TagIds tags -> // highTODO create card (with tag)
                conceptBranch authorId command None                     "Default" |> create
            | NewCopy_SourceLeafId_TagIds (sourceLeafId, tags) -> // highTODO create card (with tag)
                conceptBranch authorId command (% sourceLeafId |> Some) "Default" |> create
            | NewBranch_Title title ->
                branchWriter.Create(branch authorId command title)
            | NewLeaf_Title title ->
                let branch        = branch authorId command title
                let edited : Branch.Events.Edited =
                    { LeafId             = branch.LeafIds.Head
                      Title              = branch.Title
                      TemplateRevisionId = branch.TemplateRevisionId
                      FieldValues        = branch.FieldValues
                      EditSummary        = branch.EditSummary }
                branchWriter.Edit(edited, branch.Id, authorId)

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

module Ztack =
    open Ztack

    type Writer internal (resolve, tableClient: TableClient) =
        let resolve templateId : Stream<_, _> = resolve templateId

        member _.Create (summary: Events.Summary) = async {
            let stream = resolve summary.Id
            let! doesRevisionExist = tableClient.Exists summary.ExpressionRevisionId
            return! stream.Transact(decideCreate summary doesRevisionExist)
            }
        member _.ChangeTags (tagsChanged: Events.TagsChanged) callerId templateId =
            let stream = resolve templateId
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

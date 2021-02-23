module EventService

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
    
    type Service internal (resolve) =
        let resolve branchId : Stream<_, _> = resolve branchId

        member _.Create(state: Events.Summary) =
            let stream = resolve state.Id
            stream.Transact(decideCreate state)
        member _.Edit(state, branchId, callerId) =
            let stream = resolve branchId
            stream.Transact(decideEdit state callerId)

    let create resolve =
        let resolve id = Stream(Log.ForContext<Service>(), resolve (streamName id), maxAttempts=3)
        Service(resolve)

module Stack =
    open Stack

    type Service internal (resolve, tableClient: TableClient) =
        let resolve stackId : Stream<_, _> = resolve stackId

        member internal _.Create(state: Events.Summary) =
            let stream = resolve state.Id
            stream.Transact(decideCreate state)
        member _.ChangeDefaultBranch stackId (newDefaultBranchId: BranchId) callerId = asyncResult {
            let stream = resolve stackId
            let! b, _ = tableClient.GetBranch newDefaultBranchId
            return! decideDefaultBranchChanged b.Id b.StackId callerId |> stream.Transact
        }

    let create resolve tableClient =
        let resolve id = Stream(Log.ForContext<Service>(), resolve (streamName id), maxAttempts=3)
        Service(resolve, tableClient)

module StackBranch =

    let branch authorId command tags title : Branch.Events.Summary =
        { Id = % command.Ids.BranchId
          LeafId = % command.Ids.LeafId
          LeafIds = [ % command.Ids.LeafId ]
          Title = title
          StackId = % command.Ids.StackId
          AuthorId = authorId
          GrompleafId = % command.Grompleaf.Id
          AnkiNoteId = None
          GotDMCAed = false
          FieldValues = command.FieldValues |> Seq.map (fun x -> x.EditField.Name, x.Value) |> Map.ofSeq
          EditSummary = command.EditSummary
          Tags = tags }
    let stack authorId command sourceLeafId : Stack.Events.Summary =
        { Id = % command.Ids.StackId
          DefaultBranchId = % command.Ids.BranchId
          AuthorId = authorId
          CopySourceLeafId = sourceLeafId }
    let stackBranch authorId command sourceLeafId tags title =
        (stack  authorId command sourceLeafId),
        (branch authorId command tags title)

    type Service
        (   stackS  : Stack.Service,
            branchS : Branch.Service) =
    
        let create (stackSummary, branchSummary) = asyncResult { // medTODO turn into a real saga
            do!     stackS .Create stackSummary
            return! branchS.Create branchSummary
        }

        member _.Upsert (authorId, command) =
            match command.Kind with
            | NewOriginal_TagIds tags ->
                stackBranch authorId command None                     tags "Default" |> create
            | NewCopy_SourceLeafId_TagIds (sourceLeafId, tags) ->
                stackBranch authorId command (% sourceLeafId |> Some) tags "Default" |> create
            | NewBranch_Title title ->
                branchS.Create(branch authorId command Set.empty title)
            | NewLeaf_Title title ->
                let branch  =   branch authorId command Set.empty title
                let edited : Branch.Events.Edited =
                    { LeafId      = branch.LeafId
                      Title       = branch.Title
                      GrompleafId = branch.GrompleafId
                      FieldValues = branch.FieldValues
                      EditSummary = branch.EditSummary }
                branchS.Edit(edited, branch.Id, authorId)

module User =
    open User

    type Service internal (resolve, tableClient: TableClient) =
        let resolve userId : Stream<_, _> = resolve userId

        member _.OptionsEdited userId (o: Events.OptionsEdited) = async {
            let stream = resolve userId
            let! deck, _ = tableClient.GetDeck o.DefaultDeckId
            return! stream.Transact(decideOptionsEdited o deck.UserId)
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
        let resolve id = Stream(Log.ForContext<Service>(), resolve (streamName id), maxAttempts=3)
        Service(resolve, tableClient)

module Deck =
    open Deck

    type Service internal (resolve, tableClient: TableClient) =
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
        let resolve id = Stream(Log.ForContext<Service>(), resolve (streamName id), maxAttempts=3)
        Service(resolve, tableClient)

module UserSaga = // medTODO turn into a real saga
    open User

    type Service internal (resolve, deckService: Deck.Service) =
        let resolve userId : Stream<_, _> = resolve userId

        member _.Create (summary: Events.Summary) = asyncResult {
            let stream = resolve summary.Id
            do! Deck.Events.defaultSummary summary.Id summary.DefaultDeckId |> deckService.Create
            return! stream.Transact(decideCreate summary)
            }

    let create deckService resolve =
        let resolve id = Stream(Log.ForContext<Service>(), resolve (streamName id), maxAttempts=3)
        Service(resolve, deckService)
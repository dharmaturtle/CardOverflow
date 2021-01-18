module EventService

open Equinox
open FsCodec
open FsCodec.NewtonsoftJson
open Serilog
open TypeShape
open CardOverflow.Pure
open CardOverflow.Api
open FsToolkit.ErrorHandling
open Domain.Infrastructure

module Branch =
    open Domain.Branch
    
    type Service internal (resolve) =

        member _.Create(state: Events.Snapshotted) =
            let stream : Stream<_, _> = resolve state.Id
            stream.Transact(decideCreate state)
        member _.Edit(state, branchId, callerId) =
            let stream : Stream<_, _> = resolve branchId
            stream.Transact(decideEdit state callerId)

    let create resolve =
        let resolve id = Stream(Log.ForContext<Service>(), resolve (streamName id), maxAttempts=3)
        Service(resolve)

module Stack =
    open Domain.Stack

    type Service internal (resolve, tableClient: TableClient) =

        member internal _.Create(state: Events.Snapshotted) =
            let stream : Stream<_, _> = resolve state.Id
            stream.Transact(decideCreate state)
        member _.ChangeDefaultBranch stackId (newDefaultBranchId: BranchId) callerId = asyncResult {
            let stream : Stream<_, _> = resolve stackId
            let! b, _ = tableClient.GetBranch newDefaultBranchId
            return! decideDefaultBranchChanged b.Id b.StackId callerId |> stream.Transact
        }

    let create resolve tableClient =
        let resolve id = Stream(Log.ForContext<Service>(), resolve (streamName id), maxAttempts=3)
        Service(resolve, tableClient)

module StackBranch =

    open Domain
    open FSharp.UMX

    let branch authorId command tags title : Branch.Events.Snapshotted =
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
    let stack authorId command sourceLeafId : Stack.Events.Snapshotted =
        { Id = % command.Ids.StackId
          DefaultBranchId = % command.Ids.BranchId
          AuthorId = authorId
          CopySourceLeafId = sourceLeafId }
    let stackBranch authorId command sourceLeafId tags title =
        (stack  authorId command sourceLeafId),
        (branch authorId command tags title)

    type Service
        (   stacks : Stack.Service,
            branches : Branch.Service) =
    
        let create (stackSnapshot, branchSnapshot) = asyncResult {
            do!     stacks  .Create stackSnapshot
            return! branches.Create branchSnapshot
        }

        member _.Upsert (authorId, command) =
            match command.Kind with
            | NewOriginal_TagIds tags ->
                stackBranch authorId command None                     tags "Default" |> create
            | NewCopy_SourceLeafId_TagIds (sourceLeafId, tags) ->
                stackBranch authorId command (% sourceLeafId |> Some) tags "Default" |> create
            | NewBranch_Title title ->
                branches.Create(branch authorId command [] title)
            | NewLeaf_Title title ->
                let branch  =   branch authorId command [] title
                let edited : Branch.Events.Edited =
                    { LeafId      = branch.LeafId
                      Title       = branch.Title
                      GrompleafId = branch.GrompleafId
                      FieldValues = branch.FieldValues
                      EditSummary = branch.EditSummary }
                branches.Edit(edited, branch.Id, authorId)

module User =
    open Domain.User

    type Service internal (resolve) =

        member _.Create(state: Events.Snapshotted) =
            let stream : Stream<_, _> = resolve state.UserId
            stream.Transact(decideCreate state)

    let create resolve =
        let resolve id = Stream(Log.ForContext<Service>(), resolve (streamName id), maxAttempts=3)
        Service(resolve)

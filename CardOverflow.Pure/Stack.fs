module Domain.Stack

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape

let streamName (id: StackId) = StreamName.create "Stack" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =
    
    type Snapshot =
        { Id: StackId
          DefaultBranchId: BranchId
          AuthorId: UserId
          CopySourceLeafId: LeafId Option }
    type DefaultBranchChanged = { BranchId: BranchId }

    type Event =
        | DefaultBranchChanged of DefaultBranchChanged
        | Snapshot             of Snapshot
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event>()

module Fold =
    
    type State =
        | Initial
        | Active of Events.Snapshot
    let initial = State.Initial

    let evolve state =
        function
        | Events.Snapshot s -> State.Active s
        | Events.DefaultBranchChanged b ->
            match state with
            | State.Initial  -> invalidOp "Can't change the default branch of an Initial Stack"
            | State.Active a -> { a with DefaultBranchId = b.BranchId } |> State.Active
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Snapshot _ -> true | _ -> false

let decideCreate state = function
    | Fold.State.Initial  -> Ok ()                  , [ Events.Snapshot state ]
    | Fold.State.Active _ -> Error "Already created", []

let decideDefaultBranchChanged (branchId: BranchId) (branchsStackId: StackId) callerId = function
    | Fold.State.Initial  -> Error "Can't edit a branch that doesn't exist", []
    | Fold.State.Active s ->
        if s.AuthorId <> callerId then
            Error $"Stack {s.Id} doesn't belong to User {callerId}"        , []
        elif s.Id <> branchsStackId then
            Error $"Branch {branchId} doesn't belong to Stack {s.Id}"      , []
        else
            Ok ()                                                          , [ { Events.DefaultBranchChanged.BranchId = branchId } |> Events.DefaultBranchChanged ]

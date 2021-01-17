module Domain.Stack

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape

let streamName (id: StackId) = StreamName.create "Stack" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =
    
    type Snapshotted =
        { Id: StackId
          DefaultBranchId: BranchId
          AuthorId: UserId
          CopySourceLeafId: LeafId Option }
    type DefaultBranchChanged = { BranchId: BranchId }

    type Event =
        | DefaultBranchChanged of DefaultBranchChanged
        | Snapshotted          of Snapshotted
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event>()

[<RequireQualifiedAccess>] type Shot = Events.Snapshotted

module Fold =
    
    type State =
        | Initial
        | Active of Events.Snapshotted
    let initial = State.Initial

    let evolve state =
        function
        | Events.Snapshotted s -> State.Active s
        | Events.DefaultBranchChanged b ->
            match state with
            | State.Initial  -> invalidOp "Can't change the default branch of an Initial Stack"
            | State.Active a -> { a with DefaultBranchId = b.BranchId } |> State.Active
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Snapshotted _ -> true | _ -> false

let decideCreate state = function
    | Fold.State.Initial  -> Ok ()                  , [ Events.Snapshotted state ]
    | Fold.State.Active _ -> Error "Already created", []

module Domain.Stack

open Equinox
open FsCodec
open FsCodec.NewtonsoftJson
open Serilog
open TypeShape

let streamName (id: StackId) = StreamName.create "Stack" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =
    
    type Snapshotted =
        { StackId: StackId
          DefaultBranchId: BranchId
          AuthorId: UserId
          CopySourceLeafId: LeafId Option }

    type Event =
        | DefaultBranchChanged of {| BranchId: BranchId |}
        | Snapshotted          of Snapshotted
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event>()

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

type Service internal (resolve) =

    member _.Create(state: Events.Snapshotted) =
        let stream : Stream<_, _> = resolve state.StackId
        stream.Transact(decideCreate state)

let create resolve =
    let resolve id = Stream(Log.ForContext<Service>(), resolve (streamName id), maxAttempts=3)
    Service(resolve)

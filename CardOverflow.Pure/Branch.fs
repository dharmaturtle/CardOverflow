module Domain.Branch

open Equinox
open FsCodec
open FsCodec.NewtonsoftJson
open Serilog
open TypeShape

let streamName (id: BranchId) = StreamName.create "Branch" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Snapshotted =
        { BranchId: BranchId
          LeafId: LeafId
          Name: string
          StackId: StackId
          AuthorId: AuthorId
          GrompleafId: GrompleafId
          AnkiNoteId: int64 option
          GotDMCAed: bool
          FieldValues: string list
          EditSummary: string
          Tags: string list }
    type Edited =
        { LeafId: LeafId
          Name: string
          GrompleafId: GrompleafId
          FieldValues: string list
          EditSummary: string }

    type Event =
        | Snapshotted of Snapshotted
        | Edited      of Edited
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event>()

module Fold =

    type State =
        | Initial
        | Active of Events.Snapshotted
    let initial : State = State.Initial
    
    let evolve state =
        function
        | Events.Snapshotted s -> State.Active s
        | Events.Edited b ->
            match state with
            | State.Initial -> invalidOp "Can't edit an Initial Branch"
            | State.Active a ->
                { a with
                    LeafId = b.LeafId
                    Name = b.Name
                    GrompleafId = b.GrompleafId
                    FieldValues = b.FieldValues
                    EditSummary = b.EditSummary
                } |> State.Active

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Snapshotted _ -> true | _ -> false

let decideCreate state = function
    | Fold.State.Initial  -> Ok ()                  , [ Events.Snapshotted state ]
    | Fold.State.Active _ -> Error "Already created", []

type Service internal (resolve) =

    member _.Create(state: Events.Snapshotted) =
        let stream : Stream<_, _> = resolve state.BranchId
        stream.Transact(decideCreate state)

let create resolve =
    let resolve id = Stream(Log.ForContext<Service>(), resolve (streamName id), maxAttempts=3)
    Service(resolve)

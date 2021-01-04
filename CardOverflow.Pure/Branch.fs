module Domain.Branch

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open CardOverflow.Pure

let streamName (id: BranchId) = StreamName.create "Branch" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Snapshotted =
        { Id: BranchId
          LeafId: LeafId
          LeafIds: LeafId list
          Title: string
          StackId: StackId
          AuthorId: UserId
          GrompleafId: GrompleafId
          AnkiNoteId: int64 option
          GotDMCAed: bool
          FieldValues: Map<string, string>
          EditSummary: string
          Tags: string list }
    type Edited =
        { LeafId: LeafId
          Title: string
          GrompleafId: GrompleafId
          FieldValues: Map<string, string>
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
                    LeafId      = b.LeafId
                    LeafIds     = b.LeafId :: a.LeafIds
                    Title       = b.Title
                    GrompleafId = b.GrompleafId
                    FieldValues = b.FieldValues
                    EditSummary = b.EditSummary
                } |> State.Active

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Snapshotted _ -> true | _ -> false

let decideCreate snapshotted = function
    | Fold.State.Initial  -> Ok ()                  , [ Events.Snapshotted snapshotted ]
    | Fold.State.Active _ -> Error "Already created", []

let decideEdit (edited: Events.Edited) callerId = function
    | Fold.State.Initial  -> Error "Can't edit a branch that doesn't exist", []
    | Fold.State.Active x ->
        if x.AuthorId <> callerId then
            Error "You aren't the author"       , []
        elif x.LeafIds |> Seq.contains edited.LeafId then
            Error $"Duplicate leafId:{x.LeafId}", []
        else
            Ok ()                               , [ Events.Edited edited ]

module Domain.Branch

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling

let streamName (id: BranchId) = StreamName.create "Branch" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Snapshot =
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
        | Snapshot of Snapshot
        | Edited   of Edited
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type State =
        | Initial
        | Active of Events.Snapshot
    let initial : State = State.Initial
    
    let evolveEdited (e: Events.Edited) (s: Events.Snapshot) =
        { s with
            LeafId      = e.LeafId
            LeafIds     = e.LeafId :: s.LeafIds
            Title       = e.Title
            GrompleafId = e.GrompleafId
            FieldValues = e.FieldValues
            EditSummary = e.EditSummary
        }
    
    let evolve state =
        function
        | Events.Snapshot s -> State.Active s
        | Events.Edited e ->
            match state with
            | State.Initial -> invalidOp "Can't edit an Initial Branch"
            | State.Active s -> evolveEdited e s |> State.Active

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Snapshot _ -> true | _ -> false

let decideCreate snapshot = function
    | Fold.State.Initial  -> Ok ()                  , [ Events.Snapshot snapshot ]
    | Fold.State.Active _ -> Error "Already created", []

let decideEdit (edited: Events.Edited) callerId state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit a branch that doesn't exist"
    | Fold.State.Active x -> result {
        do! Result.requireEqual x.AuthorId callerId                         "You aren't the author"
        do! x.LeafIds |> Seq.contains edited.LeafId |> Result.requireFalse $"Duplicate leafId:{x.LeafId}"
    } |> addEvent (Events.Edited edited)

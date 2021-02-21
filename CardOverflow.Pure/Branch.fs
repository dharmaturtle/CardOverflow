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

    type Summary =
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
        | Created of Summary
        | Edited  of Edited
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type State =
        | Initial
        | Active of Events.Summary
    let initial : State = State.Initial
    
    let evolveEdited (e: Events.Edited) (s: Events.Summary) =
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
        | Events.Created s -> State.Active s
        | Events.Edited e ->
            match state with
            | State.Initial -> invalidOp "Can't edit an Initial Branch"
            | State.Active s -> evolveEdited e s |> State.Active

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Created _ -> true | _ -> false

let decideCreate summary state =
    match state with
    | Fold.State.Active s -> Error $"Branch '{s.Id}' already exists."
    | Fold.State.Initial  -> Ok ()
    |> addEvent (Events.Created summary)

let decideEdit (edited: Events.Edited) callerId state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit a branch that doesn't exist"
    | Fold.State.Active x -> result {
        do! Result.requireEqual x.AuthorId callerId                         "You aren't the author"
        do! x.LeafIds |> Seq.contains edited.LeafId |> Result.requireFalse $"Duplicate leafId:{x.LeafId}"
    } |> addEvent (Events.Edited edited)

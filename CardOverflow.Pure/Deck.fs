module Domain.Deck

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open NodaTime
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open FSharp.UMX

let streamName (id: DeckId) = StreamName.create "Deck" (string id)

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =
    
    type Summary =
        {   Id: DeckId
            UserId: UserId
            Name: string
            IsPublic: bool
            SourceId: DeckId option }
    let defaultSummary userId deckId =
        {   Id = deckId
            UserId = userId
            Name = "Default Deck"
            IsPublic = false
            SourceId = None }
    type Edited =
        {   Name: string
            SourceId: DeckId option }

    type Event =
        | Edited  of Edited
        | Created of Summary
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =
    
    type State =
        | Initial
        | Active of Events.Summary
    let initial = State.Initial

    let mapActive f = function
        | Active a -> f a |> Active
        | x -> x

    let evolveEdited (e: Events.Edited) (s: Events.Summary) =
        { s with
            Name = e.Name
            SourceId = e.SourceId }

    let evolve state = function
        | Events.Created s -> State.Active s
        | Events.Edited  o -> state |> mapActive (evolveEdited o)
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve

let validateName (name: string) = result {
    do! (1 <= name.Length && name.Length <= 100) |> Result.requireTrue $"The name '{name}' must be between 1 and 100 characters."
    do! Result.requireEqual name (name.Trim()) $"Remove the spaces before and/or after the name: '{name}'."
    }

let validateSourceId doesSourceExist (sourceId: DeckId option) =
    match sourceId with
    | Some sourceId -> doesSourceExist |> Result.requireTrue $"The source deck '{sourceId}' doesn't exist"
    | None -> Ok()

let decideCreate (summary: Events.Summary) doesSourceExist state =
    match state with
    | Fold.State.Active s -> Error $"Deck '{s.Id}' already exists."
    | Fold.State.Initial  -> result {
        do! validateSourceId doesSourceExist summary.SourceId
        do! validateName summary.Name
    } |> addEvent (Events.Created summary)

let decideEdited (e: Events.Edited) callerId doesSourceExist state =
    match state with
    | Fold.State.Initial  -> Error $"You ({callerId}) can't edit a deck that doesn't exist."
    | Fold.State.Active s -> result {
        do! Result.requireEqual callerId s.UserId $"You ({callerId}) didn't author this deck ({s.Id})."
        do! validateSourceId doesSourceExist e.SourceId
        do! validateName e.Name
    } |> addEvent (Events.Edited e)

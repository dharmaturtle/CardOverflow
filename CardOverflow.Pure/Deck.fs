module Domain.Deck

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open NodaTime
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open Domain.Summary

let streamName (id: DeckId) = StreamName.create "Deck" (string id)

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Edited =
        {   Name: string
            Description: string }

    type Event =
        | Edited  of Edited
        | Created of Deck
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =
    
    type State =
        | Initial
        | Active of Deck
    let initial = State.Initial

    let mapActive f = function
        | Active a -> f a |> Active
        | x -> x

    let evolveEdited (e: Events.Edited) (s: Deck) =
        { s with
            Name = e.Name
            Description = e.Description }

    let evolve state = function
        | Events.Created s -> State.Active s
        | Events.Edited  o -> state |> mapActive (evolveEdited o)
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve

let validateName (name: string) = result {
    let! _ = Result.requireNotNull "Name cannot be null." name
    do! (1 <= name.Length && name.Length <= 100) |> Result.requireTrue $"The name '{name}' must be between 1 and 100 characters."
    do! Result.requireEqual name (name.Trim()) $"Remove the spaces before and/or after the name: '{name}'."
    }

let validateDescription (description: string) = result {
    let! _ = Result.requireNotNull "Description cannot be null." description
    do! (0 <= description.Length && description.Length <= 300) |> Result.requireTrue $"The description '{description}' must be between 0 and 300 characters."
    do! Result.requireEqual description (description.Trim()) $"Remove the spaces before and/or after the description: '{description}'."
    }

let validateSummary (summary: Deck) = result {
    do! validateName summary.Name
    do! validateDescription summary.Description
    }

let validateEdit callerId (summary: Deck) (edit: Events.Edited) = result {
    do! Result.requireEqual callerId summary.AuthorId $"You ({callerId}) didn't author this deck ({summary.Id})."
    do! validateName edit.Name
    do! validateDescription summary.Description
    }

let decideCreate (summary: Deck) state =
    match state with
    | Fold.State.Active s -> Error $"Deck '{s.Id}' already exists."
    | Fold.State.Initial  -> validateSummary summary
    |> addEvent (Events.Created summary)

let decideEdited (edit: Events.Edited) callerId state =
    match state with
    | Fold.State.Initial  -> Error $"You ({callerId}) can't edit a deck that doesn't exist."
    | Fold.State.Active s -> validateEdit callerId s edit
    |> addEvent (Events.Edited edit)

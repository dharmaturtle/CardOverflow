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

    type Edited =  // copy fields from this to Created
        { Meta: Meta  
          Name: string
          Description: string }

    type Created =
        { Meta: Meta
          Id: DeckId
          Visibility: Visibility
          
          // from Edited above
          Name: string
          Description: string }
    
    type Event =
        | Edited  of Edited
        | Created of Created
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

    let evolveCreated (created: Events.Created) =
        { Id          = created.Id
          AuthorId    = created.Meta.UserId
          Name        = created.Name
          Description = created.Description
          Visibility  = created.Visibility }

    let evolve state = function
        | Events.Created s -> s |> evolveCreated |> State.Active
        | Events.Edited  o -> state |> mapActive (evolveEdited o)
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit      : Events.Event seq -> State = fold initial

let defaultDeck meta deckId : Events.Created =
    { Meta = meta
      Id = deckId
      Name = "Default Deck"
      Description = ""
      Visibility = Private }

let checkPermissions (meta: Meta) (t: Deck) =
    Result.requireEqual meta.UserId t.AuthorId "You aren't allowed to edit this Deck."

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

let validateCreated (created: Events.Created) = result {
    do! validateName created.Name
    do! validateDescription created.Description
    }

let validateEdit (deck: Deck) (edit: Events.Edited) = result {
    do! checkPermissions edit.Meta deck
    do! validateName edit.Name
    do! validateDescription deck.Description
    }

let decideCreate (created: Events.Created) state =
    match state with
    | Fold.State.Active s -> Error $"Deck '{s.Id}' already exists."
    | Fold.State.Initial  -> validateCreated created
    |> addEvent (Events.Created created)

let decideEdited (edit: Events.Edited) state =
    match state with
    | Fold.State.Initial  -> Error $"You can't edit a deck that doesn't exist."
    | Fold.State.Active s -> validateEdit s edit
    |> addEvent (Events.Edited edit)

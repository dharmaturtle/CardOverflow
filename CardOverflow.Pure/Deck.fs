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
    type Discarded =
        { Meta: Meta }

    module Compaction =
        type State =
            | Active  of Deck
            | Discard of CommandId Set
        type Snapshotted = { State: State }
    
    type Event =
        | Edited      of Edited
        | Created     of Created
        | Discarded   of Discarded
        | // revise this tag if you break the unfold schema
          //[<System.Runtime.Serialization.DataMember(Name="snapshot-v1")>]
          Snapshotted of Compaction.Snapshotted
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =
    type Extant =
        | Active  of Deck
        | Discard of CommandId Set
    
    type State =
        | Initial
        | Extant of Extant
    let initial = State.Initial

    let toSnapshot (s: Extant) : Events.Compaction.Snapshotted =
        match s with
        | Active  x -> { State = Events.Compaction.Active  x }
        | Discard x -> { State = Events.Compaction.Discard x }
    let ofSnapshot ({ State = s }: Events.Compaction.Snapshotted) : Extant =
        match s with
        | Events.Compaction.Active  x -> Active  x
        | Events.Compaction.Discard x -> Discard x

    let mapActive f = function
        | Extant (Active a) ->
          Extant (Active (f a))
        | x -> x

    let evolveEdited (e: Events.Edited) (s: Deck) =
        { s with
            CommandIds  = s.CommandIds |> Set.add e.Meta.CommandId
            Name        = e.Name
            Description = e.Description }

    let evolveCreated (created: Events.Created) =
        { CommandIds  = created.Meta.CommandId |> Set.singleton
          Id          = created.Id
          AuthorId    = created.Meta.UserId
          Name        = created.Name
          Description = created.Description
          Visibility  = created.Visibility }
    
    let evolveDiscarded (discarded: Events.Discarded) = function
        | Extant (Active s) -> s.CommandIds |> Set.add discarded.Meta.CommandId |> Discard |> Extant
        | x -> x

    let evolve state = function
        | Events.Created     s -> s |> evolveCreated |> Active |> State.Extant
        | Events.Edited      o -> state |> mapActive (evolveEdited o)
        | Events.Discarded   e -> state |> evolveDiscarded e
        | Events.Snapshotted s -> s |> ofSnapshot |> State.Extant
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit events =
        match fold initial events with
        | State.Extant x -> x
        | Initial        -> failwith "impossible"
    let isOrigin = function Events.Snapshotted _ -> true | _ -> false

let getActive state =
    match state with
    | Fold.Extant (Fold.Active d) -> Ok d
    | _ -> CCError "Deck doesn't exist."

let defaultDeck meta deckId : Events.Created =
    { Meta = meta
      Id = deckId
      Name = "Default Deck"
      Description = ""
      Visibility = Private }

let checkMeta (meta: Meta) (d: Deck) = result {
    do! Result.requireEqual meta.UserId d.AuthorId (CError "You aren't allowed to edit this Deck.")
    do! idempotencyCheck meta d.CommandIds
    }
    
let validateName (name: string) = result {
    let! _ = Result.requireNotNull (CError "Name cannot be null.") name
    do! (1 <= name.Length && name.Length <= 100) |> Result.requireTrue (CError $"The name '{name}' must be between 1 and 100 characters.")
    do! Result.requireEqual name (name.Trim()) (CError $"Remove the spaces before and/or after the name: '{name}'.")
    }

let validateDescription (description: string) = result {
    let! _ = Result.requireNotNull (CError "Description cannot be null.") description
    do! (0 <= description.Length && description.Length <= 300) |> Result.requireTrue (CError $"The description '{description}' must be between 0 and 300 characters.")
    do! Result.requireEqual description (description.Trim()) (CError $"Remove the spaces before and/or after the description: '{description}'.")
    }

let validateCreated (created: Events.Created) = result {
    do! validateName created.Name
    do! validateDescription created.Description
    }

let validateEdit (deck: Deck) (edit: Events.Edited) = result {
    do! checkMeta edit.Meta deck
    do! validateName edit.Name
    do! validateDescription deck.Description
    }

let validateDiscard (deck: Deck) (discarded: Events.Discarded) = result {
    do! checkMeta discarded.Meta deck
    }

let decideCreate (created: Events.Created) state =
    match state with
    | Fold.Extant s ->
        match s with
        | Fold.Active  s -> idempotencyCheck created.Meta s.CommandIds |> bindCCError $"Deck '{created.Id}' already exists."
        | Fold.Discard s -> idempotencyCheck created.Meta s            |> bindCCError $"Deck '{created.Id}' is discarded."
    | Fold.State.Initial  -> validateCreated created
    |> addEvent (Events.Created created)

let decideEdited (edit: Events.Edited) state =
    match state with
    | Fold.Extant s ->
        match s with
        | Fold.Active  s -> validateEdit s edit
        | Fold.Discard s -> idempotencyCheck edit.Meta s         |> bindCCError $"Deck is discarded."
    | Fold.State.Initial -> idempotencyCheck edit.Meta Set.empty |> bindCCError $"You can't edit a deck that doesn't exist."
    |> addEvent (Events.Edited edit)

let decideDiscarded (discarded: Events.Discarded) state =
    match state with
    | Fold.Extant s ->
        match s with
        | Fold.Active  s -> validateDiscard s discarded
        | Fold.Discard s -> idempotencyCheck discarded.Meta s         |> bindCCError $"Deck is already discarded."
    | Fold.State.Initial -> idempotencyCheck discarded.Meta Set.empty |> bindCCError $"You can't discarded a deck that doesn't exist."
    |> addEvent (Events.Discarded discarded)

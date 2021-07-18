module Domain.Deck

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open NodaTime
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open Domain.Summary

let streamName (id: DeckId) = StreamName.create "Deck" (string id)

type Discard =
    { Id: DeckId
      ServerDiscardedAt: Instant
      CommandIds: CommandId Set }

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
          IsDefault: bool
          SourceId: DeckId Option
          Name: string
          Description: string }
    type IsDefaultChanged =
        { Meta: Meta
          IsDefault: bool}
    type SourceChanged =
        { Meta: Meta
          SourceId: DeckId Option }
    type VisibilityChanged =
        { Meta: Meta
          Visibility: Visibility }
    type Discarded =
        { Meta: Meta }

    module Compaction =
        type State =
            | Initial
            | Active  of Deck
            | Discard of Discard
        type Snapshotted = { State: State }
    
    type Event =
        | Edited            of Edited
        | Created           of Created
        | Discarded         of Discarded
        | IsDefaultChanged  of IsDefaultChanged
        | SourceChanged     of SourceChanged
        | VisibilityChanged of VisibilityChanged
        | // revise this tag if you break the unfold schema
          //[<System.Runtime.Serialization.DataMember(Name="snapshot-v1")>]
          Snapshotted of Compaction.Snapshotted
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =
    
    type State =
        | Initial
        | Active  of Deck
        | Discard of Discard
    let initial = State.Initial

    let toSnapshot (s: State) : Events.Compaction.Snapshotted =
        match s with
        | Initial   -> { State = Events.Compaction.Initial   }
        | Active  x -> { State = Events.Compaction.Active  x }
        | Discard x -> { State = Events.Compaction.Discard x }
    let ofSnapshot ({ State = s }: Events.Compaction.Snapshotted) : State =
        match s with
        | Events.Compaction.Initial   -> Initial
        | Events.Compaction.Active  x -> Active  x
        | Events.Compaction.Discard x -> Discard x

    let mapActive f = function
        | Active a -> a |> f |> Active
        | x -> x

    let guard (old: Deck) (meta: Meta) updated =
        if old.CommandIds.Contains meta.CommandId
        then old
        else { updated with
                   ServerModified = meta.ServerReceivedAt.Value
                   CommandIds = old.CommandIds |> Set.add meta.CommandId }
    
    let evolveVisibilityChanged (e: Events.VisibilityChanged) (s: Deck) =
        guard s e.Meta { s with Visibility = e.Visibility }

    let evolveSourceChanged (e: Events.SourceChanged) (s: Deck) =
        guard s e.Meta { s with SourceId   = e.SourceId }

    let evolveIsDefaultChanged (e: Events.IsDefaultChanged) (s: Deck) =
        guard s e.Meta { s with IsDefault  = e.IsDefault }

    let evolveEdited (e: Events.Edited) (s: Deck) =
        guard s e.Meta
            { s with
                Name           = e.Name
                Description    = e.Description }

    let evolveCreated (created: Events.Created) =
        { CommandIds     = created.Meta.CommandId |> Set.singleton
          Id             = created.Id
          IsDefault      = created.IsDefault
          SourceId       = created.SourceId
          AuthorId       = created.Meta.UserId
          Name           = created.Name
          Description    = created.Description
          ServerCreated  = created.Meta.ServerReceivedAt.Value
          ServerModified = created.Meta.ServerReceivedAt.Value
          Visibility     = created.Visibility
          Extra          = "" }
    
    let evolveDiscarded (discarded: Events.Discarded) = function
        | Active s ->
            { Id = s.Id
              ServerDiscardedAt = discarded.Meta.ServerReceivedAt.Value
              CommandIds = s.CommandIds |> Set.add discarded.Meta.CommandId }
            |> Discard
        | x -> x

    let evolve state = function
        | Events.Created           s -> s |> evolveCreated |> Active
        | Events.Edited            o -> state |> mapActive (evolveEdited o)
        | Events.VisibilityChanged o -> state |> mapActive (evolveVisibilityChanged o)
        | Events.SourceChanged     o -> state |> mapActive (evolveSourceChanged o)
        | Events.IsDefaultChanged  o -> state |> mapActive (evolveIsDefaultChanged o)
        | Events.Discarded         e -> state |> evolveDiscarded e
        | Events.Snapshotted       s -> s |> ofSnapshot
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit :      Events.Event seq -> State = Seq.fold evolve initial
    let isOrigin = function Events.Snapshotted _ -> true | _ -> false

    let snapshot (state: State) : Events.Event =
        state |> toSnapshot |> Events.Snapshotted

let getActive state =
    match state with
    | Fold.Active d -> Ok d
    | _ -> Error "Deck doesn't exist."
let getActive' = getActive >> Result.mapError CError

let defaultDeck meta deckId : Events.Created =
    { Meta = meta
      Id = deckId
      Name = "Default Deck"
      IsDefault = true
      SourceId = None
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

let validateVisibilityChange (deck: Deck) (visibilityChanged: Events.VisibilityChanged) = result {
    do! checkMeta visibilityChanged.Meta deck
    }

let validateIsDefaultChange (deck: Deck) (isDefaultChanged: Events.IsDefaultChanged) = result {
    do! checkMeta isDefaultChanged.Meta deck
    }

let validateSourceChange (deck: Deck) (sourceChanged: Events.SourceChanged) (source: Fold.State Option) = result {
    do! checkMeta sourceChanged.Meta deck
    let sourceIsVisible =
        match  source, sourceChanged.SourceId with
        |           _, None -> true
        |        None, Some sourceId -> failwith $"This should be an illegal state. Someone yell at the programmer... sourceId is: '{sourceId}' but there is no source?! Is something wrong with the EventAppender?"
        | Some source, _ ->
            match getActive source with
            | Ok source ->
                match source.Visibility with
                | Public -> true
                | Private -> source.AuthorId = sourceChanged.Meta.UserId
            | Error _ -> false
    do! sourceIsVisible
        |> Result.requireTrue (CError $"Deck {sourceChanged.SourceId} either doesn't exist or isn't visible to you.")
    }

let validateDiscard (deck: Deck) (discarded: Events.Discarded) = result {
    do! checkMeta discarded.Meta deck
    }

let decideCreate (created: Events.Created) state =
    match state with
    | Fold.Active  s -> idempotencyCheck created.Meta s.CommandIds |> bindCCError $"Deck '{created.Id}' already exists."
    | Fold.Discard s -> idempotencyCheck created.Meta s.CommandIds |> bindCCError $"Deck '{created.Id}' is discarded."
    | Fold.Initial   -> validateCreated created
    |> addEvent (Events.Created created)

let decideEdited (edit: Events.Edited) state =
    match state with
    | Fold.Active  s -> validateEdit s edit
    | Fold.Discard s -> idempotencyCheck edit.Meta s.CommandIds |> bindCCError $"Deck is discarded."
    | Fold.Initial   -> idempotencyBypass                       |> bindCCError $"You can't edit a deck that doesn't exist."
    |> addEvent (Events.Edited edit)

let decideVisibilityChanged (visibilityChanged: Events.VisibilityChanged) state =
    match state with
    | Fold.Active  s -> validateVisibilityChange s visibilityChanged
    | Fold.Discard s -> idempotencyCheck visibilityChanged.Meta s.CommandIds |> bindCCError $"Deck is discarded."
    | Fold.Initial   -> idempotencyBypass                                    |> bindCCError $"You can't change the visibility of a deck that doesn't exist."
    |> addEvent (Events.VisibilityChanged visibilityChanged)

let decideSourceChanged (sourceChanged: Events.SourceChanged) sourceState state =
    match state with
    | Fold.Active  s -> validateSourceChange s sourceChanged sourceState
    | Fold.Discard s -> idempotencyCheck sourceChanged.Meta s.CommandIds |> bindCCError $"Deck is discarded."
    | Fold.Initial   -> idempotencyBypass                                |> bindCCError $"You can't change the source of a deck that doesn't exist."
    |> addEvent (Events.SourceChanged sourceChanged)

let decideIsDefaultChanged (isDefaultChanged: Events.IsDefaultChanged) state =
    match state with
    | Fold.Active  s -> validateIsDefaultChange s isDefaultChanged
    | Fold.Discard s -> idempotencyCheck isDefaultChanged.Meta s.CommandIds |> bindCCError $"Deck is discarded."
    | Fold.Initial   -> idempotencyBypass                                   |> bindCCError $"You can't change the default status of a deck that doesn't exist."
    |> addEvent (Events.IsDefaultChanged isDefaultChanged)

let decideDiscarded (discarded: Events.Discarded) state =
    match state with
    | Fold.Active  s -> validateDiscard s discarded
    | Fold.Discard s -> idempotencyCheck discarded.Meta s.CommandIds |> bindCCError $"Deck is already discarded."
    | Fold.Initial   -> idempotencyBypass                            |> bindCCError $"You can't discarded a deck that doesn't exist."
    |> addEvent (Events.Discarded discarded)

module Domain.Stack

open FsCodec
open FsCodec.NewtonsoftJson
open NodaTime
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open Domain.Summary

let streamName (id: StackId) = StreamName.create "Stack" (string id)

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Created =
        { Meta: Meta
          Id: StackId
          ExampleRevisionId: ExampleRevisionId
          FrontPersonalField: string
          BackPersonalField: string
          Tags: string Set
          Cards: Card list }

    type TagsChanged =
        { Meta: Meta; Tags: string Set }
    type RevisionChanged =
        { Meta: Meta; RevisionId: ExampleRevisionId }
    type CardStateChanged =
        { Meta: Meta
          State: CardState
          Pointer: CardTemplatePointer }
    type Discarded =
        { Meta: Meta }

    module Compaction =
        type State =
            | Active  of Stack
            | Discard of CommandId Set
        type Snapshotted = { State: State }
    
    type Event =
        | Created          of Created
        | Discarded        of Discarded
        | TagsChanged      of TagsChanged
        | RevisionChanged  of RevisionChanged
        | CardStateChanged of CardStateChanged
        | // revise this tag if you break the unfold schema
          //[<System.Runtime.Serialization.DataMember(Name="snapshot-v1")>]
          Snapshotted      of Compaction.Snapshotted
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =
    
    type Extant =
        | Active  of Stack
        | Discard of CommandId Set

    type State =
        | Initial
        | Extant of Extant
    let initial : State = State.Initial

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
    
    let mapCard pointer f (card: Card) =
        if card.Pointer = pointer
        then f card
        else card

    let mapCards pointer f =
        List.map (mapCard pointer f)
        
    let evolveTagsChanged
        (e: Events.TagsChanged)
        (s: Stack) =
        { s with
            CommandIds = s.CommandIds |> Set.add e.Meta.CommandId
            Tags = e.Tags }
        
    let evolveRevisionChanged
        (e: Events.RevisionChanged)
        (s: Stack) =
        { s with
            CommandIds = s.CommandIds |> Set.add e.Meta.CommandId
            ExampleRevisionId = e.RevisionId }
        
    let evolveCardStateChanged
        (e: Events.CardStateChanged)
        (s: Stack) =
        { s with
            CommandIds = s.CommandIds |> Set.add e.Meta.CommandId
            Cards = s.Cards |> mapCards e.Pointer (fun c -> { c with State = e.State }) }

    let evolveCreated (created: Events.Created) =
        { Id                  = created.Id
          CommandIds          = created.Meta.CommandId |> Set.singleton
          AuthorId            = created.Meta.UserId
          ExampleRevisionId   = created.ExampleRevisionId
          FrontPersonalField  = created.FrontPersonalField
          BackPersonalField   = created.BackPersonalField
          Tags                = created.Tags
          Cards               = created.Cards }

    let evolveDiscarded (discarded: Events.Discarded) = function
        | Extant (Active s) -> s.CommandIds |> Set.add discarded.Meta.CommandId |> Discard |> Extant
        | x -> x
    
    let evolve state = function
        | Events.Created          s -> s |> evolveCreated |> Active |> State.Extant
        | Events.Discarded        e -> state |> evolveDiscarded e
        | Events.TagsChanged      e -> state |> mapActive (evolveTagsChanged e)
        | Events.RevisionChanged  e -> state |> mapActive (evolveRevisionChanged e)
        | Events.CardStateChanged e -> state |> mapActive (evolveCardStateChanged e)
        | Events.Snapshotted s -> s |> ofSnapshot |> State.Extant

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit events =
        match fold initial events with
        | State.Extant x -> x
        | Initial        -> failwith "impossible"
    let isOrigin = function Events.Snapshotted _ -> true | _ -> false

    let snapshot (state: State) : Events.Event =
        match state with
        | Extant x -> x |> toSnapshot |> Events.Snapshotted
        | Initial -> failwith "impossible"

let getActive state =
    match state with
    | Fold.Extant (Fold.Active s) -> Ok s
    | _ -> Error "Stack doesn't exist."

let initCard now cardSettingId newCardsStartingEaseFactor deckId pointer : Card =
    { Pointer = pointer
      CardSettingId = cardSettingId
      DeckId = deckId
      EaseFactor = newCardsStartingEaseFactor
      IntervalOrStepsIndex = IntervalOrStepsIndex.NewStepsIndex 0uy
      Due = now
      IsLapsed = false
      History = []
      State = CardState.Normal }

let init id meta exampleRevisionId cardSettingId newCardsStartingEaseFactor deckId pointers now : Events.Created =
    { Meta = meta
      Id = id
      ExampleRevisionId = exampleRevisionId
      FrontPersonalField = ""
      BackPersonalField = ""
      Tags = Set.empty
      Cards = pointers |> List.map (initCard now cardSettingId newCardsStartingEaseFactor deckId) }

let validateTag (tag: string) = result {
    do! Result.requireEqual tag (tag.Trim()) (CError $"Remove the spaces before and/or after the tag: '{tag}'.")
    do! (1 <= tag.Length && tag.Length <= 100) |> Result.requireTrue (CError $"Tags must be between 1 and 100 characters, but '{tag}' has {tag.Length} characters.")
    }

let validateTags (tags: string Set) = result {
    for tag in tags do
        do! validateTag tag
    }

let checkMeta (meta: Meta) (t: Stack) = result {
    do! Result.requireEqual meta.UserId t.AuthorId (CError "You aren't allowed to edit this Stack.")
    do! idempotencyCheck meta t.CommandIds
    }

let validateCardTemplatePointers (currentCards: Card list) revision templateRevision = result {
    let! newPointers = Template.getCardTemplatePointers templateRevision revision.FieldValues |> Result.map Set.ofList |> Result.mapError CError
    let currentPointers = currentCards |> List.map (fun x -> x.Pointer) |> Set.ofList
    let removed = Set.difference currentPointers newPointers |> Set.toList
    do! Result.requireEmpty (CError $"Some card(s) were removed: {removed}. This is currently unsupported - remove them manually.") removed // medTODO support this, and also "renaming"
    }

let validateRevisionChanged (revisionChanged: Events.RevisionChanged) example template current = result {
    do! checkMeta revisionChanged.Meta current
    let!  exampleRevision = example  |> Example .getRevision revisionChanged.RevisionId |> Result.mapError CError
    let! templateRevision = template |> Template.getRevision exampleRevision.TemplateRevisionId
    do! validateCardTemplatePointers current.Cards exampleRevision templateRevision
    }

let validateCreated (created: Events.Created) (template: Template.Fold.State) (example: Example.Fold.State) = result {
    let!  exampleRevision = example  |> Example .getRevision created.ExampleRevisionId |> Result.mapError CError
    let! templateRevision = template |> Template.getRevision exampleRevision.TemplateRevisionId
    do! validateCardTemplatePointers created.Cards exampleRevision templateRevision
    do! validateTags created.Tags
    }

let validateTagsChanged (tagsChanged: Events.TagsChanged) (s: Stack) = result {
    do! checkMeta tagsChanged.Meta s
    do! validateTags tagsChanged.Tags
    }

let validateCardStateChanged (cardStateChanged: Events.CardStateChanged) (s: Stack) = result {
    do! checkMeta cardStateChanged.Meta s
    }

let decideChangeCardState (cardStateChanged: Events.CardStateChanged) state =
    match state with
    | Fold.Extant s ->
        match s with
        | Fold.Active  s -> validateCardStateChanged cardStateChanged s
        | Fold.Discard s -> idempotencyCheck cardStateChanged.Meta s         |> bindCCError $"This stack is currently discarded, so you can't change any of its cards' state"
    | Fold.State.Initial -> idempotencyCheck cardStateChanged.Meta Set.empty |> bindCCError $"Can't change the state of a stack which doesn't exist"
    |> addEvent (Events.CardStateChanged cardStateChanged)

let validateDiscarded (discarded: Events.Discarded) (s: Stack) = result {
    do! checkMeta discarded.Meta s
    }

let decideCreate (created: Events.Created) templateRevision revision state =
    match state with
    | Fold.Extant s ->
        match s with
        | Fold.Active  s -> idempotencyCheck created.Meta s.CommandIds |> bindCCError $"Stack '{created.Id}' already exists."
        | Fold.Discard s -> idempotencyCheck created.Meta s            |> bindCCError $"Stack '{created.Id}' already exists (though it's discarded.)"
    | Fold.State.Initial -> validateCreated created templateRevision revision
    |> addEvent (Events.Created created)

let decideDiscard (id: StackId) (discarded: Events.Discarded) state =
    match state with
    | Fold.Extant s ->
        match s with
        | Fold.Active  s -> validateDiscarded discarded s
        | Fold.Discard s -> idempotencyCheck discarded.Meta s         |> bindCCError $"Stack '{id}' is already discarded"
    | Fold.State.Initial -> idempotencyCheck discarded.Meta Set.empty |> bindCCError $"Stack '{id}' doesn't exist, so it can't be discarded"
    |> addEvent (Events.Discarded discarded)

let decideChangeTags (tagsChanged: Events.TagsChanged) state =
    match state with
    | Fold.Extant s ->
        match s with
        | Fold.Active  s -> validateTagsChanged tagsChanged s
        | Fold.Discard s -> idempotencyCheck tagsChanged.Meta s         |> bindCCError $"Stack is discarded."
    | Fold.State.Initial -> idempotencyCheck tagsChanged.Meta Set.empty |> bindCCError "Can't change the tags of a Stack that doesn't exist."
    |> addEvent (Events.TagsChanged tagsChanged)

let decideChangeRevision (revisionChanged: Events.RevisionChanged) templateRevision revision state =
    match state with
    | Fold.Extant s ->
        match s with
        | Fold.Active current -> validateRevisionChanged revisionChanged revision templateRevision current
        | Fold.Discard      s -> idempotencyCheck revisionChanged.Meta s         |> bindCCError $"Stack is discarded, so you can't change its revision."
    | Fold.State.Initial      -> idempotencyCheck revisionChanged.Meta Set.empty |> bindCCError "Can't change the revision of a Stack that doesn't exist."
    |> addEvent (Events.RevisionChanged revisionChanged)

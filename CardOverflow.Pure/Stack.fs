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
          DeckIds: DeckId Set
          Tags: string Set
          Cards: Card list }

    type CardEdited =
        { Pointer: CardTemplatePointer
          CardSettingId: CardSettingId
          State: CardState }
    type Edited =
        { Meta: Meta
          ExampleRevisionId: ExampleRevisionId
          FrontPersonalField: string
          BackPersonalField: string
          DeckIds: DeckId Set
          Tags: string Set
          CardEdits: CardEdited list }

    type TagsChanged =
        { Meta: Meta; Tags: string Set }
    type RevisionChanged =
        { Meta: Meta; RevisionId: ExampleRevisionId }
    type CardStateChanged =
        { Meta: Meta
          State: CardState
          Pointer: CardTemplatePointer }
    type DecksChanged =
        { Meta: Meta
          DeckIds: DeckId Set }
    type CardSettingChanged =
        { Meta: Meta
          CardSettingId: CardSettingId
          Pointer: CardTemplatePointer }
    type Reviewed =
        { Meta: Meta
          Review: Review
          Pointer: CardTemplatePointer }
    type Discarded =
        { Meta: Meta }

    module Compaction =
        type State =
            | Initial
            | Active  of Stack
            | Discard of CommandId Set
        type Snapshotted = { State: State }
    
    type Event =
        | Created            of Created
        | Edited             of Edited
        | Discarded          of Discarded
        | TagsChanged        of TagsChanged
        | RevisionChanged    of RevisionChanged
        | CardStateChanged   of CardStateChanged
        | DecksChanged       of DecksChanged
        | CardSettingChanged of CardSettingChanged
        | Reviewed           of Reviewed
        | // revise this tag if you break the unfold schema
          //[<System.Runtime.Serialization.DataMember(Name="snapshot-v1")>]
          Snapshotted      of Compaction.Snapshotted
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type State =
        | Initial
        | Active  of Stack
        | Discard of CommandId Set
    let initial : State = State.Initial

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
        
    let evolveEdited
        (e: Events.Edited)
        (s: Stack) =
        let evolveCardEdited
            ((c: Card), (e: Events.CardEdited)) =
            { c with
                CardSettingId = e.CardSettingId
                State = e.State }
        { s with
            CommandIds = s.CommandIds |> Set.add e.Meta.CommandId
            ExampleRevisionId = e.ExampleRevisionId
            FrontPersonalField = e.FrontPersonalField
            BackPersonalField = e.BackPersonalField
            Tags = e.Tags
            DeckIds = e.DeckIds
            Cards =
                List.zipOn s.Cards e.CardEdits (fun c e -> c.Pointer = e.Pointer)
                |> List.choose
                    (function
                    | Some c, Some e -> Some (c,e)
                    | _ -> None)
                |> List.map evolveCardEdited
        }
        
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

    let evolveCardSettingChanged
        (e: Events.CardSettingChanged)
        (s: Stack) =
        { s with
            CommandIds = s.CommandIds |> Set.add e.Meta.CommandId
            Cards = s.Cards |> mapCards e.Pointer (fun c -> { c with CardSettingId = e.CardSettingId }) }
        
    let evolveReviewed
        (e: Events.Reviewed)
        (s: Stack) =
        { s with
            CommandIds = s.CommandIds |> Set.add e.Meta.CommandId
            Cards = s.Cards |> mapCards e.Pointer (fun c -> { c with Reviews = e.Review :: c.Reviews }) }
        
    let evolveDecksChanged
        (e: Events.DecksChanged)
        (s: Stack) =
        { s with
            CommandIds = s.CommandIds |> Set.add e.Meta.CommandId
            DeckIds = e.DeckIds }

    let evolveCreated (created: Events.Created) =
        { Id                  = created.Id
          CommandIds          = created.Meta.CommandId |> Set.singleton
          AuthorId            = created.Meta.UserId
          ExampleRevisionId   = created.ExampleRevisionId
          FrontPersonalField  = created.FrontPersonalField
          BackPersonalField   = created.BackPersonalField
          DeckIds             = created.DeckIds
          Tags                = created.Tags
          Cards               = created.Cards }

    let evolveDiscarded (discarded: Events.Discarded) = function
        | Active s -> s.CommandIds |> Set.add discarded.Meta.CommandId |> Discard
        | x -> x
    
    let evolve state = function
        | Events.Created            s -> s |> evolveCreated |> Active
        | Events.Discarded          e -> state |> evolveDiscarded e
        | Events.Edited             e -> state |> mapActive (evolveEdited e)
        | Events.TagsChanged        e -> state |> mapActive (evolveTagsChanged e)
        | Events.RevisionChanged    e -> state |> mapActive (evolveRevisionChanged e)
        | Events.CardStateChanged   e -> state |> mapActive (evolveCardStateChanged e)
        | Events.DecksChanged       e -> state |> mapActive (evolveDecksChanged e)
        | Events.CardSettingChanged e -> state |> mapActive (evolveCardSettingChanged e)
        | Events.Reviewed           e -> state |> mapActive (evolveReviewed e)
        | Events.Snapshotted s -> s |> ofSnapshot

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit :      Events.Event seq -> State = Seq.fold evolve initial
    let isOrigin = function Events.Snapshotted _ -> true | _ -> false

    let snapshot (state: State) : Events.Event =
        state |> toSnapshot |> Events.Snapshotted

let getActive state =
    match state with
    | Fold.Active s -> Ok s
    | _ -> Error "Stack doesn't exist."

let initCard due cardSettingId newCardsStartingEaseFactor pointer : Card =
    { Pointer = pointer
      CardSettingId = cardSettingId
      EaseFactor = newCardsStartingEaseFactor
      IntervalOrStepsIndex = IntervalOrStepsIndex.NewStepsIndex 0uy
      Due = due
      IsLapsed = false
      Reviews = []
      State = CardState.Normal }

let init id meta exampleId deckIds cards : Events.Created =
    { Meta = meta
      Id = id
      ExampleRevisionId = exampleId, Example.Fold.initialExampleRevisionOrdinal
      FrontPersonalField = ""
      BackPersonalField = ""
      DeckIds = deckIds
      Tags = Set.empty
      Cards = cards }

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

let validateEdited (edited: Events.Edited) example template (current: Stack) = result {
    do! checkMeta edited.Meta current
    let!  exampleRevision = example  |> Example .getRevision edited.ExampleRevisionId |> Result.mapError CError
    let! templateRevision = template |> Template.getRevision exampleRevision.TemplateRevisionId
    do! validateCardTemplatePointers current.Cards exampleRevision templateRevision
    do! validateTags edited.Tags
    }

let validateDiscarded (discarded: Events.Discarded) (s: Stack) = result {
    do! checkMeta discarded.Meta s
    }

let validateCardStateChanged (cardStateChanged: Events.CardStateChanged) (s: Stack) = result {
    do! checkMeta cardStateChanged.Meta s
    }

let validateDecksChanged (decksChanged: Events.DecksChanged) (decks: Deck.Fold.State []) (s: Stack) = result {
    do! checkMeta decksChanged.Meta s
    let! decks =
        decks
        |> Array.map Deck.getActive
        |> Array.toSeq
        |> Result.consolidate
        |> Result.map List.ofSeq
        |> Result.mapError (
            Seq.map (function | Custom s -> s | Idempotent -> "Idempotent error")
            >> String.concat "\r\n"
            >> CError)
    for deckId in decksChanged.DeckIds do
        let deck = decks |> List.find (fun x -> x.Id = deckId)
        do! Result.requireEqual deck.AuthorId decksChanged.Meta.UserId (CError $"Deck {deckId} doesn't belong to you.")
    }

open FSharp.UMX
let validateCardSettingChanged (cardSettingChanged: Events.CardSettingChanged) (user: User.Fold.State) (s: Stack) = result {
    do! checkMeta cardSettingChanged.Meta s
    let! user = User.getActive user |> Result.mapError CError
    do! user.CardSettings
        |> List.map (fun x -> % x.Id)
        |> List.contains cardSettingChanged.CardSettingId
        |> Result.requireTrue (CError $"You don't have the card setting '{cardSettingChanged.CardSettingId}'")
    }

let validateReviewed (reviewed: Events.Reviewed) (s: Stack) = result {
    do! checkMeta reviewed.Meta s
    }

let decideChangeCardState (cardStateChanged: Events.CardStateChanged) state =
    match state with
    | Fold.Active       s -> validateCardStateChanged cardStateChanged s
    | Fold.Discard      s -> idempotencyCheck cardStateChanged.Meta s |> bindCCError $"This stack is currently discarded, so you can't change any of its cards' state"
    | Fold.State.Initial  -> idempotencyBypass                        |> bindCCError $"Can't change the state of a stack which doesn't exist"
    |> addEvent (Events.CardStateChanged cardStateChanged)

let decideChangeDecks (decksChanged: Events.DecksChanged) decks state =
    match state with
    | Fold.Active       s -> validateDecksChanged decksChanged decks s
    | Fold.Discard      s -> idempotencyCheck decksChanged.Meta s |> bindCCError $"This stack is currently discarded, so you can't change any of its cards' decks"
    | Fold.State.Initial  -> idempotencyBypass                    |> bindCCError $"Can't change the deck of a stack which doesn't exist"
    |> addEvent (Events.DecksChanged decksChanged)

let decideChangeCardSetting (cardSettingChanged: Events.CardSettingChanged) user state =
    match state with
    | Fold.Active       s -> validateCardSettingChanged cardSettingChanged user s
    | Fold.Discard      s -> idempotencyCheck cardSettingChanged.Meta s |> bindCCError $"This stack is currently discarded, so you can't change any card settings."
    | Fold.State.Initial  -> idempotencyBypass                          |> bindCCError $"Can't change the card setting of a stack which doesn't exist."
    |> addEvent (Events.CardSettingChanged cardSettingChanged)

let decideReview (reviewed: Events.Reviewed) state =
    match state with
    | Fold.Active       s -> validateReviewed reviewed s
    | Fold.Discard      s -> idempotencyCheck reviewed.Meta s |> bindCCError $"This stack is currently discarded, so you can't review it"
    | Fold.State.Initial  -> idempotencyBypass                |> bindCCError $"Can't review a stack which doesn't exist"
    |> addEvent (Events.Reviewed reviewed)

let decideCreate (created: Events.Created) templateRevision revision state =
    match state with
    | Fold.Active       s -> idempotencyCheck created.Meta s.CommandIds |> bindCCError $"Stack '{created.Id}' already exists."
    | Fold.Discard      s -> idempotencyCheck created.Meta s            |> bindCCError $"Stack '{created.Id}' already exists (though it's discarded.)"
    | Fold.State.Initial  -> validateCreated created templateRevision revision
    |> addEvent (Events.Created created)

let decideDiscard (id: StackId) (discarded: Events.Discarded) state =
    match state with
    | Fold.Active       s -> validateDiscarded discarded s
    | Fold.Discard      s -> idempotencyCheck discarded.Meta s |> bindCCError $"Stack '{id}' is already discarded"
    | Fold.State.Initial  -> idempotencyBypass                 |> bindCCError $"Stack '{id}' doesn't exist, so it can't be discarded"
    |> addEvent (Events.Discarded discarded)

let decideChangeTags (tagsChanged: Events.TagsChanged) state =
    match state with
    | Fold.Active       s -> validateTagsChanged tagsChanged s
    | Fold.Discard      s -> idempotencyCheck tagsChanged.Meta s |> bindCCError $"Stack is discarded."
    | Fold.State.Initial  -> idempotencyBypass                   |> bindCCError "Can't change the tags of a Stack that doesn't exist."
    |> addEvent (Events.TagsChanged tagsChanged)

let decideEdited (edited: Events.Edited) example template state =
    match state with
    | Fold.Active       s -> validateEdited edited example template s
    | Fold.Discard      s -> idempotencyCheck edited.Meta s |> bindCCError $"Stack is discarded."
    | Fold.State.Initial  -> idempotencyBypass              |> bindCCError "Can't edit a Stack that doesn't exist."
    |> addEvent (Events.Edited edited)

let decideChangeRevision (revisionChanged: Events.RevisionChanged) example template state =
    match state with
    | Fold.Active current -> validateRevisionChanged revisionChanged example template current
    | Fold.Discard      s -> idempotencyCheck revisionChanged.Meta s |> bindCCError $"Stack is discarded, so you can't change its revision."
    | Fold.State.Initial  -> idempotencyBypass                       |> bindCCError "Can't change the revision of a Stack that doesn't exist."
    |> addEvent (Events.RevisionChanged revisionChanged)

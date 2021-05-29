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

    type Event =
        | Created          of Created
        | Discarded        of Discarded
        | TagsChanged      of TagsChanged
        | RevisionChanged  of RevisionChanged
        | CardStateChanged of CardStateChanged
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type State =
        | Initial
        | Active of Stack
        | Discard
    let initial : State = State.Initial
    
    let mapActive f = function
        | Active a -> f a |> Active
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
        { s with Tags = e.Tags }
        
    let evolveRevisionChanged
        (e: Events.RevisionChanged)
        (s: Stack) =
        { s with ExampleRevisionId = e.RevisionId }
        
    let evolveCardStateChanged
        (e: Events.CardStateChanged)
        (s: Stack) =
        { s with Cards = s.Cards |> mapCards e.Pointer (fun c -> { c with State = e.State }) }

    let evolveCreated (created: Events.Created) =
        { Id                  = created.Id
          AuthorId            = created.Meta.UserId
          ExampleRevisionId   = created.ExampleRevisionId
          FrontPersonalField  = created.FrontPersonalField
          BackPersonalField   = created.BackPersonalField
          Tags                = created.Tags
          Cards               = created.Cards }
    
    let evolve state = function
        | Events.Created          s -> s |> evolveCreated |> State.Active
        | Events.Discarded        _ -> State.Discard
        | Events.TagsChanged      e -> state |> mapActive (evolveTagsChanged e)
        | Events.RevisionChanged  e -> state |> mapActive (evolveRevisionChanged e)
        | Events.CardStateChanged e -> state |> mapActive (evolveCardStateChanged e)

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit      : Events.Event seq -> State = fold initial
    let isOrigin = function Events.Created _ -> true | _ -> false

let getActive state =
    match state with
    | Fold.State.Active s -> Ok s
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
    do! Result.requireEqual tag (tag.Trim()) $"Remove the spaces before and/or after the tag: '{tag}'."
    do! (1 <= tag.Length && tag.Length <= 100) |> Result.requireTrue $"Tags must be between 1 and 100 characters, but '{tag}' has {tag.Length} characters."
    }

let validateTags (tags: string Set) = result {
    for tag in tags do
        do! validateTag tag
    }

let checkPermissions (meta: Meta) (t: Stack) =
    Result.requireEqual meta.UserId t.AuthorId "You aren't allowed to edit this Stack."

let validateCardTemplatePointers (currentCards: Card list) revision templateRevision = result {
    let! newPointers = Template.getCardTemplatePointers templateRevision revision.FieldValues |> Result.map Set.ofList
    let currentPointers = currentCards |> List.map (fun x -> x.Pointer) |> Set.ofList
    let removed = Set.difference currentPointers newPointers |> Set.toList
    do! Result.requireEmpty $"Some card(s) were removed: {removed}. This is currently unsupported - remove them manually." removed // medTODO support this, and also "renaming"
    }

let validateRevisionChanged (revisionChanged: Events.RevisionChanged) example template current = result {
    do! checkPermissions revisionChanged.Meta current
    let!  exampleRevision = example  |> Example .getRevision revisionChanged.RevisionId
    let! templateRevision = template |> Template.getRevision exampleRevision.TemplateRevisionId
    do! validateCardTemplatePointers current.Cards exampleRevision templateRevision
    }

let validateCreated (created: Events.Created) (template: Template.Fold.State) (example: Example.Fold.State) = result {
    let! exampleRevision = example |> Example.getRevision created.ExampleRevisionId
    let! templateRevision = template |> Template.getRevision exampleRevision.TemplateRevisionId
    do! validateCardTemplatePointers created.Cards exampleRevision templateRevision
    do! validateTags created.Tags
    }

let validateTagsChanged (tagsChanged: Events.TagsChanged) (s: Stack) = result {
    do! checkPermissions tagsChanged.Meta s
    do! validateTags tagsChanged.Tags
    }

let validateCardStateChanged (cardStateChanged: Events.CardStateChanged) (s: Stack) = result {
    do! checkPermissions cardStateChanged.Meta s
    }

let decideChangeCardState (cardStateChanged: Events.CardStateChanged) state =
    match state with
    | Fold.State.Initial  -> Error $"Can't change the state of a stack which doesn't exist"
    | Fold.State.Discard  -> Error $"This stack is currently discarded, so you can't change any of its cards' state"
    | Fold.State.Active s -> validateCardStateChanged cardStateChanged s
    |> addEvent (Events.CardStateChanged cardStateChanged)

let validateDiscarded (discarded: Events.Discarded) (s: Stack) = result {
    do! checkPermissions discarded.Meta s
    }

let decideDiscarded (discarded: Events.Discarded) state =
    match state with
    | Fold.State.Initial  -> Error $"Can't discard a stack which doesn't exist"
    | Fold.State.Discard  -> Error $"This stack is already discarded"
    | Fold.State.Active s -> validateDiscarded discarded s
    |> addEvent (Events.Discarded discarded)

let decideCreate (created: Events.Created) templateRevision revision state =
    match state with
    | Fold.State.Active _ -> Error $"Stack '{created.Id}' already exists."
    | Fold.State.Discard  -> Error $"Stack '{created.Id}' already exists (though it's discarded.)"
    | Fold.State.Initial  -> validateCreated created templateRevision revision
    |> addEvent (Events.Created created)

let decideDiscard (id: StackId) discarded state =
    match state with
    | Fold.State.Discard  -> Error $"Stack '{id}' is already discarded"
    | Fold.State.Initial  -> Error $"Stack '{id}' doesn't exist, so it can't be discarded"
    | Fold.State.Active _ -> Ok ()
    |> addEvent (Events.Discarded discarded)

let decideChangeTags tagsChanged state =
    match state with
    | Fold.State.Initial -> Error "Can't change the tags of a Stack that doesn't exist."
    | Fold.State.Discard -> Error $"Stack is discarded."
    | Fold.State.Active s -> validateTagsChanged tagsChanged s
    |> addEvent (Events.TagsChanged tagsChanged)

let decideChangeRevision (revisionChanged: Events.RevisionChanged) templateRevision revision state =
    match state with
    | Fold.State.Initial -> Error "Can't change the revision of a Stack that doesn't exist."
    | Fold.State.Discard -> Error $"Stack is discarded, so you can't change its revision."
    | Fold.State.Active current -> validateRevisionChanged revisionChanged revision templateRevision current
    |> addEvent (Events.RevisionChanged revisionChanged)

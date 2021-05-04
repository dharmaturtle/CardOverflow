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
    let isOrigin = function Events.Created _ -> true | _ -> false

let initCard now cardSettingId newCardsStartingEaseFactor deckId pointer : Card =
    { Pointer = pointer
      Created = now
      Modified = now
      CardSettingId = cardSettingId
      DeckId = deckId
      Details =
        {  EaseFactor = newCardsStartingEaseFactor
           IntervalOrStepsIndex = IntervalOrStepsIndex.NewStepsIndex 0uy
           Due = now
           IsLapsed = false
           History = [] }
        |> ShadowableDetails
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

let validateCardTemplatePointers (currentCards: Card list) (revision: Example.RevisionSummary) = result {
    let! newPointers = Template.getCardTemplatePointers revision.TemplateRevision revision.FieldValues |> Result.map Set.ofList
    let currentPointers = currentCards |> List.map (fun x -> x.Pointer) |> Set.ofList
    let removed = Set.difference currentPointers newPointers |> Set.toList
    do! Result.requireEmpty $"Some card(s) were removed: {removed}. This is currently unsupported - remove them manually." removed // medTODO support this, and also "renaming"
    }

let validateRevisionChanged (current: Stack) (revisionChanged: Events.RevisionChanged) (revision: Example.RevisionSummary) = result {
    let callerId = revisionChanged.Meta.UserId
    do! Result.requireEqual current.AuthorId callerId $"You ({callerId}) aren't the author"
    do! validateCardTemplatePointers current.Cards revision
    }

let validateCreated (created: Events.Created) revision = result {
    do! validateCardTemplatePointers created.Cards revision
    do! validateTags created.Tags
    }

let validateTagsChanged (summary: Stack) (tagsChanged: Events.TagsChanged) = result {
    let callerId = tagsChanged.Meta.UserId
    do! Result.requireEqual summary.AuthorId callerId $"You ({callerId}) aren't the author"
    do! validateTags tagsChanged.Tags
    }

let decideCreate (created: Events.Created) revision state =
    match state with
    | Fold.State.Active _ -> Error $"Stack '{created.Id}' already exists."
    | Fold.State.Discard  -> Error $"Stack '{created.Id}' already exists (though it's discarded.)"
    | Fold.State.Initial  -> validateCreated created revision
    |> addEvent (Events.Created created)

let decideDiscard (id: StackId) discarded state =
    match state with
    | Fold.State.Discard  -> Error $"Stack '{id}' is already discarded"
    | Fold.State.Initial  -> Error $"Stack '{id}' doesn't exist, so it can't be discarded"
    | Fold.State.Active _ -> Ok ()
    |> addEvent (Events.Discarded discarded)

let decideChangeTags (tagsChanged: Events.TagsChanged) state =
    match state with
    | Fold.State.Initial -> Error "Can't change the tags of a Stack that doesn't exist."
    | Fold.State.Discard -> Error $"Stack is discarded."
    | Fold.State.Active summary -> validateTagsChanged summary tagsChanged
    |> addEvent (Events.TagsChanged tagsChanged)

let decideChangeRevision (revisionChanged: Events.RevisionChanged) (revision: Example.RevisionSummary) state =
    match state with
    | Fold.State.Initial -> Error "Can't change the revision of a Stack that doesn't exist."
    | Fold.State.Discard -> Error $"Stack is discarded, so you can't change its revision."
    | Fold.State.Active current -> validateRevisionChanged current revisionChanged revision
    |> addEvent (Events.RevisionChanged revisionChanged)

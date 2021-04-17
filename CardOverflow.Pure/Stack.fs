module Domain.Stack

open FsCodec
open FsCodec.NewtonsoftJson
open NodaTime
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling

let streamName (id: StackId) = StreamName.create "Stack" (string id)

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =
    type Review =
        { Index: int
          Score: int
          Created: Instant
          IntervalWithUnusedStepsIndex: int
          EaseFactor: float
          TimeFromSeeingQuestionToScore: Duration }
    type ShadowableDetails =
        { EaseFactor: float
          IntervalOrStepsIndex: IntervalOrStepsIndex // highTODO bring all the types here. ALSO CONSIDER A BETTER NAME
          Due: Instant
          IsLapsed: bool
          History: Review list }
    type Details =
        //| Shadow of StackId * CardTemplatePointer // medTODO don't allow more than 1 hop to prevent infinite loop
        | ShadowableDetails of ShadowableDetails
    type Card =
        { Pointer: CardTemplatePointer
          Created: Instant
          Modified: Instant
          CardSettingId: CardSettingId
          DeckId: DeckId
          Details: Details
          State: CardState }
    type Summary =
        { Id: StackId
          AuthorId: UserId
          ExampleRevisionId: RevisionId
          FrontPersonalField: string
          BackPersonalField: string
          Tags: string Set
          Cards: Card list }
    type TagsChanged =
        { Tags: string Set }
    type RevisionChanged =
        { RevisionId: RevisionId }
    type CardStateChanged =
        { State: CardState
          Pointer: CardTemplatePointer }

    type Event =
        | Created          of Summary
        | Discarded
        | TagsChanged      of TagsChanged
        | RevisionChanged  of RevisionChanged
        | CardStateChanged of CardStateChanged
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type State =
        | Initial
        | Active of Events.Summary
        | Discard
    let initial : State = State.Initial
    
    let mapActive f = function
        | Active a -> f a |> Active
        | x -> x
    
    let mapCard pointer f (card: Events.Card) =
        if card.Pointer = pointer
        then f card
        else card

    let mapCards pointer f =
        List.map (mapCard pointer f)
        
    let evolveTagsChanged
        (e: Events.TagsChanged)
        (s: Events.Summary) =
        { s with Tags = e.Tags }
        
    let evolveRevisionChanged
        (e: Events.RevisionChanged)
        (s: Events.Summary) =
        { s with ExampleRevisionId = e.RevisionId }
        
    let evolveCardStateChanged
        (e: Events.CardStateChanged)
        (s: Events.Summary) =
        { s with Cards = s.Cards |> mapCards e.Pointer (fun c -> { c with State = e.State }) }
    
    let evolve state = function
        | Events.Created          s -> State.Active s
        | Events.Discarded          -> State.Discard
        | Events.TagsChanged      e -> state |> mapActive (evolveTagsChanged e)
        | Events.RevisionChanged  e -> state |> mapActive (evolveRevisionChanged e)
        | Events.CardStateChanged e -> state |> mapActive (evolveCardStateChanged e)

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Created _ -> true | _ -> false

let initCard now cardSettingId newCardsStartingEaseFactor deckId pointer : Events.Card =
    { Pointer = pointer
      Created = now
      Modified = now
      CardSettingId = cardSettingId
      DeckId = deckId
      Details =
        ({ EaseFactor = newCardsStartingEaseFactor
           IntervalOrStepsIndex = IntervalOrStepsIndex.NewStepsIndex 0uy
           Due = now
           IsLapsed = false
           History = [] } : Events.ShadowableDetails)
        |> Events.ShadowableDetails
      State = CardState.Normal }

let init id authorId exampleRevisionId cardSettingId newCardsStartingEaseFactor deckId pointers now : Events.Summary =
    { Id = id
      AuthorId = authorId
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

let validateCardTemplatePointers (current: Events.Summary) (revision: Example.RevisionSummary) = result {
    let! newPointers = Template.getCardTemplatePointers revision.TemplateRevision revision.FieldValues |> Result.map Set.ofList
    let currentPointers = current.Cards |> List.map (fun x -> x.Pointer) |> Set.ofList
    let removed = Set.difference currentPointers newPointers |> Set.toList
    do! Result.requireEmpty $"Some card(s) were removed: {removed}. This is currently unsupported - remove them manually." removed // medTODO support this, and also "renaming"
    }

let validateRevisionChanged (current: Events.Summary) callerId (revision: Example.RevisionSummary) = result {
    do! Result.requireEqual current.AuthorId callerId $"You ({callerId}) aren't the author"
    do! validateCardTemplatePointers current revision
    }

let validateSummary (summary: Events.Summary) revision = result {
    do! validateCardTemplatePointers summary revision
    do! validateTags summary.Tags
    }

let validateTagsChanged (summary: Events.Summary) callerId (tagsChanged: Events.TagsChanged) = result {
    do! Result.requireEqual summary.AuthorId callerId $"You ({callerId}) aren't the author"
    do! validateTags tagsChanged.Tags
    }

let decideCreate (summary: Events.Summary) revision state =
    match state with
    | Fold.State.Active s -> Error $"Stack '{summary.Id}' already exists."
    | Fold.State.Discard  -> Error $"Stack '{summary.Id}' already exists (though it's discarded.)"
    | Fold.State.Initial  -> validateSummary summary revision
    |> addEvent (Events.Created summary)

let decideDiscard (id: StackId) state =
    match state with
    | Fold.State.Discard  -> Error $"Stack '{id}' is already discarded"
    | Fold.State.Initial  -> Error $"Stack '{id}' doesn't exist, so it can't be discarded"
    | Fold.State.Active _ -> Ok ()
    |> addEvent Events.Discarded

let decideChangeTags (tagsChanged: Events.TagsChanged) callerId state =
    match state with
    | Fold.State.Initial -> Error "Can't change the tags of a Stack that doesn't exist."
    | Fold.State.Discard -> Error $"Stack is discarded."
    | Fold.State.Active summary -> validateTagsChanged summary callerId tagsChanged
    |> addEvent (Events.TagsChanged tagsChanged)

let decideChangeRevision callerId (revision: Example.RevisionSummary) state =
    match state with
    | Fold.State.Initial -> Error "Can't change the revision of a Stack that doesn't exist."
    | Fold.State.Discard -> Error $"Stack is discarded, so you can't change its revision."
    | Fold.State.Active current -> validateRevisionChanged current callerId revision
    |> addEvent (Events.RevisionChanged { RevisionId = revision.Id })

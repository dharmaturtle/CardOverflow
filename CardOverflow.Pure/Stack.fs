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
        //| Shadow of StackId * SubtemplateName // medTODO don't allow more than 1 hop to prevent infinite loop
        | ShadowableDetails of ShadowableDetails
    type Card =
        { SubtemplateName: SubtemplateName
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
    type CardStateChanged =
        { State: CardState
          SubtemplateName: SubtemplateName }

    type Event =
        | Created          of Summary
        | TagsChanged      of TagsChanged
        | CardStateChanged of CardStateChanged
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type State =
        | Initial
        | Active of Events.Summary
    let initial : State = State.Initial
    
    let mapActive f = function
        | Active a -> f a |> Active
        | x -> x
    
    let mapCard subtemplateName f (card: Events.Card) =
        if card.SubtemplateName = subtemplateName
        then f card
        else card

    let mapCards subtemplateName f =
        List.map (mapCard subtemplateName f)
        
    let evolveTagsChanged
        (e: Events.TagsChanged)
        (s: Events.Summary) =
        { s with Tags = e.Tags }
        
    let evolveCardStateChanged
        (e: Events.CardStateChanged)
        (s: Events.Summary) =
        { s with Cards = s.Cards |> mapCards e.SubtemplateName (fun c -> { c with State = e.State }) }
    
    let evolve state = function
        | Events.Created          s -> State.Active s
        | Events.TagsChanged      e -> state |> mapActive (evolveTagsChanged e)
        | Events.CardStateChanged e -> state |> mapActive (evolveCardStateChanged e)

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Created _ -> true | _ -> false

let validateTag (tag: string) = result {
    do! Result.requireEqual tag (tag.Trim()) $"Remove the spaces before and/or after the tag: '{tag}'."
    do! (1 <= tag.Length && tag.Length <= 100) |> Result.requireTrue $"Tags must be between 1 and 100 characters, but '{tag}' has {tag.Length} characters."
    }

let validateTags (tags: string Set) = result {
    for tag in tags do
        do! validateTag tag
    }

let validateRevisionExists doesRevisionExist (revisionId: RevisionId) =
    doesRevisionExist |> Result.requireTrue $"ExampleRevisionId '{revisionId}' doesn't exist."

let validateSummary (summary: Events.Summary) doesRevisionExist = result {
    do! validateRevisionExists doesRevisionExist summary.ExampleRevisionId
    do! Result.requireNotEmpty "There must be at least 1 card" summary.Cards
    do! validateTags summary.Tags
    }

let validateTagsChanged (summary: Events.Summary) callerId (tagsChanged: Events.TagsChanged) = result {
    do! Result.requireEqual summary.AuthorId callerId $"You ({callerId}) aren't the author"
    do! validateTags tagsChanged.Tags
    }

let decideCreate (summary: Events.Summary) doesRevisionExist state =
    match state with
    | Fold.State.Active s -> Error $"Stack '{s.Id}' already exists."
    | Fold.State.Initial  -> validateSummary summary doesRevisionExist
    |> addEvent (Events.Created summary)

let decideChangeTags (tagsChanged: Events.TagsChanged) callerId state =
    match state with
    | Fold.State.Initial -> Error "Can't change tags of a card that doesn't exist."
    | Fold.State.Active summary -> validateTagsChanged summary callerId tagsChanged
    |> addEvent (Events.TagsChanged tagsChanged)

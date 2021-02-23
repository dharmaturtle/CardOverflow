module Domain.User

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open NodaTime
open CardOverflow.Pure
open FsToolkit.ErrorHandling

let streamName (id: UserId) = StreamName.create "User" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =
    
    type Summary =
        {  Id: UserId
           DisplayName: string
           DefaultDeckId: DeckId
           ShowNextReviewTime: bool
           ShowRemainingCardCount: bool
           StudyOrder: StudyOrder
           NextDayStartsAt: LocalTime
           LearnAheadLimit: Duration
           TimeboxTimeLimit: Duration
           IsNightMode: bool
           Created: Instant
           Modified: Instant
           Timezone: DateTimeZone
           CardSettings: CardSetting list
           FollowedDecks: DeckId Set
        }
    type OptionsEdited =
        {  DefaultDeckId: DeckId
           ShowNextReviewTime: bool
           ShowRemainingCardCount: bool
           StudyOrder: StudyOrder
           NextDayStartsAt: LocalTime
           LearnAheadLimit: Duration
           TimeboxTimeLimit: Duration
           IsNightMode: bool
           Timezone: DateTimeZone
        }
    type DeckFollowed   = { DeckId: DeckId}
    type DeckUnfollowed = { DeckId: DeckId}

    type CardSettingsEdited =
        { CardSettings: CardSetting list }

    type Event =
        | CardSettingsEdited of CardSettingsEdited
        | OptionsEdited      of OptionsEdited
        | DeckFollowed       of DeckFollowed
        | DeckUnfollowed     of DeckUnfollowed
        | Created            of Summary
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =
    
    type State =
        | Initial
        | Active of Events.Summary
    let initial = State.Initial

    let mapActive f = function
        | Active a -> f a |> Active
        | x -> x

    let evolveDeckFollowed (d: Events.DeckFollowed) (s: Events.Summary) =
        { s with FollowedDecks = s.FollowedDecks |> Set.add d.DeckId }

    let evolveDeckUnfollowed (d: Events.DeckUnfollowed) (s: Events.Summary) =
        { s with FollowedDecks = s.FollowedDecks |> Set.remove d.DeckId }

    let evolveCardSettingsEdited (cs: Events.CardSettingsEdited) (s: Events.Summary) =
        { s with CardSettings = cs.CardSettings }

    let evolveOptionsEdited (o: Events.OptionsEdited) (s: Events.Summary) =
        { s with
            DefaultDeckId          = o.DefaultDeckId
            ShowNextReviewTime     = o.ShowNextReviewTime
            ShowRemainingCardCount = o.ShowRemainingCardCount
            StudyOrder             = o.StudyOrder
            NextDayStartsAt        = o.NextDayStartsAt
            LearnAheadLimit        = o.LearnAheadLimit
            TimeboxTimeLimit       = o.TimeboxTimeLimit
            IsNightMode            = o.IsNightMode
            Timezone               = o.Timezone }

    let evolve state =
        function
        | Events.Created             s -> State.Active s
        | Events.OptionsEdited       o -> state |> mapActive (evolveOptionsEdited o)
        | Events.CardSettingsEdited cs -> state |> mapActive (evolveCardSettingsEdited cs)
        | Events.DeckFollowed        d -> state |> mapActive (evolveDeckFollowed d)
        | Events.DeckUnfollowed      d -> state |> mapActive (evolveDeckUnfollowed d)
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve

let validateDisplayName (displayName: string) =
    (4 <= displayName.Length && displayName.Length <= 18)
    |> Result.requireTrue $"The display name '{displayName}' must be between 4 and 18 characters."

let validateSummary (summary: Events.Summary) = result {
    do! validateDisplayName summary.DisplayName
    do! Result.requireEqual
            (summary.DisplayName)
            (summary.DisplayName.Trim())
            $"Remove the spaces before and/or after your display name: '{summary.DisplayName}'."
    }

let isDeckFollowed (summary: Events.Summary) deckId =
    summary.FollowedDecks.Contains deckId

let validateDeckFollowed (summary: Events.Summary) deckId =
    Result.requireTrue
        $"You don't follow the deck '{deckId}'."
        (isDeckFollowed summary deckId)

let validateDeckNotFollowed (summary: Events.Summary) deckId =
    Result.requireFalse
        $"You already follow the deck '{deckId}'."
        (isDeckFollowed summary deckId)

let validateDeckExists doesDeckExist deckId =
    Result.requireTrue
        $"The deck '{deckId}' doesn't exist."
        doesDeckExist

let decideCreate (summary: Events.Summary) state =
    match state with
    | Fold.State.Active s -> Error $"User '{s.Id}' already exists."
    | Fold.State.Initial  -> validateSummary summary
    |> addEvent (Events.Created summary)

let decideOptionsEdited (o: Events.OptionsEdited) defaultDeckUserId state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit the options of a user that doesn't exist."
    | Fold.State.Active s -> result {
        do! Result.requireEqual s.Id defaultDeckUserId $"Deck {o.DefaultDeckId} doesn't belong to User {s.Id}"
    } |> addEvent (Events.OptionsEdited o)

let decideCardSettingsEdited (cs: Events.CardSettingsEdited) state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit the options of a user that doesn't exist."
    | Fold.State.Active _ -> result {
        do! cs.CardSettings |> List.filter (fun x -> x.IsDefault) |> List.length |> Result.requireEqualTo 1 "You must have 1 default card setting."
    } |> addEvent (Events.CardSettingsEdited cs)

let decideFollowDeck (deckId: DeckId) deckExists state =
    match state with
    | Fold.State.Initial  -> Error "You can't follow a deck if you don't exist..."
    | Fold.State.Active s -> result {
        do! validateDeckNotFollowed s deckId
        do! validateDeckExists deckExists deckId
    } |> addEvent (Events.DeckFollowed { Events.DeckFollowed.DeckId = deckId })

let decideUnfollowDeck (deckId: DeckId) state =
    match state with
    | Fold.State.Initial  -> Error "You can't unfollow a deck if you don't exist..."
    | Fold.State.Active s -> result {
        do! validateDeckFollowed s deckId
    } |> addEvent (Events.DeckUnfollowed { Events.DeckUnfollowed.DeckId = deckId })

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
           DefaultCardSettingId: CardSettingId
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
        }
    type OptionsEdited =
        {  DefaultCardSettingId: CardSettingId
           DefaultDeckId: DeckId
           ShowNextReviewTime: bool
           ShowRemainingCardCount: bool
           StudyOrder: StudyOrder
           NextDayStartsAt: LocalTime
           LearnAheadLimit: Duration
           TimeboxTimeLimit: Duration
           IsNightMode: bool
           Timezone: DateTimeZone
        }

    type CardSettingsEdited =
        { CardSettings: CardSetting list }

    type Event =
        | CardSettingsEdited of CardSettingsEdited
        | OptionsEdited      of OptionsEdited
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

    let evolveCardSettingsEdited (cs: Events.CardSettingsEdited) (s: Events.Summary) =
        { s with CardSettings = cs.CardSettings }

    let evolveOptionsEdited (o: Events.OptionsEdited) (s: Events.Summary) =
        { s with
            DefaultCardSettingId   = o.DefaultCardSettingId
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
        | Events.Created s             -> State.Active s
        | Events.OptionsEdited o       -> state |> mapActive (evolveOptionsEdited o)
        | Events.CardSettingsEdited cs -> state |> mapActive (evolveCardSettingsEdited cs)
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve

let validateName (name: string) =
    (4 <= name.Length && name.Length <= 18)
    |> Result.requireTrue $"The name '{name}' must be between 4 and 18 characters."

let validateSummary (summary: Events.Summary) = result {
    do! validateName summary.DisplayName
    do! Result.requireEqual
            (summary.DisplayName)
            (summary.DisplayName.Trim())
            $"Remove the spaces before and/or after your display name: '{summary.DisplayName}'."
    }

let decideCreate (summary: Events.Summary) state =
    match state with
    | Fold.State.Active s -> Error $"User '{s.Id}' already exists."
    | Fold.State.Initial  -> validateSummary summary
    |> addEvent (Events.Created summary)

let decideOptionsEdited (o: Events.OptionsEdited) defaultDeckUserId defaultCardSettingUserId state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit the options of a user that doesn't exist."
    | Fold.State.Active s -> result {
        do! Result.requireEqual s.Id defaultDeckUserId        $"Deck {o.DefaultDeckId} doesn't belong to User {s.Id}"
        do! Result.requireEqual s.Id defaultCardSettingUserId $"CardSetting {o.DefaultCardSettingId} doesn't belong to User {s.Id}"
    } |> addEvent (Events.OptionsEdited o)

let decideCardSettingsEdited (cs: Events.CardSettingsEdited) state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit the options of a user that doesn't exist."
    | Fold.State.Active _ -> result {
        do! cs.CardSettings |> List.filter (fun x -> x.IsDefault) |> List.length |> Result.requireEqualTo 1 "You must have 1 default card setting."
    } |> addEvent (Events.CardSettingsEdited cs)

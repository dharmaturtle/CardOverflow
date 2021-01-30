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
    
    type Snapshot =
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
        | Snapshot           of Snapshot
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =
    
    type State =
        | Initial
        | Active of Events.Snapshot
    let initial = State.Initial

    let evolveCardSettingsEdited (cs: Events.CardSettingsEdited) (s: Events.Snapshot) =
        { s with CardSettings = cs.CardSettings }

    let evolveOptionsEdited (o: Events.OptionsEdited) (s: Events.Snapshot) =
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
        | Events.Snapshot s -> State.Active s
        | Events.OptionsEdited o ->
            match state with
            | State.Initial  -> invalidOp "User doesn't exist"
            | State.Active s -> evolveOptionsEdited o s |> State.Active
        | Events.CardSettingsEdited cs ->
            match state with
            | State.Initial  -> invalidOp "User doesn't exist"
            | State.Active s -> evolveCardSettingsEdited cs s |> State.Active
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Snapshot _ -> true | _ -> false

let decideCreate state = function
    | Fold.State.Initial  -> Ok ()                  , [ Events.Snapshot state ]
    | Fold.State.Active _ -> Error "Already created", []

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

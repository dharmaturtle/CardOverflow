module Domain.User

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open NodaTime
open CardOverflow.Pure

let streamName (id: UserId) = StreamName.create "User" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =
    
    type Snapshot =
        {  UserId: UserId
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
        }

    type Event =
        | DefaultCardSettingIdChanged of {| CardSettingId: CardSettingId |}
        | DefaultDeckIdChanged        of {| DeckId: DeckId |}
        | Snapshot                    of Snapshot
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event>()

module Fold =
    
    type State =
        | Initial
        | Active of Events.Snapshot
    let initial = State.Initial

    let evolve state =
        function
        | Events.Snapshot s -> State.Active s
        | Events.DefaultCardSettingIdChanged b ->
            match state with
            | State.Initial  -> invalidOp "User doesn't exist"
            | State.Active a -> { a with DefaultCardSettingId = b.CardSettingId } |> State.Active
        | Events.DefaultDeckIdChanged b ->
            match state with
            | State.Initial  -> invalidOp "User doesn't exist"
            | State.Active a -> { a with DefaultDeckId = b.DeckId } |> State.Active
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Snapshot _ -> true | _ -> false

let decideCreate state = function
    | Fold.State.Initial  -> Ok ()                  , [ Events.Snapshot state ]
    | Fold.State.Active _ -> Error "Already created", []

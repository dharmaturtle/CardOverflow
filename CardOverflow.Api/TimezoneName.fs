namespace CardOverflow.Api

open CardOverflow.Debug
open MappingTools
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Pure.Core
open System
open System.Linq
open FsToolkit.ErrorHandling
open System.Security.Cryptography
open System.Text
open System.Collections.Generic
open System.Text.RegularExpressions
open System.Collections
open NUlid
open NodaTime
open NpgsqlTypes
open CardOverflow.Entity

type DateTimeX =
    static member UtcNow with get () = SystemClock.Instance.GetCurrentInstant()

module Period =
    let toDuration (p: Period) =
        p.ToDuration()

module Duration =
    let toPeriod (d: Duration) =
        d.TotalMilliseconds |> Math.Round |> int64 |> Period.FromMilliseconds
    let toLocalTime (d: Duration) =
        d.TotalTicks |> Math.Round |> int64 |> LocalTime.FromTicksSinceMidnight

module LocalTime =
    let toDuration (lt: LocalTime) =
        lt.TickOfDay |> Duration.FromTicks

module TimezoneName =
    let private timezoneNameType = typeof<TimezoneName>
    let private allEnums =
        Enum.GetValues(timezoneNameType)
            .Cast<TimezoneName>()
        |> Seq.toList
    let private getName (tz: TimezoneName) =
        timezoneNameType
            .GetMember(tz.ToString())
            .Single(fun m -> m.DeclaringType = timezoneNameType)
            .GetCustomAttributes(true)
            .First()
        :?> PgNameAttribute
        |> fun x -> x.PgName
    let all =
        allEnums
        |> List.map getName
    let nameByEnum =
        allEnums
        |> List.map (fun x -> x, getName x)
        |> Map.ofList
    let allPretty (clock: IClock) =
        all
        |> List.map (fun x -> DateTimeZoneProviders.Tzdb.Item x)
        |> List.map (fun x -> sprintf "(UTC%A) %s" (clock.GetCurrentInstant() |> x.GetUtcOffset) x.Id)
        |> List.sort
    let allNodaTime =
        DateTimeZoneProviders.Tzdb.Ids
        |> Seq.map (fun x -> DateTimeZoneProviders.Tzdb.Item x)

type UserSetting = {
    UserId: Guid
    DisplayName: string
    DefaultCardSettingId: Guid
    DefaultDeckId: Guid
    ShowNextReviewTime: bool
    ShowRemainingCardCount: bool
    StudyOrder: StudyOrder
    NextDayStartsAt: Duration
    LearnAheadLimit: Duration
    TimeboxTimeLimit: Duration
    IsNightMode: bool
    Created: Instant
    Timezone: TimezoneName
}

module SetUserSetting =
    type _command = { // use create. The ctor isn't private cause that prevents access to members
        DisplayName: string
        DefaultCardSettingId: Guid
        DefaultDeckId: Guid
        ShowNextReviewTime: bool
        ShowRemainingCardCount: bool
        StudyOrder: StudyOrder
        NextDayStartsAt: Duration
        LearnAheadLimit: Duration
        TimeboxTimeLimit: Duration
        IsNightMode: bool
        Timezone: TimezoneName
    }
    let create
            displayName
            defaultCardSettingId
            defaultDeckId
            showNextReviewTime
            showRemainingCardCount
            studyOrder
            (nextDayStartsAt: Duration)
            (learnAheadLimit: Duration)
            (timeboxTimeLimit: Duration)
            isNightMode
            timezone = result {
        let! displayName = displayName |> Result.requireNotNull "Display name cannot be null." |> Result.map MappingTools.standardizeWhitespace
        do! displayName.Length >= 3 |> Result.requireTrue "Display name must be at least 3 characters."
        do! displayName.Length <= 32 |> Result.requireTrue "Display name has a maximum of 32 characters."
        do! nextDayStartsAt.TotalHours <= 24. |> Result.requireTrue "The next day must start at a maximum of 24 hrs."
        do! learnAheadLimit.TotalHours <= 24. |> Result.requireTrue "The learn ahead limit has a maximum of 24 hrs."
        do! timeboxTimeLimit.TotalHours <= 24. |> Result.requireTrue "The time box time limit has a maximum of 24 hrs."
        do! Enum.IsDefined(typeof<TimezoneName>, timezone) |> Result.requireTrue "Invalid timezone enum."
        do! Enum.IsDefined(typeof<StudyOrder>, studyOrder) |> Result.requireTrue "Invalid study order."
        return
            {   DisplayName = displayName
                DefaultCardSettingId = defaultCardSettingId
                DefaultDeckId = defaultDeckId
                ShowNextReviewTime = showNextReviewTime
                ShowRemainingCardCount = showRemainingCardCount
                StudyOrder = studyOrder
                NextDayStartsAt = nextDayStartsAt
                LearnAheadLimit = learnAheadLimit
                TimeboxTimeLimit = timeboxTimeLimit
                IsNightMode = isNightMode
                Timezone = timezone
            }
        }

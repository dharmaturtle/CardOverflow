[<AutoOpen>]
module Domain.Infrastructure

open FSharp.UMX
open System

type UserId = Guid<userId>
    and [<Measure>] userId

type DeckId = Guid<deckId>
    and [<Measure>] deckId
type CardSettingId = Guid<cardSettingId>
    and [<Measure>] cardSettingId

type ExampleId = Guid<exampleId>
    and [<Measure>] exampleId

type ExampleRevisionOrdinal = int<exampleRevisionOrdinal>
    and [<Measure>] exampleRevisionOrdinal
type ExampleRevisionId = ExampleId * ExampleRevisionOrdinal
module ExampleRevisionId =
    let ser (id: ExampleRevisionId) : string =
        let example, revision = id
        let example = (FSharp.UMX.UMX.untag example).ToString "D"
        $"{example}.%i{revision}"
    let des (id: string) : ExampleRevisionId =
        let arr = id.Split('.', 2)
        let example  = Guid .Parse arr.[0]
        let revision = Int32.Parse arr.[1]
        % example, % revision

type StackId = Guid<stackId>
    and [<Measure>] stackId
[<RequireQualifiedAccess>]
type CardTemplatePointer =
    | Normal of Guid
    | Cloze of int

type TemplateId = Guid<templateId>
    and [<Measure>] templateId
type TemplateRevisionOrdinal = int<templateRevisionOrdinal>
    and [<Measure>] templateRevisionOrdinal
type TemplateRevisionId = TemplateId * TemplateRevisionOrdinal
module TemplateRevisionId =
    let ser (id: TemplateRevisionId) : string =
        let template, revision = id
        let template = (FSharp.UMX.UMX.untag template).ToString "D"
        $"{template}.%i{revision}"
    let des (id: string) : TemplateRevisionId =
        let arr = id.Split('.', 2)
        let template = Guid .Parse arr.[0]
        let revision = Int32.Parse arr.[1]
        % template, % revision

type AppendError =
    | Custom of string
    | Idempotent

let CError  x = x |> Custom
let CCError x = x |> Custom |> Error

let addEvent okEvent = function
    | Ok   () ->        Ok ()  , [okEvent]
    | Error x ->
        match x with
        | Custom s   -> Error s, []
        | Idempotent -> Ok ()  , []

open NodaTime.Serialization.JsonNet
let jsonSerializerSettings = Newtonsoft.Json.JsonSerializerSettings().ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb)

type Visibility =
    | Public
    | Private

type CommandId = Guid<commandId>
    and [<Measure>] commandId

type DmcaTakeDownId = Guid<dmcaTakeDownId>
    and [<Measure>] dmcaTakeDownId
open NodaTime
type DmcaTakeDown =
    {   Id: DmcaTakeDownId
        CommandIds: CommandId Set
        CopyrightHolderName: string
        Received: Instant
    }

type Meta = {
    ServerReceivedAt: Instant Option // ref: https://snowplowanalytics.com/blog/2015/09/15/improving-snowplows-understanding-of-time/ and https://discourse.snowplowanalytics.com/t/which-timestamp-is-the-best-to-see-when-an-event-occurred/538
    ClientCreatedAt: Instant
    ClientSentAt: Instant Option
    CommandId: CommandId
    UserId: UserId
}

open FsToolkit.ErrorHandling
let idempotencyCheck (meta: Meta) (cs: CommandId Set) =
    cs |> Set.contains meta.CommandId |> Result.requireFalse Idempotent
let idempotencyBypass = Result.Ok ()

let bindCCError error = Result.bind (fun () -> CCError error)

// We want to index ServerCreatedAt to quickly find un-server-synced events on the browser/client
// IndexedDB (and therefore Dexie.js) can't index "null": https://github.com/dfahlander/Dexie.js/issues/153
// A real Instant must therefore be used as the "unsynced" value
// I'm arbitrary choosing the epoch plus a few seconds
// I'm adding 13 seconds to help with disambiguation since the epoch is the default/fallback value for many things
let unsynced = Instant.FromUtc(1970,1,1,0,0,13)

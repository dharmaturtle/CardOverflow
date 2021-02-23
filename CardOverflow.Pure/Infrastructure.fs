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

type StackId = Guid<stackId>
    and [<Measure>] stackId
type BranchId = Guid<branchId>
    and [<Measure>] branchId
type LeafId = Guid<leafId>
    and [<Measure>] leafId

type TemplateId = Guid<templateId>
    and [<Measure>] templateId
type TemplateRevisionId = Guid<templateRevisionId>
    and [<Measure>] templateRevisionId

let addEvent okEvent = function
    | Ok x    -> Ok x   , [okEvent]
    | Error x -> Error x, []

open NodaTime.Serialization.JsonNet
let jsonSerializerSettings = Newtonsoft.Json.JsonSerializerSettings().ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb)
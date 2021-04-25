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
type RevisionId = ExampleId * ExampleRevisionOrdinal
module RevisionId =
    let ser (id: RevisionId) : string =
        let example, revision = id
        let example = (FSharp.UMX.UMX.untag example).ToString "D"
        $"{example}.%i{revision}"
    let des (id: string) : RevisionId =
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

let addEvent okEvent = function
    | Ok x    -> Ok x   , [okEvent]
    | Error x -> Error x, []

open NodaTime.Serialization.JsonNet
let jsonSerializerSettings = Newtonsoft.Json.JsonSerializerSettings().ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb)

type DmcaTakeDownId = Guid<dmcaTakeDownId>
    and [<Measure>] dmcaTakeDownId
open NodaTime
type DmcaTakeDown =
    {   Id: DmcaTakeDownId
        CopyrightHolderName: string
        Received: Instant
    }

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
type ParentedExampleId =
    { ExampleId: ExampleId
      ParentId:  ExampleId option }
module ParentedExampleId =
    let create exampleId parentId = {
        ExampleId = exampleId
        ParentId  = parentId
    }
type RevisionId = int<revisionId>
    and [<Measure>] revisionId
type StackId = Guid<stackId>
    and [<Measure>] stackId
[<RequireQualifiedAccess>]
type CardTemplatePointer =
    | Normal of Guid
    | Cloze of int

type TemplateId = Guid<templateId>
    and [<Measure>] templateId
type TemplateRevisionId = int<templateRevisionId>
    and [<Measure>] templateRevisionId

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

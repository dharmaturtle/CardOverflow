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
type RevisionId = Guid<revisionId>
    and [<Measure>] revisionId
type StackId = Guid<stackId>
    and [<Measure>] stackId
type SubtemplateName = string<subtemplateName>
    and [<Measure>] subtemplateName
module SubtemplateName =
    let toString (value : SubtemplateName) : string = % value
    let fromString (value : string) : SubtemplateName = % value

type TemplateId = Guid<templateId>
    and [<Measure>] templateId
type TemplateRevisionId = Guid<templateRevisionId>
    and [<Measure>] templateRevisionId

let addEvent okEvent = function
    | Ok x    -> Ok x   , [okEvent]
    | Error x -> Error x, []

open NodaTime.Serialization.JsonNet
let jsonSerializerSettings = Newtonsoft.Json.JsonSerializerSettings().ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb)
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

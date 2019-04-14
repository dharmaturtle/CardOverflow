module MappingTools

open System

let delimiter = ' '

let stringOfMinutesToTimeSpanList(string: string) =
    string.Split [|delimiter|] |> Seq.map (Double.Parse >> TimeSpan.FromMinutes) |> Seq.toList

let timeSpanListToStringOfMinutes(timeSpans: TimeSpan list) =
    timeSpans |> List.map (fun x -> x.TotalMinutes.ToString()) |> fun x -> String.Join(delimiter, x)

let stringIntToBool =
    function
    | "0" -> false
    | _-> true

// https://en.wikipedia.org/wiki/C0_and_C1_control_codes

let split separator (string: string) =
    string.Split [|separator|] |> Array.toList

let splitByFileSeparator =
    split '\x1c'

let splitByGroupSeparator =
    split '\x1d'

let splitByRecordSeparator =
    split '\x1e'

let splitByUnitSeparator =
    split '\x1f'

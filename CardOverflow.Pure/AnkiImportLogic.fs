namespace CardOverflow.Pure

open CardOverflow.Debug
open System.Linq
open FsToolkit.ErrorHandling
open System
open CardOverflow.Pure
open Microsoft.FSharp.Core.Operators.Checked
open System.Text.RegularExpressions

module Cloze =
    type ClozeRegex = FSharp.Text.RegexProvider.Regex< """{{c(?<clozeIndex>\d+)::(?<answer>.*?)(?:::(?<hint>.*?))?}}""" >
    type ClozeTemplateRegex = FSharp.Text.RegexProvider.Regex< """{{cloze:(?<fieldName>.*?)}}""" >
    let regex =
        Regex.compiledIgnoreCase |> ClozeRegex
    let templateRegex =
        Regex.compiledIgnoreCase |> ClozeTemplateRegex
    let isCloze questionXemplate =
        templateRegex.IsMatch questionXemplate

module AnkiImportLogic =
    let maxClozeIndex errorMessage (valuesByFieldName: Map<string, string>) = // veryLowTodo option - no need to make this a Result
        Cloze.templateRegex.TypedMatches
        >> Seq.collect (fun m -> valuesByFieldName.[m.fieldName.Value] |> Cloze.regex.TypedMatches)
        >> List.ofSeq
        >> function
        | [] -> Error errorMessage
        | x ->
            let indexes = x |> List.map (fun x -> x.clozeIndex.Value |> int16) |> List.sort
            let max = indexes.Last()
            Seq.zip
                [ 1s .. max ]
                indexes
            |> Seq.forall(fun (x, y) -> x = y)
            |> fun isConsecutive ->
                match isConsecutive && max > 0s with
                | true -> Ok max
                | false -> Error errorMessage
    let clozeFields questionXemplate =
        Cloze.templateRegex.TypedMatches questionXemplate
        |> Seq.map(fun x -> x.fieldName.Value)
        |> List.ofSeq

module ClozeLogic =
    let maxClozeIndexInclusive errorMessage (valuesByFieldName: Map<string, string>) =
        AnkiImportLogic.maxClozeIndex errorMessage valuesByFieldName >>
        Result.map ((+) -1s)

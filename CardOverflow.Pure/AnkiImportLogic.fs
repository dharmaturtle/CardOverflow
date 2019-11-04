namespace CardOverflow.Pure

open FsToolkit.ErrorHandling
open FSharp.Text.RegexProvider
open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations

module AnkiImportLogic =
    type ClozeRegex = Regex< """{{c(?<clozeIndex>\d+)::(?<answer>.*?)(?:::(?<hint>.*?))?}}""" >
    let maxClozeIndex fields errorMessage =
        fields
        |> List.map (ClozeRegex().TypedMatches)
        |> function
        | [] -> Error errorMessage
        | x -> x
            |> Seq.collect id
            |> Seq.map (fun x -> x.clozeIndex.Value |> int)
            |> Seq.max
            |> Ok
    let multipleClozeToSingleCloze (index: byte) =
        List.map (fun field ->
            (field, ClozeRegex().TypedMatches field)
            ||> Seq.fold (fun field m -> 
                if m.clozeIndex.Value = string index then
                    let hint =
                        if String.IsNullOrWhiteSpace m.hint.Value
                        then ""
                        else "::" + m.hint.Value
                    field.Replace(m.Value, "{{c" + string index + "::" + m.answer.Value + hint + "}}")
                else
                    field.Replace(m.Value, m.answer.Value)
        ))

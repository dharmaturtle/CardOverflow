namespace CardOverflow.Pure

open CardOverflow.Debug
open System.Linq
open FsToolkit.ErrorHandling
open FSharp.Text.RegexProvider
open System
open Microsoft.FSharp.Core.Operators.Checked

type ClozeRegex = Regex< """{{c(?<clozeIndex>\d+)::(?<answer>.*?)(?:::(?<hint>.*?))?}}""" >
type ClozeTemplateRegex = Regex< """{{cloze:(?<fieldName>.*?)}}""" >

module AnkiImportLogic =
    let maxClozeIndex errorMessage (valuesByFieldName: Map<string, string>) =
        ClozeTemplateRegex().TypedMatches
        >> Seq.map (fun m -> valuesByFieldName.[m.fieldName.Value] |> ClozeRegex().TypedMatches)
        >> Seq.collect id
        >> List.ofSeq
        >> function
        | [] -> Error errorMessage
        | x ->
            let indexes = x |> List.map (fun x -> x.clozeIndex.Value |> int) |> List.sort
            let max = indexes.Last()
            Seq.zip
                [ 1 .. max ]
                indexes
            |> Seq.forall(fun (x, y) -> x = y)
            |> function
            | true -> Ok max
            | false -> Error errorMessage
    let multipleClozeToSingleCloze (index: byte) field =
        (field, ClozeRegex().TypedMatches field)
        ||> Seq.fold (fun field m -> 
            if m.clozeIndex.Value = string index then
                let hint =
                    if String.IsNullOrWhiteSpace m.hint.Value
                    then ""
                    else "::" + m.hint.Value
                field.Replace(m.Value, "{{c" + string index + "::" + m.answer.Value + hint + "}}")
            else
                field.Replace(m.Value, m.answer.Value))
    let multipleClozeToSingleClozeList (index: byte) =
        List.map (multipleClozeToSingleCloze index)
    let clozeFields questionTemplate =
        ClozeTemplateRegex().TypedMatches questionTemplate
        |> Seq.map(fun x -> x.fieldName.Value)
        |> List.ofSeq

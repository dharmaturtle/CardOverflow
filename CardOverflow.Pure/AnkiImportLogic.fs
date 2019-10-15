namespace CardOverflow.Pure

open FsToolkit.ErrorHandling
open FSharp.Text.RegexProvider
open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations

module AnkiImportLogic =
    type ClozeRegex = Regex< """{{c(?<clozeIndex>\d+)::(?<answer>.*?)(?:::(?<hint>.*?))?}}""" >
    let maxClozeIndex fields noteId = result {
        let! r =
            ClozeRegex().TypedMatches fields
            |> List.ofSeq
            |> function
            | [] -> Error <| sprintf "Anki Note Id #%s is malformed. It claims to be a cloze deletion but doesn't have the syntax of one. Its fields are: %s" noteId fields
            | x -> Ok x
        return
            r
            |> Seq.map (fun x -> x.clozeIndex.Value |> int)
            |> Seq.max
    }
    let multipleClozeToSingleCloze fields (index: byte) =
        (fields, ClozeRegex().TypedMatches fields)
        ||> Seq.fold (fun fields m -> 
            if m.clozeIndex.Value = string index then
                let hint =
                    if String.IsNullOrWhiteSpace m.hint.Value
                    then ""
                    else "::" + m.hint.Value
                fields.Replace(m.Value, "{{c::" + m.answer.Value + hint + "}}")
            else
                fields.Replace(m.Value, m.answer.Value)
        )

namespace CardOverflow.Pure

open CardOverflow.Debug
open System.Linq
open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open CardOverflow.Debug
open System.Text.RegularExpressions
open NodaTime
open FsToolkit.ErrorHandling
open Domain.Infrastructure
open Domain

[<CompilationRepresentationAttribute(CompilationRepresentationFlags.ModuleSuffix)>] // https://stackoverflow.com/questions/793536
module Template =
    let getSubtemplateNames (templateRevision: Template.RevisionSummary) (fieldValues: Map<string, string>) =
        match templateRevision.CardTemplates with
        | Cloze t -> result {
            let! max = ClozeLogic.maxClozeIndexInclusive "Something's wrong with your cloze indexes." fieldValues t.Front
            return [0s .. max] |> List.choose (fun clozeIndex ->
                CardHtml.tryGenerate
                    <| (fieldValues |> Map.toList)
                    <| t.Front
                    <| t.Back
                    <| templateRevision.Css
                    <| CardHtml.Cloze clozeIndex
                |> Option.map (fun _ -> clozeIndex |> string |> SubtemplateName.fromString)
            )}
        | Standard ts ->
            ts |> List.choose (fun t ->
                CardHtml.tryGenerate
                    <| (fieldValues |> Map.toList)
                    <| t.Front
                    <| t.Back
                    <| templateRevision.Css
                    <| CardHtml.Standard
                |> Option.map (fun _ -> t.Name |> SubtemplateName.fromString)
            ) |> Result.requireNotEmptyX "No cards generated because the front is unchanged."

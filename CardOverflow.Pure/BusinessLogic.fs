namespace CardOverflow.Pure

open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open System.Text.RegularExpressions

module Relationship =
    type RelationshipRegex = FSharp.Text.RegexProvider.Regex< """(?<source>.+)\/(?<target>.+)""" >
    let split name =
        let x = RelationshipRegex().TypedMatch name
        if x.Success then
            x.source.Value, x.target.Value
        else
            name, ""
    let flipName name =
        let x = RelationshipRegex().TypedMatch name
        if x.Success then
            x.target.Value + "/" + x.source.Value
        else
            name

module Cloze =
    let isCloze questionTemplate =
        AnkiImportLogic.ClozeTemplateRegex().IsMatch questionTemplate

module CardHtml =
    type ClozeRegex = FSharp.Text.RegexProvider.Regex< """{{c\d+::(?<answer>.*?)(?:::(?<hint>.*?))?}}""" >
    let generate fieldNameValueMap questionTemplate answerTemplate css =
        let questionTemplate, answerTemplate =
            fieldNameValueMap
            |> List.filter(fun (_, value) -> ClozeRegex().IsMatch value)
            |> List.tryExactlyOne
            |> function
            | None -> questionTemplate, answerTemplate
            | Some (fieldName, _) ->
                let irrelevantCloze = Regex <| "{{cloze:(?!" + fieldName + ").+?}}"
                irrelevantCloze.Replace(questionTemplate, ""), irrelevantCloze.Replace(answerTemplate, "")
        let replaceFields isFront template =
            (template, fieldNameValueMap)
            ||> List.fold(fun (previous: string) (fieldName, value) -> 
                let simple =
                    previous.Replace("{{" + fieldName + "}}", value)
                let showIfHasText =
                    let regex = Regex <| "{{#" + fieldName + @"}}(.*?){{\/" + fieldName + "}}"
                    if String.IsNullOrWhiteSpace value
                    then regex.Replace(simple, "")
                    else regex.Replace(simple, "$1")
                let showIfEmpty =
                    let regex = Regex <| @"{{\^" + fieldName + @"}}(.*?){{\/" + fieldName + "}}"
                    if String.IsNullOrWhiteSpace value
                    then regex.Replace(showIfHasText, "$1")
                    else regex.Replace(showIfHasText, "")
                let stripHtml =
                    showIfEmpty.Replace("{{text:" + fieldName + "}}", MappingTools.stripHtmlTags value)
                let cloze =
                    if isFront then
                        let brackets = """
        <span class="cloze-brackets-front">[</span>
        <span class="cloze-filler-front">...</span>
        <span class="cloze-brackets-front">]</span>
        """
                        let hidden = ClozeRegex().Replace(value, brackets) // medTODO show the hint
                        stripHtml.Replace("{{cloze:" + fieldName + "}}", hidden)
                    else
                        let html =
                            sprintf """
        <span class="cloze-brackets-back">[</span>
        %s
        <span class="cloze-brackets-back">]</span>
        """
                        let answer = ClozeRegex().Replace(value, html <| ClozeRegex().TypedMatch(value).answer.Value)
                        stripHtml.Replace("{{cloze:" + fieldName + "}}", answer)
                cloze
            )
        let frontSide =
            replaceFields true questionTemplate
        let backSide =
            (replaceFields false answerTemplate).Replace("{{FrontSide}}", frontSide)
        let htmlBase =
            sprintf """<!DOCTYPE html>
    <head>
        <style>
            .cloze-brackets-front {
                font-size: 150%%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-filler-front {
                font-size: 150%%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-brackets-back {
                font-size: 150%%;
                font-family: monospace;
                font-weight: bolder;
                color: red;
            }
        </style>
        <style>
            %s
        </style>
    </head>
    <body>
        %s
        <script type="text/javascript" src="/js/iframeResizer.contentWindow.min.js"></script> 
    </body>
</html>"""
                css
        htmlBase frontSide,
        htmlBase backSide,
        MappingTools.stripHtmlTags <| frontSide,
        MappingTools.stripHtmlTags <| (replaceFields false answerTemplate).Replace("{{FrontSide}}", "")

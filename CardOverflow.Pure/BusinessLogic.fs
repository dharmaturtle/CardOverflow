namespace CardOverflow.Pure

open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open System.Text.RegularExpressions

module CardHtml =
    type ClozeRegex = FSharp.Text.RegexProvider.Regex< """{{c::(?<answer>.*?)(?:::(?<hint>.*?))?}}""" >
    let generate fieldNameValueMap questionTemplate answerTemplate css =
        let replaceFields isFront template =
            (template, fieldNameValueMap)
            ||> Seq.fold(fun (previous: string) (fieldName, value) -> 
                let simple =
                    previous.Replace("{{" + fieldName + "}}", value)
                let showIfHasText =
                    let regex = Regex("{{#" + fieldName + @"}}(.*?){{\/" + fieldName + "}}")
                    if String.IsNullOrWhiteSpace value
                    then regex.Replace(simple, "")
                    else regex.Replace(simple, "$1")
                let showIfEmpty =
                    let regex = Regex(@"{{\^" + fieldName + @"}}(.*?){{\/" + fieldName + "}}")
                    if String.IsNullOrWhiteSpace value
                    then regex.Replace(showIfHasText, "$1")
                    else regex.Replace(showIfHasText, "")
                let stripHtml =
                    Regex("{{text:" + fieldName + "}}").Replace(showIfEmpty, MappingTools.stripHtmlTags value)
                let cloze =
                    if isFront then
                        let hidden = ClozeRegex().Replace(value, "[...]") // medTODO show the hint
                        Regex("{{cloze:" + fieldName + "}}").Replace(stripHtml, hidden)
                    else
                        let answer = ClozeRegex().Replace(value, ClozeRegex().TypedMatch(value).answer.Value)
                        Regex("{{cloze:" + fieldName + "}}").Replace(stripHtml, answer)
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

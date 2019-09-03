namespace CardOverflow.Pure

open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open System.Text.RegularExpressions

module CardHtml =
    let generate fieldNameValueMap questionTemplate answerTemplate css =
        let replaceFields template =
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
                stripHtml
            )
        let frontSide =
            replaceFields questionTemplate
        let backSide =
            (replaceFields answerTemplate).Replace("{{FrontSide}}", frontSide)
        let htmlBase =
            sprintf """<html>
    <head>
        <style>
            %s
        </style>
    </head>
    <body>
        %s
    </body>
</html>"""
                css
        htmlBase frontSide,
        htmlBase backSide,
        MappingTools.stripHtmlTags <| frontSide,
        MappingTools.stripHtmlTags <| (replaceFields answerTemplate).Replace("{{FrontSide}}", "")

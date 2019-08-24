namespace CardOverflow.Pure

open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations

module CardHtml =
    let generate fieldNameValueMap questionTemplate answerTemplate css =
        let replaceFields template =
            (template, fieldNameValueMap)
            ||> Seq.fold(fun (previous: string) (fieldName, value) -> 
                previous
                    .Replace("{{" + fieldName + "}}", value)
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
        (htmlBase frontSide, htmlBase backSide)

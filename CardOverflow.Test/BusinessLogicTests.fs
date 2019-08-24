module BusinessLogicTests

open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System

let assertBody expectedBody actualHtml =
    Assert.Equal(
        sprintf "<html>
    <head>
        <style>
            
        </style>
    </head>
    <body>
        %s
    </body>
</html>" expectedBody,
        actualHtml
    )

[<Fact>]
let ``CardHtml generates proper basic card template``() =
    let frontSide, backSide =
        CardHtml.generate
            [("Back", "Ottawa"); ("Front", "What is the capital of Canada?")]
            "{{Front}}"
            "{{FrontSide}}
        <hr id=answer>
        {{Back}}"
            ""

    assertBody
        "What is the capital of Canada?"
        frontSide
    assertBody
        "What is the capital of Canada?
        <hr id=answer>
        Ottawa",
        backSide

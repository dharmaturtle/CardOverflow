module BusinessLogicTests

open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System

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
    Assert.Equal(
        "<html>
    <head>
        <style>
            
        </style>
    </head>
    <body>
        What is the capital of Canada?
    </body>
</html>",
        frontSide
    )

    Assert.Equal(
        "<html>
    <head>
        <style>
            
        </style>
    </head>
    <body>
        What is the capital of Canada?
        <hr id=answer>
        Ottawa
    </body>
</html>",
        backSide
    )

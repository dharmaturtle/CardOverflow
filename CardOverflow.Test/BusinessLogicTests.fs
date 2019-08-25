module BusinessLogicTests

open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System

let assertBody expectedBody actualHtml =
    Assert.Equal<string>(
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
let ``CardHtml generates proper basic card template``(): unit =
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
        Ottawa"
        backSide

[<Fact>]
let ``CardHtml generates proper basic with optional reversed custom card template``(): unit =
    let frontSide, backSide =
        CardHtml.generate
            [("Back", "Ottawa")
             ("Front", "What is the capital of Canada?")
             ("Back2", "Canada")
             ("Front2", "What is Ottawa the capital of?")
            ]
            "{{#Front2}}{{Front2}}{{/Front2}}"
            "{{FrontSide}}
        <hr id=answer>
        {{Back2}}"
            ""

    assertBody
        "What is Ottawa the capital of?"
        frontSide
    assertBody
        "What is Ottawa the capital of?
        <hr id=answer>
        Canada"
        backSide

[<Fact>]
let ``CardHtml generates proper basic with optional reversed custom card template, but for {{Front}}``(): unit =
    let frontSide, backSide =
        CardHtml.generate
            [("Back", "Ottawa")
             ("Front", "What is the capital of Canada?")
             ("Back2", "Canada")
             ("Front2", "What is Ottawa the capital of?")
            ]
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
        Ottawa"
        backSide

[<Fact>]
let ``CardHtml generates proper basic card template, but with (empty) conditional Category``(): unit =
    let frontSide, backSide =
        CardHtml.generate
            [("Back", "Ottawa")
             ("Front", "What is the capital of Canada?")
             ("Category", "")
            ]
            "{{#Category}}Category: {{Category}}<br/>{{/Category}}{{Front}}"
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
        Ottawa"
        backSide

[<Fact>]
let ``CardHtml generates proper basic card template, but with conditional Category that's shown``(): unit =
    let frontSide, backSide =
        CardHtml.generate
            [("Back", "Ottawa")
             ("Front", "What is the capital of Canada?")
             ("Category", "Nations and Capitals")
            ]
            "{{#Category}}Category: {{Category}}<br/>{{/Category}}{{Front}}"
            "{{FrontSide}}
        <hr id=answer>
        {{Back}}"
            ""

    assertBody
        "Category: Nations and Capitals<br/>What is the capital of Canada?"
        frontSide
    assertBody
        "Category: Nations and Capitals<br/>What is the capital of Canada?
        <hr id=answer>
        Ottawa"
        backSide

[<Fact>]
let ``CardHtml generates proper basic card template, with conditional Category (inverted and empty)``(): unit =
    let frontSide, backSide =
        CardHtml.generate
            [("Back", "Ottawa")
             ("Front", "What is the capital of Canada?")
             ("Category", "")
            ]
            "{{^Category}}Category: {{Category}}No category was given<br/>{{/Category}}{{Front}}"
            "{{FrontSide}}
        <hr id=answer>
        {{Back}}"
            ""

    assertBody
        "Category: No category was given<br/>What is the capital of Canada?"
        frontSide
    assertBody
        "Category: No category was given<br/>What is the capital of Canada?
        <hr id=answer>
        Ottawa"
        backSide

[<Fact>]
let ``CardHtml generates proper basic card template, with conditional Category (inverted and populated)``(): unit =
    let frontSide, backSide =
        CardHtml.generate
            [("Back", "Ottawa")
             ("Front", "What is the capital of Canada?")
             ("Category", "Nations and Capitals")
            ]
            "{{^Category}}Category: {{Category}}No category was given<br/>{{/Category}}{{Front}}"
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
        Ottawa"
        backSide

[<Fact>]
let ``CardHtml renders {{text:FieldName}} properly``(): unit =
    let frontSide, backSide =
        CardHtml.generate
            [("Back", "<b>Ottawa</b>")
             ("Front", "What is the capital of Canada?")
            ]
            "{{Front}}"
            """{{FrontSide}}
        <hr id=answer>
        {{Back}}<br/><a href="http://example.com/search?q={{text:Back}}">check in dictionary</a>"""
            ""

    assertBody
        "What is the capital of Canada?"
        frontSide
    assertBody
        """What is the capital of Canada?
        <hr id=answer>
        <b>Ottawa</b><br/><a href="http://example.com/search?q=Ottawa">check in dictionary</a>"""
        backSide

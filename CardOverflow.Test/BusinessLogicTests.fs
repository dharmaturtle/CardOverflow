module BusinessLogicTests

open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System

let assertBody expectedBody actualHtml =
    Assert.Equal<string>(
        sprintf """<!DOCTYPE html>
    <head>
        <style>
            
        </style>
    </head>
    <body>
        %s
        <script type="text/javascript" src="/js/iframeResizer.contentWindow.min.js"></script> 
    </body>
</html>""" expectedBody,
        actualHtml
    )

[<Fact>]
let ``CardHtml generates proper basic card template``(): unit =
    let front, back, frontVoice, backVoice =
        CardHtml.generate
            [("Back", "Ottawa"); ("Front", "What is the capital of Canada?")]
            "{{Front}}"
            "{{FrontSide}}
        <hr id=answer>
        {{Back}}"
            ""

    assertBody
        "What is the capital of Canada?"
        front
    assertBody
        "What is the capital of Canada?
        <hr id=answer>
        Ottawa"
        back
    Assert.Equal("What is the capital of Canada?", frontVoice)
    Assert.Equal("Ottawa", backVoice)

[<Fact>]
let ``CardHtml generates proper basic with optional reversed custom card template``(): unit =
    let front, back, _, _ =
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
        front
    assertBody
        "What is Ottawa the capital of?
        <hr id=answer>
        Canada"
        back

[<Fact>]
let ``CardHtml generates proper basic with optional reversed custom card template, but for {{Front}}``(): unit =
    let front, back, _, _ =
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
        front
    assertBody
        "What is the capital of Canada?
        <hr id=answer>
        Ottawa"
        back

[<Fact>]
let ``CardHtml generates proper basic card template, but with (empty) conditional Category``(): unit =
    let front, back, _, _ =
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
        front
    assertBody
        "What is the capital of Canada?
        <hr id=answer>
        Ottawa"
        back

[<Fact>]
let ``CardHtml generates proper basic card template, but with conditional Category that's shown``(): unit =
    let front, back, _, _ =
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
        front
    assertBody
        "Category: Nations and Capitals<br/>What is the capital of Canada?
        <hr id=answer>
        Ottawa"
        back

[<Fact>]
let ``CardHtml generates proper basic card template, with conditional Category (inverted and empty)``(): unit =
    let front, back, _, _ =
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
        front
    assertBody
        "Category: No category was given<br/>What is the capital of Canada?
        <hr id=answer>
        Ottawa"
        back

[<Fact>]
let ``CardHtml generates proper basic card template, with conditional Category (inverted and populated)``(): unit =
    let front, back, _, _ =
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
        front
    assertBody
        "What is the capital of Canada?
        <hr id=answer>
        Ottawa"
        back

[<Fact>]
let ``CardHtml renders {{text:FieldName}} properly``(): unit =
    let front, back, _, _ =
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
        front
    assertBody
        """What is the capital of Canada?
        <hr id=answer>
        <b>Ottawa</b><br/><a href="http://example.com/search?q=Ottawa">check in dictionary</a>"""
        back
        
[<Fact>]
let ``CardHtml renders {{cloze:FieldName}} properly``(): unit =
    let front, back, _, _ =
        CardHtml.generate
            [("Text", "Canberra was founded in {{c::1913}}.")
             ("Extra", "Some extra stuff.")
            ]
            "{{cloze:Text}}"
            """{{cloze:Text}}<br>{{Extra}}"""
            ""

    assertBody
        "Canberra was founded in [...]."
        front
    assertBody
        """Canberra was founded in 1913.<br>Some extra stuff."""
        back

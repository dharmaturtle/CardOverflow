module BusinessLogicTests

open LoadersAndCopiers
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
            CardHtml.Standard
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
let ``CardHtml generates empty string when Front field is missing``(): unit =
    let front, back, frontVoice, backVoice =
        CardHtml.generate
            [("Back", "Ottawa"); ("FrontX", "What is the capital of Canada?")]
            "{{Front}}"
            "{{FrontSide}}
        <hr id=answer>
        {{Back}}"
            ""
            CardHtml.Standard
    Assert.Equal("", front)
    Assert.Equal("", back)
    Assert.Equal("", frontVoice)
    Assert.Equal("", backVoice)

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
            CardHtml.Standard
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
            CardHtml.Standard
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
            CardHtml.Standard
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
            CardHtml.Standard
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
            CardHtml.Standard
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
            CardHtml.Standard
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
            CardHtml.Standard
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
            [("Text", "Canberra was founded in {{c1::1913}}.")
             ("Extra", "Some extra stuff.")
            ]
            "{{cloze:Text}}"
            """{{cloze:Text}}<br>{{Extra}}"""
            ""
            (CardHtml.Cloze 0s)
    assertBody
        """Canberra was founded in 
        <span class="cloze-brackets-front">[</span>
        <span class="cloze-filler-front">...</span>
        <span class="cloze-brackets-front">]</span>
        ."""
        front
    assertBody
        """Canberra was founded in 
        <span class="cloze-brackets-back">[</span>
        1913
        <span class="cloze-brackets-back">]</span>
        .<br>Some extra stuff."""
        back

let assertStripped expected actual =
    Assert.Equal(
        MappingTools.stripHtmlTags expected,
        MappingTools.stripHtmlTags actual)

[<Fact>]
let ``CardHtml renders multiple cloze templates properly 1``(): unit =
    let front, back, _, _ =
        CardHtml.generate
            [   "Field1", "Columbus first crossed the Atlantic in {{c1::1492}}"
                "Field2", "In {{c2::1492}}, Columbus sailed the ocean {{c3::blue}}."
                "Extra", "Some extra info" ]
            "{{cloze:Field1}}{{cloze:Field2}}"
            "{{cloze:Field1}}{{cloze:Field2}}<br>{{Extra}}"
            ""
            (CardHtml.Cloze 0s)
    assertStripped
        "Columbus first crossed the Atlantic in [ ... ]"
        front
    assertStripped
        "Columbus first crossed the Atlantic in [ 1492 ] Some extra info"
        back

[<Fact>]
let ``CardHtml renders multiple cloze templates properly 2``(): unit =
    let front, back, _, _ =
        CardHtml.generate
            [   "Field1", "Columbus first crossed the Atlantic in {{c1::1492}}"
                "Field2", "In {{c2::1492}}, Columbus sailed the ocean {{c3::blue}}."
                "Extra", "Some extra info" ]
            "{{cloze:Field1}}{{cloze:Field2}}"
            "{{cloze:Field1}}{{cloze:Field2}}<br>{{Extra}}"
            ""
            (CardHtml.Cloze 1s)
    assertStripped
        "In [ ... ] , Columbus sailed the ocean blue."
        front
    assertStripped
        "In [ 1492 ] , Columbus sailed the ocean blue.Some extra info"
        back

[<Fact>]
let ``CardHtml renders multiple cloze templates properly 3``(): unit =
    let front, back, _, _ =
        CardHtml.generate
            [   "Field1", "Columbus first crossed the Atlantic in {{c1::1492}}"
                "Field2", "In {{c2::1492}}, Columbus sailed the ocean {{c3::blue}}."
                "Extra", "Some extra info" ]
            "{{cloze:Field1}}{{cloze:Field2}}"
            "{{cloze:Field1}}{{cloze:Field2}}<br>{{Extra}}"
            ""
            (CardHtml.Cloze 2s)
    assertStripped
        "In 1492, Columbus sailed the ocean [ ... ] ."
        front
    assertStripped
        "In 1492, Columbus sailed the ocean [ blue ] .Some extra info"
        back

[<Fact>]
let ``CardHtml renders multiple cloze templates properly 4``(): unit =
    let front, back, _, _ =
        CardHtml.generate
            [   "Field1", "{{c1::Columbus}} first crossed the Atlantic in {{c1::1492}}"
                "Field2", "In 1492, Columbus sailed the ocean blue."
                "Extra", "Some extra info" ]
            "{{cloze:Field1}}{{cloze:Field2}}"
            "{{cloze:Field1}}{{cloze:Field2}}<br>{{Extra}}"
            ""
            (CardHtml.Cloze 0s)
    assertStripped
        "[ ... ] first crossed the Atlantic in [ ... ]"
        front
    assertStripped
        "[ Columbus ] first crossed the Atlantic in [ 1492 ] Some extra info"
        back

        
[<Fact>]
let ``CardHtml renders {{cloze:FieldName}} properly with hint``(): unit =
    let front, back, _, _ =
        CardHtml.generate
            [("Text", "Canberra was founded in {{c1::1913::year}}.")
             ("Extra", "Some extra stuff.")
            ]
            "{{cloze:Text}}"
            """{{cloze:Text}}<br>{{Extra}}"""
            ""
            (CardHtml.Cloze 0s)
    assertBody
        """Canberra was founded in 
        <span class="cloze-brackets-front">[</span>
        <span class="cloze-filler-front">year</span>
        <span class="cloze-brackets-front">]</span>
        ."""
        front
    assertBody
        """Canberra was founded in 
        <span class="cloze-brackets-back">[</span>
        1913
        <span class="cloze-brackets-back">]</span>
        .<br>Some extra stuff."""
        back

[<Fact>]
let ``CardHtml renders multiple cloze templates properly 1 with hint``(): unit =
    let front, back, _, _ =
        CardHtml.generate
            [   "Field1", "Columbus first crossed the Atlantic in {{c1::1492::year}}"
                "Field2", "In {{c2::1492::year}}, Columbus sailed the ocean {{c3::blue::color}}."
                "Extra", "Some extra info" ]
            "{{cloze:Field1}}{{cloze:Field2}}"
            "{{cloze:Field1}}{{cloze:Field2}}<br>{{Extra}}"
            ""
            (CardHtml.Cloze 0s)
    assertStripped
        "Columbus first crossed the Atlantic in [ year ]"
        front
    assertStripped
        "Columbus first crossed the Atlantic in [ 1492 ] Some extra info"
        back

[<Fact>]
let ``CardHtml renders multiple cloze templates properly 2 with hint``(): unit =
    let front, back, _, _ =
        CardHtml.generate
            [   "Field1", "Columbus first crossed the Atlantic in {{c1::1492::year}}"
                "Field2", "In {{c2::1492::year}}, Columbus sailed the ocean {{c3::blue::color}}."
                "Extra", "Some extra info" ]
            "{{cloze:Field1}}{{cloze:Field2}}"
            "{{cloze:Field1}}{{cloze:Field2}}<br>{{Extra}}"
            ""
            (CardHtml.Cloze 1s)
    assertStripped
        "In [ year ] , Columbus sailed the ocean blue."
        front
    assertStripped
        "In [ 1492 ] , Columbus sailed the ocean blue.Some extra info"
        back

[<Fact>]
let ``CardHtml renders multiple cloze templates properly 3 with hint``(): unit =
    let front, back, _, _ =
        CardHtml.generate
            [   "Field1", "Columbus first crossed the Atlantic in {{c1::1492::year}}"
                "Field2", "In {{c2::1492::year}}, Columbus sailed the ocean {{c3::blue::color}}."
                "Extra", "Some extra info" ]
            "{{cloze:Field1}}{{cloze:Field2}}"
            "{{cloze:Field1}}{{cloze:Field2}}<br>{{Extra}}"
            ""
            (CardHtml.Cloze 2s)
    assertStripped
        "In 1492, Columbus sailed the ocean [ color ] ."
        front
    assertStripped
        "In 1492, Columbus sailed the ocean [ blue ] .Some extra info"
        back

[<Fact>]
let ``CardHtml renders multiple cloze templates properly 4 with hint``(): unit =
    let front, back, _, _ =
        CardHtml.generate
            [   "Field1", "{{c1::Columbus::person}} first crossed the Atlantic in {{c1::1492::year}}"
                "Field2", "In 1492, Columbus sailed the ocean blue."
                "Extra", "Some extra info" ]
            "{{cloze:Field1}}{{cloze:Field2}}"
            "{{cloze:Field1}}{{cloze:Field2}}<br>{{Extra}}"
            ""
            (CardHtml.Cloze 0s)
    assertStripped
        "[ person ] first crossed the Atlantic in [ year ]"
        front
    assertStripped
        "[ Columbus ] first crossed the Atlantic in [ 1492 ] Some extra info"
        back

[<Fact>]
let ``TemplateRevision.FrontBackFrontSynthBackSynth works``(): unit =
    let front, back, _, _ = (TemplateRevision.initialize Ulid.create Ulid.create).FrontBackFrontSynthBackSynth() |> Seq.exactlyOne
    assertStripped
        "{{Front}}"
        front
    assertStripped
        "{{Front}} {{Back}}"
        back

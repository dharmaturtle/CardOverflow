module AnkiImportLogicTests

open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System

[<Fact>]
let ``AnkiImportLogic.clozeFields works with one cloze``(): unit =
    let questionTemplate = "{{cloze:Text}}<br>{{Extra}}"
    let expected = "Text"
    
    let actal = AnkiImportLogic.clozeFields questionTemplate |> Seq.exactlyOne
    
    Assert.Equal(expected, actal)

[<Fact>]
let ``AnkiImportLogic.clozeFields works with two clozes``(): unit =
    let questionTemplate = "{{cloze:Field1}}{{cloze:Field2}}<br>{{Extra}}"
    let expected = ["Field1"; "Field2"]
    
    let actual = AnkiImportLogic.clozeFields questionTemplate
    
    Assert.Equal<string seq>(expected, actual)

[<Fact>]
let ``maxClozeIndex doesn't throw given bad data``(): unit =
    let expectedErrorMessage = Guid.NewGuid()
    let actualErrorMessage = AnkiImportLogic.maxClozeIndex expectedErrorMessage Map.empty "" |> Result.getError
    Assert.Equal(expectedErrorMessage, actualErrorMessage)

let run index before expected =
    let index = byte index
    Assert.Equal<string seq>(
        [expected],
        AnkiImportLogic.multipleClozeToSingleCloze index [before])

[<Fact>]
let ``Cloze: c1 is transformed into my cloze``(): unit =
    run 1
        "Canberra was founded in {{c1::1913}}."
        "Canberra was founded in {{c1::1913}}."

[<Fact>]
let ``Cloze: c1 and c2 is transformed into my cloze, index 1``(): unit =
    run 1
        "{{c2::Canberra}} was founded in {{c1::1913}}."
                "Canberra was founded in {{c1::1913}}."

[<Fact>]
let ``Cloze: c1 and c2 is transformed into my cloze, index 2``(): unit =
    run 2
        "{{c2::Canberra}} was founded in {{c1::1913}}."
        "{{c2::Canberra}} was founded in 1913."

[<Fact>]
let ``Cloze: c1 and c1 is transformed into my cloze``(): unit =
    run 1
        "{{c1::Canberra}} was founded in {{c1::1913}}."
        "{{c1::Canberra}} was founded in {{c1::1913}}."

[<Fact>]
let ``Cloze with hint: c1 is transformed into my cloze``(): unit =
    run 1
        "Canberra was founded in {{c1::1913::year}}."
        "Canberra was founded in {{c1::1913::year}}."

[<Fact>]
let ``Cloze with hint: c1 and c2 is transformed into my cloze, index 1``(): unit =
    run 1
        "{{c2::Canberra}} was founded in {{c1::1913::year}}."
                "Canberra was founded in {{c1::1913::year}}."

[<Fact>]
let ``Cloze with hint: c1 and c2 is transformed into my cloze, index 2``(): unit =
    run 2
        "{{c2::Canberra::city}} was founded in {{c1::1913}}."
        "{{c2::Canberra::city}} was founded in 1913."

[<Fact>]
let ``Cloze with hint: c1 and c1 is transformed into my cloze``(): unit =
    run 1
        "{{c1::Canberra::city}} was founded in {{c1::1913}}."
        "{{c1::Canberra::city}} was founded in {{c1::1913}}."

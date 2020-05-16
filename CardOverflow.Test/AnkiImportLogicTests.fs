module AnkiImportLogicTests

open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System

[<Fact>]
let ``AnkiImportLogic.clozeFields works with one cloze``(): unit =
    let questionXemplate = "{{cloze:Text}}<br>{{Extra}}"
    let expected = "Text"
    
    let actal = AnkiImportLogic.clozeFields questionXemplate |> Seq.exactlyOne
    
    Assert.Equal(expected, actal)

[<Fact>]
let ``AnkiImportLogic.clozeFields works with two clozes``(): unit =
    let questionXemplate = "{{cloze:Field1}}{{cloze:Field2}}<br>{{Extra}}"
    let expected = ["Field1"; "Field2"]
    
    let actual = AnkiImportLogic.clozeFields questionXemplate
    
    Assert.Equal<string seq>(expected, actual)

[<Fact>]
let ``maxClozeIndex doesn't throw given bad data``(): unit =
    let expectedErrorMessage = Guid.NewGuid()

    let actualErrorMessage = AnkiImportLogic.maxClozeIndex expectedErrorMessage Map.empty "" |> Result.getError
    
    Assert.Equal(expectedErrorMessage, actualErrorMessage)

[<Fact>]
let ``maxClozeIndex has error with nonconsecutive cloze``(): unit =
    let expectedErrorMessage = Guid.NewGuid()
    let keyvalues =
        [   "Extra", ""
            "Text", "{{c2::stuff}}" ]
        |> Map.ofSeq

    let actualErrorMessage = AnkiImportLogic.maxClozeIndex expectedErrorMessage keyvalues "{{cloze:Text}}" |> Result.getError
    
    Assert.Equal(expectedErrorMessage, actualErrorMessage)

[<Fact>]
let ``maxClozeIndex has error with 0``(): unit =
    let expectedErrorMessage = Guid.NewGuid()
    let keyvalues =
        [   "Extra", ""
            "Text", "{{c0::stuff}}" ]
        |> Map.ofSeq

    let actualErrorMessage = AnkiImportLogic.maxClozeIndex expectedErrorMessage keyvalues "{{cloze:Text}}" |> Result.getError
    
    Assert.Equal(expectedErrorMessage, actualErrorMessage)

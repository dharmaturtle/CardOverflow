module AnkiImportLogicTests

open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System

let run index before expected =
    Assert.Equal(
        expected,
        AnkiImportLogic.multipleClozeToSingleCloze before index
    )

[<Fact>]
let ``Cloze: c1 is transformed into my cloze``(): unit =
    run 1
        "Canberra was founded in {{c1::1913}}."
        "Canberra was founded in {{c::1913}}."

[<Fact>]
let ``Cloze: c1 and c2 is transformed into my cloze, index 1``(): unit =
    run 1
        "{{c2::Canberra}} was founded in {{c1::1913}}."
                "Canberra was founded in {{c::1913}}."

[<Fact>]
let ``Cloze: c1 and c2 is transformed into my cloze, index 2``(): unit =
    run 2
        "{{c2::Canberra}} was founded in {{c1::1913}}."
         "{{c::Canberra}} was founded in 1913."

[<Fact>]
let ``Cloze: c1 and c1 is transformed into my cloze``(): unit =
    run 1
        "{{c1::Canberra}} was founded in {{c1::1913}}."
         "{{c::Canberra}} was founded in {{c::1913}}."

[<Fact>]
let ``Cloze with hint: c1 is transformed into my cloze``(): unit =
    run 1
        "Canberra was founded in {{c1::1913::year}}."
        "Canberra was founded in {{c::1913::year}}."

[<Fact>]
let ``Cloze with hint: c1 and c2 is transformed into my cloze, index 1``(): unit =
    run 1
        "{{c2::Canberra}} was founded in {{c1::1913::year}}."
                "Canberra was founded in {{c::1913::year}}."

[<Fact>]
let ``Cloze with hint: c1 and c2 is transformed into my cloze, index 2``(): unit =
    run 2
        "{{c2::Canberra::city}} was founded in {{c1::1913}}."
         "{{c::Canberra::city}} was founded in 1913."

[<Fact>]
let ``Cloze with hint: c1 and c1 is transformed into my cloze``(): unit =
    run 1
        "{{c1::Canberra::city}} was founded in {{c1::1913}}."
         "{{c::Canberra::city}} was founded in {{c::1913}}."

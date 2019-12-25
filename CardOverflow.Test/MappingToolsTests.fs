module MappingToolsTests

open MappingTools
open Xunit

[<Fact>]
let ``stringOfIntsToIntList with empty string returns empty list`` (): unit =
    stringOfIntsToIntList "" |> Assert.Empty

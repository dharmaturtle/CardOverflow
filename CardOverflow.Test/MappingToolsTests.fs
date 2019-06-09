module MappingToolsTests

open MappingTools
open Xunit

[<Fact>]
let ``stringOfIntsToIntList with empty string returns empty list``() =
    stringOfIntsToIntList "" |> Assert.Empty

module MappingToolsTests

open MappingTools
open Xunit

[<Fact>]
let ``stringOfIntsToIntList with empty string returns empty list`` (): unit =
    stringOfIntsToIntList "" |> Assert.Empty

[<Fact>]
let ``stripHtmlTagsForDisplay replaces <img> with [ Image ]``(): unit =
    let htmlStr = "An ocean soul<img></img>"
    
    let actual = MappingTools.stripHtmlTagsForDisplay htmlStr
    
    Assert.Equal("An ocean soul [ Image ]", actual)

[<Fact>]
let ``stripHtmlTagsForDisplay replaces empty output with [ Empty ]``(): unit =
    let htmlStr = "<div> <span> &nbsp; </span> </div>"
    
    let actual = MappingTools.stripHtmlTagsForDisplay htmlStr
    
    Assert.Equal("[ Empty ]", actual)

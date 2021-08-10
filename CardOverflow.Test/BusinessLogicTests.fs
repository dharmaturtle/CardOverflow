module BusinessLogicTests

open LoadersAndCopiers
open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System
open CardOverflow.Test

[<Theory>]
[<InlineData( 0L, 0)>]
[<InlineData( 1L, 1)>]
[<InlineData(10L, 1)>]
[<InlineData(11L, 2)>]
[<InlineData(19L, 2)>]
[<InlineData(20L, 2)>]
[<InlineData(21L, 3)>]
[<InlineData(3_000_000_000L,
               300_000_000)>] // past int32's max
let ``PagedList.build's PageCount`` total expected =
    let pagedList = PagedList.create [] 0 total 10
    Assert.equal expected pagedList.Details.PageCount

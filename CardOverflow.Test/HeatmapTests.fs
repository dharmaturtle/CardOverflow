module HeatmapTests

open CardOverflow.Api
open CardOverflow.Debug
open System.Linq
open CardOverflow.Pure
open Xunit
open System

let heatmapMaxConseuctiveData: Object [] [] = [|
        [| 0; [0] |]
        [| 0; [0;0;0] |]
        [| 1; [0;1;0] |]
        [| 1; [1;0;1;0;1] |]
        [| 2; [0;1;1;0] |]
        [| 2; [1;0;1;1;0;1;0] |]
        [| 3; [1;1;1;0] |]
        [| 3; [0;1;1;0;1;1;1;0] |]
        [| 4; [0;1;1;1;1;0;1;1;1;0] |]
        [| 4; [0;0;0;1;1;1;0;1;1;1;1;] |]
        [| 4; [0;0;0;213;3135;115656;0;1;13;56;54;] |]
    |]

[<Theory>]
[<MemberData(nameof heatmapMaxConseuctiveData)>]
let ``Heatmap.maxConseuctive works`` (expected: int, xs: int list): unit =
    let actual = Heatmap.maxConseuctive xs
    
    Assert.Equal(expected, actual)

[<Fact>]
let ``Heatmap.allDateCounts works`` (): unit =
    let startDate =  DateTime(2000, 1, 1)
    let endDate =    DateTime(2000, 1, 3)
    let a = { Date = startDate;            Count = 1 }
    let c = { Date = endDate;              Count = 3 }
    let b = { Date = DateTime(2000, 1, 2); Count = 0 }

    let actual = [ a; c ] |> Heatmap.allDateCounts startDate endDate

    Assert.Equal<DateCount seq>([ a; b; c ], actual)

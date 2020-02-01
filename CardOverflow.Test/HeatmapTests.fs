module HeatmapTests

open CardOverflow.Api
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

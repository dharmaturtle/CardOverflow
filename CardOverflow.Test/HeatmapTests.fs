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
    let b = { Date = DateTime(2000, 1, 2); Count = 0 }
    let c = { Date = endDate;              Count = 3 }

    let actual = [ a; c ] |> Heatmap.allDateCounts startDate endDate

    Assert.Equal<DateCount seq>([ a; b; c ], actual)

[<Fact>]
let ``Heatmap.get works 0`` (): unit =
    let startDate =  DateTime(2000, 1, 1)
    let endDate =    DateTime(2000, 1, 4)
    let a =  { Date = startDate;            Count =  0 }
    let d =  { Date = endDate;              Count =  0 }

    let actual = [ a; d ] |> Heatmap.get startDate endDate
    
    let _a = { Date = startDate;            Count =  0 ; Level =  0 }
    let _b = { Date = DateTime(2000, 1, 2); Count =  0 ; Level =  0 }
    let _c = { Date = DateTime(2000, 1, 3); Count =  0 ; Level =  0 }
    let _d = { Date = endDate;              Count =  0 ; Level =  0 }
    Assert.Equal(
        {   DateCountLevels = [ _a; _b; _c; _d ]
            DailyAverageReviews = 0
            DaysLearnedPercent = 0
            LongestStreakDays = 0
            CurrentStreakDays = 0
        }, actual)

[<Fact>]
let ``Heatmap.get works 1`` (): unit =
    let startDate =  DateTime(2000, 1, 1)
    let endDate =    DateTime(2000, 1, 3)
    let a =  { Date = startDate;            Count = 1 }
    let c =  { Date = endDate;              Count = 3 }

    let actual = [ a; c ] |> Heatmap.get startDate endDate
    
    let _a = { Date = startDate;            Count = 1 ; Level =  3 }
    let _b = { Date = DateTime(2000, 1, 2); Count = 0 ; Level =  0 }
    let _c = { Date = endDate;              Count = 3 ; Level = 10 }
    Assert.Equal(
        {   DateCountLevels = [ _a; _b; _c ]
            DailyAverageReviews = 1
            DaysLearnedPercent = 67
            LongestStreakDays = 1
            CurrentStreakDays = 1
        }, actual)
        
[<Fact>]
let ``Heatmap.get works 2`` (): unit =
    let startDate =  DateTime(2000, 1, 1)
    let endDate =    DateTime(2000, 1, 4)
    let a =  { Date = startDate;            Count = 10 }
    let d =  { Date = endDate;              Count =  0 }

    let actual = [ a; d ] |> Heatmap.get startDate endDate
    
    let _a = { Date = startDate;            Count = 10 ; Level = 10 }
    let _b = { Date = DateTime(2000, 1, 2); Count =  0 ; Level =  0 }
    let _c = { Date = DateTime(2000, 1, 3); Count =  0 ; Level =  0 }
    let _d = { Date = endDate;              Count =  0 ; Level =  0 }
    Assert.Equal(
        {   DateCountLevels = [ _a; _b; _c; _d ]
            DailyAverageReviews = 3
            DaysLearnedPercent = 25
            LongestStreakDays = 1
            CurrentStreakDays = 0
        }, actual)

[<Fact>]
let ``Heatmap.get works 3`` (): unit =
    let startDate =  DateTime(2000, 1, 1)
    let endDate =    DateTime(2000, 1, 4)
    let a =  { Date = startDate;            Count =  0 }
    let c =  { Date = DateTime(2000, 1, 3); Count = 10 }
    let d =  { Date = endDate;              Count = 10 }

    let actual = [ a; c; d ] |> Heatmap.get startDate endDate
    
    let _a = { Date = startDate;            Count =  0 ; Level =  0 }
    let _b = { Date = DateTime(2000, 1, 2); Count =  0 ; Level =  0 }
    let _c = { Date = DateTime(2000, 1, 3); Count = 10 ; Level = 10 }
    let _d = { Date = endDate;              Count = 10 ; Level = 10 }
    Assert.Equal(
        {   DateCountLevels = [ _a; _b; _c; _d ]
            DailyAverageReviews = 10
            DaysLearnedPercent = 100
            LongestStreakDays = 2
            CurrentStreakDays = 2
        }, actual)

[<Fact>]
let ``Heatmap.get works 4`` (): unit =
    let startDate   = DateTime(1999, 12, 31)
    let endDate     = DateTime(2000,  1, 10)
    let a =  { Date = startDate;             Count =  0 }
    let b =  { Date = DateTime(2000, 1,  1); Count =  1 }
    let c =  { Date = DateTime(2000, 1,  2); Count =  2 }
    let d =  { Date = DateTime(2000, 1,  3); Count =  3 }
    let e =  { Date = DateTime(2000, 1,  4); Count =  4 }
    let f =  { Date = DateTime(2000, 1,  5); Count =  5 }
    let g =  { Date = DateTime(2000, 1,  6); Count =  6 }
    let h =  { Date = DateTime(2000, 1,  7); Count =  7 }
    let i =  { Date = DateTime(2000, 1,  8); Count =  8 }
    let j =  { Date = DateTime(2000, 1,  9); Count =  9 }
    let k =  { Date = endDate;               Count = 10 }

    let actual = [ a; b; c; d; e; f; g; h; i; j; k ] |> Heatmap.get startDate endDate
    
    let _a =  { Date = startDate;             Count =  0 ; Level =  0 }
    let _b =  { Date = DateTime(2000, 1,  1); Count =  1 ; Level =  1 }
    let _c =  { Date = DateTime(2000, 1,  2); Count =  2 ; Level =  2 }
    let _d =  { Date = DateTime(2000, 1,  3); Count =  3 ; Level =  3 }
    let _e =  { Date = DateTime(2000, 1,  4); Count =  4 ; Level =  4 }
    let _f =  { Date = DateTime(2000, 1,  5); Count =  5 ; Level =  5 }
    let _g =  { Date = DateTime(2000, 1,  6); Count =  6 ; Level =  6 }
    let _h =  { Date = DateTime(2000, 1,  7); Count =  7 ; Level =  7 }
    let _i =  { Date = DateTime(2000, 1,  8); Count =  8 ; Level =  8 }
    let _j =  { Date = DateTime(2000, 1,  9); Count =  9 ; Level =  9 }
    let _k =  { Date = endDate;               Count = 10 ; Level = 10 }

    Assert.Equal(
        {   DateCountLevels = [ _a; _b; _c; _d; _e; _f; _g; _h; _i; _j; _k ]
            DailyAverageReviews = 6
            DaysLearnedPercent = 100
            LongestStreakDays = 10
            CurrentStreakDays = 10
        }, actual)

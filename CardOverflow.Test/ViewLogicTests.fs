module ViewLogicTests

open Xunit
open System

[<Fact>]
let ``TimeSpan to string looks pretty``() =
    TimeSpan.FromSeconds 10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("1 min", x)
    TimeSpan.FromMinutes 10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("10 min", x)
    TimeSpan.FromHours   10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("10 h", x)
    TimeSpan.FromDays    10.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("10 d", x)
    TimeSpan.FromDays   100.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("3.3 mo", x)
    TimeSpan.FromDays  1000.123456789 |> ViewLogic.toString |> fun x -> Assert.Equal("2.7 yr", x)

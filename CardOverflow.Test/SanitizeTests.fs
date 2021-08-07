module SanitizeTests

open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System
open CardOverflow.Test
open NodaTime

[<Fact>]
let ``Minutes.fromString 10 = 10m`` (): unit =
    let actual = Minutes.fromString "10"
    let expected = Duration.FromMinutes 10.
    Assert.Equal(expected, actual)

[<Fact>]
let ``Minutes.fromStringList "1 10" = 10m`` (): unit =
    let actual = Minutes.fromStringList "1 10"
    let expected = [ 1. ; 10. ] |> List.map Duration.FromMinutes
    Assert.Equal<Duration seq>(expected, actual)
    
[<Fact>]
let ``Minutes.toString 10 = 10`` (): unit =
    let actual = 10. |> Duration.FromMinutes |> Minutes.toString
    let expected = "10"
    Assert.Equal(expected, actual)

[<Fact>]
let ``Minutes.toStringList [1 10] = "1 10"``(): unit =
    let actual = [ 1. ; 10. ] |> List.map Duration.FromMinutes |> Minutes.toStringList
    let expected = "1 10"
    Assert.Equal(expected, actual)

[<Fact>]
let ``Convert.toPercent 0.5 = 50``(): unit =
    let actual = Convert.toPercent 0.5
    let expected = 50
    Assert.Equal(expected, actual)

[<Fact>]
let ``Convert.fromPercent 50 = 0.5``(): unit =
    let actual = Convert.fromPercent 50
    let expected = 0.5
    Assert.Equal(expected, actual)

[<Fact>]
let ``ViewCardSetting.load and copyTo reverse each other``(): unit =
    let id = Guid.NewGuid()
    let actual = CardSetting.newUserCardSettings id |> ViewCardSetting.load |> fun x -> x.copyTo
    Assert.Equal(CardSetting.newUserCardSettings id, actual)

module SanitizeTests

open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System
open CardOverflow.Sanitation
open CardOverflow.Test

[<Fact>]
let ``GetCardId fails on bork``(): unit =
    SanitizeRelationshipRepository.GetCardId "bork"
    |> Result.isOk
    |> Assert.False

[<Fact>]
let ``Minutes.fromString 10 = 10m``() =
    let actual = Minutes.fromString "10"
    let expected = TimeSpan.FromMinutes 10.
    Assert.Equal(expected, actual)

[<Fact>]
let ``Minutes.fromStringList "1 10" = 10m``() =
    let actual = Minutes.fromStringList "1 10"
    let expected = [ 1. ; 10. ] |> List.map TimeSpan.FromMinutes
    Assert.Equal<TimeSpan seq>(expected, actual)
    
[<Fact>]
let ``Minutes.toString 10 = 10``() =
    let actual = 10. |> TimeSpan.FromMinutes |> Minutes.toString
    let expected = "10"
    Assert.Equal(expected, actual)

[<Fact>]
let ``Minutes.toStringList [1 10] = "1 10"``() =
    let actual = [ 1. ; 10. ] |> List.map TimeSpan.FromMinutes |> Minutes.toStringList
    let expected = "1 10"
    Assert.Equal(expected, actual)

[<Fact>]
let ``Convert.toPercent 0.5 = 50``() =
    let actual = Convert.toPercent 0.5
    let expected = 50
    Assert.Equal(expected, actual)

[<Fact>]
let ``Convert.fromPercent 50 = 0.5``() =
    let actual = Convert.fromPercent 50
    let expected = 0.5
    Assert.Equal(expected, actual)

[<Fact>]
let ``ViewCardOption.load and copyTo reverse each other``() =
    let actual = CardOptionsRepository.defaultCardOptions |> ViewCardOption.load |> fun x -> x.copyTo
    Assert.Equal(CardOptionsRepository.defaultCardOptions, actual)

type GetCardIdIsOkData () =
    inherit XunitClassDataBase
        ([  [| 123; "123" |]
            [| 12 ; "www.cardoverflow.com/card/12" |]
            [| 11 ; "www.cardoverflow.com:19/curate/card/11" |] ])

[<Theory>]
[<ClassData(typeof<GetCardIdIsOkData>)>]
let ``GetCardId works`` expected raw : unit =
    SanitizeRelationshipRepository.GetCardId raw
    |> Result.getOk
    |> fun x -> Assert.Equal(expected, x)

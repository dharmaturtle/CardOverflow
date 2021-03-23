module SanitizeTests

open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System
open CardOverflow.Sanitation
open CardOverflow.Test
open NodaTime

[<Fact>]
let ``GetConceptId fails on bork``(): unit =
    SanitizeRelationshipRepository.GetConceptId "bork"
    |> Result.isOk
    |> Assert.False

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

type GetCardIdIsOkData () =
    inherit XunitClassDataBase
        ([  [| Guid.Parse("05A4E537-FF49-49A5-827E-77F9280D7D54") ; "05A4E537-FF49-49A5-827E-77F9280D7D54" |]
            [| Guid.Parse("3360F6FF-C342-4575-A1FA-C5A43BE111EB") ; "www.cardoverflow.com/card/3360F6FF-C342-4575-A1FA-C5A43BE111EB" |]
            [| Guid.Parse("D9AD6A65-C645-4E50-9374-749C4BF213EA") ; "www.cardoverflow.com:19/curate/card/D9AD6A65-C645-4E50-9374-749C4BF213EA" |] ])

[<Theory>]
[<ClassData(typeof<GetCardIdIsOkData>)>]
let ``GetConceptId works`` expected raw : unit =
    SanitizeRelationshipRepository.GetConceptId raw
    |> Result.getOk
    |> Assert.equal expected

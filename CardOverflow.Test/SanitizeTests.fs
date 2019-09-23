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

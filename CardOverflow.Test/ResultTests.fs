module ResultTests

open CardOverflow.Api
open Xunit
open CardOverflow.Pure

[<Fact>]
let ``Consolidating list of Ok ints is Ok`` (): unit =
    [ 0; 1 ] |> List.map Ok
    
    |> Result.consolidateString
    
    |> Result.isOk |> Assert.True

[<Fact>]
let ``Consolidating list of Ok ints yields ints`` (): unit =
    let expected = [ 0; 1 ]
    
    let actual = expected |> List.map Ok |> Result.consolidateString
    
    Assert.Equal<int>(expected, actual |> Result.getOk)

[<Fact>]
let ``Consolidating list of Errors is not Ok`` (): unit =
    ["A"; "B"] |> List.map Error
    
    |> Result.consolidateString |> Result.isOk
    
    |> Assert.False

[<Fact>]
let ``Consolidating list of Errors yields concatenated errors`` (): unit =
    let actual =
        ["A"; "B"]
        |> List.map Error
        |> Result.consolidateString

    Assert.Equal("A\r\nB", actual |> Result.getError)

[<Fact>]
let ``Consolidating list of int and Error is not Ok``(): unit =
    [ Ok 0; Error "" ]
    
    |> Result.consolidateString |> Result.isOk
    
    |> Assert.False

[<Fact>]
let ``Consolidating list of int and Errors yields concatenated errors``(): unit =
    let actual =
        [ Ok 0
          Error "B"
          Error "C" ]
        |> Result.consolidateString

    Assert.Equal("B\r\nC", actual |> Result.getError)

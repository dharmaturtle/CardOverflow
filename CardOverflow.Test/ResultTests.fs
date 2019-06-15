module ResultTests

open CardOverflow.Api
open Xunit
open CardOverflow.Pure

[<Fact>]
let ``Consolidating list of Ok ints is Ok``() =
    [ 0; 1 ] |> List.map Ok
    
    |> Result.consolidate
    
    |> Result.isOk |> Assert.True

[<Fact>]
let ``Consolidating list of Ok ints yields ints``() =
    let expected = [ 0; 1 ]
    
    let actual = expected |> List.map Ok |> Result.consolidate
    
    Assert.Equal<int>(expected, actual |> Result.getOk)

[<Fact>]
let ``Consolidating list of Errors is not Ok``() =
    ["A"; "B"] |> List.map Error
    
    |> Result.consolidate |> Result.isOk
    
    |> Assert.False

[<Fact>]
let ``Consolidating list of Errors yields concatenated errors``() =
    let actual =
        ["A"; "B"]
        |> List.map Error
        |> Result.consolidate

    Assert.Equal("A\r\nB", actual |> Result.getError)

[<Fact>]
let ``Consolidating list of int and Error is not Ok``() =
    [ Ok 0; Error "" ]
    
    |> Result.consolidate |> Result.isOk
    
    |> Assert.False

[<Fact>]
let ``Consolidating list of int and Errors yields concatenated errors``() =
    let actual =
        [ Ok 0
          Error "B"
          Error "C" ]
        |> Result.consolidate

    Assert.Equal("B\r\nC", actual |> Result.getError)

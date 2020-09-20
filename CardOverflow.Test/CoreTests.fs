module CoreTests

open CardOverflow.Api
open CardOverflow.Pure
open Xunit
open System
open FsCheck.Xunit
open FsCheck
open CardOverflow.Test
open CardOverflow.Debug

[<Fact>]
let ``Random.cryptographicString is somewhat random``(): unit =
    let l = 10
    
    let s1 = Random.cryptographicString l
    let s2 = Random.cryptographicString l
    
    Assert.NotEqual<string>(s1, s2)

[<Fact>]
let ``Random.cryptographicString of sufficient length contains A, a, 1, _, and -``(): unit =
    let l = 1000

    let s = Random.cryptographicString l

    Assert.Contains("A", s)
    Assert.Contains("a", s)
    Assert.Contains("1", s)
    Assert.Contains("-", s)
    Assert.Contains("_", s)

[<Fact>]
let ``Random.cryptographicString produces a string of the specified length``(): unit =
    let r = new Random()
    let l = r.Next(1, 1000)
    
    let s = Random.cryptographicString l

    Assert.Equal(l, s.Length)
    
[<Property>]
let ``Ulid.resizeList works`` (target: PositiveInt) (tail: PositiveInt): unit =
    let targetLength = target.Get
    let tailLength = tail.Get - 1
    let head = Ulid.create
    let tail = Ulid.createMany tailLength

    let resized = (head :: tail) |> Ulid.resizeList targetLength
    
    Assert.equal targetLength resized.Length
    Assert.equal resized (resized |> List.distinct)
    Assert.equal head resized.Head

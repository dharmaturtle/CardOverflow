module CoreTests

open CardOverflow.Api
open Xunit
open System

[<Fact>]
let ``Random.cryptographicString is somewhat random``() =
    let l = 10
    
    let s1 = Random.cryptographicString l
    let s2 = Random.cryptographicString l
    
    Assert.NotEqual<string>(s1, s2)

[<Fact>]
let ``Random.cryptographicString of sufficient length contains A, a, 1, _, and -``() =
    let l = 1000

    let s = Random.cryptographicString l

    Assert.Contains("A", s)
    Assert.Contains("a", s)
    Assert.Contains("1", s)
    Assert.Contains("-", s)
    Assert.Contains("_", s)

[<Fact>]
let ``Random.cryptographicString produces a string of the specified length``() =
    let r = new Random()
    let l = r.Next(1, 1000)
    
    let s = Random.cryptographicString l

    Assert.Equal(l, s.Length)

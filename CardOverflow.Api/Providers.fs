namespace CardOverflow.Api

open System

type TimeProvider () =
    member __.utcNow = DateTime.UtcNow

type RandomProvider () =
    let r = Random()
    member __.float(minInclusive, maxExclusive) = // https://stackoverflow.com/questions/1064901
        r.NextDouble() * (maxExclusive - minInclusive) + minInclusive

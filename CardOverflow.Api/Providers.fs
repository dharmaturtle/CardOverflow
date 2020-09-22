namespace CardOverflow.Api

open System
open NodaTime

type TimeProvider () =
    member __.utcNow = SystemClock.Instance.GetCurrentInstant()

type RandomProvider () =
    let r = Random()
    member __.float(minInclusive, maxExclusive) = // https://stackoverflow.com/questions/1064901
        r.NextDouble() * (maxExclusive - minInclusive) + minInclusive

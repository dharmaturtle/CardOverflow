namespace CardOverflow.Api

open System

type ITimeProvider =
    abstract UtcNow: DateTime

type TimeProvider() =
    interface ITimeProvider with
        member __.UtcNow = DateTime.UtcNow

type IRandomProvider =
    abstract GetRandomFloat: float * float -> float

type RandomProvider() =
    let r = Random()
    interface IRandomProvider with
        member __.GetRandomFloat(minInclusive, maxExclusive) = // https://stackoverflow.com/questions/1064901
            r.NextDouble() * (maxExclusive - minInclusive) + minInclusive

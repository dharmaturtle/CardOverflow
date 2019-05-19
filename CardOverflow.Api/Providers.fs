namespace CardOverflow.Api

open System

type TimeProvider = DateTime

module TimeProvider =
    let utcNow = DateTime.UtcNow

type RandomFloatProvider = float * float -> float

module RandomProvider =
    let r = Random()
    let randomFloat(minInclusive, maxExclusive) = // https://stackoverflow.com/questions/1064901
        r.NextDouble() * (maxExclusive - minInclusive) + minInclusive

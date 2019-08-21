module LoadersAndCopiersTests

open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
open ContainerExtensions
open System
open Xunit
open SimpleInjector
open System.Data.SqlClient
open System.IO
open System.Linq
open SimpleInjector.Lifestyles

[<Fact>]
let ``CardOptions load and copy defaultCardOptions are equal``() =
    let record = UserRepository.defaultCardOptions.CopyToNew 3 |> CardOption.Load

    Assert.Equal(UserRepository.defaultCardOptions, record)

[<Fact>]
let ``Interval, all NewStepsIndexes map to db and back``() =
    // lowTODO assert that the type of NewStepsIndex is a unsigned byte
    for i in [ Byte.MinValue .. Byte.MaxValue ] do
        NewStepsIndex i
        |> AcquiredCard.intervalToDb
        |> AcquiredCard.intervalFromDb
        |> function
        | NewStepsIndex x -> Assert.Equal(i, x)
        | LapsedStepsIndex x -> failwithf "%A" x
        | Interval x -> failwithf "%A" x

[<Fact>]
let ``Interval, all LapsedStepsIndexes map to db and back``() =
    // lowTODO assert that the type of LapsedStepsIndex is a unsigned byte
    for i in [ Byte.MinValue .. Byte.MaxValue ] do
        LapsedStepsIndex i
        |> AcquiredCard.intervalToDb
        |> AcquiredCard.intervalFromDb
        |> function
        | NewStepsIndex x -> failwithf "%A" x
        | LapsedStepsIndex x -> Assert.Equal(i, x)
        | Interval x -> failwithf "%A" x

[<Fact>]
let ``Interval, all minutes map to db and back``() =
    for i in [ 0. .. 1440. ] do
        let i = TimeSpan.FromMinutes i
        Interval i
        |> AcquiredCard.intervalToDb
        |> AcquiredCard.intervalFromDb
        |> function
        | NewStepsIndex x -> failwithf "%A" x
        | LapsedStepsIndex x -> failwithf "%A" x
        | Interval x -> Assert.Equal(i, x)

[<Fact>]
let ``Interval, first 100 days map to db and back``() =
    for i in [ 1. .. 100. ] do
        let i = TimeSpan.FromDays i
        Interval i
        |> AcquiredCard.intervalToDb
        |> AcquiredCard.intervalFromDb
        |> function
        | NewStepsIndex x -> failwithf "%A" x
        | LapsedStepsIndex x -> failwithf "%A" x
        | Interval x -> Assert.Equal(i, x)

[<Fact>]
let ``Interval, last 100 days map to db and back``() =
    let minutesInADay = TimeSpan.FromDays(1.).TotalMinutes
    let n1 = Int16.MinValue + int16 Byte.MaxValue |> float
    let l0 = n1 + 1.
    let l1 = l0 + float Byte.MaxValue
    let m0 = l1 + 1.
    let d0 = m0 + float minutesInADay  // see implementation for what d0 means
    let maxValue = Math.Abs(float d0) + float Int16.MaxValue
    for i in [ maxValue-100. .. maxValue ] do
        let i = TimeSpan.FromDays i
        Interval i
        |> AcquiredCard.intervalToDb
        |> AcquiredCard.intervalFromDb
        |> function
        | NewStepsIndex x -> failwithf "%A" x
        | LapsedStepsIndex x -> failwithf "%A" x
        | Interval x -> Assert.Equal(i, x)

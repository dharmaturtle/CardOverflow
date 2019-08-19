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
let ``Interval, all step indexes map to db and back``() =
    // lowTODO assert that the type of StepIndex is a unsigned byte
    for i in [ Byte.MinValue .. Byte.MaxValue ] do
        Choice1Of2 i
        |> AcquiredCard.intervalToDb
        |> AcquiredCard.intervalFromDb
        |> function
        | Choice1Of2 x -> Assert.Equal(i, x)
        | Choice2Of2 x -> failwithf "%A" x

[<Fact>]
let ``Interval, all minutes map to db and back``() =
    for i in [ 0. .. 1440. ] do
        let i = TimeSpan.FromMinutes i
        Choice2Of2 i
        |> AcquiredCard.intervalToDb
        |> AcquiredCard.intervalFromDb
        |> function
        | Choice1Of2 x -> failwithf "%A" x
        | Choice2Of2 x -> Assert.Equal(i, x)

[<Fact>]
let ``Interval, first 100 days map to db and back``() =
    for i in [ 1. .. 100. ] do
        let i = TimeSpan.FromDays i
        Choice2Of2 i
        |> AcquiredCard.intervalToDb
        |> AcquiredCard.intervalFromDb
        |> function
        | Choice1Of2 x -> failwithf "%A" x
        | Choice2Of2 x -> Assert.Equal(i, x)

[<Fact>]
let ``Interval, last 100 days map to db and back``() =
    let d0 = Int16.MinValue + int16 Byte.MaxValue + int16 (TimeSpan.FromDays(1.).TotalMinutes) // see implementation for what d0 means
    let maxValue = Math.Abs(float d0) + float Int16.MaxValue
    for i in [ maxValue-100. .. maxValue ] do
        let i = TimeSpan.FromDays i
        Choice2Of2 i
        |> AcquiredCard.intervalToDb
        |> AcquiredCard.intervalFromDb
        |> function
        | Choice1Of2 x -> failwithf "%A" x
        | Choice2Of2 x -> Assert.Equal(i, x)

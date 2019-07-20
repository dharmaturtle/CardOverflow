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

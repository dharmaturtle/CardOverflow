module TagRepositoryTests

open CardOverflow.Entity
open CardOverflow.Debug
open CardOverflow.Pure
open Microsoft.EntityFrameworkCore
open CardOverflow.Api
open CardOverflow.Test
open System
open System.Linq
open Xunit
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Control.Tasks
open FsToolkit.ErrorHandling
open LoadersAndCopiers

open TagRepository
[<Fact>]
let ``Tag "a" parses ``(): unit =
    ["a"]

    |> TagRepository.parse

    |> Assert.equal
        [{  Id = "a"
            ParentId = ""
            Name = "a"
            IsExpanded = false
            HasChildren = false }]

[<Fact>]
let ``Tags "a" and "a/b" parse``(): unit =
    let a = "a"
    let a_b = a +/+ "b"
    [a; a_b]

    |> TagRepository.parse

    |> Assert.equal
        [{  Id = "a"
            ParentId = ""
            Name = "a"
            IsExpanded = false
            HasChildren = true }
         {  Id = "a/b"
            ParentId = "a"
            Name = "b"
            IsExpanded = false
            HasChildren = false }]

[<Fact>]
let ``Tag "a/b" parses``(): unit =
    "a" +/+ "b"
    |> List.singleton

    |> TagRepository.parse

    |> Assert.equal
        [{  Id = "a"
            ParentId = ""
            Name = "a"
            IsExpanded = false
            HasChildren = true }
         {  Id = "a/b"
            ParentId = "a"
            Name = "b"
            IsExpanded = false
            HasChildren = false }]

[<Fact>]
let ``Tag "a/b/c" parses``(): unit =
    "a" +/+ "b" +/+ "c"
    |> List.singleton

    |> TagRepository.parse

    |> Assert.equal
        [{  Id = "a"
            ParentId = ""
            Name = "a"
            IsExpanded = false
            HasChildren = true }
         {  Id = "a/b"
            ParentId = "a"
            Name = "b"
            IsExpanded = false
            HasChildren = true }
         {  Id = "a/b/c"
            ParentId = "a/b"
            Name = "c"
            IsExpanded = false
            HasChildren = false }]

//[<Theory>]
//[<InlineData("a/b/c", "A/B/C")>]
//[<InlineData(" a / b / c ", "A/B/C")>]
//[<InlineData(" ax / by / cz ", "Ax/By/Cz")>]
//[<InlineData(" aX / bY / cZ ", "Ax/By/Cz")>]
//[<InlineData("! aX @/ #bY &/% cZ        & ",
//             "! Ax @/#By &/% Cz &")>]
//let ``SanitizeTagRepository.sanitize works`` input expected: unit =
//    input

//    |> SanitizeTagRepository.sanitize

//    |> Assert.equal expected

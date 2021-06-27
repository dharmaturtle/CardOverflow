module ConceptRepositoryTests

open CardOverflow.Pure
open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Api
open ContainerExtensions
open LoadersAndCopiers
open Helpers
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Test
open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Quotations
open System.Linq
open Xunit
open System
open SimpleInjector
open SimpleInjector.Lifestyles
open System.Diagnostics
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Sanitation
open System.Collections
open System.Security.Cryptography
open FsToolkit.ErrorHandling
open FSharp.UMX
open Domain

let sanitizeSearchData: Object [] [] = [|
        [| "{{c1::cloze deletion}}"
           "{{c1::cloze deletion}}"
           "" |]
        [| "wild*"
           ""
           "wild:*" |]
        [| "0wild*"
           "0"
           "wild:*" |]
        [| "0wild* word"
           "0 word"
           "wild:*" |]
        [| "zombie 0wild* word"
           "zombie 0 word"
           "wild:*" |]
        [| "wild*card"
           "wild*card"
           "" |]
        [| "*wild"
           "*wild"
           "" |]
        [| "wild*."
           "wild*."
           "" |]
        [| "wild* card"
           " card"
           "wild:*" |]
        [| "wild* card*"
           " "
           "wild:* card:*" |]
    |]
[<Theory>]
[<MemberData(nameof sanitizeSearchData)>]
let ``Sanitize search`` (input: string, expectedPlain: string, expectedWildcards: string): unit =
    let actualPlain, actualWildcards = FullTextSearch.parse input

    Assert.Equal(expectedPlain, actualPlain)
    Assert.Equal(expectedWildcards, actualWildcards)

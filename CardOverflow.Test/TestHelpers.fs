namespace CardOverflow.Test

open LoadersAndCopiers
open Helpers
open CardOverflow.Api
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

module Assert =
    let SingleI x =
        Assert.Single x |> ignore

type XunitClassDataBase(generator : obj [] seq) = // https://stackoverflow.com/questions/35026735/
    interface seq<obj []> with
        member __.GetEnumerator() = generator.GetEnumerator()
        member __.GetEnumerator() = 
            generator.GetEnumerator() :> System.Collections.IEnumerator

[<AutoOpen>]
module Extensions =
    type Result<'a, 'b> with
        member this.Value =
            Result.getOk this

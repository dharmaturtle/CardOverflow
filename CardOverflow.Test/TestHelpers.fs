namespace CardOverflow.Test

open System.Threading.Tasks
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
open FsToolkit.ErrorHandling
open FSharp.Control.Tasks
open CardOverflow.Debug

module Assert =
    let SingleI x =
        Assert.Single x |> ignore
    let areEquivalent (x: 'T seq) (y: 'T seq) =
        Assert.Equal<'T>
            (x |> Seq.sort |> List.ofSeq
            ,y |> Seq.sort |> List.ofSeq)
    let equal (x: 'T) (y: 'T) =
        try
            Assert.Equal<'T>(x, y)
        with
            | _ ->
                Diff.ToConsole(sprintf "%A" x,
                               sprintf "%A" y)
                reraise()
    let dateTimeEqual delta (x: DateTime) (y: DateTime) =
        Math.Abs((x - y).TotalSeconds) < delta
        |> Assert.True

type XunitClassDataBase(generator : obj [] seq) = // https://stackoverflow.com/questions/35026735/
    interface seq<obj []> with
        member _.GetEnumerator() = generator.GetEnumerator()
        member _.GetEnumerator() = 
            generator.GetEnumerator() :> System.Collections.IEnumerator

[<AutoOpen>]
module Extensions =
    type Result<'a, 'b> with
        member this.Value =
            Result.getOk this
        member this.error =
            Result.getError this

module TaskResult =
    let getOk x =
        x |> Task.map Result.getOk
    let getError x =
        x |> Task.map Result.getError

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
open System.Collections

module internal Assert =
    let SingleI x =
        Assert.Single x |> ignore
    let areEquivalent (x: 'T seq) (y: 'T seq) =
        Assert.Equal<'T>
            (x |> Seq.sort |> List.ofSeq
            ,y |> Seq.sort |> List.ofSeq)
    let areEquivalentCustom equalityComparer (x: 'T seq) (y: 'T seq) =
        Assert.Equal<'T>
            (x |> Seq.sort |> toResizeArray // toResizeArray needed because otherwise the equalityComparer isn't used for some reason
            ,y |> Seq.sort |> toResizeArray
            , equalityComparer)
    let equal (x: 'T) (y: 'T) =
        try
            Assert.Equal<'T>(x, y)
        with
            | _ ->
                printfn "\r\n   ===   Equality check failed!   ==="
                Diff.ToConsole(sprintf "%A" x,
                               sprintf "%A" y)
                reraise()
    let equalCustom<'T> equalityComparer (x: 'T) y =
        try
            Assert.Equal(x, y, equalityComparer)
        with
            | _ ->
                printfn "\r\n   ===   Equality check failed!   ==="
                Diff.ToConsole(sprintf "%A" x,
                               sprintf "%A" y)
                reraise()
    let equalsCustom<'T> equalityComparer (x: 'T seq) (y: 'T seq) =
        try
            Assert.Equal<'T>(x.ToList(), y.ToList(), equalityComparer) // .ToList() needed because otherwise the equalityComparer isn't used for some reason
        with
            | _ ->
                printfn "\r\n   ===   Equality check failed!   ==="
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

[<AutoOpen>]
module Constants =
    let newGuid        = Guid.NewGuid()
    let user_          = sprintf    "00000000-0000-0000-0000-00000000000%i" >> Guid.Parse
    let gromplate_     = sprintf    "00000000-0000-0000-0000-7e390000000%i" >> Guid.Parse
    let grompleaf_     = sprintf    "00000000-0000-0000-0000-7e390000100%i" >> Guid.Parse
    let stack_         = sprintf    "00000000-0000-0000-0000-57ac0000000%i" >> Guid.Parse
    let branch_        = sprintf    "00000000-0000-0000-0000-b12a0000000%i" >> Guid.Parse
    let leaf_          = sprintf    "00000000-0000-0000-0000-1eaf0000000%i" >> Guid.Parse
    let card_          = sprintf    "00000000-0000-0000-0000-ca12d000000%i" >> Guid.Parse
    let deck_          = sprintf    "00000000-0000-0000-0000-decc0000000%i" >> Guid.Parse
    let notification_  = sprintf    "00000000-0000-0000-0000-A1071f00000%i" >> Guid.Parse
    let setting_       = sprintf    "00000000-0000-0000-0000-5e770000000%i" >> Guid.Parse
    let commield_      = sprintf    "00000000-0000-0000-0000-c033f1e1d00%i" >> Guid.Parse
    let commeaf_       = sprintf    "00000000-0000-0000-0000-c0331eaf000%i" >> Guid.Parse
    let user_1         = user_ 1
    let user_2         = user_ 2
    let user_3         = user_ 3
    let gromplate_1    = gromplate_ 1
    let gromplate_2    = gromplate_ 2
    let gromplate_3    = gromplate_ 3
    let gromplate_4    = gromplate_ 4
    let gromplate_5    = gromplate_ 5
    let grompleaf_1    = grompleaf_ 1
    let grompleaf_2    = grompleaf_ 2
    let grompleaf_3    = grompleaf_ 3
    let grompleaf_4    = grompleaf_ 4
    let grompleaf_5    = grompleaf_ 5
    let grompleaf_6    = grompleaf_ 6
    let grompleaf_7    = grompleaf_ 7
    let stack_1        = stack_ 1
    let stack_2        = stack_ 2
    let stack_3        = stack_ 3
    let branch_1       = branch_ 1
    let branch_2       = branch_ 2
    let branch_3       = branch_ 3
    let leaf_1         = leaf_ 1
    let leaf_2         = leaf_ 2
    let leaf_3         = leaf_ 3
    let card_1         = card_ 1
    let card_2         = card_ 2
    let card_3         = card_ 3
    let deck_1         = deck_ 1
    let deck_2         = deck_ 2
    let deck_3         = deck_ 3
    let notification_1 = notification_ 1
    let notification_2 = notification_ 2
    let notification_3 = notification_ 3
    let setting_1      = setting_ 1
    let setting_2      = setting_ 2
    let setting_3      = setting_ 3
    let commield_1     = commield_ 1
    let commield_2     = commield_ 2
    let commield_3     = commield_ 3
    let commeaf_1      = commeaf_ 1
    let commeaf_2      = commeaf_ 2
    let commeaf_3      = commeaf_ 3

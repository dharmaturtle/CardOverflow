namespace CardOverflow.Api

open CardOverflow.Debug
open MappingTools
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Pure.Core
open System
open System.Linq
open FsToolkit.ErrorHandling
open System.Security.Cryptography
open System.Text
open System.Collections.Generic
open System.Text.RegularExpressions
open System.Collections
open NUlid
open NodaTime

type Ulid =
    static member create with get () = Ulid.NewUlid().ToGuid()
    static member infinite with get () = Seq.initInfinite (fun _ -> Ulid.create)
    static member resizeList i xs = 
        List.resize i Ulid.infinite xs
    static member createMany i = [1..i] |> List.map (fun _ -> Ulid.create)

type UpsertIdsModule =
    static member create with get () =
        {   ConceptId = Ulid.create
            ExampleId = Ulid.create
            LeafId = Ulid.create
            CardIds = [ Ulid.create ] }

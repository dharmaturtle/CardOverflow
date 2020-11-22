[<AutoOpen>]
module Domain.Infrastructure

open FSharp.UMX
open System

type AuthorId = Guid<authorId>
    and [<Measure>] authorId

type StackId = Guid<stackId>
    and [<Measure>] stackId
type BranchId = Guid<branchId>
    and [<Measure>] branchId
type LeafId = Guid<leafId>
    and [<Measure>] leafId

type GromplateId = Guid<gromplateId>
    and [<Measure>] gromplateId
type GrompleafId = Guid<grompleafId>
    and [<Measure>] grompleafId

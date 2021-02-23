module StackTests

open Xunit
open CardOverflow.Pure
open Serilog
open System
open Domain
open Equinox.MemoryStore
open FSharp.UMX
open FsCheck.Xunit
open CardOverflow.Pure
open CardOverflow.Test
open EventWriter
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp

[<StandardProperty>]
let ``ChangeDefaultBranch works`` (authorId, { NewOriginal = s; NewBranch = b; BranchTitle = _ }) = asyncResult {
    let c = TestEsContainer()
    let stackBranchWriter = c.StackBranchWriter()
    let stackWriter = c.StackWriter()
    do! stackBranchWriter.Upsert(authorId, s)
    do! stackBranchWriter.Upsert(authorId, b)

    do! stackWriter.ChangeDefaultBranch (% s.Ids.StackId) (% b.Ids.BranchId) authorId

    % b.Ids.StackId
    |> c.StackEvents
    |> Seq.last
    |> Assert.equal (Stack.Events.DefaultBranchChanged { BranchId = % b.Ids.BranchId })
    }

[<StandardProperty>]
let ``ChangeDefaultBranch fails when branch is on a different stack`` (authorId, { NewOriginal = s1; NewBranch = _; BranchTitle = _ }, { NewOriginal = s2; NewBranch = b2; BranchTitle = _ }) = asyncResult {
    let c = TestEsContainer()
    let stackBranchWriter = c.StackBranchWriter()
    let stackWriter = c.StackWriter()
    do! stackBranchWriter.Upsert(authorId, s1)
    do! stackBranchWriter.Upsert(authorId, s2)
    do! stackBranchWriter.Upsert(authorId, b2)

    do! stackWriter.ChangeDefaultBranch (% s1.Ids.StackId) (% b2.Ids.BranchId) authorId
        
    |>% Result.getError
    |>% Assert.equal $"Branch {b2.Ids.BranchId} doesn't belong to Stack {s1.Ids.StackId}"
    }

[<StandardProperty>]
let ``ChangeDefaultBranch fails when branch author tries to be default`` (stackAuthorId, branchAuthorId, { NewOriginal = s; NewBranch = b; BranchTitle = _ }) = asyncResult {
    let c = TestEsContainer()
    let stackBranchWriter = c.StackBranchWriter()
    let stackWriter = c.StackWriter()
    do! stackBranchWriter.Upsert(stackAuthorId,  s)
    do! stackBranchWriter.Upsert(branchAuthorId, b)

    do! stackWriter.ChangeDefaultBranch (% s.Ids.StackId) (% b.Ids.BranchId) branchAuthorId

    |>% Result.getError
    |>% Assert.equal $"Stack {s.Ids.StackId} doesn't belong to User {branchAuthorId}"
    }

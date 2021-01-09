module StackBranchTests

open Xunit
open Serilog
open System
open Domain
open Equinox.MemoryStore
open FSharp.UMX
open FsCheck.Xunit
open CardOverflow.Pure
open CardOverflow.Test
open FsCodec
open EventService

[<Generators>]
let ``StackBranch.Service.Upsert persists both snapshots`` (authorId, command, tags) =
    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags }
    let c = TestEsContainer()
    let stackBranchService = c.StackBranchService()
    let expectedStack, expectedBranch = StackBranch.stackBranch authorId command None tags "Default"
    
    stackBranchService.Upsert(authorId, command)
    
    |> RunSynchronously.OkEquals ()
    % expectedStack.Id
    |> c.StackEvents
    |> Assert.Single
    |> Assert.equal (Stack.Events.Snapshotted expectedStack)
    % expectedBranch.Id
    |> c.BranchEvents
    |> Assert.Single
    |> Assert.equal (Branch.Events.Snapshotted expectedBranch)
    
[<Generators>]
let ``StackBranch.Service.Upsert persists edit`` (authorId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewLeaf_Title title; Ids = { command1.Ids with LeafId = command2.Ids.LeafId } }
    let c = TestEsContainer()
    let stackBranchService = c.StackBranchService()
    let expectedBranch : Branch.Events.Edited =
        let _, b = StackBranch.stackBranch authorId command2 None tags title
        { LeafId      = b.LeafId
          Title       = b.Title
          GrompleafId = b.GrompleafId
          FieldValues = b.FieldValues
          EditSummary = b.EditSummary }
    stackBranchService.Upsert(authorId, command1) |> RunSynchronously.OkEquals ()
        
    stackBranchService.Upsert(authorId, command2) |> RunSynchronously.OkEquals ()

    % command2.Ids.BranchId
    |> c.BranchEvents
    |> Seq.last
    |> Assert.equal (Branch.Events.Edited expectedBranch)

[<Generators>]
let ``StackBranch.Service.Upsert fails to persist edit with duplicate leafId`` (authorId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewLeaf_Title title; Ids = command1.Ids }
    let c = TestEsContainer()
    let stackBranchService = c.StackBranchService()
    stackBranchService.Upsert(authorId, command1) |> RunSynchronously.OkEquals ()
        
    stackBranchService.Upsert(authorId, command2)

    |> RunSynchronously.ErrorEquals $"Duplicate leafId:{command1.Ids.LeafId}"

[<Generators>]
let ``StackBranch.Service.Upsert fails to persist edit with another author`` (authorId, hackerId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewLeaf_Title title; Ids = command1.Ids }
    let c = TestEsContainer()
    let stackBranchService = c.StackBranchService()
    stackBranchService.Upsert(authorId, command1) |> RunSynchronously.OkEquals ()
        
    stackBranchService.Upsert(hackerId, command2)

    |> RunSynchronously.ErrorEquals $"You aren't the author"

[<Generators>]
let ``StackBranch.Service.Upsert fails to insert twice`` (authorId, command, tags) =
    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags }
    let c = TestEsContainer()
    let stackBranchService = c.StackBranchService()
    stackBranchService.Upsert(authorId, command) |> RunSynchronously.OkEquals ()
        
    stackBranchService.Upsert(authorId, command)

    |> RunSynchronously.ErrorEquals $"Already created"

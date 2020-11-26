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

type TestVolatileStore() =
    inherit VolatileStore<byte[]>()
    
    member private this.events(streamName, codec: IEventCodec<_, _, _>) =
        streamName.ToString()
        |> (this.TryLoad >> Option.get)
        |> Array.map (codec.TryDecode >> Option.get)
    member this.StackEvents (id) = this.events(Stack .streamName id, Stack .Events.codec)
    member this.BranchEvents(id) = this.events(Branch.streamName id, Branch.Events.codec)

module Stack =
    open Stack
    let memoryStore store =
        Resolver(store, Events.codec, Fold.fold, Fold.initial).Resolve
        |> create
module Branch =
    open Branch
    let memoryStore store =
        Resolver(store, Events.codec, Fold.fold, Fold.initial).Resolve
        |> create

let stackBranchService store =
    let stacks   = Stack .memoryStore store
    let branches = Branch.memoryStore store
    StackBranch.Service(stacks, branches)

let runSynchronouslySuccessfully x =
    x
    |> Async.RunSynchronously
    |> Result.getOk
    |> Assert.Null

[<Generators>]
let ``StackBranch.Service.Upsert persists both snapshots`` (authorId, command, tags) =
    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags }
    let store = TestVolatileStore()
    let stackBranchService = stackBranchService store
    let expectedStack, expectedBranch = StackBranch.stackBranch authorId command None tags "Default"
    
    stackBranchService.Upsert(authorId, command)
    
    |> runSynchronouslySuccessfully
    % expectedStack.StackId
    |> store.StackEvents
    |> Assert.Single
    |> Assert.equal (Stack.Events.Snapshotted expectedStack)
    % expectedBranch.BranchId
    |> store.BranchEvents
    |> Assert.Single
    |> Assert.equal (Branch.Events.Snapshotted expectedBranch)
    
[<Generators>]
let ``StackBranch.Service.Upsert persists edit`` (authorId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewLeaf_Title title; Ids = { command1.Ids with LeafId = command2.Ids.LeafId } }
    let store = TestVolatileStore()
    let stackBranchService = stackBranchService store
    let expectedBranch : Branch.Events.Edited =
        let _, b = StackBranch.stackBranch authorId command2 None tags title
        { LeafId      = b.LeafId
          Title       = b.Title
          GrompleafId = b.GrompleafId
          FieldValues = b.FieldValues
          EditSummary = b.EditSummary }
    stackBranchService.Upsert(authorId, command1) |> runSynchronouslySuccessfully
        
    stackBranchService.Upsert(authorId, command2) |> runSynchronouslySuccessfully

    % command2.Ids.BranchId
    |> store.BranchEvents
    |> Seq.last
    |> Assert.equal (Branch.Events.Edited expectedBranch)

[<Generators>]
let ``StackBranch.Service.Upsert fails to persist edit with duplicate leafId`` (authorId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewLeaf_Title title; Ids = command1.Ids }
    let store = TestVolatileStore()
    let stackBranchService = stackBranchService store
    stackBranchService.Upsert(authorId, command1) |> runSynchronouslySuccessfully
        
    stackBranchService.Upsert(authorId, command2)

    |> Async.RunSynchronously
    |> Result.getError
    |> Assert.equal $"Duplicate leafId:{command1.Ids.LeafId}"

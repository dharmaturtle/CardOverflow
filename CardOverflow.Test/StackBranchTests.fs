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

[<Property>]
let ``StackBranch.Service.Create persists both snapshots`` (stack: Stack.Events.Snapshotted, branch: Branch.Events.Snapshotted) =
    let store = TestVolatileStore()
    let stackBranchService = stackBranchService store
    Async.RunSynchronously <| async {
        let! r = stackBranchService.Create(stack, branch)
        Assert.Null(Result.getOk r)

        stack.StackId
        |> store.StackEvents
        |> Assert.Single
        |> Assert.equal (Stack.Events.Snapshotted stack)
        
        branch.BranchId
        |> store.BranchEvents
        |> Assert.Single
        |> Assert.equal (Branch.Events.Snapshotted branch)
    }

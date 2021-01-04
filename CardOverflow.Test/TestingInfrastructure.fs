module TestingInfrastructure

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

type TestVolatileStore() =
    inherit VolatileStore<byte[]>()
    
    member private this.events(streamName, codec: IEventCodec<_, _, _>) =
        streamName.ToString()
        |> (this.TryLoad >> Option.get)
        |> Array.map (codec.TryDecode >> Option.get)
    member this.StackEvents (id) = this.events(Stack .streamName id, Stack .Events.codec)
    member this.BranchEvents(id) = this.events(Branch.streamName id, Branch.Events.codec)
    member this.UserEvents  (id) = this.events(User  .streamName id, User  .Events.codec)

module User =
    open User
    let memoryStore store =
        Resolver(store, Events.codec, Fold.fold, Fold.initial).Resolve
        |> create

let userService store =
    User.memoryStore store

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

module RunSynchronously =
    let OkEquals ex =
        Async.RunSynchronously
        >> Result.getOk
        >> Assert.equal ex
    
    let ErrorEquals ex =
        Async.RunSynchronously
        >> Result.getError
        >> Assert.equal ex
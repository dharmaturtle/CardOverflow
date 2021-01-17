module TableClientTests

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
open Hedgehog
open CardOverflow.Api

[<StandardProperty>]
let ``can insert, get, and update stack snapshot`` (stack: Stack.Events.Snapshotted, leafId: LeafId) = async {
    let tc = TestEsContainer().TableClient()

    // insert
    let! _ = tc.InsertOrReplace stack
    
    // get
    let! actual, _ = tc.Get<Stack.Events.Snapshotted> stack.Id

    Assert.equal stack actual 

    // update
    do! tc.Update(fun (x: Stack.Events.Snapshotted) -> { x with CopySourceLeafId = Some leafId }) stack.Id
    
    let! actual, _ = tc.Get<Stack.Events.Snapshotted> stack.Id

    Assert.equal { stack with CopySourceLeafId = Some leafId } actual 
    }

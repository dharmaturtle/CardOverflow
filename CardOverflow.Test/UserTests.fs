module UserTests

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
open EventService
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp

[<StandardProperty>]
let ``Create snapshot roundtrips`` (userSnapshot: User.Events.Snapshot) = asyncResult {
    let c = TestEsContainer()
    let userService = c.UserService()

    do! userService.Create userSnapshot

    userSnapshot.Id
    |> c.UserEvents
    |> Seq.exactlyOne
    |> Assert.equal (User.Events.Snapshot userSnapshot)
    }

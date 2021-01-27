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
let ``Create snapshot roundtrips (event store)`` (userSnapshot: User.Events.Snapshot) = asyncResult {
    let c = TestEsContainer()
    let userService = c.UserService()

    do! userService.Create userSnapshot

    userSnapshot.Id
    |> c.UserEvents
    |> Seq.exactlyOne
    |> Assert.equal (User.Events.Snapshot userSnapshot)
    }

[<StandardProperty>]
let ``Create snapshot roundtrips (azure table)`` (userSnapshot: User.Events.Snapshot) = asyncResult {
    let c = TestEsContainer()
    let userService = c.UserService()
    let tableClient = c.TableClient()

    do! userService.Create userSnapshot

    let! user, _ = tableClient.GetUser userSnapshot.Id
    Assert.equal userSnapshot user
    }

//[<Fact>]
let ``Azure Tables max payload size`` () : unit =
    let userSnapshotGen =
        let config =
            GenX.defaults
            |> AutoGenConfig.addGenerator instantGen
            |> AutoGenConfig.addGenerator durationGen
            |> AutoGenConfig.addGenerator timezoneGen
            |> AutoGenConfig.addGenerator localTimeGen
        gen {
            let! snapshot = GenX.autoWith<Domain.User.Events.Snapshot> config
            let! cardSettings = GenX.autoWith<CardSetting> config
            return { snapshot with CardSettings = List.replicate 200 cardSettings }
        }
    property {
        let! userSnapshot = userSnapshotGen
        asyncResult {
            let c = TestEsContainer()
            let userService = c.UserService()
            let tableClient = c.TableClient()

            do! userService.Create userSnapshot

            let! user, _ = tableClient.GetUser userSnapshot.Id
            Assert.equal userSnapshot user
        } |> Async.RunSynchronously |> Result.getOk
    } |> Property.check

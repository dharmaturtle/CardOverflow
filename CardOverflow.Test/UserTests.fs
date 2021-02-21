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
let ``Create summary roundtrips (event store)`` (userSummary: User.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let userService = c.UserService()

    do! userService.Create userSummary

    userSummary.Id
    |> c.UserEvents
    |> Seq.exactlyOne
    |> Assert.equal (User.Events.Created userSummary)
    }

[<StandardProperty>]
let ``Create summary roundtrips (azure table)`` (userSummary: User.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let userService = c.UserService()
    let tableClient = c.TableClient()

    do! userService.Create userSummary

    let! user, _ = tableClient.GetUser userSummary.Id
    Assert.equal userSummary user
    }

[<StandardProperty>]
let ``CardSettingsEdited roundtrips (event store)`` (userSummary: User.Events.Summary) (cardSettings: User.Events.CardSettingsEdited) = asyncResult {
    let c = TestEsContainer()
    let userService = c.UserService()
    do! userService.Create userSummary
    
    do! userService.CardSettingsEdited userSummary.Id cardSettings

    userSummary.Id
    |> c.UserEvents
    |> Seq.last
    |> Assert.equal (User.Events.CardSettingsEdited cardSettings)
    }

[<StandardProperty>]
let ``CardSettingsEdited roundtrips (azure table)`` (userSummary: User.Events.Summary) (cardSettings: User.Events.CardSettingsEdited) = asyncResult {
    let c = TestEsContainer()
    let userService = c.UserService()
    let tableClient = c.TableClient()
    do! userService.Create userSummary
    
    do! userService.CardSettingsEdited userSummary.Id cardSettings

    let! user, _ = tableClient.GetUser userSummary.Id
    Assert.equal { userSummary with CardSettings = cardSettings.CardSettings } user
    }

//[<Fact>]
let ``Azure Tables max payload size`` () : unit =
    let userSummaryGen =
        let config =
            GenX.defaults
            |> AutoGenConfig.addGenerator instantGen
            |> AutoGenConfig.addGenerator durationGen
            |> AutoGenConfig.addGenerator timezoneGen
            |> AutoGenConfig.addGenerator localTimeGen
        gen {
            let! summary = GenX.autoWith<Domain.User.Events.Summary> config
            let! cardSettings = GenX.autoWith<CardSetting> config
            return { summary with CardSettings = List.replicate 200 cardSettings }
        }
    property {
        let! userSummary = userSummaryGen
        asyncResult {
            let c = TestEsContainer()
            let userService = c.UserService()
            let tableClient = c.TableClient()

            do! userService.Create userSummary

            let! user, _ = tableClient.GetUser userSummary.Id
            Assert.equal userSummary user
        } |> Async.RunSynchronously |> Result.getOk
    } |> Property.check

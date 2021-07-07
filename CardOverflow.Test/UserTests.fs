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
open EventAppender
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp
open Domain.Summary

[<StandardProperty>]
let ``Create summary roundtrips`` (userSignedUp: User.Events.SignedUp) = asyncResult {
    let c = TestEsContainer()
    let userSaga = c.UserSagaAppender()

    do! userSaga.Create userSignedUp

    // event store roundtrips
    userSignedUp.Meta.UserId
    |> c.UserEvents
    |> Seq.exactlyOne
    |> Assert.equal (User.Events.SignedUp userSignedUp)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetUser userSignedUp.Meta.UserId
    userSignedUp |> User.Fold.evolveSignedUp |> Assert.equal actual
    }

[<StandardProperty>]
let ``CardSettingsEdited roundtrips`` { SignedUp = userSignedUp; CardSettingsEdited = cardSettings } = asyncResult {
    let c = TestEsContainer()
    let userAppender = c.UserAppender()
    do! c.UserSagaAppender().Create userSignedUp
    
    do! userAppender.CardSettingsEdited cardSettings

    // event store roundtrips
    userSignedUp.Meta.UserId
    |> c.UserEvents
    |> Seq.last
    |> Assert.equal (User.Events.CardSettingsEdited cardSettings)

    // azure table roundtrips
    let! actualUser = c.KeyValueStore().GetUser userSignedUp.Meta.UserId
    userSignedUp |> User.Fold.evolveSignedUp |> User.Fold.evolveCardSettingsEdited cardSettings |> Assert.equal actualUser
    }

[<StandardProperty>]
let ``OptionsEdited roundtrips`` { SignedUp = signedUp; DeckCreated = deckCreated; OptionsEdited = optionsEdited } = asyncResult {
    let c = TestEsContainer()
    do! c.DeckAppender().Create deckCreated
    do! c.UserSagaAppender().Create signedUp
    let userAppender = c.UserAppender()
    
    do! userAppender.OptionsEdited optionsEdited

    // event store roundtrips
    signedUp.Meta.UserId
    |> c.UserEvents
    |> Seq.last
    |> Assert.equal (User.Events.OptionsEdited optionsEdited)

    // azure table roundtrips
    let! actualUser = c.KeyValueStore().GetUser signedUp.Meta.UserId
    signedUp |> User.Fold.evolveSignedUp |> User.Fold.evolveOptionsEdited optionsEdited |> Assert.equal actualUser
    }

//[<Fact>]
let ``Azure Tables max payload size`` () : unit =
    let userSignedUpGen =
        let config =
            GenX.defaults
            |> AutoGenConfig.addGenerator instantGen
            |> AutoGenConfig.addGenerator durationGen
            |> AutoGenConfig.addGenerator timezoneGen
            |> AutoGenConfig.addGenerator localTimeGen
        gen {
            let! signedUp     = Hedgehog.userSignedUpGen
            let! cardSettings = GenX.autoWith<CardSetting> config
            return { signedUp with CardSettings = List.replicate 200 cardSettings }
        }
    property {
        let! userSignedUp = userSignedUpGen
        asyncResult {
            let c = TestEsContainer()
            let userSaga = c.UserSagaAppender()
            let keyValueStore = c.KeyValueStore()

            do! userSaga.Create userSignedUp

            let userSummary = User.Fold.evolveSignedUp userSignedUp
            let! user = keyValueStore.GetUser userSummary.Id
            Assert.equal userSummary user
        } |> Async.RunSynchronously |> Result.getOk
    } |> Property.check

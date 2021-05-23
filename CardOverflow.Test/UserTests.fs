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
let ``upgradeRevision swaps when current exists`` others currentRevision newRevision =
    let collectedTemplates = currentRevision :: others
    let expected           = newRevision     :: others
    
    User.upgradeRevision collectedTemplates currentRevision newRevision
    
    |> Assert.equal expected

[<StandardProperty>]
let ``upgradeRevision appends when current doesn't exist`` others currentRevision newRevision =
    let expected = others @ [newRevision]
    
    User.upgradeRevision others currentRevision newRevision
    
    |> Assert.equal expected

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

[<StandardProperty>]
let ``(Un)FollowDeck roundtrips`` { SignedUp = signedUp; DeckCreated = deckCreated; DeckFollowed = followed; DeckUnfollowed = unfollowed } = asyncResult {
    let deckCreated = { deckCreated with Visibility = Public }
    let c = TestEsContainer()
    let userAppender = c.UserAppender()
    let deckAppender = c.DeckAppender()
    do! c.UserSagaAppender().Create signedUp
    let userSummary = signedUp |> User.Fold.evolveSignedUp
    do! deckAppender.Create deckCreated
    

    (***   when followed, then azure table updated   ***)
    do! userAppender.DeckFollowed followed

    let! actual = c.KeyValueStore().GetUser signedUp.Meta.UserId
    Assert.equal { userSummary with FollowedDecks = signedUp.FollowedDecks |> Set.add deckCreated.Id } actual


    (***   when unfollowed, then azure table updated   ***)
    do! userAppender.DeckUnfollowed unfollowed

    let! actual = c.KeyValueStore().GetUser userSummary.Id
    Assert.equal userSummary actual

    
    (***   following nonexistant deck, fails   ***)
    let nonexistantDeckId = % Guid.NewGuid()
    
    let! (r: Result<_,_>) = userAppender.DeckFollowed { followed with DeckId = nonexistantDeckId }

    Assert.equal $"Deck doesn't exist." r.error
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

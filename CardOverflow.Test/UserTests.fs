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
let ``CardSettingsEdited roundtrips`` (userSignedUp: User.Events.SignedUp) (cardSettings: User.Events.CardSettingsEdited) = asyncResult {
    let c = TestEsContainer()
    let userAppender = c.UserAppender()
    do! c.UserSagaAppender().Create userSignedUp
    
    do! userAppender.CardSettingsEdited userSignedUp.Meta.UserId cardSettings

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
let ``OptionsEdited roundtrips`` (userSignedUp: User.Events.SignedUp) (deckSummary: Deck) (optionsEdited: User.Events.OptionsEdited) = asyncResult {
    let c = TestEsContainer()
    do! c.DeckAppender().Create { deckSummary with AuthorId = userSignedUp.Meta.UserId }
    let optionsEdited = { optionsEdited with DefaultDeckId = deckSummary.Id }
    do! c.UserSagaAppender().Create userSignedUp
    let userAppender = c.UserAppender()
    
    do! userAppender.OptionsEdited userSignedUp.Meta.UserId optionsEdited

    // event store roundtrips
    userSignedUp.Meta.UserId
    |> c.UserEvents
    |> Seq.last
    |> Assert.equal (User.Events.OptionsEdited optionsEdited)

    // azure table roundtrips
    let! actualUser = c.KeyValueStore().GetUser userSignedUp.Meta.UserId
    userSignedUp |> User.Fold.evolveSignedUp |> User.Fold.evolveOptionsEdited optionsEdited |> Assert.equal actualUser
    }

[<StandardProperty>]
let ``(Un)FollowDeck roundtrips`` (userSignedUp: User.Events.SignedUp) (deckSummary: Deck) (meta: Meta) = asyncResult {
    let deckSummary = { deckSummary with Visibility = Public }
    let meta = { meta with UserId = userSignedUp.Meta.UserId }
    let c = TestEsContainer()
    let userAppender = c.UserAppender()
    let deckAppender = c.DeckAppender()
    do! c.UserSagaAppender().Create userSignedUp
    let userSummary = userSignedUp |> User.Fold.evolveSignedUp
    do! deckAppender.Create deckSummary
    

    (***   when followed, then azure table updated   ***)
    do! userAppender.DeckFollowed { DeckId = deckSummary.Id; Meta = meta }

    let! actual = c.KeyValueStore().GetUser userSignedUp.Meta.UserId
    Assert.equal { userSummary with FollowedDecks = userSignedUp.FollowedDecks |> Set.add deckSummary.Id } actual


    (***   when unfollowed, then azure table updated   ***)
    do! userAppender.DeckUnfollowed { DeckId = deckSummary.Id; Meta = meta }

    let! actual = c.KeyValueStore().GetUser userSummary.Id
    Assert.equal userSummary actual

    
    (***   following nonexistant deck, fails   ***)
    let nonexistantDeckId = % Guid.NewGuid()
    
    let! (r: Result<_,_>) = userAppender.DeckFollowed { DeckId = nonexistantDeckId; Meta = meta }

    Assert.equal $"The deck '{nonexistantDeckId}' doesn't exist." r.error
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

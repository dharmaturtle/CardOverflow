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
let ``Create summary roundtrips`` (userSummary: User.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let userSaga = c.UserSagaAppender()

    do! userSaga.Create userSummary

    // event store roundtrips
    userSummary.Id
    |> c.UserEvents
    |> Seq.exactlyOne
    |> Assert.equal (User.Events.Created userSummary)

    // azure table roundtrips
    let! user = c.KeyValueStore().GetUser userSummary.Id
    Assert.equal userSummary user
    }

[<StandardProperty>]
let ``CardSettingsEdited roundtrips`` (userSummary: User.Events.Summary) (cardSettings: User.Events.CardSettingsEdited) = asyncResult {
    let c = TestEsContainer()
    let userAppender = c.UserAppender()
    do! c.UserSagaAppender().Create userSummary
    
    do! userAppender.CardSettingsEdited userSummary.Id cardSettings

    // event store roundtrips
    userSummary.Id
    |> c.UserEvents
    |> Seq.last
    |> Assert.equal (User.Events.CardSettingsEdited cardSettings)

    // azure table roundtrips
    let! user = c.KeyValueStore().GetUser userSummary.Id
    Assert.equal { userSummary with CardSettings = cardSettings.CardSettings } user
    }

[<StandardProperty>]
let ``OptionsEdited roundtrips`` (userSummary: User.Events.Summary) (deckSummary: Deck) (optionsEdited: User.Events.OptionsEdited) = asyncResult {
    let c = TestEsContainer()
    do! c.DeckAppender().Create { deckSummary with AuthorId = userSummary.Id }
    let optionsEdited = { optionsEdited with DefaultDeckId = deckSummary.Id }
    do! c.UserSagaAppender().Create userSummary
    let userAppender = c.UserAppender()
    
    do! userAppender.OptionsEdited userSummary.Id optionsEdited

    // event store roundtrips
    userSummary.Id
    |> c.UserEvents
    |> Seq.last
    |> Assert.equal (User.Events.OptionsEdited optionsEdited)

    // azure table roundtrips
    let! user = c.KeyValueStore().GetUser userSummary.Id
    Assert.equal (User.Fold.evolveOptionsEdited optionsEdited userSummary) user
    }

[<StandardProperty>]
let ``(Un)FollowDeck roundtrips`` (userSummary: User.Events.Summary) (deckSummary: Deck) = asyncResult {
    let c = TestEsContainer()
    let userAppender = c.UserAppender()
    let deckAppender = c.DeckAppender()
    do! c.UserSagaAppender().Create userSummary
    do! deckAppender.Create deckSummary
    

    (***   when followed, then azure table updated   ***)
    do! userAppender.DeckFollowed userSummary.Id deckSummary.Id

    let! actual = c.KeyValueStore().GetUser userSummary.Id
    Assert.equal { userSummary with FollowedDecks = userSummary.FollowedDecks |> Set.add deckSummary.Id } actual


    (***   when unfollowed, then azure table updated   ***)
    do! userAppender.DeckUnfollowed userSummary.Id deckSummary.Id

    let! actual = c.KeyValueStore().GetUser userSummary.Id
    Assert.equal userSummary actual

    
    (***   following nonexistant deck, fails   ***)
    let nonexistantDeckId = % Guid.NewGuid()
    
    let! (r: Result<_,_>) = userAppender.DeckFollowed userSummary.Id nonexistantDeckId

    Assert.equal $"The deck '{nonexistantDeckId}' doesn't exist." r.error
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
            let userSaga = c.UserSagaAppender()
            let keyValueStore = c.KeyValueStore()

            do! userSaga.Create userSummary

            let! user = keyValueStore.GetUser userSummary.Id
            Assert.equal userSummary user
        } |> Async.RunSynchronously |> Result.getOk
    } |> Property.check

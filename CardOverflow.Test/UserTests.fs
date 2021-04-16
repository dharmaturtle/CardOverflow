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
open EventWriter
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp

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
    let userSaga = c.UserSagaWriter()

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
    let userWriter = c.UserWriter()
    do! c.UserSagaWriter().Create userSummary
    
    do! userWriter.CardSettingsEdited userSummary.Id cardSettings

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
let ``OptionsEdited roundtrips`` (userSummary: User.Events.Summary) (deckSummary: Deck.Events.Summary) (optionsEdited: User.Events.OptionsEdited) = asyncResult {
    let c = TestEsContainer()
    do! c.DeckWriter().Create { deckSummary with AuthorId = userSummary.Id }
    let optionsEdited = { optionsEdited with DefaultDeckId = deckSummary.Id }
    do! c.UserSagaWriter().Create userSummary
    let userWriter = c.UserWriter()
    
    do! userWriter.OptionsEdited userSummary.Id optionsEdited

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
let ``(Un)FollowDeck roundtrips`` (userSummary: User.Events.Summary) (deckSummary: Deck.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let userWriter = c.UserWriter()
    let deckWriter = c.DeckWriter()
    do! c.UserSagaWriter().Create userSummary
    do! deckWriter.Create deckSummary
    

    (***   when followed, then azure table updated   ***)
    do! userWriter.DeckFollowed userSummary.Id deckSummary.Id

    let! actual = c.KeyValueStore().GetUser userSummary.Id
    Assert.equal { userSummary with FollowedDecks = userSummary.FollowedDecks |> Set.add deckSummary.Id } actual


    (***   when unfollowed, then azure table updated   ***)
    do! userWriter.DeckUnfollowed userSummary.Id deckSummary.Id

    let! actual = c.KeyValueStore().GetUser userSummary.Id
    Assert.equal userSummary actual

    
    (***   following nonexistant deck, fails   ***)
    let nonexistantDeckId = % Guid.NewGuid()
    
    let! (r: Result<_,_>) = userWriter.DeckFollowed userSummary.Id nonexistantDeckId

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
            let userSaga = c.UserSagaWriter()
            let keyValueStore = c.KeyValueStore()

            do! userSaga.Create userSummary

            let! user = keyValueStore.GetUser userSummary.Id
            Assert.equal userSummary user
        } |> Async.RunSynchronously |> Result.getOk
    } |> Property.check

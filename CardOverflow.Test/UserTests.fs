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
let ``Create summary roundtrips`` (userSummary: User.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let userSaga = c.UserSaga()

    do! userSaga.Create userSummary

    // event store roundtrips
    userSummary.Id
    |> c.UserEvents
    |> Seq.exactlyOne
    |> Assert.equal (User.Events.Created userSummary)

    // azure table roundtrips
    let! user, _ = c.TableClient().GetUser userSummary.Id
    Assert.equal userSummary user
    }

[<StandardProperty>]
let ``CardSettingsEdited roundtrips`` (userSummary: User.Events.Summary) (cardSettings: User.Events.CardSettingsEdited) = asyncResult {
    let c = TestEsContainer()
    let userService = c.UserService()
    do! c.UserSaga().Create userSummary
    
    do! userService.CardSettingsEdited userSummary.Id cardSettings

    // event store roundtrips
    userSummary.Id
    |> c.UserEvents
    |> Seq.last
    |> Assert.equal (User.Events.CardSettingsEdited cardSettings)

    // azure table roundtrips
    let! user, _ = c.TableClient().GetUser userSummary.Id
    Assert.equal { userSummary with CardSettings = cardSettings.CardSettings } user
    }

[<StandardProperty>]
let ``OptionsEdited roundtrips`` (userSummary: User.Events.Summary) (deckSummary: Deck.Events.Summary) (optionsEdited: User.Events.OptionsEdited) = asyncResult {
    let c = TestEsContainer()
    do! c.DeckService().Create { deckSummary with UserId = userSummary.Id }
    let optionsEdited = { optionsEdited with DefaultDeckId = deckSummary.Id }
    do! c.UserSaga().Create userSummary
    let userService = c.UserService()
    
    do! userService.OptionsEdited userSummary.Id optionsEdited

    // event store roundtrips
    userSummary.Id
    |> c.UserEvents
    |> Seq.last
    |> Assert.equal (User.Events.OptionsEdited optionsEdited)

    // azure table roundtrips
    let! user, _ = c.TableClient().GetUser userSummary.Id
    Assert.equal (User.Fold.evolveOptionsEdited optionsEdited userSummary) user
    }

[<StandardProperty>]
let ``(Un)FollowDeck roundtrips`` (userSummary: User.Events.Summary) (deckSummary: Deck.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let userService = c.UserService()
    let deckService = c.DeckService()
    do! c.UserSaga().Create userSummary
    do! deckService.Create deckSummary
    

    (***   when followed, then azure table updated   ***)
    do! userService.DeckFollowed userSummary.Id deckSummary.Id

    let! actual, _ = c.TableClient().GetUser userSummary.Id
    Assert.equal { userSummary with FollowedDecks = userSummary.FollowedDecks |> Set.add deckSummary.Id } actual


    (***   when unfollowed, then azure table updated   ***)
    do! userService.DeckUnfollowed userSummary.Id deckSummary.Id

    let! actual, _ = c.TableClient().GetUser userSummary.Id
    Assert.equal userSummary actual

    
    (***   following nonexistant deck, fails   ***)
    let nonexistantDeckId = % Guid.NewGuid()
    
    let! (r: Result<_,_>) = userService.DeckFollowed userSummary.Id nonexistantDeckId

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
            let userSaga = c.UserSaga()
            let tableClient = c.TableClient()

            do! userSaga.Create userSummary

            let! user, _ = tableClient.GetUser userSummary.Id
            Assert.equal userSummary user
        } |> Async.RunSynchronously |> Result.getOk
    } |> Property.check

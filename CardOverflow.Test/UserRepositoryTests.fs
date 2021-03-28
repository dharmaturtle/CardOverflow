module UserRepositoryTests

open CardOverflow.Entity
open CardOverflow.Debug
open CardOverflow.Pure
open Microsoft.EntityFrameworkCore
open CardOverflow.Api
open CardOverflow.Test
open System
open System.Linq
open Xunit
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Control.Tasks
open CardOverflow.Sanitation
open FsToolkit.ErrorHandling
open NodaTime

[<Fact(Skip=PgSkip.reason)>]
let ``UserRepository works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let displayName = Guid.NewGuid().ToString().[0..31]
    Assert.equal 32 displayName.Length
    let userId = user_ 4

    do! UserRepository.create c.Db userId displayName
    
    Assert.equal userId <| c.Db.User.Single(fun x -> x.DisplayName = displayName).Id
    let! (ucs: User_TemplateRevisionEntity ResizeArray) = c.Db.User_TemplateRevision.Where(fun x -> x.UserId = userId).ToListAsync()
    Assert.areEquivalent ([1; 2; 3; 6; 5] |> List.map templateRevision_) <| ucs.Select(fun x -> x.TemplateRevisionId)
    let! (cardSetting: CardSettingEntity) = c.Db.CardSetting.SingleAsync(fun x -> x.UserId = user_ 4)
    Assert.equal cardSetting.Id <| ucs.Select(fun x -> x.DefaultCardSettingId).Distinct().Single()
    } |> TaskResult.getOk)
    
[<Fact(Skip=PgSkip.reason)>]
let ``UserRepository settings works``(): Task<unit> = (taskResult {
    use c = new TestContainer()

    // testing get
    let! (settings: UserSetting) = UserRepository.getSettings c.Db user_3
    
    Assert.equal
        {   UserId = user_3
            DisplayName = "RoboTurtle"
            DefaultCardSettingId = setting_3
            DefaultDeckId = deck_3
            ShowNextReviewTime = true
            ShowRemainingCardCount = true
            StudyOrder = StudyOrder.Mixed
            NextDayStartsAt = Duration.FromHours 4
            LearnAheadLimit = Duration.FromMinutes 20.
            TimeboxTimeLimit = Duration.Zero
            IsNightMode = false
            Created = settings.Created // untested
            Timezone = TimezoneName.America_Chicago
        }
        settings

    // testing set
    let newSettings =
        SetUserSetting.create
            "Zombie Dino"
            (setting_ 4)
            (deck_ 4)
            false
            false
            StudyOrder.NewCardsFirst
            (Duration.FromHours 12)
            (Duration.FromHours 13)
            (Duration.FromHours 14)
            true
            TimezoneName.Europe_London
        |> fun x -> x.Value

    // someone else's deck fails
    let! (x: Result<_, _>) = UserRepository.setSettings c.Db user_3 { newSettings with DefaultDeckId = deck_1 }
    
    Assert.equal
        "Deck 00000000-0000-0000-0000-decc00000001 doesn't belong to User 00000000-0000-0000-0000-000000000003"
        x.error

    // nonexistant deck fails
    let! (x: Result<_, _>) = UserRepository.setSettings c.Db user_3 newSettings
    
    Assert.equal
        "Deck 00000000-0000-0000-0000-decc00000004 doesn't exist"
        x.error

    do! SanitizeDeckRepository.create c.Db user_3 "new deck!" (deck_ 4)
    // someone else's card setting fails
    let! (x: Result<_, _>) = UserRepository.setSettings c.Db user_3 { newSettings with DefaultCardSettingId = setting_2 }
    
    Assert.equal
        "Card setting 00000000-0000-0000-0000-5e7700000002 doesn't belong to User 00000000-0000-0000-0000-000000000003"
        x.error

    // nonexistant card setting fails
    let! (x: Result<_, _>) = UserRepository.setSettings c.Db user_3 newSettings
    
    Assert.equal
        "Card setting 00000000-0000-0000-0000-5e7700000004 doesn't exist"
        x.error

    // add card setting
    let! (settings: ViewCardSetting ResizeArray) = SanitizeCardSettingRepository.getAll c.Db user_3
    let options =
        settings.Append
            { (Guid.Empty |> CardSetting.newUserCardSettings |> ViewCardSetting.load) with
                IsDefault = false }
        |> toResizeArray
    let! (ids: Guid list) = SanitizeCardSettingRepository.upsertMany c.Db user_3 options
    let newCardSettingId = ids.Single(fun x -> x <> setting_3)
    
    // setSettings works
    do! UserRepository.setSettings c.Db user_3 { newSettings with DefaultCardSettingId = newCardSettingId }
    
    let! (settings: UserSetting) = UserRepository.getSettings c.Db user_3
    Assert.equal
        {   UserId = user_3
            DisplayName = newSettings.DisplayName
            DefaultCardSettingId = newCardSettingId
            DefaultDeckId = newSettings.DefaultDeckId
            ShowNextReviewTime = newSettings.ShowNextReviewTime
            ShowRemainingCardCount = newSettings.ShowRemainingCardCount
            StudyOrder = newSettings.StudyOrder
            NextDayStartsAt = newSettings.NextDayStartsAt
            LearnAheadLimit = newSettings.LearnAheadLimit
            TimeboxTimeLimit = newSettings.TimeboxTimeLimit
            IsNightMode = newSettings.IsNightMode
            Created = settings.Created // untested
            Timezone = newSettings.Timezone
        }
        settings
    } |> TaskResult.getOk)

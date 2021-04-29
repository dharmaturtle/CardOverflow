module Domain.Summary

open FSharp.UMX
open System
open CardOverflow.Pure
open NodaTime

type User =
    { Id: UserId
      DisplayName: string
      DefaultDeckId: DeckId
      ShowNextReviewTime: bool
      ShowRemainingCardCount: bool
      StudyOrder: StudyOrder
      NextDayStartsAt: LocalTime
      LearnAheadLimit: Duration
      TimeboxTimeLimit: Duration
      IsNightMode: bool
      Created: Instant
      Modified: Instant
      Timezone: DateTimeZone
      CardSettings: CardSetting list // medTODO move card settings here
      FollowedDecks: DeckId Set
      CollectedTemplates: TemplateRevisionId list }

type Deck =
    { Id: DeckId
      AuthorId: UserId
      Name: string
      Description: string
      Visibility: Visibility }
let defaultDeck userId deckId =
    { Id = deckId
      AuthorId = userId
      Name = "Default Deck"
      Description = ""
      Visibility = Private }

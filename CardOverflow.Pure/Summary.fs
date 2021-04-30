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

type Template =
    { Id: TemplateId
      CurrentRevision: TemplateRevisionOrdinal
      AuthorId: UserId
      Name: string
      Css: string
      Fields: Field list // highTODO bring all the types here
      Created: Instant
      Modified: Instant
      LatexPre: string
      LatexPost: string
      CardTemplates: TemplateType // highTODO bring all the types here
      Visibility: Visibility
      EditSummary: string }
  with
    member this.CurrentRevisionId = this.Id, this.CurrentRevision

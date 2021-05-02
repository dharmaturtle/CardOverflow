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

type Example =
    { Id: ExampleId
      ParentId: ExampleId option
      CurrentRevision: ExampleRevisionOrdinal
      Title: string
      AuthorId: UserId
      TemplateRevisionId: TemplateRevisionId
      AnkiNoteId: int64 option
      FieldValues: Map<string, string>
      Visibility: Visibility
      EditSummary: string }
  with
    member this.CurrentRevisionId = this.Id, this.CurrentRevision

type Review =
    { Index: int
      Score: int
      Created: Instant
      IntervalWithUnusedStepsIndex: int
      EaseFactor: float
      TimeFromSeeingQuestionToScore: Duration }
type ShadowableDetails =
    { EaseFactor: float
      IntervalOrStepsIndex: IntervalOrStepsIndex // highTODO bring all the types here. ALSO CONSIDER A BETTER NAME
      Due: Instant
      IsLapsed: bool
      History: Review list }
type Details =
    //| Shadow of StackId * CardTemplatePointer // medTODO don't allow more than 1 hop to prevent infinite loop
    | ShadowableDetails of ShadowableDetails
type Card =
    { Pointer: CardTemplatePointer
      Created: Instant
      Modified: Instant
      CardSettingId: CardSettingId
      DeckId: DeckId
      Details: Details
      State: CardState }
type Stack =
    { Id: StackId
      AuthorId: UserId
      ExampleRevisionId: ExampleRevisionId
      FrontPersonalField: string
      BackPersonalField: string
      Tags: string Set
      Cards: Card list }

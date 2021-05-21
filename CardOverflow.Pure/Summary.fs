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

type TemplateRevision =
    { Ordinal: TemplateRevisionOrdinal
      Name: string
      Css: string
      Fields: Field list // highTODO bring all the types here
      Created: Instant
      LatexPre: string
      LatexPost: string
      CardTemplates: TemplateType // highTODO bring all the types here
      EditSummary: string }

type Template =
    { Id: TemplateId
      AuthorId: UserId
      Revisions: TemplateRevision list
      Modified: Instant
      Visibility: Visibility }
  with
    member this.CurrentRevision = this.Revisions |> List.maxBy (fun x -> x.Ordinal)
    member this.CurrentRevisionId = this.Id, this.CurrentRevision.Ordinal

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
type Card =
    { Pointer: CardTemplatePointer
      CardSettingId: CardSettingId
      DeckId: DeckId
      EaseFactor: float
      IntervalOrStepsIndex: IntervalOrStepsIndex // highTODO bring all the types here. ALSO CONSIDER A BETTER NAME
      Due: Instant
      IsLapsed: bool
      History: Review list
      State: CardState }
type Stack =
    { Id: StackId
      AuthorId: UserId
      ExampleRevisionId: ExampleRevisionId
      FrontPersonalField: string
      BackPersonalField: string
      Tags: string Set
      Cards: Card list }

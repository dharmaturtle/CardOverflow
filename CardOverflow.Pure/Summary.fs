module Domain.Summary

open FSharp.UMX
open System
open CardOverflow.Pure
open NodaTime

type User =
    { Id: UserId
      CommandIds: CommandId Set
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
    { CommandIds: CommandId Set
      Id: DeckId
      AuthorId: UserId
      Name: string
      Description: string
      Visibility: Visibility }

[<CLIMutable>]
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
  with
    member this.JustCardTemplates =
        match this.CardTemplates with
        | Cloze t -> [t]
        | Standard ts -> ts

type Template =
    { Id: TemplateId
      CommandIds: CommandId Set
      AuthorId: UserId
      Revisions: TemplateRevision list
      Modified: Instant
      Visibility: Visibility }
  with
    member this.CurrentRevision = this.Revisions |> List.maxBy (fun x -> x.Ordinal)
    member this.CurrentRevisionId = this.Id, this.CurrentRevision.Ordinal

type ExampleRevision =
    { Ordinal: ExampleRevisionOrdinal
      Title: string
      TemplateRevisionId: TemplateRevisionId
      FieldValues: EditFieldAndValue list
      EditSummary: string }

type Example =
    { Id: ExampleId
      CommandIds: CommandId Set
      ParentId: ExampleId option
      Revisions: ExampleRevision list
      AuthorId: UserId
      AnkiNoteId: int64 option
      Visibility: Visibility }
  with
    member this.CurrentRevision = this.Revisions |> List.maxBy (fun x -> x.Ordinal)
    member this.CurrentRevisionId = this.Id, this.CurrentRevision.Ordinal

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
      DeckIds: DeckId list
      EaseFactor: float
      IntervalOrStepsIndex: IntervalOrStepsIndex // highTODO bring all the types here. ALSO CONSIDER A BETTER NAME
      Due: Instant
      IsLapsed: bool
      History: Review list
      State: CardState }
type Stack =
    { Id: StackId
      CommandIds: CommandId Set
      AuthorId: UserId
      ExampleRevisionId: ExampleRevisionId
      FrontPersonalField: string
      BackPersonalField: string
      Tags: string Set
      Cards: Card list }

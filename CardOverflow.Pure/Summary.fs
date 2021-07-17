module Domain.Summary

open FSharp.UMX
open System
open CardOverflow.Pure
open NodaTime

type User =
    { Id: UserId
      CommandIds: CommandId Set
      DisplayName: string
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
      CollectedTemplates: TemplateRevisionId list }

type Deck =
    { CommandIds: CommandId Set
      Id: DeckId
      IsDefault: bool
      SourceId: DeckId Option
      AuthorId: UserId
      Name: string
      Description: string
      ServerCreated: Instant
      ServerModified: Instant
      Visibility: Visibility
      Extra: string }

[<CLIMutable>]
type TemplateRevision =
    { Ordinal: TemplateRevisionOrdinal
      Name: string
      Css: string
      Fields: Field list // highTODO bring all the types here
      Meta: Meta
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
      Visibility: Visibility }
  with
    member this.CurrentRevision = this.Revisions |> List.maxBy (fun x -> x.Ordinal)
    member this.FirstRevision   = this.Revisions |> List.minBy (fun x -> x.Ordinal)
    member this.CurrentRevisionId = this.Id, this.CurrentRevision.Ordinal

type ExampleRevision =
    { Ordinal: ExampleRevisionOrdinal
      Title: string
      TemplateRevisionId: TemplateRevisionId
      FieldValues: EditFieldAndValue list
      Meta: Meta
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
    member this.FirstRevision   = this.Revisions |> List.minBy (fun x -> x.Ordinal)
    member this.CurrentRevisionId = this.Id, this.CurrentRevision.Ordinal

type Review =
    { Score: Score
      Created: Instant
      IntervalOrStepsIndex: IntervalOrStepsIndex
      EaseFactor: float
      TimeFromSeeingQuestionToScore: Duration }
[<CLIMutable>]
type Card =
    { Pointer: CardTemplatePointer
      CardSettingId: CardSettingId
      EaseFactor: float
      IntervalOrStepsIndex: IntervalOrStepsIndex // highTODO bring all the types here. ALSO CONSIDER A BETTER NAME
      Due: Instant
      IsLapsed: bool
      Reviews: Review list
      State: CardState }
type Stack =
    { Id: StackId
      CommandIds: CommandId Set
      AuthorId: UserId
      ExampleRevisionId: ExampleRevisionId
      FrontPersonalField: string
      BackPersonalField: string
      DeckIds: DeckId Set
      Tags: string Set
      Cards: Card list }

module Domain.Summary

open FSharp.UMX
open System
open CardOverflow.Pure
open NodaTime

type Comment = {
    Id              : CommentId
    User            : string
    UserId          : UserId
    Text            : string
    ServerCreatedAt : Instant
}

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
      ServerCreatedAt: Instant
      ServerModifiedAt: Instant
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

type PublicTemplate =
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
      Visibility: Visibility
      Comments: Comment list }
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

module FrontBackFrontSynthBackSynth = // this doesn't belong in Summary, but whatever
    open System.Linq
    let create fieldNameValueMap pointer (templateInstance: TemplateRevision) =
        match pointer with
        | CardTemplatePointer.Normal g ->
            match templateInstance.CardTemplates with
            | Standard ts ->
                let t = ts.Single(fun x -> x.Id = g)
                CardHtml.generate
                <| fieldNameValueMap
                <| t.Front
                <| t.Back
                <| templateInstance.Css
                <| CardHtml.Standard
            | _ -> failwith "Must generate a standard view for a standard template."
        | CardTemplatePointer.Cloze i ->
            match templateInstance.CardTemplates with
            | Cloze c ->
                CardHtml.generate
                <| fieldNameValueMap
                <| c.Front
                <| c.Back
                <| templateInstance.Css
                <| CardHtml.Cloze (int16 i)
            | _ -> failwith "Must generate a cloze view for a cloze template."
    let fromEditFieldAndValueList (editFieldAndValueList: EditFieldAndValue list) =
        editFieldAndValueList |> List.map (fun x -> x.EditField.Name, x.Value) |> create

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
      FrontPersonalField: string
      BackPersonalField: string
      DeckIds: DeckId Set
      Tags: string Set
      Cards: Card list
      ExampleRevisionId: ExampleRevisionId Option
      AnkiNoteId: int64 option
      Title: string
      TemplateRevisionId: TemplateRevisionId
      FieldValues: EditFieldAndValue list
      ClientCreatedAt: Instant
      ClientModifiedAt: Instant }
    member this.FrontBackFrontSynthBackSynth pointer template = // don't curry; consumed by C#
        FrontBackFrontSynthBackSynth.fromEditFieldAndValueList this.FieldValues pointer template

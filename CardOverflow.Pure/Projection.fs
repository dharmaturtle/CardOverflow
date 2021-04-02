module Domain.Projection

open FsCodec
open FsCodec.NewtonsoftJson
open NodaTime
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling

[<CLIMutable>]
type ExampleSearch =
    { Id: ExampleId
      ParentId: ExampleId option
      RevisionId: RevisionId
      Title: string
      AuthorId: UserId
      Author: string
      TemplateRevision: Template.RevisionSummary
      FieldValues: Map<string, string>
      EditSummary: string }

module ExampleSearch =
    let fromSummary (summary: Example.Events.Summary) displayName templateRevision =
        { Id = summary.Id
          ParentId = summary.ParentId
          RevisionId = summary.RevisionIds.Head
          Title = summary.Title
          AuthorId = summary.AuthorId
          Author = displayName
          TemplateRevision = templateRevision
          FieldValues = summary.FieldValues
          EditSummary = summary.EditSummary }
    let fromEdited (exampleSearch: ExampleSearch) (edited: Example.Events.Edited) templateRevision =
        { exampleSearch with
            RevisionId = edited.RevisionId
            Title = edited.Title
            TemplateRevision = templateRevision
            FieldValues = edited.FieldValues
            EditSummary = edited.EditSummary }

type CardSearch =
    { SubtemplateName: SubtemplateName
      CardSettingId: CardSettingId
      DeckId: DeckId
      Due: Instant
      IsLapsed: bool
      State: CardState
      Tags: string Set }
type StackSearch =
    { Id: StackId
      AuthorId: UserId
      ExampleRevisionId: RevisionId
      FrontPersonalField: string
      BackPersonalField: string
      Cards: CardSearch list }
module StackSearch =
    let fromSummary (summary: Stack.Events.Summary) =
        let fromCardSummary (card: Stack.Events.Card) =
            let details =
                match card.Details with
                | Stack.Events.ShadowableDetails d -> d
            { SubtemplateName = card.SubtemplateName
              CardSettingId = card.CardSettingId
              DeckId = card.DeckId
              Due = details.Due
              IsLapsed = details.IsLapsed
              State = card.State
              Tags = card.Tags }
        { Id = summary.Id
          AuthorId = summary.AuthorId
          ExampleRevisionId = summary.ExampleRevisionId
          FrontPersonalField = summary.FrontPersonalField
          BackPersonalField = summary.BackPersonalField
          Cards = summary.Cards |> List.map fromCardSummary }
    let fromTagsChanged tagsChanged (stackSearch: StackSearch) =
        stackSearch // highTODO fix
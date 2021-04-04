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
      State: CardState }
type StackSearch =
    { Id: StackId
      AuthorId: UserId
      ExampleId: ExampleId
      ExampleRevisionId: RevisionId
      FrontPersonalField: string
      BackPersonalField: string
      Tags: string Set
      Cards: CardSearch list }
module StackSearch =
    let fromSummary (summary: Stack.Events.Summary) exampleId =
        let fromCardSummary (card: Stack.Events.Card) =
            let details =
                match card.Details with
                | Stack.Events.ShadowableDetails d -> d
            { SubtemplateName = card.SubtemplateName
              CardSettingId = card.CardSettingId
              DeckId = card.DeckId
              Due = details.Due
              IsLapsed = details.IsLapsed
              State = card.State }
        { Id = summary.Id
          AuthorId = summary.AuthorId
          ExampleId = exampleId
          ExampleRevisionId = summary.ExampleRevisionId
          FrontPersonalField = summary.FrontPersonalField
          BackPersonalField = summary.BackPersonalField
          Tags = summary.Tags
          Cards = summary.Cards |> List.map fromCardSummary }
    let fromTagsChanged (e: Stack.Events.TagsChanged) (stackSearch: StackSearch) =
        { stackSearch with Tags = e.Tags }
    let private mapCard subtemplateName f (card: CardSearch) =
        if card.SubtemplateName = subtemplateName
        then f card
        else card
    let private mapCards subtemplateName f =
        List.map (mapCard subtemplateName f)
    let fromCardStateChanged (e: Stack.Events.CardStateChanged) (stack: StackSearch) =
        let cards = stack.Cards |> mapCards e.SubtemplateName (fun x -> { x with State = e.State})
        { stack with Cards = cards }

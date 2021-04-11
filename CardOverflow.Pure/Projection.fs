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
      Collected: RevisionId Option
      EditSummary: string }
type ExampleSearch_OnCollected =
    { ExampleId: ExampleId
      CollectorId: UserId
      RevisionId: RevisionId }
type ExampleSearch_OnDiscarded =
    { ExampleId: ExampleId
      DiscarderId: UserId }

let n = Unchecked.defaultof<ExampleSearch>
module ExampleSearch =
    let fromSummary (summary: Example.Events.Summary) displayName templateRevision =
        [ nameof n.Id              , summary.Id               |> box
          nameof n.ParentId        , summary.ParentId         |> box
          nameof n.RevisionId      , summary.RevisionIds.Head |> box
          nameof n.Title           , summary.Title            |> box
          nameof n.AuthorId        , summary.AuthorId         |> box
          nameof n.Author          , displayName              |> box
          nameof n.TemplateRevision, templateRevision         |> box
          nameof n.FieldValues     , summary.FieldValues      |> box
          nameof n.EditSummary     , summary.EditSummary      |> box
        ] |> Map.ofList
    let fromEdited (exampleId: ExampleId) (edited: Example.Events.Edited) templateRevision =
        [ nameof n.Id              , exampleId               |> box
          nameof n.RevisionId      , edited.RevisionId       |> box
          nameof n.Title           , edited.Title            |> box
          nameof n.TemplateRevision, templateRevision        |> box
          nameof n.FieldValues     , edited.FieldValues      |> box
          nameof n.EditSummary     , edited.EditSummary      |> box
        ] |> Map.ofList

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
    let fromRevisionChanged (e: Stack.Events.RevisionChanged) (stackSearch: StackSearch) =
        { stackSearch with ExampleRevisionId = e.RevisionId }
    let private mapCard subtemplateName f (card: CardSearch) =
        if card.SubtemplateName = subtemplateName
        then f card
        else card
    let private mapCards subtemplateName f =
        List.map (mapCard subtemplateName f)
    let fromCardStateChanged (e: Stack.Events.CardStateChanged) (stack: StackSearch) =
        let cards = stack.Cards |> mapCards e.SubtemplateName (fun x -> { x with State = e.State})
        { stack with Cards = cards }

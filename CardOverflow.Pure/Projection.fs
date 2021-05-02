module Domain.Projection

open FsCodec
open FsCodec.NewtonsoftJson
open NodaTime
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open Domain.Summary

[<CLIMutable>]
type ExampleSearch =
    { Id: ExampleId
      ParentId: ExampleId option
      CurrentRevision: ExampleRevisionOrdinal
      Title: string
      AuthorId: UserId
      Author: string
      TemplateRevision: Template.RevisionSummary
      FieldValues: Map<string, string>
      Collected: ExampleRevisionOrdinal Option
      EditSummary: string }
type ExampleSearch_OnCollected =
    { ExampleId: ExampleId
      CollectorId: UserId
      Revision: ExampleRevisionOrdinal }
type ExampleSearch_OnDiscarded =
    { ExampleId: ExampleId
      DiscarderId: UserId }

let n = Unchecked.defaultof<ExampleSearch>
module ExampleSearch =
    let fromSummary (summary: Example) displayName templateRevision =
        [ nameof n.Id              , summary.Id               |> box
          nameof n.ParentId        , summary.ParentId         |> box
          nameof n.CurrentRevision , summary.CurrentRevision  |> box
          nameof n.Title           , summary.Title            |> box
          nameof n.AuthorId        , summary.AuthorId         |> box
          nameof n.Author          , displayName              |> box
          nameof n.TemplateRevision, templateRevision         |> box
          nameof n.FieldValues     , summary.FieldValues      |> box
          nameof n.EditSummary     , summary.EditSummary      |> box
        ] |> Map.ofList
    let fromEdited (edited: Example.Events.Edited) templateRevision =
        [ nameof n.CurrentRevision , edited.Revision         |> box
          nameof n.Title           , edited.Title            |> box
          nameof n.TemplateRevision, templateRevision        |> box
          nameof n.FieldValues     , edited.FieldValues      |> box
          nameof n.EditSummary     , edited.EditSummary      |> box
        ] |> Map.ofList

type CardSearch =
    { Pointer: CardTemplatePointer
      CardSettingId: CardSettingId
      DeckId: DeckId
      Due: Instant
      IsLapsed: bool
      State: CardState }
type StackSearch =
    { Id: StackId
      AuthorId: UserId
      ExampleId: ExampleId
      ExampleRevisionId: ExampleRevisionId
      FrontPersonalField: string
      BackPersonalField: string
      Tags: string Set
      Cards: CardSearch list }
module StackSearch =
    let n = Unchecked.defaultof<StackSearch>
    let fromSummary (summary: Stack) exampleId =
        let fromCardSummary (card: Card) =
            let details =
                match card.Details with
                | ShadowableDetails d -> d
            { Pointer = card.Pointer
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
    let fromTagsChanged (e: Stack.Events.TagsChanged) =
        [ nameof n.Tags, e.Tags |> box ]
        |> Map.ofList
    let fromRevisionChanged (e: Stack.Events.RevisionChanged) =
        [ nameof n.ExampleRevisionId, e.RevisionId |> box ]
        |> Map.ofList
    let private mapCard pointer f (card: CardSearch) =
        if card.Pointer = pointer
        then f card
        else card
    let private mapCards pointer f =
        List.map (mapCard pointer f)
    let fromCardStateChanged (e: Stack.Events.CardStateChanged) (stack: StackSearch) =
        let cards = stack.Cards |> mapCards e.Pointer (fun x -> { x with State = e.State})
        { stack with Cards = cards }

[<CLIMutable>]
type TemplateSearch =
    { Id: TemplateId
      CurrentRevision: TemplateRevisionOrdinal
      AuthorId: UserId
      Author: string
      Name: string
      Css: string
      Fields: Field list
      Created: Instant
      Modified: Instant
      LatexPre: string
      LatexPost: string
      CardTemplates: TemplateType
      Collected: TemplateRevisionOrdinal Option }
type TemplateSearch_OnCollected =
    { TemplateId: TemplateId
      CollectorId: UserId
      Revision: TemplateRevisionOrdinal }
type TemplateSearch_OnDiscarded =
    { TemplateId: TemplateId
      DiscarderId: UserId }
module TemplateSearch =
    open Template
    let n = Unchecked.defaultof<TemplateSearch>
    let fromSummary (summary: Template) displayName =
        [ nameof n.Id             , summary.Id                 |> box
          nameof n.CurrentRevision, summary.CurrentRevision    |> box
          nameof n.AuthorId       , summary.AuthorId           |> box
          nameof n.Author         , displayName                |> box
          nameof n.Name           , summary.Name               |> box
          nameof n.Css            , summary.Css                |> box
          nameof n.Fields         , summary.Fields             |> box
          nameof n.Created        , summary.Created            |> box
          nameof n.Modified       , summary.Modified           |> box
          nameof n.LatexPre       , summary.LatexPre           |> box
          nameof n.LatexPost      , summary.LatexPost          |> box
          nameof n.CardTemplates  , summary.CardTemplates      |> box
        ] |> Map.ofList
    let fromEdited (edited: Events.Edited) =
        [ nameof n.CurrentRevision   , edited.Revision       |> box
          nameof n.Name              , edited.Name           |> box
          nameof n.Css               , edited.Css            |> box
          nameof n.Fields            , edited.Fields         |> box
          nameof n.Modified          , edited.Modified       |> box
          nameof n.LatexPre          , edited.LatexPre       |> box
          nameof n.LatexPost         , edited.LatexPost      |> box
          nameof n.CardTemplates     , edited.CardTemplates  |> box
        ] |> Map.ofList

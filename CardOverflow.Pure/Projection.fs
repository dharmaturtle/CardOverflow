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
            { Pointer = card.Pointer
              CardSettingId = card.CardSettingId
              DeckId = card.DeckId
              Due = card.Due
              IsLapsed = card.IsLapsed
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
    let fromSummary displayName (template: Template) =
        [ nameof n.Id             , template.Id                 |> box
          nameof n.CurrentRevision, template.CurrentRevision    |> box
          nameof n.AuthorId       , template.AuthorId           |> box
          nameof n.Author         , displayName                 |> box
          nameof n.Name           , template.Name               |> box
          nameof n.Css            , template.Css                |> box
          nameof n.Fields         , template.Fields             |> box
          nameof n.Created        , template.Created            |> box
          nameof n.Modified       , template.Modified           |> box
          nameof n.LatexPre       , template.LatexPre           |> box
          nameof n.LatexPost      , template.LatexPost          |> box
          nameof n.CardTemplates  , template.CardTemplates      |> box
        ] |> Map.ofList
    let fromEdited (edited: Events.Edited) =
        [ nameof n.CurrentRevision   , edited.Revision             |> box
          nameof n.Name              , edited.Name                 |> box
          nameof n.Css               , edited.Css                  |> box
          nameof n.Fields            , edited.Fields               |> box
          nameof n.Modified          , edited.Meta.ServerCreatedAt |> box
          nameof n.LatexPre          , edited.LatexPre             |> box
          nameof n.LatexPost         , edited.LatexPost            |> box
          nameof n.CardTemplates     , edited.CardTemplates        |> box
        ] |> Map.ofList

open System
type ClientEvent<'T> =
    { StreamId: Guid
      Event: 'T }

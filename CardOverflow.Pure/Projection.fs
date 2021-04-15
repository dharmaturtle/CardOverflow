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
    let fromEdited (edited: Example.Events.Edited) templateRevision =
        [ nameof n.RevisionId      , edited.RevisionId       |> box
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
    let n = Unchecked.defaultof<StackSearch>
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
    let fromTagsChanged (e: Stack.Events.TagsChanged) =
        [ nameof n.Tags, e.Tags |> box ]
        |> Map.ofList
    let fromRevisionChanged (e: Stack.Events.RevisionChanged) =
        [ nameof n.ExampleRevisionId, e.RevisionId |> box ]
        |> Map.ofList
    let private mapCard subtemplateName f (card: CardSearch) =
        if card.SubtemplateName = subtemplateName
        then f card
        else card
    let private mapCards subtemplateName f =
        List.map (mapCard subtemplateName f)
    let fromCardStateChanged (e: Stack.Events.CardStateChanged) (stack: StackSearch) =
        let cards = stack.Cards |> mapCards e.SubtemplateName (fun x -> { x with State = e.State})
        { stack with Cards = cards }

[<CLIMutable>]
type TemplateSearch =
    { Id: TemplateId
      RevisionId: TemplateRevisionId
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
      Collected: TemplateRevisionId Option }
type TemplateSearch_OnCollected =
    { TemplateId: TemplateId
      CollectorId: UserId
      RevisionId: TemplateRevisionId }
type TemplateSearch_OnDiscarded =
    { TemplateId: TemplateId
      DiscarderId: UserId }
module TemplateSearch =
    open Template
    let n = Unchecked.defaultof<TemplateSearch>
    let fromSummary (summary: Events.Summary) displayName =
        [ nameof n.Id            , summary.Id                 |> box
          nameof n.RevisionId    , summary.RevisionIds.Head   |> box
          nameof n.AuthorId      , summary.AuthorId           |> box
          nameof n.Author        , displayName                |> box
          nameof n.Name          , summary.Name               |> box
          nameof n.Css           , summary.Css                |> box
          nameof n.Fields        , summary.Fields             |> box
          nameof n.Created       , summary.Created            |> box
          nameof n.Modified      , summary.Modified           |> box
          nameof n.LatexPre      , summary.LatexPre           |> box
          nameof n.LatexPost     , summary.LatexPost          |> box
          nameof n.CardTemplates , summary.CardTemplates      |> box
        ] |> Map.ofList
    let fromEdited (edited: Events.Edited) =
        [ nameof n.RevisionId        , edited.RevisionId     |> box
          nameof n.Name              , edited.Name           |> box
          nameof n.Css               , edited.Css            |> box
          nameof n.Fields            , edited.Fields         |> box
          nameof n.Modified          , edited.Modified       |> box
          nameof n.LatexPre          , edited.LatexPre       |> box
          nameof n.LatexPost         , edited.LatexPost      |> box
          nameof n.CardTemplates     , edited.CardTemplates  |> box
        ] |> Map.ofList
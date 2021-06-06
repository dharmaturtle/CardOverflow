module Domain.Projection

open FsCodec
open FsCodec.NewtonsoftJson
open NodaTime
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open Domain.Summary

type TemplateInstance =
    { Ordinal: TemplateRevisionOrdinal
      TemplateId: TemplateId
      AuthorId: UserId
      Name: string
      Css: string
      Fields: Field list
      Created: Instant
      LatexPre: string
      LatexPost: string
      CardTemplates: TemplateType
      EditSummary: string }
with
    member this.Id = this.TemplateId, this.Ordinal

let toTemplateInstance (t: Template) o =
    let r = t.Revisions |> List.filter (fun x -> x.Ordinal = o) |> List.exactlyOne
    { Ordinal       = r.Ordinal
      TemplateId    = t.Id
      AuthorId      = t.AuthorId
      Name          = r.Name
      Css           = r.Css
      Fields        = r.Fields
      Created       = r.Created
      LatexPre      = r.LatexPre
      LatexPost     = r.LatexPost
      CardTemplates = r.CardTemplates
      EditSummary   = r.EditSummary }

let toCurrentTemplateInstance t =
    toTemplateInstance t t.CurrentRevision.Ordinal

[<RequireQualifiedAccess>]
module Kvs =
    type TemplateRevision =
        { Ordinal: TemplateRevisionOrdinal
          Name: string
          Css: string
          Fields: Field list
          Created: Instant
          LatexPre: string
          LatexPost: string
          CardTemplates: TemplateType
          Collectors: int
          EditSummary: string }
    
    type Template =
        { Id: TemplateId
          CommandIds: CommandId Set
          AuthorId: UserId
          Author: string
          Revisions: TemplateRevision list
          Modified: Instant
          Visibility: Visibility }
      with
        member this.CurrentRevision = this.Revisions |> List.maxBy (fun x -> x.Ordinal)
        member this.CurrentRevisionId = this.Id, this.CurrentRevision.Ordinal
    
    let toKvsTemplate author collectorsByOrdinal (template: Summary.Template) =
        let toKvsTemplateRevision (revision: Summary.TemplateRevision) =
            { Ordinal       = revision.Ordinal
              Name          = revision.Name
              Css           = revision.Css
              Fields        = revision.Fields
              Created       = revision.Created
              LatexPre      = revision.LatexPre
              LatexPost     = revision.LatexPost
              CardTemplates = revision.CardTemplates
              Collectors    = collectorsByOrdinal |> Map.tryFind revision.Ordinal |> Option.defaultValue 0
              EditSummary   = revision.EditSummary }
        { Id         = template.Id
          CommandIds = template.CommandIds
          AuthorId   = template.AuthorId
          Author     = author
          Revisions  = template.Revisions |> List.map toKvsTemplateRevision
          Modified   = template.Modified
          Visibility = template.Visibility }

    let toTemplate (template: Template) =
        let toTemplateRevision (revision: TemplateRevision) : Summary.TemplateRevision =
            { Ordinal       = revision.Ordinal
              Name          = revision.Name
              Css           = revision.Css
              Fields        = revision.Fields
              Created       = revision.Created
              LatexPre      = revision.LatexPre
              LatexPost     = revision.LatexPost
              CardTemplates = revision.CardTemplates
              EditSummary   = revision.EditSummary }
        { Id         = template.Id
          CommandIds = template.CommandIds
          AuthorId   = template.AuthorId
          Revisions  = template.Revisions |> List.map toTemplateRevision
          Modified   = template.Modified
          Visibility = template.Visibility }
    
    let toTemplateInstance ((templateId, ordinal): TemplateRevisionId) (template: Template) =
        if templateId <> template.Id then failwith "You passed in the wrong Template."
        let tr = template.Revisions |> List.filter (fun x -> x.Ordinal = ordinal) |> List.exactlyOne
        { Ordinal       = tr.Ordinal
          TemplateId    = template.Id
          AuthorId      = template.AuthorId
          Name          = tr.Name
          Css           = tr.Css
          Fields        = tr.Fields
          Created       = tr.Created
          LatexPre      = tr.LatexPre
          LatexPost     = tr.LatexPost
          CardTemplates = tr.CardTemplates
          EditSummary   = tr.EditSummary }

    let evolveKvsTemplateEdited (edited: Template.Events.Edited) (template: Template) =
        let collectorsByOrdinal = template.Revisions |> List.map (fun x -> x.Ordinal, x.Collectors) |> Map.ofList
        template |> toTemplate |> Template.Fold.evolveEdited edited |> toKvsTemplate template.Author collectorsByOrdinal // lowTODO needs fixing after multiple authors implemented
    
    type ExampleRevision =
        { Ordinal: ExampleRevisionOrdinal
          Title: string
          TemplateInstance: TemplateInstance
          FieldValues: Map<string, string>
          Collectors: int
          EditSummary: string }
    
    type Example =
        { Id: ExampleId
          CommandIds: CommandId Set
          ParentId: ExampleId option
          Revisions: ExampleRevision list
          AuthorId: UserId
          Author: string
          AnkiNoteId: int64 option
          Visibility: Visibility }
      with
        member this.CurrentRevision = this.Revisions |> List.maxBy (fun x -> x.Ordinal)
        member this.CurrentRevisionId = this.Id, this.CurrentRevision.Ordinal
        member this.Collectors = this.Revisions |> List.sumBy (fun x -> x.Collectors)
    
    let toKvsExample author collectorsByOrdinal (templateInstances: TemplateInstance list) (example: Summary.Example) =
        let toKvsExampleRevision (revision: Summary.ExampleRevision) =
            { Ordinal          = revision.Ordinal
              Title            = revision.Title
              TemplateInstance = templateInstances |> Seq.filter (fun x -> x.Id = revision.TemplateRevisionId) |> Seq.head
              FieldValues      = revision.FieldValues
              Collectors       = collectorsByOrdinal |> Map.tryFind revision.Ordinal |> Option.defaultValue 0
              EditSummary      = revision.EditSummary }
        { Id         = example.Id
          CommandIds = example.CommandIds
          ParentId   = example.ParentId
          Revisions  = example.Revisions |> List.map toKvsExampleRevision
          AuthorId   = example.AuthorId
          Author     = author
          AnkiNoteId = example.AnkiNoteId
          Visibility = example.Visibility }
    
    let toExample (example: Example) =
        let toExampleRevision (revision: ExampleRevision) =
            { Ordinal            = revision.Ordinal
              Title              = revision.Title
              TemplateRevisionId = revision.TemplateInstance.Id
              FieldValues        = revision.FieldValues
              EditSummary        = revision.EditSummary }
        { Id         = example.Id
          CommandIds = example.CommandIds
          ParentId   = example.ParentId
          Revisions  = example.Revisions |> List.map toExampleRevision
          AuthorId   = example.AuthorId
          AnkiNoteId = example.AnkiNoteId
          Visibility = example.Visibility }

    let evolveKvsExampleEdited (edited: Example.Events.Edited) templateInstances (example: Example) =
        let collectorsByOrdinal = example.Revisions |> List.map (fun x -> x.Ordinal, x.Collectors) |> Map.ofList
        example |> toExample |> Example.Fold.evolveEdited edited |> toKvsExample example.Author collectorsByOrdinal templateInstances // lowTODO needs fixing after multiple authors implemented

    let incrementExample ordinal (example: Example) =
        let tryIncrement (revision: ExampleRevision) =
            { revision with
                Collectors =
                    if revision.Ordinal = ordinal then
                        revision.Collectors + 1
                    else revision.Collectors }
        { example with
            Revisions = example.Revisions |> List.map tryIncrement }
    let decrementExample ordinal (example: Example) =
        let tryDecrement (revision: ExampleRevision) =
            { revision with
                Collectors =
                    if revision.Ordinal = ordinal then
                        revision.Collectors - 1
                    else revision.Collectors }
        { example with
            Revisions = example.Revisions |> List.map tryDecrement }
    
open System.Linq
type ExampleInstance =
    { Ordinal: ExampleRevisionOrdinal
      ExampleId: ExampleId
      Title: string
      AuthorId: UserId
      Template: TemplateInstance
      FieldValues: Map<string, string>
      EditSummary: string }
  with
    member this.Id = this.ExampleId, this.Ordinal
    member this.FrontBackFrontSynthBackSynth (pointer: CardTemplatePointer) =
        match pointer with
        | CardTemplatePointer.Normal g ->
            match this.Template.CardTemplates with
            | Standard ts ->
                let t = ts.Single(fun x -> x.Id = g)
                CardHtml.generate
                <| (this.FieldValues.Select(fun x -> x.Key, x.Value |?? lazy "") |> Seq.toList)
                <| t.Front
                <| t.Back
                <| this.Template.Css
                <| CardHtml.Standard
            | _ -> failwith "Must generate a standard view for a standard template."
        | CardTemplatePointer.Cloze i ->
            match this.Template.CardTemplates with
            | Cloze c ->
                CardHtml.generate
                <| (this.FieldValues.Select(fun x -> x.Key, x.Value |?? lazy "") |> Seq.toList)
                <| c.Front
                <| c.Back
                <| this.Template.Css
                <| CardHtml.Cloze (int16 i)
            | _ -> failwith "Must generate a cloze view for a cloze template."

open System
open FSharp.Control.Tasks
open System.Threading.Tasks

let toExampleInstance (e: Example) ordinal (getTemplateInstance: Func<TemplateRevisionId, Task<TemplateInstance>>) = task {
    let r = e.Revisions |> List.filter (fun x -> x.Ordinal = ordinal) |> List.exactlyOne
    let! templateInstance = getTemplateInstance.Invoke r.TemplateRevisionId
    return
        { Ordinal          = r.Ordinal
          ExampleId        = e.Id
          Title            = r.Title
          AuthorId         = e.AuthorId
          Template         = templateInstance
          FieldValues      = r.FieldValues
          EditSummary      = r.EditSummary }
    }

[<CLIMutable>]
type ExampleSearch =
    { Id: ExampleId
      ParentId: ExampleId option
      CurrentOrdinal: ExampleRevisionOrdinal
      Title: string
      AuthorId: UserId
      Author: string
      TemplateInstance: TemplateInstance
      FieldValues: Map<string, string>
      Collectors: int
      EditSummary: string }

let n = Unchecked.defaultof<ExampleSearch>
module ExampleSearch =
    let fromSummary (summary: Example) displayName (templateInstance: TemplateInstance) =
        [ nameof n.Id              , summary.Id                          |> box
          nameof n.ParentId        , summary.ParentId                    |> box
          nameof n.CurrentOrdinal  , summary.CurrentRevision.Ordinal     |> box
          nameof n.Title           , summary.CurrentRevision.Title       |> box
          nameof n.AuthorId        , summary.AuthorId                    |> box
          nameof n.Author          , displayName                         |> box
          nameof n.TemplateInstance, templateInstance                    |> box
          nameof n.FieldValues     , summary.CurrentRevision.FieldValues |> box
          nameof n.EditSummary     , summary.CurrentRevision.EditSummary |> box
        ] |> Map.ofList
    let fromEdited (edited: Example.Events.Edited) (templateInstance: TemplateInstance) =
        [ nameof n.CurrentOrdinal  , edited.Ordinal          |> box
          nameof n.Title           , edited.Title            |> box
          nameof n.TemplateInstance, templateInstance        |> box
          nameof n.FieldValues     , edited.FieldValues      |> box
          nameof n.EditSummary     , edited.EditSummary      |> box
        ] |> Map.ofList

[<CLIMutable>]
type TemplateSearch =
    { Id: TemplateId
      CurrentOrdinal: TemplateRevisionOrdinal
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
      Ordinal: TemplateRevisionOrdinal }
type TemplateSearch_OnDiscarded =
    { TemplateId: TemplateId
      DiscarderId: UserId }
module TemplateSearch =
    open Template
    let n = Unchecked.defaultof<TemplateSearch>
    let fromSummary (displayName: string) (template: Template) =
        [ nameof n.Id             , template.Id                            |> box
          nameof n.CurrentOrdinal , template.CurrentRevision.Ordinal       |> box
          nameof n.AuthorId       , template.AuthorId                      |> box
          nameof n.Author         , displayName                            |> box
          nameof n.Name           , template.CurrentRevision.Name          |> box
          nameof n.Css            , template.CurrentRevision.Css           |> box
          nameof n.Fields         , template.CurrentRevision.Fields        |> box
          nameof n.Created        , template.CurrentRevision.Created       |> box
          nameof n.Modified       , template.Modified                      |> box
          nameof n.LatexPre       , template.CurrentRevision.LatexPre      |> box
          nameof n.LatexPost      , template.CurrentRevision.LatexPost     |> box
          nameof n.CardTemplates  , template.CurrentRevision.CardTemplates |> box
        ] |> Map.ofList
    let fromEdited (edited: Events.Edited) =
        [ nameof n.CurrentOrdinal    , edited.Ordinal              |> box
          nameof n.Name              , edited.Name                 |> box
          nameof n.Css               , edited.Css                  |> box
          nameof n.Fields            , edited.Fields               |> box
          nameof n.Modified          , edited.Meta.ServerCreatedAt |> box
          nameof n.LatexPre          , edited.LatexPre             |> box
          nameof n.LatexPost         , edited.LatexPost            |> box
          nameof n.CardTemplates     , edited.CardTemplates        |> box
        ] |> Map.ofList

type ClientEvent<'T> =
    { StreamId: Guid
      Event: 'T }

[<CLIMutable>]
type ViewDeck = {
    Id: DeckId
    Visibility: Visibility
    IsDefault: bool
    [<System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 250 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Name: string
    DueCount: int
    AllCount: int
}

module Dexie =
    let private _user events =
        match User.Fold.fold User.Fold.initial events with
        | User.Fold.Active u ->
            [ "id"         , u.Id |> string
              "summary"    , Serdes.Serialize(u, jsonSerializerSettings)
            ] |> Map.ofList
        | User.Fold.Initial -> failwith "impossible"
    let private _deck events =
        match Deck.Fold.fold Deck.Fold.initial events with
        | Deck.Fold.Active d ->
            [ "id"         , d.Id |> string
              "name"       , d.Name
              "description", d.Description
              "summary"    , Serdes.Serialize(d, jsonSerializerSettings)
            ] |> Map.ofList
        | Deck.Fold.Initial -> failwith "impossible"
    let private _template events =
        match Template.Fold.fold Template.Fold.initial events with
        | Template.Fold.Active t ->
            [ "id"         , t.Id |> string
              "summary"    , Serdes.Serialize(t, jsonSerializerSettings)
            ] |> Map.ofList |> Some
        | Template.Fold.Dmca _ -> None // lowTODO display something
        | Template.Fold.Initial -> failwith "impossible"
    let private _example events =
        match Example.Fold.fold Example.Fold.initial events with
        | Example.Fold.Active e ->
            [ "id"         , e.Id |> string
              "summary"    , Serdes.Serialize(e, jsonSerializerSettings)
            ] |> Map.ofList |> Some
        | Example.Fold.Dmca _ -> None // lowTODO display something
        | Example.Fold.Initial -> failwith "impossible"
    let private _stackAndCards events =
        match Stack.Fold.fold Stack.Fold.initial events with
        | Stack.Fold.Active stack ->
            let stackSummary =
                [ "id"         , stack.Id |> string |> box
                  "dues"       , stack.Cards |> List.map (fun x -> x.Due.ToString("g", System.Globalization.CultureInfo.InvariantCulture)) |> box
                  "summary"    , Serdes.Serialize(stack, jsonSerializerSettings) |> box
                ] |> Map.ofList
            let cardSummaries =
                stack.Cards |> List.map (fun card ->
                    let pointer =
                        match card.Pointer with
                        | CardTemplatePointer.Normal g -> $"Normal-{g}"
                        | CardTemplatePointer.Cloze i  -> $"Cloze-{i}"
                    [ "id"         , $"{stack.Id}-{pointer}"
                      "due"        , card.Due.ToString("g", System.Globalization.CultureInfo.InvariantCulture)
                      "deckId"     , card.DeckId |> string
                      "state"      , card.State  |> string
                      "summary"    , Serdes.Serialize(stack, jsonSerializerSettings)
                    ] |> Map.ofList
                )
            (stackSummary, cardSummaries) |> Some
        | Stack.Fold.Discard _ -> None
        | Stack.Fold.Initial -> failwith "impossible"
    let summarizeUsers (events: seq<ClientEvent<User.Events.Event>>) =
        events
        |> Seq.groupBy (fun x -> x.StreamId)
        |> Seq.map (fun (_, xs) -> xs |> Seq.map (fun x -> x.Event) |> _user)
    let summarizeDecks (events: seq<ClientEvent<Deck.Events.Event>>) =
        events
        |> Seq.groupBy (fun x -> x.StreamId)
        |> Seq.map (fun (_, xs) -> xs |> Seq.map (fun x -> x.Event) |> _deck)
    let summarizeTemplates (events: seq<ClientEvent<Template.Events.Event>>) =
        events
        |> Seq.groupBy (fun x -> x.StreamId)
        |> Seq.choose (fun (_, xs) -> xs |> Seq.map (fun x -> x.Event) |> _template)
    let summarizeExamples (events: seq<ClientEvent<Example.Events.Event>>) =
        events
        |> Seq.groupBy (fun x -> x.StreamId)
        |> Seq.choose (fun (_, xs) -> xs |> Seq.map (fun x -> x.Event) |> _example)
    let summarizeStacksAndCards (events: seq<ClientEvent<Stack.Events.Event>>) =
        let stacksAndCards =
            events
            |> Seq.groupBy (fun x -> x.StreamId)
            |> Seq.choose (fun (_, xs) -> xs |> Seq.map (fun x -> x.Event) |> _stackAndCards)
        let stacks = stacksAndCards |> Seq.map     (fun (s, _) -> s)
        let cards  = stacksAndCards |> Seq.collect (fun (_, c) -> c)
        stacks, cards
    
    let parseNextQuizCard (stackJson: string) =
        if stackJson = null then
            None
        else
            let stack = Serdes.Deserialize<Summary.Stack>(stackJson, jsonSerializerSettings)
            let card = stack.Cards |> Seq.minBy (fun x -> x.Due)
            (stack, card) |> Some
    let toViewDeck (deck: Summary.Deck) allCount dueCount defaultDeckId =
        {   Id         = deck.Id
            Visibility = deck.Visibility
            IsDefault  = deck.Id = defaultDeckId
            Name       = deck.Name
            DueCount   = dueCount
            AllCount   = allCount }
        


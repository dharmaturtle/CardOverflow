module Domain.Example

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open Domain.Summary

let streamName (id: ExampleId) = StreamName.create "Example" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Edited = // copy fields from this to Created
        { Meta: Meta
          Revision: ExampleRevisionOrdinal
          Title: string
          TemplateRevisionId: TemplateRevisionId
          FieldValues: Map<string, string>
          EditSummary: string }
    
    type Created =
        { Meta: Meta
          Id: ExampleId
          ParentId: ExampleId option
          AnkiNoteId: int64 option
          Visibility: Visibility
            
          // from Edited above
          //Revision: ExampleRevisionOrdinal // automatically set to 0
          Title: string
          TemplateRevisionId: TemplateRevisionId
          FieldValues: Map<string, string>
          EditSummary: string }

    type Event =
        | Created of Created
        | Edited  of Edited
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type State =
        | Initial
        | Active of Example
        | Dmca of DmcaTakeDown
    let initial : State = State.Initial
    let initialExampleRevisionOrdinal = 0<exampleRevisionOrdinal>
    
    let mapActive f = function
        | Active a -> f a |> Active
        | x -> x
    
    let evolveEdited
        ({  Revision = revision
            Title = title
            TemplateRevisionId = templateRevisionId
            FieldValues = fieldValues
            EditSummary = editSummary }: Events.Edited)
        (s: Example) =
        { s with
            CurrentRevision    = revision
            Title              = title
            TemplateRevisionId = templateRevisionId
            FieldValues        = fieldValues
            EditSummary        = editSummary }
    
    let evolveCreated (created: Events.Created) =
        {   Id                 = created.Id
            ParentId           = created.ParentId
            CurrentRevision    = initialExampleRevisionOrdinal
            Title              = created.Title
            AuthorId           = created.Meta.UserId
            TemplateRevisionId = created.TemplateRevisionId
            AnkiNoteId         = created.AnkiNoteId
            FieldValues        = created.FieldValues
            Visibility         = created.Visibility
            EditSummary        = created.EditSummary }
    
    let evolve state = function
        | Events.Created s -> s |> evolveCreated |> State.Active
        | Events.Edited e -> state |> mapActive (evolveEdited e)

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit      : Events.Event seq -> State = fold initial
    let isOrigin = function Events.Created _ -> true | _ -> false

let getActive state =
    match state with
    | Fold.State.Active e -> Ok e
    | _ -> Error "Example doesn't exist."

open System.Linq
open CardOverflow.Pure.Extensions
type Revision =
    { Revision: ExampleRevisionOrdinal
      ExampleId: ExampleId
      Title: string
      AuthorId: UserId
      TemplateRevision: TemplateRevision
      FieldValues: Map<string, string>
      EditSummary: string }
  with
    member this.Id = this.ExampleId, this.Revision
    member this.FrontBackFrontSynthBackSynth (pointer: CardTemplatePointer) =
        match pointer with
        | CardTemplatePointer.Normal g ->
            match this.TemplateRevision.CardTemplates with
            | Standard ts ->
                let t = ts.Single(fun x -> x.Id = g)
                CardHtml.generate
                <| this.FieldValues.Select(fun x -> x.Key, x.Value |?? lazy "").ToFList()
                <| t.Front
                <| t.Back
                <| this.TemplateRevision.Css
                <| CardHtml.Standard
            | _ -> failwith "Must generate a standard view for a standard template."
        | CardTemplatePointer.Cloze i ->
            match this.TemplateRevision.CardTemplates with
            | Cloze c ->
                CardHtml.generate
                <| this.FieldValues.Select(fun x -> x.Key, x.Value |?? lazy "").ToFList()
                <| c.Front
                <| c.Back
                <| this.TemplateRevision.Css
                <| CardHtml.Cloze (int16 i)
            | _ -> failwith "Must generate a cloze view for a cloze template."

let toRevision templateRevision (b: Example) =
    { Revision = b.CurrentRevision
      ExampleId = b.Id
      Title = b.Title
      AuthorId = b.AuthorId
      TemplateRevision = templateRevision
      FieldValues = b.FieldValues
      EditSummary = b.EditSummary }

let validateFieldValues (fieldValues: Map<string, string>) = result {
    for field, value in fieldValues |> Map.toSeq do
        do! Template.validateFieldName field
        do! (value.Length <= 10_000) |> Result.requireTrue $"The value of '{field}' must be less than 10,000 characters, but it has {value.Length} characters."
    }

let editSummaryMax = 200
let validateEditSummary (editSummary: string) = result {
    do! (editSummary.Length <= editSummaryMax) |> Result.requireTrue $"The edit summary must be less than {editSummaryMax} characters, but it has {editSummary.Length} characters."
    }

let titleMax = 200
let validateTitle (title: string) = result {
    do! (title.Length <= titleMax) |> Result.requireTrue $"The title must be less than {titleMax} characters, but it has {title.Length} characters."
    }

let validateCreate (created: Events.Created) = result {
    do! validateFieldValues created.FieldValues
    do! validateEditSummary created.EditSummary
    do! validateTitle created.Title
    }

let validateRevisionIncrements (example: Example) (edited: Events.Edited) =
    let expected = example.CurrentRevision + 1<exampleRevisionOrdinal>
    Result.requireEqual
        expected
        edited.Revision
        $"The new Revision was expected to be '{expected}', but is instead '{edited.Revision}'. This probably means you edited the example, saved, then edited an *old* version of the example and then tried to save it."

let checkPermissions (meta: Meta) (e: Example) =
    Result.requireEqual meta.UserId e.AuthorId "You aren't allowed to edit this Example."

let validateEdit (example: Example) (edited: Events.Edited) = result {
    do! checkPermissions edited.Meta example
    do! validateRevisionIncrements example edited
    do! validateFieldValues edited.FieldValues
    do! validateEditSummary edited.EditSummary
    do! validateTitle edited.Title
    }

let decideCreate (created: Events.Created) state =
    match state with
    | Fold.State.Active _ -> Error $"Example '{created.Id}' already exists."
    | Fold.State.Dmca   _ -> Error $"Example '{created.Id}' already exists (though it's DMCAed)."
    | Fold.State.Initial  -> validateCreate created
    |> addEvent (Events.Created created)

let decideEdit (edited: Events.Edited) (exampleId: ExampleId) state =
    match state with
    | Fold.State.Initial  -> Error $"Template '{exampleId}' doesn't exist so you can't edit it."
    | Fold.State.Dmca   _ -> Error $"Template '{exampleId}' is DMCAed so you can't edit it."
    | Fold.State.Active s -> validateEdit s edited
    |> addEvent (Events.Edited edited)

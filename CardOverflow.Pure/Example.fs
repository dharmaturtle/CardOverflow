module Domain.Example

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling

let streamName (id: ExampleId) = StreamName.create "Example" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Summary =
        { Id: ExampleId
          ParentId: ExampleId option
          RevisionIds: RevisionId list
          Title: string
          AuthorId: UserId
          TemplateRevisionId: TemplateRevisionId
          AnkiNoteId: int64 option
          FieldValues: Map<string, string>
          EditSummary: string }
    type Edited =
        { RevisionId: RevisionId
          Title: string
          TemplateRevisionId: TemplateRevisionId
          FieldValues: Map<string, string>
          EditSummary: string }

    type Event =
        | Created of Summary
        | Edited  of Edited
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type State =
        | Initial
        | Active of Events.Summary
        | Dmca of DmcaTakeDown
    let initial : State = State.Initial
    
    let mapActive f = function
        | Active a -> f a |> Active
        | x -> x
    
    let evolveEdited
        ({  RevisionId = revisionId
            Title = title
            TemplateRevisionId = templateRevisionId
            FieldValues = fieldValues
            EditSummary = editSummary }: Events.Edited)
        (s: Events.Summary) =
        { s with
            RevisionIds            = revisionId :: s.RevisionIds
            Title              = title
            TemplateRevisionId = templateRevisionId
            FieldValues        = fieldValues
            EditSummary        = editSummary }
    
    let evolve state = function
        | Events.Created s -> State.Active s
        | Events.Edited e -> state |> mapActive (evolveEdited e)

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Created _ -> true | _ -> false

type RevisionSummary =
    { Id: RevisionId
      ParentedExampleId: ParentedExampleId
      Title: string
      AuthorId: UserId
      TemplateRevision: Template.RevisionSummary
      FieldValues: Map<string, string>
      EditSummary: string }

let toRevisionSummary templateRevision (b: Events.Summary) =
    { Id = b.RevisionIds.Head
      ParentedExampleId = ParentedExampleId.create b.Id b.ParentId
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

let validateRevisionIsUnique doesRevisionExist (revisionId: RevisionId) =
    doesRevisionExist |> Result.requireFalse $"Something already exists with the id '{revisionId}'."

let validateOneRevision (revisionIds: RevisionId list) =
    revisionIds |> List.tryExactlyOne |> Result.requireSome $"There are {revisionIds.Length} RevisionIds, but there must be exactly 1."

let validateCreate doesRevisionExist (summary: Events.Summary) = result {
    do! validateOneRevision summary.RevisionIds |> Result.ignore
    do! validateRevisionIsUnique doesRevisionExist summary.RevisionIds.Head
    do! validateFieldValues summary.FieldValues
    do! validateEditSummary summary.EditSummary
    do! validateTitle summary.Title
    }

let validateEdit callerId (summary: Events.Summary) doesRevisionExist (edited: Events.Edited) = result {
    do! validateRevisionIsUnique doesRevisionExist edited.RevisionId
    do! validateFieldValues edited.FieldValues
    do! validateEditSummary edited.EditSummary
    do! validateTitle edited.Title
    do! Result.requireEqual summary.AuthorId callerId $"You ({callerId}) aren't the author of Example {summary.Id}."
    do! summary.RevisionIds |> Seq.contains edited.RevisionId |> Result.requireFalse $"Duplicate RevisionId:{edited.RevisionId}"
    }

// medTODO validate revisionId global uniqueness

let decideCreate (summary: Events.Summary) doesRevisionExist state =
    match state with
    | Fold.State.Active _ -> Error $"Example '{summary.Id}' already exists."
    | Fold.State.Dmca _   -> Error $"Example '{summary.Id}' already exists (though it's DMCAed)."
    | Fold.State.Initial  -> validateCreate doesRevisionExist summary
    |> addEvent (Events.Created summary)

let decideEdit (edited: Events.Edited) callerId (exampleId: ExampleId) doesRevisionExist state =
    match state with
    | Fold.State.Initial  -> Error $"Template '{exampleId}' doesn't exist so you can't edit it."
    | Fold.State.Dmca   _ -> Error $"Template '{exampleId}' is DMCAed so you can't edit it."
    | Fold.State.Active s -> validateEdit callerId s doesRevisionExist edited
    |> addEvent (Events.Edited edited)

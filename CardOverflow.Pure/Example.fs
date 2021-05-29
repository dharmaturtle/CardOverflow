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
          Ordinal: ExampleRevisionOrdinal
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
          //Ordinal: ExampleRevisionOrdinal // automatically set to 0
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
        ({  Ordinal = ordinal
            Title = title
            TemplateRevisionId = templateRevisionId
            FieldValues = fieldValues
            EditSummary = editSummary }: Events.Edited)
        (s: Example) =
        if s.CurrentRevision.Ordinal + 1<exampleRevisionOrdinal> = ordinal then
            { s with
                Revisions          = { Ordinal            = ordinal
                                       Title              = title
                                       TemplateRevisionId = templateRevisionId
                                       FieldValues        = fieldValues
                                       EditSummary        = editSummary } :: s.Revisions }
        else s
    
    let evolveCreated (created: Events.Created) =
        {   Id                 = created.Id
            ParentId           = created.ParentId
            Revisions          = { Ordinal            = initialExampleRevisionOrdinal
                                   Title              = created.Title
                                   TemplateRevisionId = created.TemplateRevisionId
                                   FieldValues        = created.FieldValues
                                   EditSummary        = created.EditSummary } |> List.singleton
            AuthorId           = created.Meta.UserId
            AnkiNoteId         = created.AnkiNoteId
            Visibility         = created.Visibility }
    
    let evolve state = function
        | Events.Created s -> s |> evolveCreated |> State.Active
        | Events.Edited e -> state |> mapActive (evolveEdited e)

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit      : Events.Event seq -> State = fold initial
    let isOrigin = function Events.Created _ -> true | _ -> false

let getRevision ((exampleId, ordinal): ExampleRevisionId) (example: Fold.State) = result {
    let! example =
        match example with
        | Fold.State.Active e -> Ok e
        | _ -> Error "Example doesn't exist."
    do! Result.requireEqual example.Id exampleId "ExampleId doesn't match provided Example. This is the programmer's fault and should never be seen by users."
    return!
        example.Revisions
        |> List.filter (fun x -> x.Ordinal = ordinal)
        |> List.tryExactlyOne
        |> Result.requireSome $"Ordinal '{ordinal}' not found."
    }

let validateFieldValues (fieldValues: Map<string, string>) = result {
    for field, value in fieldValues |> Map.toSeq do
        do! Template.validateFieldName field
        do! (value.Length <= 10_000) |> Result.requireTrue (CError $"The value of '{field}' must be less than 10,000 characters, but it has {value.Length} characters.")
    }

let editSummaryMax = 200
let validateEditSummary (editSummary: string) = result {
    do! (editSummary.Length <= editSummaryMax) |> Result.requireTrue (CError $"The edit summary must be less than {editSummaryMax} characters, but it has {editSummary.Length} characters.")
    }

let titleMax = 200
let validateTitle (title: string) = result {
    do! (title.Length <= titleMax) |> Result.requireTrue (CError $"The title must be less than {titleMax} characters, but it has {title.Length} characters.")
    }

let validateCreatesCards templateRevision fieldValues =
    fieldValues
    |> Template.getCardTemplatePointers templateRevision
    |> Result.bind (Result.requireNotEmpty "This Example will not generate any cards.")
    |> Result.mapError CError

let validateCreate template (created: Events.Created) = result {
    do! validateFieldValues created.FieldValues
    do! validateEditSummary created.EditSummary
    do! validateTitle created.Title
    let! templateRevision = template |> Template.getRevision created.TemplateRevisionId
    do! validateCreatesCards templateRevision created.FieldValues
    }

let validateRevisionIncrements (example: Example) (edited: Events.Edited) =
    let expected = example.CurrentRevision.Ordinal + 1<exampleRevisionOrdinal>
    Result.requireEqual
        expected
        edited.Ordinal
        (CError $"The new Ordinal was expected to be '{expected}', but is instead '{edited.Ordinal}'. This probably means you edited the example, saved, then edited an *old* version of the example and then tried to save it.")

let checkPermissions (meta: Meta) (e: Example) =
    Result.requireEqual meta.UserId e.AuthorId (CError "You aren't allowed to edit this Example.")

let validateEdit template (example: Example) (edited: Events.Edited) = result {
    do! checkPermissions edited.Meta example
    do! validateRevisionIncrements example edited
    do! validateFieldValues edited.FieldValues
    do! validateEditSummary edited.EditSummary
    do! validateTitle edited.Title
    let! templateRevision = template |> Template.getRevision edited.TemplateRevisionId
    do! validateCreatesCards templateRevision edited.FieldValues
    }

let decideCreate template (created: Events.Created) state =
    match state with
    | Fold.State.Active _ -> CCError $"Example '{created.Id}' already exists."
    | Fold.State.Dmca   _ -> CCError $"Example '{created.Id}' already exists (though it's DMCAed)."
    | Fold.State.Initial  -> validateCreate template created
    |> addEvent (Events.Created created)

let decideEdit template (edited: Events.Edited) (exampleId: ExampleId) state =
    match state with
    | Fold.State.Initial  -> CCError $"Template '{exampleId}' doesn't exist so you can't edit it."
    | Fold.State.Dmca   _ -> CCError $"Template '{exampleId}' is DMCAed so you can't edit it."
    | Fold.State.Active s -> validateEdit template s edited
    |> addEvent (Events.Edited edited)

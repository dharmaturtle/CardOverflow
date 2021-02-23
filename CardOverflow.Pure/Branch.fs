module Domain.Branch

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling

let streamName (id: BranchId) = StreamName.create "Branch" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Summary =
        { Id: BranchId
          LeafId: LeafId
          LeafIds: LeafId list
          Title: string
          StackId: StackId
          AuthorId: UserId
          TemplateRevisionId: TemplateRevisionId
          AnkiNoteId: int64 option
          FieldValues: Map<string, string>
          EditSummary: string
          Tags: string Set }
    type Edited =
        { LeafId: LeafId
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
        //| Dmca of Events.Summary * DmcaMetadata // medTODO
    let initial : State = State.Initial
    
    let mapActive f = function
        | Active a -> f a |> Active
        | x -> x
    
    let evolveEdited (e: Events.Edited) (s: Events.Summary) =
        { s with
            LeafId             = e.LeafId
            LeafIds            = e.LeafId :: s.LeafIds
            Title              = e.Title
            TemplateRevisionId = e.TemplateRevisionId
            FieldValues        = e.FieldValues
            EditSummary        = e.EditSummary }
    
    let evolve state = function
        | Events.Created s -> State.Active s
        | Events.Edited e -> state |> mapActive (evolveEdited e)

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Created _ -> true | _ -> false

let validateFieldName (field: string) = result {
    do! Result.requireEqual field (field.Trim()) $"Remove the spaces before and/or after the field name: '{field}'."
    do! (1 <= field.Length && field.Length <= 50) |> Result.requireTrue $"The field name '{field}' must be between 1 and 50 characters."
    }

let validateFieldValues (fieldValues: Map<string, string>) = result {
    for field, value in fieldValues |> Map.toSeq do
        do! validateFieldName field
        do! (value.Length <= 10_000) |> Result.requireTrue $"The value of '{field}' must be less than 10,000 characters, but it has {value.Length} characters."
    }

let validateTag (tag: string) = result {
    do! Result.requireEqual tag (tag.Trim()) $"Remove the spaces before and/or after the tag: '{tag}'."
    do! (1 <= tag.Length && tag.Length <= 100) |> Result.requireTrue $"Tags must be between 1 and 100 characters, but '{tag}' has {tag.Length} characters."
    }

let validateTags (tags: string Set) = result {
    for tag in tags do
        do! validateTag tag
    }

let validateEditSummary (editSummary: string) = result {
    do! (editSummary.Length <= 200) |> Result.requireTrue $"The edit summary must be less than 200 characters, but it has {editSummary.Length} characters."
    }

let validateTitle (title: string) = result {
    do! (title.Length <= 200) |> Result.requireTrue $"The title must be less than 200 characters, but it has {title.Length} characters."
    }

// medTODO validate leafId global uniqueness

let decideCreate (summary: Events.Summary) state =
    match state with
    | Fold.State.Active s -> Error $"Branch '{s.Id}' already exists."
    | Fold.State.Initial  -> result {
        do! validateFieldValues summary.FieldValues
        do! validateEditSummary summary.EditSummary
        do! validateTitle summary.Title
        do! validateTags summary.Tags
    } |> addEvent (Events.Created summary)

let decideEdit (edited: Events.Edited) callerId state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit a branch that doesn't exist"
    | Fold.State.Active x -> result {
        do! Result.requireEqual x.AuthorId callerId                         "You aren't the author"
        do! x.LeafIds |> Seq.contains edited.LeafId |> Result.requireFalse $"Duplicate leafId:{x.LeafId}"
    } |> addEvent (Events.Edited edited)

module Domain.Template

open FsCodec
open FsCodec.NewtonsoftJson
open NodaTime
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling

let streamName (id: TemplateId) = StreamName.create "Template" (string id)

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Summary =
        { Id: TemplateId
          RevisionIds: TemplateRevisionId list
          AuthorId: UserId
          Name: string
          Css: string
          Fields: Field list
          Created: Instant
          Modified: Instant
          LatexPre: string
          LatexPost: string
          Templates: GromplateType
          EditSummary: string }
    type Edited =
        { RevisionId: TemplateRevisionId
          Name: string
          Css: string
          Fields: Field list
          Modified: Instant
          LatexPre: string
          LatexPost: string
          Templates: GromplateType
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
    
    let evolveEdited
        ({  RevisionId = revisionId
            Name = name
            Css = css
            Fields = fields
            Modified = modified
            LatexPre = latexPre
            LatexPost = latexPost
            Templates = templates
            EditSummary = editSummary } : Events.Edited)
        (s: Events.Summary) =
        { s with
            RevisionIds = revisionId :: s.RevisionIds
            Name = name
            Css = css
            Fields = fields
            Modified = modified
            LatexPre = latexPre
            LatexPost = latexPost
            Templates = templates
            EditSummary = editSummary }
    
    let evolve state = function
        | Events.Created s -> State.Active s
        | Events.Edited e -> state |> mapActive (evolveEdited e)

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Created _ -> true | _ -> false

let validateFieldName (field: string) = result {
    do! Result.requireEqual field (field.Trim()) $"Remove the spaces before and/or after the field name: '{field}'."
    do! (1 <= field.Length && field.Length <= 50) |> Result.requireTrue $"The field name '{field}' must be between 1 and 50 characters."
    }

let validateFields (fields: Field list) = result {
    let uniqueCount = fields |> List.map (fun x -> x.Name) |> Set.ofList |> Set.count
    do! Result.requireEqual uniqueCount fields.Length "Field names must be unique."
    for field in fields do
        do! validateFieldName field.Name
    }

let validateEditSummary (editSummary: string) = result {
    do! (editSummary.Length <= 200) |> Result.requireTrue $"The edit summary must be less than 200 characters, but it has {editSummary.Length} characters."
    }

let validateName (name: string) = result {
    do! (name.Length <= 200) |> Result.requireTrue $"The title must be less than 200 characters, but it has {name.Length} characters."
    }

// medTODO validate revisionId global uniqueness

let decideCreate (summary: Events.Summary) state =
    match state with
    | Fold.State.Active s -> Error $"Template '{s.Id}' already exists."
    | Fold.State.Initial  -> result {
        do! validateFields summary.Fields
        do! validateEditSummary summary.EditSummary
        do! validateName summary.Name
    } |> addEvent (Events.Created summary)

let decideEdit (edited: Events.Edited) callerId state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit a template that doesn't exist"
    | Fold.State.Active x -> result {
        do! Result.requireEqual x.AuthorId callerId $"You ({callerId}) aren't the author"
        do! x.RevisionIds |> Seq.contains edited.RevisionId |> Result.requireFalse $"Duplicate revision id:{edited.RevisionId}"
    } |> addEvent (Events.Edited edited)

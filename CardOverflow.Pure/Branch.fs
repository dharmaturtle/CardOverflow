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
          LeafIds: LeafId list
          Title: string
          ConceptId: ConceptId
          AuthorId: UserId
          TemplateRevisionId: TemplateRevisionId
          AnkiNoteId: int64 option
          FieldValues: Map<string, string>
          EditSummary: string }
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
    
    let evolveEdited
        ({  LeafId = leafId
            Title = title
            TemplateRevisionId = templateRevisionId
            FieldValues = fieldValues
            EditSummary = editSummary }: Events.Edited)
        (s: Events.Summary) =
        { s with
            LeafIds            = leafId :: s.LeafIds
            Title              = title
            TemplateRevisionId = templateRevisionId
            FieldValues        = fieldValues
            EditSummary        = editSummary }
    
    let evolve state = function
        | Events.Created s -> State.Active s
        | Events.Edited e -> state |> mapActive (evolveEdited e)

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Created _ -> true | _ -> false

type LeafSummary =
    { Id: LeafId
      BranchId: BranchId
      Title: string
      ConceptId: ConceptId
      AuthorId: UserId
      TemplateRevisionId: TemplateRevisionId
      FieldValues: Map<string, string>
      EditSummary: string }

let toLeafSummary (b: Events.Summary) =
    { Id = b.LeafIds.Head
      BranchId = b.Id
      Title = b.Title
      ConceptId = b.ConceptId
      AuthorId = b.AuthorId
      TemplateRevisionId = b.TemplateRevisionId
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

let validateSummary (summary: Events.Summary) = result {
    do! validateFieldValues summary.FieldValues
    do! validateEditSummary summary.EditSummary
    do! validateTitle summary.Title
    }

// medTODO validate leafId global uniqueness

let decideCreate (summary: Events.Summary) state =
    match state with
    | Fold.State.Active s -> Error $"Branch '{s.Id}' already exists."
    | Fold.State.Initial  -> validateSummary summary
    |> addEvent (Events.Created summary)

let decideEdit (edited: Events.Edited) callerId state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit a branch that doesn't exist"
    | Fold.State.Active x -> result {
        do! Result.requireEqual x.AuthorId callerId $"You ({callerId}) aren't the author"
        do! x.LeafIds |> Seq.contains edited.LeafId |> Result.requireFalse $"Duplicate leafId:{edited.LeafId}"
    } |> addEvent (Events.Edited edited)

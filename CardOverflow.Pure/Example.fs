module Domain.Example

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open NodaTime
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open Domain.Summary

let streamName (id: ExampleId) = StreamName.create "Example" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Edited = // copy fields from this to Created
        { Meta              : Meta
          Ordinal           : ExampleRevisionOrdinal
          Title             : string
          TemplateRevisionId: TemplateRevisionId
          FieldValues       : EditFieldAndValue list
          EditSummary       : string }
    
    type Created =
        { Meta      : Meta
          Id        : ExampleId
          ParentId  : ExampleId option
          AnkiNoteId: int64 option
            
          // from Edited above
          //Ordinal: ExampleRevisionOrdinal // automatically set
          Title             : string
          TemplateRevisionId: TemplateRevisionId
          FieldValues       : EditFieldAndValue list
          EditSummary       : string }
    
    type CommentAdded =
        { Meta    : Meta
          Id      : CommentId
          User    : string
          Text    : string }

    module Compaction =
        type State =
            | Initial
            | Active of Example
            | Dmca   of DmcaTakeDown
        type Snapshotted = { State: State }
    
    type Event =
        | CommentAdded of CommentAdded
        | Created      of Created
        | Edited       of Edited
        | // revise this tag if you break the unfold schema
          //[<System.Runtime.Serialization.DataMember(Name="snapshot-v1")>]
          Snapshotted of Compaction.Snapshotted
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type State =
        | Initial
        | Active of Example
        | Dmca   of DmcaTakeDown
    let initial : State = State.Initial
    let impossibleExampleRevisionOrdinal = 0<exampleRevisionOrdinal>
    let    initialExampleRevisionOrdinal = 1<exampleRevisionOrdinal>

    let toSnapshot (s: State) : Events.Compaction.Snapshotted =
        match s with
        | Initial  -> { State = Events.Compaction.Initial  }
        | Active x -> { State = Events.Compaction.Active x }
        | Dmca   x -> { State = Events.Compaction.Dmca   x }
    let ofSnapshot ({ State = s }: Events.Compaction.Snapshotted) : State =
        match s with
        | Events.Compaction.Initial  -> Initial
        | Events.Compaction.Active x -> Active x
        | Events.Compaction.Dmca   x -> Dmca   x
    
    let mapActive f = function
        | Active a -> a |> f |> Active
        | x -> x
    
    let evolveEdited
        ({  Meta = meta
            Ordinal = ordinal
            Title = title
            TemplateRevisionId = templateRevisionId
            FieldValues = fieldValues
            EditSummary = editSummary }: Events.Edited)
        (s: Example) =
        { s with
            CommandIds         = s.CommandIds |> Set.add meta.CommandId
            Revisions          = { Ordinal            = ordinal
                                   Title              = title
                                   TemplateRevisionId = templateRevisionId
                                   FieldValues        = fieldValues
                                   Meta               = meta
                                   EditSummary        = editSummary } :: s.Revisions }
    
    let evolveCreated (created: Events.Created) =
        {   Id                 = created.Id
            CommandIds         = created.Meta.CommandId |> Set.singleton
            ParentId           = created.ParentId
            Revisions          = { Ordinal            = initialExampleRevisionOrdinal
                                   Title              = created.Title
                                   TemplateRevisionId = created.TemplateRevisionId
                                   FieldValues        = created.FieldValues
                                   Meta               = created.Meta
                                   EditSummary        = created.EditSummary } |> List.singleton
            AuthorId           = created.Meta.UserId
            AnkiNoteId         = created.AnkiNoteId
            Comments           = [] }
    
    let evolveCommentAdded (e: Events.CommentAdded) (s: Example) =
        { s with
            CommandIds         = s.CommandIds |> Set.add e.Meta.CommandId
            Comments           = { Id              = e.Id
                                   User            = e.User
                                   UserId          = e.Meta.UserId
                                   Text            = e.Text
                                   ServerCreatedAt = e.Meta.ServerReceivedAt.Value
                                 } :: s.Comments
        }
    
    let evolve state = function
        | Events.Created      s -> s |> evolveCreated |> Active
        | Events.Edited       e -> state |> mapActive (evolveEdited e)
        | Events.CommentAdded e -> state |> mapActive (evolveCommentAdded e)
        | Events.Snapshotted  s -> s |> ofSnapshot

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit :      Events.Event seq -> State = Seq.fold evolve initial
    let isOrigin = function Events.Snapshotted _ -> true | _ -> false

    let snapshot (state: State) : Events.Event =
        state |> toSnapshot |> Events.Snapshotted

let getRevision ((exampleId, ordinal): ExampleRevisionId) (example: Fold.State) = result {
    let! example =
        match example with
        | Fold.Active e -> Ok e
        | _ -> Error "Example doesn't exist."
    do! Result.requireEqual example.Id exampleId "ExampleId doesn't match provided Example. This is the programmer's fault and should never be seen by users."
    return!
        example.Revisions
        |> List.filter (fun x -> x.Ordinal = ordinal)
        |> List.tryExactlyOne
        |> Result.requireSome $"Ordinal '{ordinal}' not found."
    }

let validateFieldValues (fieldValues: EditFieldAndValue list) = result {
    for field, value in fieldValues |> EditFieldAndValue.simplify do
        do! PublicTemplate.validateFieldName field
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
    |> PublicTemplate.getCardTemplatePointers templateRevision
    |> Result.bind (Result.requireNotEmpty "This Example will not generate any cards.")
    |> Result.mapError CError

let validateCreate template (created: Events.Created) = result {
    do! validateFieldValues created.FieldValues
    do! validateEditSummary created.EditSummary
    do! validateTitle created.Title
    let! templateRevision = template |> PublicTemplate.getRevision created.TemplateRevisionId
    do! validateCreatesCards templateRevision created.FieldValues
    }

let validateRevisionIncrements (example: Example) (edited: Events.Edited) =
    let expected = example.CurrentRevision.Ordinal + 1<exampleRevisionOrdinal>
    Result.requireEqual
        expected
        edited.Ordinal
        (CError $"The new Ordinal was expected to be '{expected}', but is instead '{edited.Ordinal}'. This probably means you edited the example, saved, then edited an *old* version of the example and then tried to save it.")

let checkMeta_AllowNonAuthor (meta: Meta) (e: Example) =
    idempotencyCheck meta e.CommandIds

let checkMeta (meta: Meta) (e: Example) = result {
    do! Result.requireEqual meta.UserId e.AuthorId (CError "You aren't allowed to edit this Example.")
    do! checkMeta_AllowNonAuthor meta e
    }

let validateEdit template (example: Example) (edited: Events.Edited) = result {
    do! checkMeta edited.Meta example
    do! validateRevisionIncrements example edited
    do! validateFieldValues edited.FieldValues
    do! validateEditSummary edited.EditSummary
    do! validateTitle edited.Title
    let! templateRevision = template |> PublicTemplate.getRevision edited.TemplateRevisionId
    do! validateCreatesCards templateRevision edited.FieldValues
    }

let commentMin = 15
let commentMax = 500
let validateCommentAdded (example: Example) (commentAdded: Events.CommentAdded) = result {
    do! checkMeta_AllowNonAuthor commentAdded.Meta example
    do! Result.requireEqual
            (commentAdded.Text                                      )
            (commentAdded.Text |> MappingTools.standardizeWhitespace)
            (CError "Comment has invalid whitespace. Remove leading and trailing spaces, as well as any occurance of multiple spaces.")
    do! commentMin <= commentAdded.Text.Length |> Result.requireTrue (CError $"Comment must be {commentMin} or more characters.")
    do! commentAdded.Text.Length <= commentMax |> Result.requireTrue (CError $"Comment must be less than {commentMax} characters.")
    do! example.Comments |> List.map (fun x -> x.Id) |> List.contains commentAdded.Id |> Result.requireFalse (CError "That comment id is taken.")
    }

let decideAddComment (e: Events.CommentAdded) (exampleId: ExampleId) state =
    match state with
    | Fold.Initial  -> idempotencyBypass                    |> bindCCError $"Example '{exampleId}' doesn't exist so you can't comment on it."
    | Fold.Dmca   s -> idempotencyCheck e.Meta s.CommandIds |> bindCCError $"Example '{exampleId}' is DMCAed so you can't comment on it."
    | Fold.Active s -> validateCommentAdded s e
    |> addEvent (Events.CommentAdded e)

let decideCreate template (created: Events.Created) state =
    match state with
    | Fold.Active s -> idempotencyCheck created.Meta s.CommandIds |> bindCCError $"Example '{created.Id}' already exists."
    | Fold.Dmca   s -> idempotencyCheck created.Meta s.CommandIds |> bindCCError $"Example '{created.Id}' already exists (though it's DMCAed)."
    | Fold.Initial  -> validateCreate template created
    |> addEvent (Events.Created created)

let decideEdit template (edited: Events.Edited) (exampleId: ExampleId) state =
    match state with
    | Fold.Initial  -> idempotencyBypass                         |> bindCCError $"Example '{exampleId}' doesn't exist so you can't edit it."
    | Fold.Dmca   s -> idempotencyCheck edited.Meta s.CommandIds |> bindCCError $"Example '{exampleId}' is DMCAed so you can't edit it."
    | Fold.Active s -> validateEdit template s edited
    |> addEvent (Events.Edited edited)

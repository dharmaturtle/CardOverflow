module Domain.Template

open FsCodec
open FsCodec.NewtonsoftJson
open NodaTime
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open Domain.Summary

let streamName (id: TemplateId) = StreamName.create "Template" (string id)

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Edited = // copy fields from this to Created
        { Meta: Meta
          Ordinal: TemplateRevisionOrdinal
          Name: string
          Css: string
          Fields: Field list
          LatexPre: string
          LatexPost: string
          CardTemplates: TemplateType
          EditSummary: string }
        with
            static member fromRevision (revision: Summary.TemplateRevision) meta =
                { Meta            = meta
                  Ordinal         = revision.Ordinal
                  Name            = revision.Name
                  Css             = revision.Css
                  Fields          = revision.Fields
                  LatexPre        = revision.LatexPre
                  LatexPost       = revision.LatexPost
                  CardTemplates   = revision.CardTemplates
                  EditSummary     = revision.EditSummary }

    type Created =
        { Meta: Meta
          Id: TemplateId
          Visibility: Visibility
          
          // from Edited above
          //Ordinal: TemplateRevisionOrdinal // automatically set
          Name: string
          Css: string
          Fields: Field list
          LatexPre: string
          LatexPost: string
          CardTemplates: TemplateType
          EditSummary: string }
    
    module Compaction =
        type State =
            | Active of Template
            | Dmca   of DmcaTakeDown
        type Snapshotted = { State: State }
    
    type Event =
        | Created     of Created
        | Edited      of Edited
        | // revise this tag if you break the unfold schema
          //[<System.Runtime.Serialization.DataMember(Name="snapshot-v1")>]
          Snapshotted of Compaction.Snapshotted
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type Extant =
        | Active of Template
        | Dmca   of DmcaTakeDown
    type State =
        | Initial
        | Extant of Extant
    let initial : State = State.Initial
    let initialTemplateRevisionOrdinal = 1<templateRevisionOrdinal>

    let toSnapshot (s: Extant) : Events.Compaction.Snapshotted =
        match s with
        | Active x -> { State = Events.Compaction.Active x }
        | Dmca   x -> { State = Events.Compaction.Dmca   x }
    let ofSnapshot ({ State = s }: Events.Compaction.Snapshotted) : Extant =
        match s with
        | Events.Compaction.Active x -> Active x
        | Events.Compaction.Dmca   x -> Dmca   x
    
    let mapActive f = function
        | Extant (Active a) ->
          Extant (Active (f a))
        | x -> x
    
    let evolveEdited (edited : Events.Edited) (template: Template) =
        { template with
            CommandIds = template.CommandIds |> Set.add edited.Meta.CommandId
            Revisions = { Ordinal       = edited.Ordinal
                          Name          = edited.Name
                          Css           = edited.Css
                          Fields        = edited.Fields
                          Created       = edited.Meta.ClientCreatedAt
                          LatexPre      = edited.LatexPre
                          LatexPost     = edited.LatexPost
                          CardTemplates = edited.CardTemplates
                          EditSummary   = edited.EditSummary } :: template.Revisions
            Modified  = edited.Meta.ClientCreatedAt }
    
    let evolveCreated (s : Events.Created) =
        { Id         = s.Id
          CommandIds = s.Meta.CommandId |> Set.singleton
          Revisions  = { Ordinal       = initialTemplateRevisionOrdinal
                         Name          = s.Name
                         Css           = s.Css
                         Fields        = s.Fields
                         Created       = s.Meta.ClientCreatedAt
                         LatexPre      = s.LatexPre
                         LatexPost     = s.LatexPost
                         CardTemplates = s.CardTemplates
                         EditSummary   = s.EditSummary } |> List.singleton
          AuthorId   = s.Meta.UserId
          Modified   = s.Meta.ClientCreatedAt
          Visibility = s.Visibility }
    
    let evolve state = function
        | Events.Created     s -> s |> evolveCreated |> Extant.Active |> State.Extant
        | Events.Edited      e -> state |> mapActive (evolveEdited e)
        | Events.Snapshotted s -> s |> ofSnapshot |> State.Extant

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit :      Events.Event seq -> State = Seq.fold evolve initial
    let foldExtant events =
        match fold initial events with
        | State.Extant x -> x
        | Initial        -> failwith "requires at least 1 event"
    let isOrigin = function Events.Snapshotted _ -> true | _ -> false
    
    let snapshot (state: State) : Events.Event =
        match state with
        | Extant x -> x |> toSnapshot |> Events.Snapshotted
        | Initial -> failwith "impossible"

let getActive state =
    match state with
    | Fold.State.Extant t ->
        match t with
        | Fold.Active  t -> Ok t
        | Fold.Dmca    _ -> CCError "Template is DMCAed."
    | Fold.State.Initial -> CCError "Template doesn't exist."

let getRevision ((templateId, ordinal): TemplateRevisionId) (template: Fold.State) = result {
    let! template = template |> getActive
    do! Result.requireEqual template.Id templateId (CError "TemplateId doesn't match provided Template. This is the programmer's fault and should never be seen by users.")
    return!
        template.Revisions
        |> List.filter (fun x -> x.Ordinal = ordinal)
        |> List.tryExactlyOne
        |> Result.requireSome (CError $"Ordinal '{ordinal}' not found.")
    }

// medTODO consider deleting this, or refactoring it to return `Created`
let initialize id commandId cardTemplateId authorId now = {
    Id = id
    CommandIds = Set.singleton commandId
    Revisions = {
        Name = "New Card Template"
        Ordinal = Fold.initialTemplateRevisionOrdinal
        Css = """.card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
}"""
        Fields = [
        {   Name = "Front"
            IsRightToLeft = false
            IsSticky = false }
        {   Name = "Back"
            IsRightToLeft = false
            IsSticky = false }
        {   Name = "Source"
            IsRightToLeft = false
            IsSticky = true
        }]
        Created = now
        LatexPre = """\documentclass[12pt]{article}
\special{papersize=3in,5in}
\usepackage[utf8]{inputenc}
\usepackage{amssymb,amsmath}
\pagestyle{empty}
\setlength{\parindent}{0in}
\begin{document}
"""
        LatexPost = """\end{document}"""
        CardTemplates = TemplateType.initStandard cardTemplateId
        EditSummary = "Initial creation" 
    } |> List.singleton
    AuthorId = authorId
    Modified = now
    Visibility = Private
    }

let fieldNameMax = 50
let validateFieldName (field: string) = result {
    do! Result.requireEqual field (field.Trim()) (CError $"Remove the spaces before and/or after the field name: '{field}'.")
    do! (1 <= field.Length && field.Length <= fieldNameMax) |> Result.requireTrue (CError $"The field name '{field}' must be between 1 and {fieldNameMax} characters.")
    }

let validateFields (fields: Field list) = result {
    let uniqueCount = fields |> List.map (fun x -> x.Name) |> Set.ofList |> Set.count
    do! Result.requireEqual uniqueCount fields.Length (CError "Field names must be unique.")
    for field in fields do
        do! validateFieldName field.Name
    }

let editSummaryMax = 200
let validateEditSummary (editSummary: string) = result {
    do! (editSummary.Length <= editSummaryMax) |> Result.requireTrue (CError $"The edit summary must be less than {editSummaryMax} characters, but it has {editSummary.Length} characters.")
    }

let nameMax = 200
let validateName (name: string) = result {
    do! (name.Length <= nameMax) |> Result.requireTrue (CError $"The name must be less than {nameMax} characters, but it has {name.Length} characters.")
    }

let validateCreate (created: Events.Created) = result {
    do! validateFields created.Fields
    do! validateEditSummary created.EditSummary
    do! validateName created.Name
    }

let validateRevisionIncrements (template: Template) (edited: Events.Edited) =
    let expected = template.CurrentRevision.Ordinal + 1<templateRevisionOrdinal>
    Result.requireEqual
        expected
        edited.Ordinal
        (CError $"The new Ordinal was expected to be '{expected}', but is instead '{edited.Ordinal}'. This probably means you edited the template, saved, then edited an *old* version of the template and then tried to save it.")

let checkMeta (meta: Meta) (t: Template) = result {
    do! Result.requireEqual meta.UserId t.AuthorId (CError "You aren't allowed to edit this Template.")
    do! idempotencyCheck meta t.CommandIds
    }

let validateEdited (template: Template) (edited: Events.Edited) = result {
    do! checkMeta edited.Meta template
    do! validateRevisionIncrements template edited
    do! validateEditSummary edited.EditSummary
    }

let decideCreate (created: Events.Created) state =
    match state with
    | Fold.State.Extant s ->
        match s with
        | Fold.Active   s -> idempotencyCheck created.Meta s.CommandIds |> bindCCError $"Template '{created.Id}' already exists."
        | Fold.Dmca     s -> idempotencyCheck created.Meta s.CommandIds |> bindCCError $"Template '{created.Id}' already exists (though it's DMCAed)."
    | Fold.State.Initial  -> validateCreate created
    |> addEvent (Events.Created created)

let decideEdit (edited: Events.Edited) (templateId: TemplateId) state =
    match state with
    | Fold.State.Extant s ->
        match s with
        | Fold.Active   s -> validateEdited s edited
        | Fold.Dmca     s -> idempotencyCheck edited.Meta s.CommandIds |> bindCCError $"Template '{templateId}' is DMCAed so you can't edit it."
    | Fold.State.Initial  -> idempotencyCheck edited.Meta Set.empty    |> bindCCError $"Template '{templateId}' doesn't exist so you can't edit it."
    |> addEvent (Events.Edited edited)

let getCardTemplatePointers (templateRevision: TemplateRevision) (fieldValues: Map<string, string>) =
    match templateRevision.CardTemplates with
    | Cloze t -> result {
        let! max = ClozeLogic.maxClozeIndexInclusive "Something's wrong with your cloze indexes." fieldValues t.Front
        return [0s .. max] |> List.choose (fun clozeIndex ->
            CardHtml.tryGenerate
                <| (fieldValues |> Map.toList)
                <| t.Front
                <| t.Back
                <| templateRevision.Css
                <| CardHtml.Cloze clozeIndex
            |> Option.map (fun _ -> clozeIndex |> int |> CardTemplatePointer.Cloze )
        )}
    | Standard ts ->
        ts |> List.choose (fun t ->
            CardHtml.tryGenerate
                <| (fieldValues |> Map.toList)
                <| t.Front
                <| t.Back
                <| templateRevision.Css
                <| CardHtml.Standard
            |> Option.map (fun _ -> t.Id |> CardTemplatePointer.Normal)
        ) |> Result.requireNotEmptyX "No cards generated because the front is unchanged."

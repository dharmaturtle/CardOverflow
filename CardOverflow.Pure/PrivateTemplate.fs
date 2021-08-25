module Domain.PrivateTemplate

open FsCodec
open FsCodec.NewtonsoftJson
open NodaTime
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open Domain.Summary

let [<Literal>] category = "PrivateTemplate"
let streamName (id: PrivateTemplateId) = StreamName.create category (string id)

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type Edited = // copy fields from this to Created
        { Meta: Meta
          Ordinal: PrivateTemplateOrdinal
          Name: string
          Css: string
          Fields: Field list
          LatexPre: string
          LatexPost: string
          CardTemplates: TemplateType }

    type Created =
        { Meta: Meta
          Id: PrivateTemplateId
          
          // from Edited above
          //Ordinal: TemplateOrdinal // automatically set
          Name: string
          Css: string
          Fields: Field list
          LatexPre: string
          LatexPost: string
          CardTemplates: TemplateType }

    type Deleted =
        { Meta: Meta }
    
    module Compaction =
        type State =
            | Initial
            | Active of PrivateTemplate
            | Delete of PrivateTemplate
        type Snapshotted = { State: State }
    
    type Event =
        | Created     of Created
        | Deleted     of Deleted
        | Edited      of Edited
        | // revise this tag if you break the unfold schema
          //[<System.Runtime.Serialization.DataMember(Name="snapshot-v1")>]
          Snapshotted of Compaction.Snapshotted
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type State =
        | Initial
        | Active of PrivateTemplate
        | Delete of PrivateTemplate
    let initial : State = State.Initial
    let impossibleOrdinal = 0<privateTemplateOrdinal>
    let initialOrdinal    = 1<privateTemplateOrdinal>

    let toSnapshot (s: State) : Events.Compaction.Snapshotted =
        match s with
        | Initial  -> { State = Events.Compaction.Initial  }
        | Active x -> { State = Events.Compaction.Active x }
        | Delete x -> { State = Events.Compaction.Delete x }
    let ofSnapshot ({ State = s }: Events.Compaction.Snapshotted) : State =
        match s with
        | Events.Compaction.Initial  -> Initial
        | Events.Compaction.Active x -> Active x
        | Events.Compaction.Delete x -> Delete x
    
    let mapActive f = function
        | Active a -> a |> f |> Active
        | x -> x
    
    let guard (old: PrivateTemplate) (meta: Meta) updated =
        if old.CommandIds.Contains meta.CommandId
        then old
        else { updated with
                   ClientModifiedAt = meta.ClientCreatedAt
                   CommandIds = old.CommandIds.Add meta.CommandId }
    
    let evolveEdited (e: Events.Edited) (s: PrivateTemplate) =
        guard s e.Meta
            { s with
                Ordinal          = e.Ordinal
                Name             = e.Name
                Css              = e.Css
                Fields           = e.Fields
                LatexPre         = e.LatexPre
                LatexPost        = e.LatexPost
                CardTemplates    = e.CardTemplates }
    
    let evolveDeleted (e: Events.Deleted) (s: PrivateTemplate) =
        guard s e.Meta s
    
    let evolveCreated (s : Events.Created) =
        { Id               = s.Id
          CommandIds       = s.Meta.CommandId |> Set.singleton
          ClientCreatedAt  = s.Meta.ClientCreatedAt
          ClientModifiedAt = s.Meta.ClientCreatedAt
          Ordinal          = initialOrdinal
          Name             = s.Name
          Css              = s.Css
          Fields           = s.Fields
          LatexPre         = s.LatexPre
          LatexPost        = s.LatexPost
          CardTemplates    = s.CardTemplates
          AuthorId         = s.Meta.UserId }
    
    let evolve state = function
        | Events.Created     s -> s |> evolveCreated |> Active
        | Events.Deleted     e -> state |> mapActive (evolveDeleted e)
        | Events.Edited      e -> state |> mapActive (evolveEdited  e)
        | Events.Snapshotted s -> s |> ofSnapshot

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit :      Events.Event seq -> State = Seq.fold evolve initial
    let isOrigin = function Events.Snapshotted _ -> true | _ -> false
    
    let snapshot (state: State) : Events.Event =
        state |> toSnapshot |> Events.Snapshotted

let getActive state =
    match state with
    | Fold.Active  t -> Ok t
    | Fold.Delete  t -> Error "Template is Deleted."
    | Fold.Initial   -> Error "Template doesn't exist."
let getActive' = getActive >> Result.mapError CError

// medTODO consider deleting this, or refactoring it to return `Created`
let initialize id commandId cardTemplateId authorId meta = {
    Id = id
    CommandIds = Set.singleton commandId
    Name = "New Card Template"
    Ordinal = Fold.initialOrdinal
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
    ClientModifiedAt = meta.ClientCreatedAt
    ClientCreatedAt = meta.ClientCreatedAt
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
    AuthorId = authorId
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

let nameMax = 200
let validateName (name: string) = result {
    do! (name.Length <= nameMax) |> Result.requireTrue (CError $"The name must be less than {nameMax} characters, but it has {name.Length} characters.")
    }

let validateCreate (created: Events.Created) = result {
    do! validateFields created.Fields
    do! validateName created.Name
    }

let validateRevisionIncrements (template: PrivateTemplate) (edited: Events.Edited) =
    let expected = template.Ordinal + 1<privateTemplateOrdinal>
    Result.requireEqual
        expected
        edited.Ordinal
        (CError $"The new Ordinal was expected to be '{expected}', but is instead '{edited.Ordinal}'. This probably means you edited the template, saved, then edited an *old* version of the template and then tried to save it.")

let checkMeta (meta: Meta) (t: PrivateTemplate) = result {
    do! Result.requireEqual meta.UserId t.AuthorId (CError "You aren't allowed to edit this Template.")
    do! idempotencyCheck meta t.CommandIds
    }

let validateEdited (template: PrivateTemplate) (edited: Events.Edited) = result {
    do! checkMeta edited.Meta template
    do! validateRevisionIncrements template edited
    }

let validateDeleted (template: PrivateTemplate) (deleted: Events.Deleted) = result {
    do! checkMeta deleted.Meta template
    }

let decideCreate (created: Events.Created) state =
    match state with
    | Fold.Active s -> idempotencyCheck created.Meta s.CommandIds |> bindCCError $"Template '{created.Id}' already exists."
    | Fold.Delete s -> idempotencyCheck created.Meta s.CommandIds |> bindCCError $"Template '{created.Id}' is Deleted."
    | Fold.Initial  -> validateCreate created
    |> addEvent (Events.Created created)

let decideEdit (edited: Events.Edited) (templateId: PrivateTemplateId) state =
    match state with
    | Fold.Active s -> validateEdited s edited
    | Fold.Delete s -> idempotencyCheck edited.Meta s.CommandIds |> bindCCError $"Template '{templateId}' is Deleted so you can't edit it."
    | Fold.Initial  -> idempotencyBypass                         |> bindCCError $"Template '{templateId}' doesn't exist so you can't edit it."
    |> addEvent (Events.Edited edited)

let decideDelete (deleted: Events.Deleted) (templateId: PrivateTemplateId) state =
    match state with
    | Fold.Active s -> validateDeleted s deleted
    | Fold.Delete s -> idempotencyCheck deleted.Meta s.CommandIds |> bindCCError $"Template '{templateId}' is Deleted so you can't delete it again."
    | Fold.Initial  -> idempotencyBypass                          |> bindCCError $"Template '{templateId}' doesn't exist so you can't delete it."
    |> addEvent (Events.Deleted deleted)

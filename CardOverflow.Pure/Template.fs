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
          Revision: TemplateRevisionOrdinal
          Name: string
          Css: string
          Fields: Field list
          LatexPre: string
          LatexPost: string
          CardTemplates: TemplateType
          EditSummary: string }

    type Created =
        { Meta: Meta
          Id: TemplateId
          Visibility: Visibility
          
          // from Edited above
          //Revision: TemplateRevisionOrdinal // automatically set to 0
          Name: string
          Css: string
          Fields: Field list
          LatexPre: string
          LatexPost: string
          CardTemplates: TemplateType
          EditSummary: string }

    type Event =
        | Created of Created
        | Edited  of Edited
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =

    type State =
        | Initial
        | Active of Template
        | Dmca of DmcaTakeDown
    let initial : State = State.Initial
    let initialTemplateRevisionOrdinal = 0<templateRevisionOrdinal>
    
    let mapActive f = function
        | Active a -> f a |> Active
        | x -> x
    
    let evolveEdited (edited : Events.Edited) (template: Template) =
        { template with
            CurrentRevision = edited.Revision
            Name            = edited.Name
            Css             = edited.Css
            Fields          = edited.Fields
            Modified        = edited.Meta.ServerCreatedAt.Value
            LatexPre        = edited.LatexPre
            LatexPost       = edited.LatexPost
            CardTemplates   = edited.CardTemplates
            EditSummary     = edited.EditSummary }
    
    let evolveCreated (s : Events.Created) =
        { Id              = s.Id
          CurrentRevision = initialTemplateRevisionOrdinal
          AuthorId        = s.Meta.UserId
          Name            = s.Name
          Css             = s.Css
          Fields          = s.Fields
          Created         = s.Meta.ServerCreatedAt.Value
          Modified        = s.Meta.ServerCreatedAt.Value
          LatexPre        = s.LatexPre
          LatexPost       = s.LatexPost
          CardTemplates   = s.CardTemplates
          Visibility      = s.Visibility
          EditSummary     = s.EditSummary }
    
    let evolve state = function
        | Events.Created s -> s |> evolveCreated |> State.Active
        | Events.Edited e -> state |> mapActive (evolveEdited e)

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Created _ -> true | _ -> false

type RevisionSummary =
    { Revision: TemplateRevisionOrdinal
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
    member this.Id = this.TemplateId, this.Revision

let initialize id cardTemplateId authorId now : Template = {
    Id = id
    Name = "New Card Template"
    CurrentRevision = Fold.initialTemplateRevisionOrdinal
    AuthorId = authorId
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
    Modified = now
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
    Visibility = Private
    EditSummary = "Initial creation" }

let toRevisionSummary (b: Template) =
    { Revision = b.CurrentRevision
      TemplateId = b.Id
      AuthorId = b.AuthorId
      Name = b.Name
      Css = b.Css
      Fields = b.Fields
      Created = b.Created
      LatexPre = b.LatexPre
      LatexPost = b.LatexPost
      CardTemplates = b.CardTemplates
      EditSummary = b.EditSummary }

let fieldNameMax = 50
let validateFieldName (field: string) = result {
    do! Result.requireEqual field (field.Trim()) $"Remove the spaces before and/or after the field name: '{field}'."
    do! (1 <= field.Length && field.Length <= fieldNameMax) |> Result.requireTrue $"The field name '{field}' must be between 1 and {fieldNameMax} characters."
    }

let validateFields (fields: Field list) = result {
    let uniqueCount = fields |> List.map (fun x -> x.Name) |> Set.ofList |> Set.count
    do! Result.requireEqual uniqueCount fields.Length "Field names must be unique."
    for field in fields do
        do! validateFieldName field.Name
    }

let editSummaryMax = 200
let validateEditSummary (editSummary: string) = result {
    do! (editSummary.Length <= editSummaryMax) |> Result.requireTrue $"The edit summary must be less than {editSummaryMax} characters, but it has {editSummary.Length} characters."
    }

let nameMax = 200
let validateName (name: string) = result {
    do! (name.Length <= nameMax) |> Result.requireTrue $"The name must be less than {nameMax} characters, but it has {name.Length} characters."
    }

let validateCreate (created: Events.Created) = result {
    do! validateFields created.Fields
    do! validateEditSummary created.EditSummary
    do! validateName created.Name
    }

let validateRevisionIncrements (template: Template) (edited: Events.Edited) =
    let expected = template.CurrentRevision + 1<templateRevisionOrdinal>
    Result.requireEqual
        expected
        edited.Revision
        $"The new Revision was expected to be '{expected}', but is instead '{edited.Revision}'. This probably means you edited the template, saved, then edited an *old* version of the template and then tried to save it."

let validateEdited (template: Template) (edited: Events.Edited) = result {
    let callerId = edited.Meta.UserId
    do! Result.requireEqual template.AuthorId callerId $"You ({callerId}) aren't the author"
    do! validateRevisionIncrements template edited
    do! validateEditSummary edited.EditSummary
    }

let decideCreate (created: Events.Created) state =
    match state with
    | Fold.State.Active _ -> Error $"Template '{created.Id}' already exists."
    | Fold.State.Dmca _   -> Error $"Template '{created.Id}' already exists (though it's DMCAed)."
    | Fold.State.Initial  -> validateCreate created
    |> addEvent (Events.Created created)

let decideEdit (edited: Events.Edited) (templateId: TemplateId) state =
    match state with
    | Fold.State.Initial  -> Error $"Template '{templateId}' doesn't exist so you can't edit it."
    | Fold.State.Dmca   _ -> Error $"Template '{templateId}' is DMCAed so you can't edit it."
    | Fold.State.Active s -> validateEdited s edited
    |> addEvent (Events.Edited edited)

let getCardTemplatePointers (templateRevision: RevisionSummary) (fieldValues: Map<string, string>) =
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

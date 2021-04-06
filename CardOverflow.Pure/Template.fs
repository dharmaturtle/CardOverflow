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
          Fields: Field list // highTODO bring all the types here
          Created: Instant
          Modified: Instant
          LatexPre: string
          LatexPost: string
          CardTemplates: TemplateType // highTODO bring all the types here
          EditSummary: string }
    type Edited =
        { RevisionId: TemplateRevisionId
          Name: string
          Css: string
          Fields: Field list
          Modified: Instant
          LatexPre: string
          LatexPost: string
          CardTemplates: TemplateType
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
            CardTemplates = cardTemplates
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
            CardTemplates = cardTemplates
            EditSummary = editSummary }
    
    let evolve state = function
        | Events.Created s -> State.Active s
        | Events.Edited e -> state |> mapActive (evolveEdited e)

    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Created _ -> true | _ -> false

type RevisionSummary =
    { Id: TemplateRevisionId
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
let initialize id revisionId authorId now : Events.Summary = {
    Id = id
    Name = "New Card Template"
    RevisionIds = [revisionId]
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
    CardTemplates = TemplateType.initStandard
    EditSummary = "Initial creation" }

let toRevisionSummary (b: Events.Summary) =
    { Id = b.RevisionIds.Head
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

let validateRevisionIsUnique doesRevisionExist (revisionId: TemplateRevisionId) =
    doesRevisionExist |> Result.requireFalse $"Something already exists with the id '{revisionId}'."

let validateSummary doesRevisionExist (summary: Events.Summary) = result {
    let! revisionId = summary.RevisionIds |> Seq.tryExactlyOne |> Result.requireSome $"There are {summary.RevisionIds.Length} RevisionIds, but there must be exactly 1."
    do! validateRevisionIsUnique doesRevisionExist revisionId
    do! validateFields summary.Fields
    do! validateEditSummary summary.EditSummary
    do! validateName summary.Name
    }

let validateEdited (summary: Events.Summary) callerId doesRevisionExist (edited: Events.Edited) = result {
    do! Result.requireEqual summary.AuthorId callerId $"You ({callerId}) aren't the author"
    do! validateRevisionIsUnique doesRevisionExist edited.RevisionId
    do! summary.RevisionIds |> Seq.contains edited.RevisionId |> Result.requireFalse $"Duplicate revision id:{edited.RevisionId}"
    do! validateEditSummary edited.EditSummary
    }

let decideCreate (summary: Events.Summary) doesRevisionExist state =
    match state with
    | Fold.State.Active s -> Error $"Template '{s.Id}' already exists."
    | Fold.State.Initial  -> validateSummary doesRevisionExist summary
    |> addEvent (Events.Created summary)

let decideEdit (edited: Events.Edited) callerId doesRevisionExist state =
    match state with
    | Fold.State.Initial -> Error "Can't edit a Template that doesn't exist"
    | Fold.State.Active summary -> validateEdited summary callerId doesRevisionExist edited
    |> addEvent (Events.Edited edited)

let getSubtemplateNames (templateRevision: RevisionSummary) (fieldValues: Map<string, string>) =
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
            |> Option.map (fun _ -> clozeIndex |> string |> SubtemplateName.fromString)
        )}
    | Standard ts ->
        ts |> List.choose (fun t ->
            CardHtml.tryGenerate
                <| (fieldValues |> Map.toList)
                <| t.Front
                <| t.Back
                <| templateRevision.Css
                <| CardHtml.Standard
            |> Option.map (fun _ -> t.Name |> SubtemplateName.fromString)
        ) |> Result.requireNotEmptyX "No cards generated because the front is unchanged."

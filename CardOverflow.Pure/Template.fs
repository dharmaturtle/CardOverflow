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
          CurrentRevision: TemplateRevisionOrdinal
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
    with
        member this.CurrentRevisionId = this.Id, this.CurrentRevision
    type Edited =
        { Revision: TemplateRevisionOrdinal
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
        | Dmca of DmcaTakeDown
    let initial : State = State.Initial
    
    let mapActive f = function
        | Active a -> f a |> Active
        | x -> x
    
    let evolveEdited
        ({  Revision = revision
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
            CurrentRevision = revision
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

let initialize id cardTemplateId authorId now : Events.Summary = {
    Id = id
    Name = "New Card Template"
    CurrentRevision = 0<templateRevisionOrdinal>
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
    EditSummary = "Initial creation" }

let toRevisionSummary (b: Events.Summary) =
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

let validateRevisionIsZero (revision: TemplateRevisionOrdinal) =
    Result.requireEqual revision 0<templateRevisionOrdinal> $"Revision must be initialized to 0, but it's '{revision}'."

let validateCreate (summary: Events.Summary) = result {
    do! validateRevisionIsZero summary.CurrentRevision
    do! validateFields summary.Fields
    do! validateEditSummary summary.EditSummary
    do! validateName summary.Name
    }

let validateRevisionIncrements (summary: Events.Summary) (edited: Events.Edited) =
    let expected = summary.CurrentRevision + 1<templateRevisionOrdinal>
    Result.requireEqual
        expected
        edited.Revision
        $"The new Revision was expected to be '{expected}', but is instead '{edited.Revision}'. This probably means you edited the template, saved, then edited an *old* version of the template and then tried to save it."

let validateEdited (summary: Events.Summary) callerId (edited: Events.Edited) = result {
    do! Result.requireEqual summary.AuthorId callerId $"You ({callerId}) aren't the author"
    do! validateRevisionIncrements summary edited
    do! validateEditSummary edited.EditSummary
    }

let decideCreate (summary: Events.Summary) state =
    match state with
    | Fold.State.Active _ -> Error $"Template '{summary.Id}' already exists."
    | Fold.State.Dmca _   -> Error $"Template '{summary.Id}' already exists (though it's DMCAed)."
    | Fold.State.Initial  -> validateCreate summary
    |> addEvent (Events.Created summary)

let decideEdit (edited: Events.Edited) callerId (templateId: TemplateId) state =
    match state with
    | Fold.State.Initial  -> Error $"Template '{templateId}' doesn't exist so you can't edit it."
    | Fold.State.Dmca   _ -> Error $"Template '{templateId}' is DMCAed so you can't edit it."
    | Fold.State.Active s -> validateEdited s callerId edited
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

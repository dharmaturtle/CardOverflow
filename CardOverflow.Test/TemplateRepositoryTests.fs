module TemplateRepositoryTests

open FsToolkit.ErrorHandling
open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Debug
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open CardOverflow.Test
open System
open System.Linq
open Xunit
open CardOverflow.Pure
open System.Collections.Generic
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Sanitation

[<Fact>]
let ``TemplateRepository.UpdateFieldsToNewRevision works``(): Task<unit> = task {
    let userId = user_2
    use c = new TestContainer()
    
    let templateId = c.Db.Template.Single(fun x -> x.TemplateRevisions.Any(fun x -> x.Name = "Basic")).Id
    let! template = SanitizeTemplate.AllRevisions c.Db templateId
    let latestRevision = template.Value.Revisions |> Seq.maxBy (fun x -> x.Modified |?? lazy x.Created)
    
    Assert.Equal(
        "Basic",
        template.Value.Revisions.Single().Name)
    Assert.Equal<string seq>(
        ["Front"; "Back"],
        latestRevision.Fields.Select(fun x -> x.Name))
    Assert.Equal(
        "{{Front}}",
        latestRevision.FirstCardTemplate.Front)
    Assert.Equal(1, c.Db.TemplateRevision.Count(fun x -> x.TemplateId = templateId))

    // Testing UpdateFieldsToNewRevision
    let! _ = FacetRepositoryTests.addBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1])
    let newQuestionXemplate = "modified {{Front mutated}}"
    let newTemplateName = "new name"
    let oldRevisionId = c.Db.Card.Single().RevisionId
    let newRevisionId = Ulid.create
    let updated =
        { latestRevision with
            Id = newRevisionId
            TemplateId = templateId
            Name = newTemplateName
            CardTemplates =
                {   latestRevision.FirstCardTemplate with
                        Front = newQuestionXemplate
                } |> List.singleton |> Standard
            Fields = latestRevision.Fields |> Seq.map (fun x -> { x with Name = x.Name + " mutated" }) |> toResizeArray
        }
    
    let! r = SanitizeTemplate.Update c.Db userId updated
    Assert.Null r.Value

    let! template = SanitizeTemplate.AllRevisions c.Db templateId
    let latestRevision = template.Value.Revisions.Single(fun x -> x.Id = newRevisionId)
    Assert.Equal(
        newQuestionXemplate,
        latestRevision.FirstCardTemplate.Front)
    Assert.Equal(
        newTemplateName,
        latestRevision.Name)
    Assert.Equal<string seq>(
        ["Front mutated"; "Back mutated"],
        latestRevision.Fields.Select(fun x -> x.Name))
    Assert.Equal(userId, c.Db.Card.Single().UserId)
    Assert.NotEqual(oldRevisionId, c.Db.Card.Single().RevisionId)
    Assert.Equal(
        latestRevision.Id,
        c.Db.Card.Include(fun x -> x.Revision).Single().Revision.TemplateRevisionId)
    Assert.Equal(2, c.Db.TemplateRevision.Count(fun x -> x.TemplateId = templateId))
    Assert.Equal(2, c.Db.Revision.Count())
    let concept_1 = c.Db.Concept.Single().Id
    Assert.Equal(2, c.Db.Revision.Count(fun x -> x.Example.ConceptId = concept_1))
    Assert.Equal(2, c.Db.Revision.Count(fun x -> x.ConceptId = concept_1))
    let createds = c.Db.TemplateRevision.Where(fun x -> x.TemplateId = templateId).Select(fun x -> x.Created) |> Seq.toList
    Assert.NotEqual(createds.[0], createds.[1])
    let! x = ConceptViewRepository.get c.Db concept_1
    let front, _, _, _ = x.Value.FrontBackFrontSynthBackSynth.[0]
    Assert.Equal(
        """<!DOCTYPE html>
    <head>
        <style>
            .cloze-brackets-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-filler-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-brackets-back {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: red;
            }
        </style>
        <style>
            .card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}

        </style>
    </head>
    <body>
        modified Front
        <script type="text/javascript" src="/js/iframeResizer.contentWindow.min.js"></script> 
    </body>
</html>""",
        front,
        false, true
    )

    // test existing
    let testView getView id expectedFront expectedBack = task {
        let! (actual: Result<TemplateRevision, string>) = getView c.Db id
        let front, back, _, _ = actual.Value.FrontBackFrontSynthBackSynth() |> Seq.exactlyOne
        BusinessLogicTests.assertStripped expectedFront front
        BusinessLogicTests.assertStripped expectedBack back
    }
    
    do! testView TemplateRepository.latest templateId newQuestionXemplate <| newQuestionXemplate + " {{Back}}"

    let priorRevision = template.Value.Revisions |> Seq.minBy (fun x -> x.Created)
    do! testView TemplateRepository.revision priorRevision.Id "{{Front}}" "{{Front}} {{Back}}"

    // test missing
    let testViewError getView id expected =
        getView c.Db id
        |> Task.map(fun (x: Result<_, _>) -> Assert.Equal(expected, x.error))
    let templateMissingId = Ulid.create
    do! testViewError TemplateRepository.latest templateMissingId <| sprintf "Template #%A not found" templateMissingId // TODO base64
    let templateRevisionMissingId = Ulid.create
    do! testViewError TemplateRepository.revision   templateRevisionMissingId <| sprintf "Template Revision #%A not found" templateRevisionMissingId // TODO base64
    }

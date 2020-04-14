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
open CardOverflow.Pure.Core
open System.Collections.Generic
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Sanitation

[<Fact>]
let ``TemplateRepository.UpdateFieldsToNewInstance works``(): Task<unit> = task {
    let templateId = 1
    let userId = 3
    use c = new TestContainer()
    
    let! template = SanitizeTemplate.AllInstances c.Db templateId
    let latestInstance = template.Value.Instances |> Seq.maxBy (fun x -> x.Modified |?? lazy x.Created)
    
    Assert.Equal(
        "Basic",
        template.Value.Instances.Single().Name)
    Assert.Equal<string seq>(
        ["Front"; "Back"],
        latestInstance.Fields.OrderBy(fun x -> x.Ordinal).Select(fun x -> x.Name))
    Assert.Equal(
        "{{Front}}",
        latestInstance.QuestionTemplate)
    Assert.Equal(1, c.Db.TemplateInstance.Count(fun x -> x.TemplateId = templateId))

    // Testing UpdateFieldsToNewInstance
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId []
    let newQuestionTemplate = "modified {{Front mutated}}"
    let newTemplateName = "new name"
    let updated =
        { latestInstance with
            Name = newTemplateName
            QuestionTemplate = newQuestionTemplate
            Fields = latestInstance.Fields |> Seq.map (fun x -> { x with Name = x.Name + " mutated" }) |> toResizeArray
        } |> ViewTemplateInstance.copyTo
    
    do! TemplateRepository.UpdateFieldsToNewInstance c.Db userId updated

    let! template = SanitizeTemplate.AllInstances c.Db templateId
    let latestInstance = template.Value.Instances |> Seq.maxBy (fun x -> x.Created)
    Assert.Equal(
        newQuestionTemplate,
        latestInstance.QuestionTemplate)
    Assert.Equal(
        newTemplateName,
        latestInstance.Name)
    Assert.Equal<string seq>(
        ["Front mutated"; "Back mutated"],
        latestInstance.Fields.OrderBy(fun x -> x.Ordinal).Select(fun x -> x.Name))
    Assert.Equal(userId, c.Db.AcquiredCard.Single().UserId)
    Assert.Equal(1002, c.Db.AcquiredCard.Single().CardInstanceId)
    Assert.Equal(
        latestInstance.Id,
        c.Db.AcquiredCard.Include(fun x -> x.CardInstance).Single().CardInstance.TemplateInstanceId)
    Assert.Equal(2, c.Db.TemplateInstance.Count(fun x -> x.TemplateId = templateId))
    Assert.Equal(2, c.Db.CardInstance.Count())
    Assert.Equal(2, c.Db.CardInstance.Count(fun x -> x.CardId = 1))
    let createds = c.Db.TemplateInstance.Where(fun x -> x.TemplateId = templateId).Select(fun x -> x.Created) |> Seq.toList
    Assert.NotEqual(createds.[0], createds.[1])
    let! x = CardViewRepository.get c.Db 1
    let front, _, _, _ = x.Value.FrontBackFrontSynthBackSynth
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
        let! (actual: Result<TemplateInstance, string>) = getView c.Db id
        let front, back, _, _ = actual.Value.FrontBackFrontSynthBackSynth
        BusinessLogicTests.assertStripped expectedFront front
        BusinessLogicTests.assertStripped expectedBack back
    }
    
    do! testView TemplateRepository.latest templateId newQuestionTemplate <| newQuestionTemplate + " {{Back}}"

    let priorInstance = template.Value.Instances |> Seq.minBy (fun x -> x.Created)
    do! testView TemplateRepository.instance priorInstance.Id "{{Front}}" "{{Front}} {{Back}}"

    // test missing
    let testViewError getView id expected =
        getView c.Db id
        |> Task.map(fun (x: Result<_, _>) -> Assert.Equal(expected, x.error))
    do! testViewError TemplateRepository.latest 0 "Template #0 not found"
    do! testViewError TemplateRepository.instance 0 "Template Instance #0 not found"
    }

module CardTemplateRepositoryTests

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

[<Fact>]
let ``CardTemplateRepository.GetFromInstance isn't empty``(): Task<unit> = task {
    let templateId = 1
    let userId = 3
    use c = new TestContainer()
    
    let! cardTemplate = CardTemplateRepository.GetFromInstance c.Db templateId
    
    Assert.Equal(
        "Basic",
        cardTemplate.Name)
    Assert.Equal<string seq>(
        ["Front"; "Back"],
        cardTemplate.LatestInstance.Fields.OrderBy(fun x -> x.Ordinal).Select(fun x -> x.Name))
    Assert.Equal(
        "{{Front}}",
        cardTemplate.LatestInstance.QuestionTemplate)
    Assert.Equal(1, c.Db.CardTemplateInstance.Count(fun x -> x.CardTemplateId = templateId))

    // Testing UpdateFieldsToNewInstance
    FacetRepositoryTests.addBasicCard c.Db userId []
    let newQuestionTemplate = "modified {{Front mutated}}"
    let newTemplateName = "new name"
    let updated =
        { cardTemplate with
            Name = newTemplateName
            LatestInstance = { 
                cardTemplate.LatestInstance with
                    QuestionTemplate = newQuestionTemplate
                    Fields = cardTemplate.LatestInstance.Fields |> Seq.map (fun x -> { x with Name = x.Name + " mutated" })
            }
        }
    
    do! CardTemplateRepository.UpdateFieldsToNewInstance c.Db updated

    let! cardTemplate = CardTemplateRepository.GetFromInstance c.Db templateId
    Assert.Equal(
        newQuestionTemplate,
        cardTemplate.LatestInstance.QuestionTemplate)
    Assert.Equal(
        newTemplateName,
        cardTemplate.Name)
    Assert.Equal<string seq>(
        ["Front mutated"; "Back mutated"],
        cardTemplate.LatestInstance.Fields.OrderBy(fun x -> x.Ordinal).Select(fun x -> x.Name))
    Assert.Equal(userId, c.Db.AcquiredCard.Single().UserId)
    Assert.Equal(2, c.Db.AcquiredCard.Single().CardInstanceId)
    Assert.Equal(
        cardTemplate.LatestInstance.Id,
        c.Db.AcquiredCard.Include(fun x -> x.CardInstance).Single().CardInstance.CardTemplateInstanceId)
    Assert.Equal(2, c.Db.CardTemplateInstance.Count(fun x -> x.CardTemplateId = templateId))
    Assert.Equal(2, c.Db.CardInstance.Count())
    Assert.Equal(2, c.Db.CardInstance.Count(fun x -> x.CardId = 1))
    let createds = c.Db.CardTemplateInstance.Where(fun x -> x.CardTemplateId = templateId).Select(fun x -> x.Created) |> Seq.toList
    Assert.NotEqual(createds.[0], createds.[1])
    let! x = CardRepository.getView c.Db 1
    let front, _, _, _ = x.FrontBackFrontSynthBackSynth
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
        front
    )

    }

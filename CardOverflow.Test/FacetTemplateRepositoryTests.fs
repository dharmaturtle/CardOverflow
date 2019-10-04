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
    let newQuestionTemplate = "new question template"
    let newName = "new name"
    let updated =
        { cardTemplate with
            Name = newName
            LatestInstance = { 
                cardTemplate.LatestInstance with
                    QuestionTemplate = newQuestionTemplate
                    Fields = cardTemplate.LatestInstance.Fields |> Seq.map (fun x -> { x with Name = x.Name + " updated" })
            }
        }
    
    do! CardTemplateRepository.UpdateFieldsToNewInstance c.Db updated

    let! cardTemplate = CardTemplateRepository.GetFromInstance c.Db templateId
    Assert.Equal(
        newQuestionTemplate,
        cardTemplate.LatestInstance.QuestionTemplate)
    Assert.Equal(
        newName,
        cardTemplate.Name)
    Assert.Equal<string seq>(
        ["Front updated"; "Back updated"],
        cardTemplate.LatestInstance.Fields.OrderBy(fun x -> x.Ordinal).Select(fun x -> x.Name))
    Assert.Equal(2, c.Db.CardTemplateInstance.Count(fun x -> x.CardTemplateId = templateId))
    let createds = c.Db.CardTemplateInstance.Where(fun x -> x.CardTemplateId = templateId).Select(fun x -> x.Created) |> Seq.toList
    Assert.NotEqual(createds.[0], createds.[1])
    }

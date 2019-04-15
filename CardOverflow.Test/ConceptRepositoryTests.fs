module ConceptRepositoryTests

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Test
open System
open System.Linq
open Xunit

[<Fact>]
let ``ConceptRepository can add and retreive a Concept``() =
    use tempDb = new TempDbService()
    let user = tempDb.WithUser
    let conceptOptions = tempDb.WithDefaultConceptOptions user
    let conceptTemplate = tempDb.WithConceptTemplate conceptOptions
    let title = Guid.NewGuid().ToString()
    let concept = 
        ConceptEntity(
            Title = title,
            Description = "",
            Fields = "",
            ConceptOptionId = conceptOptions.Id,
            ConceptTemplateId = conceptTemplate.Id)
    let conceptRepository = tempDb.DbService |> ConceptRepository

    conceptRepository.SaveConcept concept

    conceptRepository.GetConcepts()
    |> Seq.filter(fun x -> x.Title = title)
    |> Assert.Single

[<Fact>]
let ``ConceptRepository's SaveConcepts updates a Concept``() =
    use tempDb = new TempDbService()
    let user = tempDb.WithUser
    let conceptOptions = tempDb.WithDefaultConceptOptions user
    let conceptTemplate = tempDb.WithConceptTemplate conceptOptions
    let concept = 
        ConceptEntity(
            Title = "",
            Description = "",
            Fields = "",
            ConceptOptionId = conceptOptions.Id,
            ConceptTemplateId = conceptTemplate.Id)
    let service = tempDb.DbService
    service.Command(fun db -> db.Concepts.Add concept)
    let conceptRepository = service |> ConceptRepository
    let updatedConcept = conceptRepository.GetConcepts().Single()
    let updatedTitle = Guid.NewGuid().ToString()
    updatedConcept.Title <- updatedTitle

    updatedConcept 
    |> Seq.singleton 
    |> ResizeArray<ConceptEntity>
    |> conceptRepository.SaveConcepts

    conceptRepository.GetConcepts()
    |> Seq.filter(fun x -> x.Title = updatedTitle)
    |> Assert.Single

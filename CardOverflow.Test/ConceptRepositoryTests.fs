module ConceptRepositoryTests

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Test
open System
open System.Linq
open Xunit

[<Fact>]
let ``ConceptRepository can add and retreive a Concept``() =
    use p = new SqlTempDbProvider()
    let title = Guid.NewGuid().ToString()
    let concept = 
        ConceptEntity(
            Title = title,
            Description = "",
            Fields = "",
            ConceptTemplateId = 1,
            Modified = DateTime.UtcNow)
    let conceptRepository = p.DbService |> ConceptRepository

    conceptRepository.SaveConcept concept

    conceptRepository.GetConcepts()
    |> Seq.filter(fun x -> x.Title = title)
    |> Assert.Single

[<Fact>]
let ``ConceptRepository's SaveConcepts updates a Concept``() =
    use p = new SqlTempDbProvider()
    let concept = 
        ConceptEntity(
            Title = "",
            Description = "",
            Fields = "",
            ConceptTemplateId = 1,
            Modified = DateTime.UtcNow)
    p.DbService.Command(fun db -> db.Concepts.Add concept)
    let conceptRepository = p.DbService |> ConceptRepository
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

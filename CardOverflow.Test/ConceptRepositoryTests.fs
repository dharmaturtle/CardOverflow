module ConceptRepositoryTests

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Test
open System
open System.Linq
open Xunit

[<Fact>]
let ``ConceptRepository can add and retreive a Concept``() =
    use c = new TestContainer()
    let title = Guid.NewGuid().ToString()
    let concept = 
        ConceptEntity(
            Title = title,
            Description = "",
            Fields = "",
            ConceptTemplateId = 1,
            Modified = DateTime.UtcNow,
            IsPublic = true,
            MaintainerId = 3)

    ConceptRepository.AddConcept c.Db concept

    ConceptRepository.GetConcepts c.Db
    |> Seq.filter(fun x -> x.Title = title)
    |> Assert.Single

// fuck merge
//[<Fact>]
//let ``ConceptRepository's SaveConcepts updates a Concept``() =
//    use c = new TestContainer()
//    let concept = 
//        ConceptEntity(
//            Title = "",
//            Description = "",
//            Fields = "",
//            ConceptTemplateId = 1,
//            Modified = DateTime.UtcNow)
    
//    ConceptRepository.AddConcept c.Db concept

//    let updatedConcept = ConceptRepository.GetConcepts c.Db |> Seq.head
//    let updatedTitle = Guid.NewGuid().ToString()
//    updatedConcept.Title <- updatedTitle

//    updatedConcept 
//    |> Seq.singleton 
//    |> ResizeArray<ConceptEntity>
//    |> ConceptRepository.SaveConcepts c.Db

//    ConceptRepository.GetConcepts c.Db
//    |> Seq.filter(fun x -> x.Title = updatedTitle)
//    |> Assert.Single

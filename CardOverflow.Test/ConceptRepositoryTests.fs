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
  let service = tempDb.RecreateDatabaseAndGetDbService()
  let conceptRepository = service |> ConceptRepository
  let concept = Concept(Title = "", Description = "")
  service.Command(fun db -> db.Concepts.Add(concept))
  let title = Guid.NewGuid().ToString()
  let description = Guid.NewGuid().ToString()

  conceptRepository.SaveConcept(Concept(Title = title, Description = description))

  conceptRepository.GetConcepts()
  |> Seq.filter(fun x -> x.Title = title && x.Description = description)
  |> Assert.Single

[<Fact>]
let ``ConceptRepository's SaveConcepts updates a Concept``() =
  use tempDb = new TempDbService()
  let service = tempDb.RecreateDatabaseAndGetDbService()
  let conceptRepository = service |> ConceptRepository
  service.Command(fun db -> db.Concepts.Add(Concept(Title = "", Description = "")))
  let updatedTitle = Guid.NewGuid().ToString()
  let updatedDescription = Guid.NewGuid().ToString()
  let updatedConcept = conceptRepository.GetConcepts().Single()
  updatedConcept.Title <- updatedTitle
  updatedConcept.Description <- updatedDescription

  updatedConcept 
  |> Seq.singleton 
  |> fun x -> new ResizeArray<Concept>(x) 
  |> conceptRepository.SaveConcepts

  conceptRepository.GetConcepts()
  |> Seq.filter(fun x -> x.Title = updatedTitle && x.Description = updatedDescription)
  |> Assert.Single

module ConceptRepositoryTests

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Test
open System
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
  |> Seq.length
  |> fun l -> Assert.Equal(1, l)

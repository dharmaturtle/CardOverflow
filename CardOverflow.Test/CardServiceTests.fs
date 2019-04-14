module CardServiceTests

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Test
open System
open Xunit

[<Fact>]
let ``CardService can add and retreive a card``() =
  use tempDb = new TempDbService()
  let service = tempDb.RecreateDatabaseAndGetDbService()
  let cardRepository = service |> CardRepository
  let concept = ConceptEntity(Title = "", Description = "")
  service.Command(fun db -> db.Concepts.Add(concept))

  cardRepository.SaveCard(CardEntity(ConceptId = concept.Id))

  cardRepository.GetCards()
  |> Seq.filter(fun x -> x.ConceptId = concept.Id)
  |> Seq.length
  |> fun l -> Assert.Equal(1, l)

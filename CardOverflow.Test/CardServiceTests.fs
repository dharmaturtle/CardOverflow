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
  let question = Guid.NewGuid().ToString()
  let answer = Guid.NewGuid().ToString()

  cardRepository.SaveCard(CardEntity(Question = question, Answer = answer, ConceptId = concept.Id))

  cardRepository.GetCards()
  |> Seq.filter(fun x -> x.Question = question && x.Answer = answer)
  |> Seq.length
  |> fun l -> Assert.Equal(1, l)

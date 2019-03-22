module CardServiceTests

open CardOverflow.Api
open System
open Xunit

let cardRepository = CardRepository(Test.DbService)

[<Fact>]
let ``CardService can add and retreive a card``() =
  let question = Guid.NewGuid().ToString()
  let answer = Guid.NewGuid().ToString()

  cardRepository.SaveCard(question, answer)

  cardRepository.GetCards()
  |> Seq.filter (fun x -> x.Question = question && x.Answer = answer)
  |> Seq.length
  |> fun l -> Assert.Equal(1, l)

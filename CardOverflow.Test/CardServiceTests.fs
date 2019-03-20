module CardServiceTests

open System
open Xunit
open CardOverflow.Api

[<Fact>]
let ``CardService can add and retreive a card`` () =
  let question = Guid.NewGuid().ToString()
  let answer = Guid.NewGuid().ToString()
  
  CardService().SaveCard(question, answer)

  CardService().GetCards() 
  |> Seq.filter (fun x -> x.Question = question && x.Answer = answer)
  |> Seq.length
  |> fun l -> Assert.Equal(1, l)

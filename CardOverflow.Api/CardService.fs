namespace CardOverflow.Api
open System
open System.Linq
open CardOverflow.Entity

type CardService () =

  member this.GetCards () =
    DbService.query(fun db -> db.Cards.ToList())
  
  member this.SaveCard (question: string, answer: string) =
    let c = Card(Question=question, Answer=answer)
    DbService.command(fun db -> db.Cards.Add(c))

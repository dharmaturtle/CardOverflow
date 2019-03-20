namespace CardOverflow.Api
open System
open System.Linq
open CardOverflow.Entity

type CardRepository(dbService:DbService) =

  member this.GetCards () =
    dbService.Query(fun db -> db.Cards.ToList())
  
  member this.SaveCard (question: string, answer: string) =
    let c = Card(Question=question, Answer=answer, ConceptId=1) // todo
    dbService.Command(fun db -> db.Cards.Add(c))

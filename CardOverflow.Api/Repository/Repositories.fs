namespace CardOverflow.Api

open CardOverflow.Api.Extensions
open CardOverflow.Entity
open System.Linq

type CardRepository(dbService: DbService) =
  member this.GetCards() =
    dbService.Query(fun db -> db.Cards.ToList())

  member this.SaveCard(card: Card) =
    dbService.Command(fun db -> db.Cards.Add card)

type ConceptRepository(dbService: DbService, dbFactory: DbFactory) =
  member this.GetConcepts() =
    dbService.Query(fun db -> db.Concepts.ToList())

  member this.SaveConcept(concept: Concept) =
    dbService.Command(fun db -> db.Concepts.Add concept)

  member this.SaveConcepts(concepts: ResizeArray<Concept>) =
    dbService.Command(fun db -> 
      this.GetConcepts().Merge concepts
        (fun (x, y) -> x.Id = y.Id)
        id
        (db.Remove >> ignore)
        (db.Add >> ignore)
        (fun d s -> d.Title <- s.Title)
    )

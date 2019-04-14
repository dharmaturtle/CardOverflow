namespace CardOverflow.Api

open CardOverflow.Api.Extensions
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open System.Collections.Generic
open System.Linq

type CardRepository(dbService: DbService) =
  member __.GetCards() =
    dbService.Query(fun db -> db.Cards.ToList())
  
  member __.GetCardsForQuiz() =
    dbService.Query(fun db -> db.Cards.Include(fun x -> x.Concept.ConceptOption).ToList()) |> Seq.map QuizCard.Create

  member __.SaveCard card =
    dbService.Command(fun db -> db.Cards.Add card)

type ConceptRepository(dbService: DbService) =
  member __.GetConcepts() =
    dbService.Query(fun db -> db.Concepts.Include(fun x -> x.Cards).ToList())

  member __.SaveConcept concept =
    dbService.Command(fun db -> db.Concepts.Add concept)

  member this.SaveConcepts(concepts: ResizeArray<ConceptEntity>) =
    dbService.Command(fun db ->
      this.GetConcepts().Merge concepts
        (fun (x, y) -> x.Id = y.Id)
        id
        (db.Remove >> ignore)
        (db.Add >> ignore)
        (fun d s -> // todo make copyto
          d.Title <- s.Title
          d.Description <- s.Description
          d.Fields <- s.Fields
          d.ConceptOption <- s.ConceptOption
          d.ConceptTemplate <- s.ConceptTemplate
          db.Update d |> ignore)
    )

type UserRepository(dbService: DbService) =
  member __.GetUser email =
    dbService.Query(fun db -> db.Users.First(fun x -> x.Email = email))

type TagRepository(dbService: DbService) =
  member __.CreateTag tag =
    dbService.Command(fun db -> db.Tags.Add tag)

  member __.SearchTags(input:string) =
    dbService.Query(fun db -> db.Tags.Where(fun t -> t.Name.ToLower().Contains(input.ToLower())))
    
  member __.UpdateTag tag =
    dbService.Command(fun db -> db.Tags.Update tag)

  member __.DeleteTag tag =
    dbService.Command(fun db -> db.Tags.Remove tag)

type DeckRepository(dbService: DbService) =
  member __.Create deck =
    dbService.Command(fun db -> db.Decks.Add deck)

  member __.Get userId =
    dbService.Query(fun db -> db.Decks.Where(fun d -> d.UserId = userId))
    
  member __.Update deck =
    dbService.Command(fun db -> db.Decks.Update deck)

  member __.Delete deck =
    dbService.Command(fun db -> db.Decks.Remove deck)

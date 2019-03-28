namespace CardOverflow.Api

open CardOverflow.Api.Extensions
open CardOverflow.Entity
open System.Linq
open System.Collections.Generic
open Microsoft.EntityFrameworkCore

type CardRepository(dbService: DbService) =
  member __.GetCards() =
    dbService.Query(fun db -> db.Cards.ToList())

  member __.SaveCard(card: Card) =
    dbService.Command(fun db -> db.Cards.Add card)

type ConceptRepository(dbService: DbService) =
  member __.GetConcepts() =
    dbService.Query(fun db -> db.Concepts.Include(fun x -> x.Cards).ToList())

  member __.SaveConcept(concept: Concept) =
    dbService.Command(fun db -> db.Concepts.Add concept)

  member this.SaveConcepts(concepts: ResizeArray<Concept>) =
    dbService.Command(fun db ->
      let updateCards(target: ICollection<Card>)(source: ICollection<Card>) = 
        target.Merge source
          (fun (x, y) -> x.Id = y.Id)
          (id)
          (target.Remove >> ignore)
          (target.Add)
          (fun d s -> // todo make copyto
            d.Question <- s.Question
            d.Answer <- s.Answer)
      this.GetConcepts().Merge concepts
        (fun (x, y) -> x.Id = y.Id)
        id
        (db.Remove >> ignore)
        (db.Add >> ignore)
        (fun d s -> // todo make copyto
          d.Title <- s.Title
          d.Description <- s.Description
          updateCards d.Cards s.Cards
          db.Update d |> ignore)
    )

type UserRepository(dbService: DbService) =
  member __.GetUser(email: string) =
    dbService.Query(fun db -> db.Users.First(fun x -> x.Email = email))

type TagRepository(dbService: DbService) =
  member __.CreateTag(tag: Tag) =
    dbService.Command(fun db -> db.Tags.Add tag)

  member __.SearchTags(input: string) =
    dbService.Query(fun db -> db.Tags.Where(fun t -> t.Name.ToLower().Contains(input.ToLower())))
    
  member __.UpdateTag(tag: Tag) =
    dbService.Command(fun db -> db.Tags.Update tag)

  member __.DeleteTag(tag: Tag) =
    dbService.Command(fun db -> db.Tags.Remove tag)

type DeckRepository(dbService: DbService) =
  member __.CreateDeck(deck: Deck) =
    dbService.Command(fun db -> db.Decks.Add deck)

  member __.GetDecks(userId: int) =
    dbService.Query(fun db -> db.Decks.Where(fun d -> d.UserId = userId))
    
  member __.UpdateDeck(deck: Deck) =
    dbService.Command(fun db -> db.Decks.Update deck)

  member __.DeleteDeck(deck: Deck) =
    dbService.Command(fun db -> db.Decks.Remove deck)

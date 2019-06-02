namespace CardOverflow.Api

open LoadersAndCopiers
open CardOverflow.Pure
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open System.Linq
open Helpers

module CardRepository =
    let GetCards (db: CardOverflowDb) =
        db.Cards.ToList()
    
    let GetCardsForQuiz (db: CardOverflowDb) =
        db.Cards.Include(fun x -> x.CardOption).ToList() 
        |> Seq.map QuizCard.Load

    let SaveCard (db: CardOverflowDb) card =
        db.Cards.AddI card
        db.SaveChangesI ()

module ConceptRepository =
    let GetConcepts (db: CardOverflowDb) =
        db.Concepts.Include(fun x -> x.Cards).ToList()

    let AddConcept (db: CardOverflowDb) concept =
        db.Concepts.AddI concept
        db.SaveChangesI ()

    // member this.SaveConcepts(concepts: ResizeArray<ConceptEntity>) =
    //                 this.GetConcepts().Merge concepts
    //             (fun (x, y) -> x.Id = y.Id)
    //             id
    //             (db.Remove >> ignore)
    //             (db.Add >> ignore)
    //             (fun d s -> // todo make copyto
    //                 d.Title <- s.Title
    //                 d.Description <- s.Description
    //                 d.Fields <- s.Fields
    //                 d.ConceptTemplate <- s.ConceptTemplate
    //                 db.Update d |> ignore)
    //     )

module UserRepository =
    let GetUser (db: CardOverflowDb) email =
        db.Users.First(fun x -> x.Email = email)

module PrivateTagRepository =
    let Add (db: CardOverflowDb) userId newTags =
        let newTags = newTags |> List.distinct // https://stackoverflow.com/a/18113534
        db.PrivateTags
            .Where(fun x -> x.UserId = userId)
            .Select(fun x -> x.Name)
            .AsEnumerable()
            .Where(newTags.Contains)
            .ToList()
            .Contains >> not
        |> newTags.Where
        |> Seq.map (fun x -> PrivateTagEntity(Name = x, UserId = userId ))
        |> db.PrivateTags.AddRange
        db.SaveChangesI ()

    let Search (db: CardOverflowDb) (input: string) =
        db.PrivateTags.Where(fun t -> t.Name.ToLower().Contains(input.ToLower()))
        
    let Update (db: CardOverflowDb) tag =
        db.PrivateTags.UpdateI tag
        db.SaveChangesI ()

    let Delete (db: CardOverflowDb) tag =
        db.PrivateTags.RemoveI tag
        db.SaveChangesI ()

module DeckRepository =
    let Create (db: CardOverflowDb) deck =
        db.Decks.AddI deck
        db.SaveChangesI ()

    let Get (db: CardOverflowDb) userId =
        db.Decks.Where(fun d -> d.UserId = userId).ToList()
        
    let Update (db: CardOverflowDb) deck =
        db.Decks.UpdateI deck
        db.SaveChangesI ()

    let Delete (db: CardOverflowDb) deck =
        db.Decks.RemoveI deck
        db.SaveChangesI ()

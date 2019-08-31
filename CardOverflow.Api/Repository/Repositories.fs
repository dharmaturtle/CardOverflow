namespace CardOverflow.Api

open System
open LoadersAndCopiers
open CardOverflow.Pure
open CardOverflow.Debug
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open System.Linq
open Helpers
open FSharp.Control.Tasks
open System.Collections.Generic
open X.PagedList

module CommentRepository =
    let addAndSaveAsync (db: CardOverflowDb) (comment: CommentFacetEntity) =
        db.CommentFacet.AddI comment
        db.SaveChangesAsyncI ()

module FacetTemplateRepository =
    let GetFromInstance (db: CardOverflowDb) instanceId =
        task {
            let! instance =
                db.FacetTemplateInstance
                    .Include(fun x -> x.FacetTemplate.FacetTemplateInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetTemplateInstanceEntity) -> x.CardTemplates)
                    .Include(fun x -> x.FacetTemplate.FacetTemplateInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetTemplateInstanceEntity) -> x.Fields)
                    .FirstAsync(fun x -> x.Id = instanceId)
            return instance.FacetTemplate |> FacetTemplate.load
        }

module HistoryRepository =
    let addAndSaveAsync (db: CardOverflowDb) e =
        db.History.AddI e
        db.SaveChangesAsyncI ()

module CardRepository =
    let private getCompleteCards (db: CardOverflowDb) =
        db.AcquiredCard
            .Include(fun x -> x.CardOption)
            .Include(fun x -> x.Card.FacetInstance.FieldValues :> IEnumerable<_>)
                .ThenInclude(fun (x: FieldValueEntity) -> x.Field)
            .Include(fun x -> x.Card.CardTemplate.FacetTemplateInstance)
    let GetTodaysCards (db: CardOverflowDb) userId =
        let tomorrow = DateTime.UtcNow.AddDays 1.
        task {
            let! cards =
                (getCompleteCards db)
                    .Where(fun x -> x.UserId = userId && x.Due < tomorrow)
                    .OrderBy(fun x -> x.Due)
                    .ToListAsync()
            return
                cards |> Seq.map QuizCard.load
        }
    let GetAllCards (db: CardOverflowDb) userId =
        (getCompleteCards db)
            .Where(fun x -> x.UserId = userId)
            .AsEnumerable()
        |> Seq.map QuizCard.load
    let AcquireCardsAsync (db: CardOverflowDb) userId cardIds =
        let defaultCardOptionId =
            db.CardOption
                .First(fun x -> x.UserId = userId && x.IsDefault).Id
        cardIds
        |> Seq.map (fun cardId ->
            let card =
                AcquiredCard.InitialCopyTo
                    userId
                    defaultCardOptionId
                    []
            card.CardId <- cardId
            card
        ) |> db.AcquiredCard.AddRange
        db.SaveChangesAsyncI ()

module ConceptRepository =
    let Get (db: CardOverflowDb) conceptId =
        task {
            let! concept =
                db.Concept
                    .Include(fun x -> x.Maintainer)
                    .Include(fun x -> x.Facets :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetEntity) -> x.FacetInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetInstanceEntity) -> x.Cards :> IEnumerable<_>)
                        .ThenInclude(fun (x: CardEntity) -> x.CardTemplate.FacetTemplateInstance)
                    .Include(fun x -> x.Facets :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetEntity) -> x.FacetInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetInstanceEntity) -> x.FieldValues :> IEnumerable<_>)
                        .ThenInclude(fun (x: FieldValueEntity) -> x.Field)
                    .Include(fun x -> x.Facets :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetEntity) -> x.CommentFacets :> IEnumerable<_>)
                        .ThenInclude(fun (x: CommentFacetEntity) -> x.User)
                    .FirstAsync(fun x -> x.Id = conceptId)
            return concept |> DetailedConcept.load 0
        }
    let GetForUser (db: CardOverflowDb) conceptId userId =
        task {
            let! concept =
                db.Concept
                    .Include(fun x -> x.Maintainer)
                    .Include(fun x -> x.Facets :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetEntity) -> x.FacetInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetInstanceEntity) -> x.Cards :> IEnumerable<_>)
                        .ThenInclude(fun (x: CardEntity) -> x.CardTemplate.FacetTemplateInstance)
                    .Include(fun x -> x.Facets :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetEntity) -> x.FacetInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetInstanceEntity) -> x.FieldValues :> IEnumerable<_>)
                        .ThenInclude(fun (x: FieldValueEntity) -> x.Field)
                    .Include(fun x -> x.Facets :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetEntity) -> x.CommentFacets :> IEnumerable<_>)
                        .ThenInclude(fun (x: CommentFacetEntity) -> x.User)
                    .Include(fun x -> x.Facets :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetEntity) -> x.FacetInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetInstanceEntity) -> x.Cards :> IEnumerable<_>)
                        .ThenInclude(fun (x: CardEntity) -> x.AcquiredCards)
                    .FirstAsync(fun x -> x.Id = conceptId)
            return concept |> DetailedConcept.load userId
        }
    let CreateConcept (db: CardOverflowDb) (concept: InitialConceptInstance) fileFacetInstances =
        fileFacetInstances |> concept.CopyToNew |> db.Concept.AddI
        db.SaveChangesI ()
    let GetAcquiredConceptsAsync (db: CardOverflowDb) (userId: int) (pageNumber: int) =
        task {
            let! r =
                db.Concept
                    .Where(fun x -> x.Facets.Any(fun x -> x.FacetInstances.Any(fun x -> x.Cards.Any(fun x -> x.AcquiredCards.Any(fun x -> x.UserId = userId)))))
                    .Include(fun x -> x.Facets :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetEntity) -> x.FacetInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetInstanceEntity) -> x.FieldValues :> IEnumerable<_>)
                        .ThenInclude(fun (x: FieldValueEntity) -> x.Field)
                    .Include(fun x -> x.Facets :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetEntity) -> x.FacetInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetInstanceEntity) -> x.Cards :> IEnumerable<_>)
                        .ThenInclude(fun (x: CardEntity) -> x.CardTemplate.FacetTemplateInstance)
                    .Include(fun x -> x.Facets :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetEntity) -> x.FacetInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: FacetInstanceEntity) -> x.Cards :> IEnumerable<_>)
                        .ThenInclude(fun (x: CardEntity) -> x.AcquiredCards :> IEnumerable<_>)
                        .ThenInclude(fun (x: AcquiredCardEntity) -> x.PrivateTag_AcquiredCards :> IEnumerable<_>)
                        .ThenInclude(fun (x: PrivateTag_AcquiredCardEntity) -> x.PrivateTag)
                    .ToPagedListAsync(pageNumber, 20)
            return {
                Results = r |> Seq.map (AcquiredConcept.load userId)
                Details = {
                    CurrentPage = r.PageNumber
                    PageCount = r.PageCount
                }
            }
                
        }
    let Update (db: CardOverflowDb) conceptId conceptName =
        task {
            let! concept = db.Concept.FirstAsync(fun x -> x.Id = conceptId)
            concept.Name <- conceptName
            db.Concept.UpdateI concept
            return! db.SaveChangesAsync()
        }

    // member this.SaveFacets(facets: ResizeArray<FacetEntity>) =
    //                 this.GetFacets().Merge facets
    //             (fun (x, y) -> x.Id = y.Id)
    //             id
    //             (db.Remove >> ignore)
    //             (db.Add >> ignore)
    //             (fun d s -> // todo make copyto
    //                 d.Title <- s.Title
    //                 d.Description <- s.Description
    //                 d.Fields <- s.Fields
    //                 d.FacetTemplate <- s.FacetTemplate
    //                 db.Update d |> ignore)
    //     )

module CardOptionsRepository =
    let defaultCardOptions =
        { Id = 0
          Name = "Default"
          IsDefault = true
          NewCardsSteps = [ TimeSpan.FromMinutes 1.; TimeSpan.FromMinutes 10. ]
          NewCardsMaxPerDay = int16 20
          NewCardsGraduatingInterval = TimeSpan.FromDays 1.
          NewCardsEasyInterval = TimeSpan.FromDays 4.
          NewCardsStartingEaseFactor = 2.5
          NewCardsBuryRelated = true
          MatureCardsMaxPerDay = int16 200
          MatureCardsEaseFactorEasyBonusFactor = 1.3
          MatureCardsIntervalFactor = 1.
          MatureCardsMaximumInterval = 36500. |> TimeSpanInt16.fromDays
          MatureCardsHardInterval = 1.2
          MatureCardsBuryRelated = true
          LapsedCardsSteps = [ TimeSpan.FromMinutes 10. ]
          LapsedCardsNewIntervalFactor = 0.
          LapsedCardsMinimumInterval = TimeSpan.FromDays 1.
          LapsedCardsLeechThreshold = byte 8
          ShowAnswerTimer = false
          AutomaticallyPlayAudio = false
          ReplayQuestionAudioOnAnswer = false }
    //let defaultAnkiCardOptions =
    //    { Id = 0
    //      Name = "Default Anki Options"
    //      NewCardsSteps = [ TimeSpan.FromMinutes 1.; TimeSpan.FromMinutes 10. ]
    //      NewCardsMaxPerDay = int16 20
    //      NewCardsGraduatingInterval = TimeSpan.FromDays 1.
    //      NewCardsEasyInterval = TimeSpan.FromDays 4.
    //      NewCardsStartingEaseFactor = 2.5
    //      NewCardsBuryRelated = false
    //      MatureCardsMaxPerDay = int16 200
    //      MatureCardsEaseFactorEasyBonusFactor = 1.3
    //      MatureCardsIntervalFactor = 1.
    //      MatureCardsMaximumInterval = 36500. |> TimeSpanInt16.fromDays
    //      MatureCardsHardInterval = 1.2
    //      MatureCardsBuryRelated = false
    //      LapsedCardsSteps = [ TimeSpan.FromMinutes 10. ]
    //      LapsedCardsNewIntervalFactor = 0.
    //      LapsedCardsMinimumInterval = TimeSpan.FromDays 1.
    //      LapsedCardsLeechThreshold = byte 8
    //      ShowAnswerTimer = false
    //      AutomaticallyPlayAudio = false
    //      ReplayQuestionAudioOnAnswer = false }

module UserRepository =
    let add (db: CardOverflowDb) name email =
        let cardOption = CardOptionsRepository.defaultCardOptions.CopyToNew 0
        cardOption.IsDefault <- true
        UserEntity(
            DisplayName = name,
            Email = email,
            CardOptions = (cardOption |> Seq.singleton |> fun x -> x.ToList())
        ) |> db.User.AddI
        db.SaveChangesI ()
    let Get (db: CardOverflowDb) email =
        db.User.FirstOrDefault(fun x -> x.Email = email)

module PrivateTagRepository =
    let Add (db: CardOverflowDb) userId newTags =
        let newTags = newTags |> Seq.distinct // https://stackoverflow.com/a/18113534
        db.PrivateTag
            .Where(fun x -> x.UserId = userId)
            .Select(fun x -> x.Name)
            .AsEnumerable()
            .Where(newTags.Contains)
            .ToList()
            .Contains >> not
        |> newTags.Where
        |> Seq.map (fun x -> PrivateTagEntity(Name = x, UserId = userId ))
        |> db.PrivateTag.AddRange
        db.SaveChangesI ()

    let Search (db: CardOverflowDb) userId (input: string) =
        db.PrivateTag.Where(fun t -> userId = t.UserId && t.Name.ToLower().Contains(input.ToLower())).ToList()
    
    let GetAll (db: CardOverflowDb) userId =
        db.PrivateTag.Where(fun t -> userId = t.UserId).ToList()
        
    let Update (db: CardOverflowDb) tag =
        db.PrivateTag.UpdateI tag
        db.SaveChangesI ()

    let Delete (db: CardOverflowDb) tag =
        db.PrivateTag.RemoveI tag
        db.SaveChangesI ()

module DeckRepository =
    let Create (db: CardOverflowDb) deck =
        db.Deck.AddI deck
        db.SaveChangesI ()

    let Get (db: CardOverflowDb) userId =
        db.Deck.Where(fun d -> d.UserId = userId).ToList()
        
    let Update (db: CardOverflowDb) deck =
        db.Deck.UpdateI deck
        db.SaveChangesI ()

    let Delete (db: CardOverflowDb) deck =
        db.Deck.RemoveI deck
        db.SaveChangesI ()

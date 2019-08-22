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

module HistoryRepository =
    let addAndSaveAsync (db: CardOverflowDb) e =
        task {
            let! _ = db.History.AddAsync e
            return! db.SaveChangesAsync()
        }

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
                cards |> Seq.map QuizCard.Load
        }
    let GetAllCards (db: CardOverflowDb) userId =
        (getCompleteCards db)
            .Where(fun x -> x.UserId = userId)
            .AsEnumerable()
        |> Seq.map QuizCard.Load
    let SaveCard (db: CardOverflowDb) card =
        db.Card.AddI card
        db.SaveChangesI ()
    let AcquireCards (db: CardOverflowDb) userId cardIds =
        let user = db.User.First(fun x -> x.Id = userId)
        cardIds
        |> List.map (fun i ->
            AcquiredCardEntity(
                UserId = userId, // eventualTODO missing FacetInstanceId and CardTemplateId
                CardState = CardState.toDb Normal,
                IsLapsed = false,
                EaseFactorInPermille = 0s,
                IntervalOrStepsIndex = Int16.MinValue,
                Due = DateTime.UtcNow,
                CardOption = user.CardOptions.First(fun x -> x.IsDefault)
            ))
        |> db.AcquiredCard.AddRange
        db.SaveChangesI ()

module ConceptRepository =
    let CreateConcept (db: CardOverflowDb) (concept: InitialConceptInstance) fileFacetInstances =
        fileFacetInstances |> concept.CopyToNew |> db.Concept.AddI
        db.SaveChangesI ()
    let GetAcquiredConceptsAsync (db: CardOverflowDb) userId =
        task {
            let! r =
                db.AcquiredCard
                    .Include(fun x -> x.Card.CardTemplate.FacetTemplateInstance)
                    .Include(fun x -> x.Card.FacetInstance.Facet.Concept)
                    .Include(fun x -> x.Card.FacetInstance.FieldValues :> IEnumerable<_>)
                        .ThenInclude(fun (x: FieldValueEntity) -> x.Field)
                    .Where(fun x -> x.UserId = userId)
                    .ToListAsync()
            return AcquiredConcept.Load r
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

module UserRepository =
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
    let add (db: CardOverflowDb) name email =
        let cardOption = defaultCardOptions.CopyToNew 0
        cardOption.IsDefault <- true
        UserEntity(
            DisplayName = name,
            Email = email,
            CardOptions = (cardOption |> Seq.singleton |> fun x -> x.ToList())
        ) |> db.User.AddI
        db.SaveChangesI ()
    let GetUser (db: CardOverflowDb) email =
        db.User.First(fun x -> x.Email = email)

module PrivateTagRepository =
    let Add (db: CardOverflowDb) userId newTags =
        let newTags = newTags |> List.distinct // https://stackoverflow.com/a/18113534
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

    let Search (db: CardOverflowDb) (input: string) =
        db.PrivateTag.Where(fun t -> t.Name.ToLower().Contains(input.ToLower()))
        
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

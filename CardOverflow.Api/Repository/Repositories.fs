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
open System.Threading.Tasks

module CommentRepository =
    let addAndSaveAsync (db: CardOverflowDb) (comment: CommentCardEntity) =
        db.CommentCard.AddI comment
        db.SaveChangesAsyncI ()

module CardTemplateRepository =
    let GetFromInstance (db: CardOverflowDb) instanceId =
        task {
            let! instance =
                db.CardTemplateInstance
                    .Include(fun x -> x.CardTemplate.CardTemplateInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: CardTemplateInstanceEntity) -> x.Fields)
                    .FirstAsync(fun x -> x.Id = instanceId)
            return instance.CardTemplate |> CardTemplate.load
        }

module HistoryRepository =
    let addAndSaveAsync (db: CardOverflowDb) e =
        db.History.AddI e
        db.SaveChangesAsyncI ()

module CardRepository =
    let private getCompleteCards (db: CardOverflowDb) =
        db.AcquiredCard
            .Include(fun x -> x.CardOption)
            .Include(fun x -> x.CardInstance.FieldValues :> IEnumerable<_>)
                .ThenInclude(fun (x: FieldValueEntity) -> x.Field.CardTemplateInstance)
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
    let AcquireCardsAsync (db: CardOverflowDb) userId cardInstanceIds =
        let defaultCardOptionId =
            db.CardOption
                .First(fun x -> x.UserId = userId && x.IsDefault).Id
        cardInstanceIds
        |> Seq.map (fun cardInstanceId ->
            let card =
                AcquiredCard.InitialCopyTo
                    userId
                    defaultCardOptionId
                    []
            card.CardInstanceId <- cardInstanceId
            card
        ) |> db.AcquiredCard.AddRange
        db.SaveChangesAsyncI ()
    let Get (db: CardOverflowDb) cardId userId =
        task {
            let! concept =
                if userId = 0
                then
                    db.Card
                        .Include(fun x -> x.Author)
                        .Include(fun x -> x.CommentCards :> IEnumerable<_>)
                            .ThenInclude(fun (x: CommentCardEntity) -> x.User )
                        .Include(fun x -> x.CardInstances :> IEnumerable<_>)
                            .ThenInclude(fun (x: CardInstanceEntity) -> x.FieldValues :> IEnumerable<_>)
                            .ThenInclude(fun (x: FieldValueEntity) -> x.Field.CardTemplateInstance)
                        //.Include(fun x -> x.Cards :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardEntity) -> x.CardInstances :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardInstanceEntity) -> x.Cards :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardEntity) -> x.CardTemplate.CardTemplateInstance)
                        //.Include(fun x -> x.Cards :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardEntity) -> x.CardInstances :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardInstanceEntity) -> x.FieldValues :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: FieldValueEntity) -> x.Field)
                        //.Include(fun x -> x.Cards :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardEntity) -> x.CommentCards :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CommentCardEntity) -> x.User)
                        .FirstAsync(fun x -> x.Id = cardId)
                else
                    db.Card
                        .Include(fun x -> x.Author)
                        .Include(fun x -> x.CommentCards :> IEnumerable<_>)
                            .ThenInclude(fun (x: CommentCardEntity) -> x.User )
                        .Include(fun x -> x.CardInstances :> IEnumerable<_>)
                            .ThenInclude(fun (x: CardInstanceEntity) -> x.FieldValues :> IEnumerable<_>)
                            .ThenInclude(fun (x: FieldValueEntity) -> x.Field.CardTemplateInstance)
                        .Include(fun x -> x.CardInstances :> IEnumerable<_>)
                            .ThenInclude(fun (x: CardInstanceEntity) -> x.AcquiredCards :> IEnumerable<_>)
                            .ThenInclude(fun (x: AcquiredCardEntity) -> x.Tag_AcquiredCards :> IEnumerable<_>)
                            .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
                        //    .ThenInclude(fun (x: CardEntity) -> x.CardInstances :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardInstanceEntity) -> x.Cards :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardEntity) -> x.CardTemplate.CardTemplateInstance)
                        //.Include(fun x -> x.Cards :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardEntity) -> x.CardInstances :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardInstanceEntity) -> x.FieldValues :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: FieldValueEntity) -> x.Field)
                        //.Include(fun x -> x.Cards :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardEntity) -> x.CommentCards :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CommentCardEntity) -> x.User)
                        //.Include(fun x -> x.Cards :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardEntity) -> x.CardInstances :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardInstanceEntity) -> x.Cards :> IEnumerable<_>)
                        //    .ThenInclude(fun (x: CardEntity) -> x.AcquiredCards)
                        .FirstAsync(fun x -> x.Id = cardId)
            return concept |> ExploreCard.load userId
        }
    let CreateCard (db: CardOverflowDb) (concept: InitialCardInstance) fileCardInstances =
        fileCardInstances |> concept.CopyToNew |> db.CardInstance.AddI
        db.SaveChangesI ()
    let private get (db: CardOverflowDb) =
        db.AcquiredCard
            .Include(fun x -> x.CardInstance.FieldValues :> IEnumerable<_>)
                .ThenInclude(fun (x: FieldValueEntity) -> x.Field.CardTemplateInstance)
            .Include(fun x -> x.CardInstance.AcquiredCards :> IEnumerable<_>)
                .ThenInclude(fun (x: AcquiredCardEntity) -> x.Tag_AcquiredCards :> IEnumerable<_>)
                .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
            //.Include(fun x -> x.Cards :> IEnumerable<_>)
            //    .ThenInclude(fun (x: CardEntity) -> x.CardInstances :> IEnumerable<_>)
            //    .ThenInclude(fun (x: CardInstanceEntity) -> x.Cards :> IEnumerable<_>)
            //    .ThenInclude(fun (x: CardEntity) -> x.CardTemplate.CardTemplateInstance)
            //.Include(fun x -> x.Cards :> IEnumerable<_>)
            //    .ThenInclude(fun (x: CardEntity) -> x.CardInstances :> IEnumerable<_>)
            //    .ThenInclude(fun (x: CardInstanceEntity) -> x.Cards :> IEnumerable<_>)
            //    .ThenInclude(fun (x: CardEntity) -> x.AcquiredCards :> IEnumerable<_>)
            //    .ThenInclude(fun (x: AcquiredCardEntity) -> x.Tag_AcquiredCards :> IEnumerable<_>)
            //    .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
    let GetAcquired (db: CardOverflowDb) (userId: int) (cardId: int) =
        get(db)
            .FirstAsync(fun x -> x.CardInstance.CardId = cardId && x.UserId = userId)
            .ContinueWith(fun (x: Task<AcquiredCardEntity>) -> AcquiredCard.load x.Result)
    let GetAcquiredPages (db: CardOverflowDb) (userId: int) (pageNumber: int) =
        task {
            let! r =
                get(db)
                    .Where(fun x -> x.UserId = userId)
                    .ToPagedListAsync(pageNumber, 15)
            return {
                Results = r |> Seq.map AcquiredCard.load
                Details = {
                    CurrentPage = r.PageNumber
                    PageCount = r.PageCount
                }
            }
        } 
    let GetAsync (db: CardOverflowDb) userId (pageNumber: int) =
        task {
            let! r =
                db.Card
                    .Include(fun x -> x.Author)
                    .Include(fun x -> x.CardInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: CardInstanceEntity) -> x.FieldValues :> IEnumerable<_>)
                        .ThenInclude(fun (x: FieldValueEntity) -> x.Field.CardTemplateInstance)
                    .Include(fun x -> x.CardInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: CardInstanceEntity) -> x.AcquiredCards :> IEnumerable<_>)
                        .ThenInclude(fun (x: AcquiredCardEntity) -> x.Tag_AcquiredCards :> IEnumerable<_>)
                        .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
                    .ToPagedListAsync(pageNumber, 15)
            return {
                Results = r |> Seq.map (ExploreCard.load userId)
                Details = {
                    CurrentPage = r.PageNumber
                    PageCount = r.PageCount
                }
            }
        }
    let UpdateFieldsToNewInstance (db: CardOverflowDb) (acquiredCard: AcquiredCard) =
        task {
            let! e = db.AcquiredCard.FirstAsync(fun x -> x.Id = acquiredCard.AcquiredCardId)
            e.CardInstance <- acquiredCard.CardInstance.CopyFieldsToNewInstance acquiredCard.CardId
            return! db.SaveChangesAsyncI()
        }

    // member this.SaveCards(cards: ResizeArray<CardEntity>) =
    //                 this.GetCards().Merge cards
    //             (fun (x, y) -> x.Id = y.Id)
    //             id
    //             (db.Remove >> ignore)
    //             (db.Add >> ignore)
    //             (fun d s -> // todo make copyto
    //                 d.Title <- s.Title
    //                 d.Description <- s.Description
    //                 d.Fields <- s.Fields
    //                 d.CardTemplate <- s.CardTemplate
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

module TagRepository =
    let tagEntities (db: CardOverflowDb) newTags =
        let newTags = newTags |> Seq.distinct // https://stackoverflow.com/a/18113534
        db.Tag // medTODO there's no filter, you're .ToListing all tags into memory
            .Select(fun x -> x.Name)
            .AsEnumerable()
            .Where(newTags.Contains)
            .ToList()
            .Contains >> not
        |> newTags.Where
        |> Seq.map (fun x -> TagEntity(Name = x))
    let Add (db: CardOverflowDb) userId newTags =
        tagEntities db newTags
        |> db.Tag.AddRange
        db.SaveChangesI ()

    let AddTo (db: CardOverflowDb) userId newTag acquiredCardId =
        defaultArg
            (db.Tag.SingleOrDefault(fun x -> x.Name = newTag) |> Option.ofObj)
            (TagEntity(Name = newTag))
        |> fun x -> Tag_AcquiredCardEntity(AcquiredCardId = acquiredCardId, Tag = x)
        |> db.Tag_AcquiredCard.AddI
        db.SaveChangesI ()
    let Search (db: CardOverflowDb) userId (input: string) =
        db.Tag.Where(fun t -> t.Name.ToLower().Contains(input.ToLower())).ToList()
    
    let GetAll (db: CardOverflowDb) userId =
        db.Tag.ToList()
        
    let Update (db: CardOverflowDb) tag =
        db.Tag.UpdateI tag
        db.SaveChangesI ()

    let Delete (db: CardOverflowDb) tag =
        db.Tag.RemoveI tag
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

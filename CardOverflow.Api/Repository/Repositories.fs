namespace CardOverflow.Api

open FsToolkit.ErrorHandling
open CardOverflow.Pure.Core
open System.Security.Cryptography
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
open Microsoft.FSharp.Core

module CommunalFieldRepository =
    let get (db: CardOverflowDb) fieldId = task {
        let! x = db.LatestCommunalFieldInstance.SingleAsync(fun x -> x.CommunalFieldId = fieldId)
        return x.Value
    }
    let getInstance (db: CardOverflowDb) instanceId = task {
        let! x = db.CommunalFieldInstance.SingleAsync(fun x -> x.Id = instanceId)
        return x.Value
    }
    let Search (db: CardOverflowDb) (query: string) = task {
        let! x =
            db.LatestCommunalFieldInstance
                .Where(fun x -> x.Value.Contains query)
                .ToListAsync()
        return x |> Seq.map CommunalFieldInstance.loadLatest |> toResizeArray
        }

module FeedbackRepository =
    let addAndSaveAsync (db: CardOverflowDb) userId title description priority =
        FeedbackEntity(
            Title = title,
            Description = description,
            UserId = userId,
            Created = DateTime.UtcNow,
            Priority = priority
        ) |> db.Feedback.AddI
        db.SaveChangesAsyncI()

module RelationshipRepository =
    let addAndSaveAsync (db: CardOverflowDb) (relationship: RelationshipEntity) =
        db.Relationship.AddI relationship
        db.SaveChangesAsyncI ()
    let removeAndSaveAsync (db: CardOverflowDb) sourceId targetId userId name =
        db.Relationship.SingleOrDefault(fun x -> 
            ((x.SourceId = sourceId && x.TargetId = targetId)  ||
             (x.SourceId = targetId && x.TargetId = sourceId)) &&
            x.Name = name &&
            x.UserId = userId
        ) |> function
        | null -> ()
        | x -> db.Relationship.RemoveI x
        db.SaveChangesAsyncI ()

module CommentRepository =
    let addAndSaveAsync (db: CardOverflowDb) (comment: CommentCardEntity) =
        db.CommentCard.AddI comment
        db.SaveChangesAsyncI ()

module CardTemplateRepository =
    let UpdateFieldsToNewInstance (db: CardOverflowDb) userId (instance: CardTemplateInstance) = task {
        let cardTemplate =
            if instance.CardTemplateId = 0 then
                Entity <| CardTemplateEntity(AuthorId = userId)
            else    
                Id <| instance.CardTemplateId
        let newTemplateInstance = instance.CopyToNewInstance cardTemplate
        db.CardTemplateInstance.AddI newTemplateInstance
        db  
            .AcquiredCard
            .Include(fun x -> x.CardInstance)
            .Where(fun x -> x.CardInstance.CardTemplateInstanceId = instance.Id)
            |> Seq.iter(fun ac ->
                db.Entry(ac.CardInstance).State <- EntityState.Added
                ac.CardInstance.Id <- ac.CardInstance.GetHashCode()
                db.Entry(ac.CardInstance).Property(Core.nameof <@ any<CardInstanceEntity>.Id @>).IsTemporary <- true
                ac.CardInstance.Created <- DateTime.UtcNow
                ac.CardInstance.Modified <- Nullable()
                ac.CardInstance.CardTemplateInstance <- newTemplateInstance
            )
        return! db.SaveChangesAsyncI()
        }

module HistoryRepository =
    let addAndSaveAsync (db: CardOverflowDb) e =
        db.History.AddI e
        db.SaveChangesAsyncI ()
    let getHeatmap (db: CardOverflowDb) userId = task {
        let oneYearAgo = DateTime.UtcNow - TimeSpan.FromDays 365.
        let! dateCounts =
            (query {
                for h in db.History do
                where (h.Timestamp >= oneYearAgo && h.AcquiredCard.UserId = userId)
                groupValBy h h.Timestamp.Date into g
                select { Date = g.Key; Count = g.Count() }
            }).ToListAsync()
        return Heatmap.get oneYearAgo DateTime.UtcNow (dateCounts |> List.ofSeq) }

module CardRepository =
    let instance (db: CardOverflowDb) instanceId = task {
        let! r =
            db.CardInstance
                .Include(fun x -> x.CardTemplateInstance)
                .SingleOrDefaultAsync(fun x -> x.Id = instanceId)
        return
            match r with
            | null -> Error "Card instance not found"
            | x -> Ok <| CardInstanceView.load x
    }
    let getView (db: CardOverflowDb) cardId = task {
        let! r =
            db.LatestCardInstance
                .Include(fun x -> x.CardTemplateInstance)
                .SingleAsync(fun x -> x.CardId = cardId)
        return CardInstanceView.loadLatest r
    }
    let Revisions (db: CardOverflowDb) userId cardId = task {
        let! r =
            db.Card
                .Include(fun x -> x.Author)
                .Include(fun x -> x.CardInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CardInstanceEntity) -> x.CardTemplateInstance)
                .SingleAsync(fun x -> x.Id = cardId)
        return CardRevision.load userId r
    }
    let AcquireCardAsync (db: CardOverflowDb) userId cardInstanceId = task {
        let! user = db.User.SingleAsync(fun x -> x.Id = userId)
        let! cardInstance = db.CardInstance.SingleAsync(fun x -> x.Id = cardInstanceId)
        match! db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.UserId = userId && x.CardInstance.CardId = cardInstance.CardId) with
        | null ->
            let card =
                AcquiredCard.initialize
                    userId
                    user.DefaultCardOptionId.Value // medTODO handle the null case
                    []
                |> fun x -> x.copyToNew [] // medTODO get tags from template
            card.CardInstanceId <- cardInstanceId
            card |> db.AcquiredCard.AddI
        | x -> x.CardInstanceId <- cardInstanceId
        return! db.SaveChangesAsyncI ()
        }
    let UnacquireCardAsync (db: CardOverflowDb) acquiredCardId =
        db.AcquiredCard.Single(fun x -> x.Id = acquiredCardId)
        |> db.AcquiredCard.RemoveI
        db.SaveChangesAsyncI ()
    let Get (db: CardOverflowDb) userId cardId =
        task {
            let! isAcquired = db.AcquiredCard.AnyAsync(fun x -> x.UserId = userId && x.CardInstance.CardId = cardId)
            let! e =
                db.LatestCardInstance
                    .Include(fun x -> x.CommunalFieldInstance_CardInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: CommunalFieldInstance_CardInstanceEntity) -> x.CommunalFieldInstance)
                    .Include(fun x -> x.CardTemplateInstance)
                    .SingleAsync(fun x -> x.CardId = cardId)
            let latestInstance = CardInstanceMeta.loadLatest isAcquired e
            let! concept =
                if userId = 0 then
                    db.Card
                        .Include(fun x -> x.Author)
                        .Include(fun x -> x.CommentCards :> IEnumerable<_>)
                            .ThenInclude(fun (x: CommentCardEntity) -> x.User )
                        .Include(fun x -> x.CardInstances :> IEnumerable<_>)
                            .ThenInclude(fun (x: CardInstanceEntity) -> x.CardTemplateInstance)
                        .SingleAsync(fun x -> x.Id = cardId)
                else
                    db.Card
                        .Include(fun x -> x.Author)
                        .Include(fun x -> x.CommentCards :> IEnumerable<_>)
                            .ThenInclude(fun (x: CommentCardEntity) -> x.User )
                        .Include(fun x -> x.CardInstances :> IEnumerable<_>)
                            .ThenInclude(fun (x: CardInstanceEntity) -> x.AcquiredCards :> IEnumerable<_>)
                            .ThenInclude(fun (x: AcquiredCardEntity) -> x.Tag_AcquiredCards :> IEnumerable<_>)
                            .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
                        .Include(fun x -> x.CardInstances :> IEnumerable<_>)
                            .ThenInclude(fun (x: CardInstanceEntity) -> x.CommunalFieldInstance_CardInstances :> IEnumerable<_>)
                            .ThenInclude(fun (x: CommunalFieldInstance_CardInstanceEntity) -> x.CommunalFieldInstance)
                        .Include(fun x -> x.RelationshipSources :> IEnumerable<_>)
                            .ThenInclude(fun (x: RelationshipEntity) -> x.Target.CardInstances :> IEnumerable<_>)
                            .ThenInclude(fun (x: CardInstanceEntity) -> x.CardTemplateInstance)
                        .Include(fun x -> x.RelationshipSources :> IEnumerable<_>)
                            .ThenInclude(fun (x: RelationshipEntity) -> x.Target.CardInstances :> IEnumerable<_>)
                            .ThenInclude(fun (x: CardInstanceEntity) -> x.AcquiredCards)
                        .Include(fun x -> x.RelationshipTargets :> IEnumerable<_>)
                            .ThenInclude(fun (x: RelationshipEntity) -> x.Source.CardInstances :> IEnumerable<_>)
                            .ThenInclude(fun (x: CardInstanceEntity) -> x.CardTemplateInstance)
                        .Include(fun x -> x.RelationshipTargets :> IEnumerable<_>)
                            .ThenInclude(fun (x: RelationshipEntity) -> x.Source.CardInstances :> IEnumerable<_>)
                            .ThenInclude(fun (x: CardInstanceEntity) -> x.AcquiredCards)
                        .SingleAsync(fun x -> x.Id = cardId)
            return concept |> ExploreCard.load userId latestInstance
        }
    let GetAcquired (db: CardOverflowDb) (userId: int) (cardId: int) = task {
        let! r =
            db.AcquiredCardIsLatest
                .Include(fun x -> x.CardInstance.CardTemplateInstance)
                .Include(fun x -> x.CardInstance.CommunalFieldInstance_CardInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_CardInstanceEntity) -> x.CommunalFieldInstance)
                .Include(fun x -> x.CardInstance.AcquiredCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: AcquiredCardEntity) -> x.Tag_AcquiredCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
                .SingleOrDefaultAsync(fun x -> x.CardInstance.CardId = cardId && x.UserId = userId)
        return
            match r with
            | null -> Error "That card doesn't exist!"
            | x -> AcquiredCard.load x
        }
    let getNew (db: CardOverflowDb) userId = task {
        let! user = db.User.SingleAsync(fun x -> x.Id = userId)
        return AcquiredCard.initialize userId user.DefaultCardOptionId.Value [] // medTODO handle the null
        }
    let private searchAcquired (db: CardOverflowDb) userId (searchTerm: string) =
        db.AcquiredCard
            .Include(fun x -> x.CardInstance.CardTemplateInstance)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                x.Tag_AcquiredCards.Any(fun x -> x.Tag.Name.Contains searchTerm) ||
                if String.IsNullOrWhiteSpace searchTerm then 
                    true
                else
                    EF.Functions.FreeText(x.CardInstance.FieldValues, searchTerm)
            )
    let private searchAcquiredIsLatest (db: CardOverflowDb) userId (searchTerm: string) =
        db.AcquiredCardIsLatest
            .Include(fun x -> x.CardInstance.CardTemplateInstance)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                x.Tag_AcquiredCards.Any(fun x -> x.Tag.Name.Contains searchTerm) ||
                if String.IsNullOrWhiteSpace searchTerm then 
                    true
                else
                    EF.Functions.FreeText(x.CardInstance.FieldValues, searchTerm)
            )
    let GetAcquiredPages (db: CardOverflowDb) (userId: int) (pageNumber: int) (searchTerm: string) =
        task {
            let! r =
                (searchAcquiredIsLatest db userId searchTerm)
                    .Include(fun x -> x.Tag_AcquiredCards :> IEnumerable<_>)
                        .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
                    .ToPagedListAsync(pageNumber, 15)
            return {
                Results = r |> Seq.map AcquiredCard.load
                Details = {
                    CurrentPage = r.PageNumber
                    PageCount = r.PageCount
                }
            }
        }
    let GetQuizBatch (db: CardOverflowDb) userId query =
        let tomorrow = DateTime.UtcNow.AddDays 1.
        task {
            let! cards =
                (searchAcquired db userId query)
                    .Where(fun x -> x.Due < tomorrow && x.CardState = CardState.toDb Normal)
                    .Include(fun x -> x.CardOption)
                    .OrderBy(fun x -> x.Due)
                    .Take(5)
                    .ToListAsync()
            return
                cards |> Seq.map QuizCard.load |> toResizeArray
        }
    let GetDueCount (db: CardOverflowDb) userId query =
        let tomorrow = DateTime.UtcNow.AddDays 1.
        (searchAcquired db userId query)
            .Where(fun x -> x.Due < tomorrow && x.CardState = CardState.toDb Normal)
            .Count()
    let SearchAsync (db: CardOverflowDb) userId (pageNumber: int) (searchTerm: string) =
        task {
            let! r =
                db.LatestCardInstance
                    .Where(fun x ->
                        x.CardInstance.AcquiredCards.Any(fun x -> x.Tag_AcquiredCards.Any(fun x -> x.Tag.Name.Contains searchTerm)) ||
                        if String.IsNullOrWhiteSpace searchTerm then 
                            true
                        else
                            EF.Functions.FreeText(x.CardInstance.FieldValues, searchTerm)
                    )
                    .Include(fun x -> x.CardTemplateInstance)
                    .Include(fun x -> x.Author)
                    .OrderByDescending(fun x -> x.CardUsers)
                    .ToPagedListAsync(pageNumber, 15)
            return {
                Results =
                    r |> Seq.map (fun c ->
                        let isAcquired = db.AcquiredCard.Any(fun x -> x.UserId = userId && x.CardInstance.CardId = c.CardId)
                        {   Id = c.CardId
                            Author = c.Author.DisplayName
                            AuthorId = c.AuthorId
                            Users = c.CardUsers
                            Instance = CardInstanceMeta.loadLatest isAcquired c
                        }
                    ) // medTODO optimize
                Details = {
                    CurrentPage = r.PageNumber
                    PageCount = r.PageCount
                }
            }
        }
    let UpdateFieldsToNewInstance (db: CardOverflowDb) (acquiredCard: AcquiredCard) (command: EditCardCommand) =
        let getTagId (db: CardOverflowDb) (input: string) =
            let r =
                match db.Tag.SingleOrDefault(fun x -> x.Name = input) with
                | null ->
                    let e = TagEntity(Name = input)
                    db.Tag.AddI e
                    e
                | x -> x
            db.SaveChangesI ()
            r.Id
        task {
            let card =
                if acquiredCard.CardId = 0 then
                    Entity <| fun () -> CardEntity(AuthorId = acquiredCard.UserId)
                else
                    Id <| acquiredCard.CardId
            let! entity =
                db.AcquiredCard
                    .Include(fun x -> x.CardInstance.CommunalFieldInstance_CardInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: CommunalFieldInstance_CardInstanceEntity) -> x.CommunalFieldInstance.CommunalField)
                    .Include(fun x -> x.CardInstance.CommunalFieldInstance_CardInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: CommunalFieldInstance_CardInstanceEntity) -> x.CommunalFieldInstance.CommunalFieldInstance_CardInstances)
                    .SingleOrDefaultAsync(fun x -> x.Id = acquiredCard.AcquiredCardId)
            let createCommunalFieldInstanceEntity c fieldName =
                CommunalFieldInstanceEntity(
                    CommunalField = CommunalFieldEntity(AuthorId = acquiredCard.UserId),
                    FieldName = fieldName,
                    Value =
                        if c.TemplateInstance.IsCloze then
                            command.FieldValues.Single(fun x -> x.EditField.Name = fieldName).Value
                        else
                            c.FieldValues.Single(fun x -> x.EditField.Name = fieldName).Value
                        ,
                    Created = DateTime.UtcNow,
                    EditSummary = c.EditSummary)
            let nonClozeCommunals =
                command.CommunalNonClozeFieldValues
                    .Select(fun x -> createCommunalFieldInstanceEntity command x.EditField.Name)
                    .ToList()
            let r =
                match entity with
                | null ->
                    let tags = acquiredCard.Tags |> Seq.map (getTagId db) // lowTODO could optimize. This is single threaded cause async saving causes issues, so try batch saving
                    if command.TemplateInstance.IsCloze then
                        let valueByFieldName = command.FieldValues.Select(fun x -> x.EditField.Name, x.Value) |> Map.ofSeq
                        AnkiImportLogic.maxClozeIndex "Something's wrong with your cloze indexes." valueByFieldName command.TemplateInstance.QuestionTemplate
                        |> Result.map (fun max ->
                        [1uy .. byte max] |> List.map (fun clozeIndex ->
                            let zip =
                                Seq.zip
                                    (valueByFieldName |> Seq.map (fun (KeyValue(k, _)) -> k))
                                    (valueByFieldName |> Seq.map (fun (KeyValue(_, v)) -> v) |> List.ofSeq |> AnkiImportLogic.multipleClozeToSingleClozeList clozeIndex)
                                |> Map.ofSeq
                            { command with
                                FieldValues =
                                    command.FieldValues.Select(fun x ->
                                    { x with
                                        Value = zip.[x.EditField.Name]}).ToList() }))
                    else Ok [ command ]
                    |> Result.map (List.map (fun c ->
                        let communalInstances =
                            c.FieldValues.Select(fun edit ->
                                let fieldName = edit.EditField.Name
                                edit.Communal |> Option.bind(fun value ->
                                    if value.CommunalCardInstanceIds.Contains 0 then
                                        match value.InstanceId with
                                        | Some id -> db.CommunalFieldInstance.Single(fun x -> x.Id = id)
                                        | None ->
                                            nonClozeCommunals.SingleOrDefault(fun x -> x.FieldName = fieldName)
                                            |> Option.ofObj
                                            |> Option.defaultValue (createCommunalFieldInstanceEntity c fieldName)
                                        |> Some
                                    else None
                                )) |> List.ofSeq |> List.choose id
                        let e = acquiredCard.copyToNew tags
                        e.CardInstance <- c.CardView.CopyFieldsToNewInstance card c.EditSummary communalInstances
                        db.AcquiredCard.AddI e
                        communalInstances
                        ) >> Seq.collect id >> toResizeArray)
                | e ->
                    let communalFields, instanceIds =
                        e.CardInstance.CommunalFieldInstance_CardInstances.Select(fun x -> x.CommunalFieldInstance)
                        |> List.ofSeq
                        |> List.map (fun old ->
                            let newValue = command.FieldValues.Single(fun x -> x.EditField.Name = old.FieldName).Value
                            if old.Value = newValue then
                                old, []
                            else
                                CommunalFieldInstanceEntity(
                                    CommunalField = old.CommunalField,
                                    FieldName = old.FieldName,
                                    Value = newValue,
                                    Created = DateTime.UtcNow,
                                    EditSummary = command.EditSummary),
                                old.CommunalFieldInstance_CardInstances
                                    .Select(fun x -> x.CardInstanceId)
                                    .Where(fun x -> x <> acquiredCard.CardInstanceMeta.Id)
                                |> List.ofSeq)
                        |> List.unzip
                    for instanceId in instanceIds |> List.collect id |> List.distinct do
                        db.AcquiredCard
                            .Include(fun x -> x.CardInstance)
                            .SingleOrDefault(fun x -> x.CardInstanceId = instanceId && x.UserId = acquiredCard.UserId)
                        |> function
                        | null -> () // null when it's an instance that isn't acquired, veryLowTODO filter out the unacquired instances
                        | ac ->
                            ac.CardInstance <- command.CardView.CopyFieldsToNewInstance (Id ac.CardInstance.CardId) command.EditSummary communalFields
                    e.CardInstance <- command.CardView.CopyFieldsToNewInstance card command.EditSummary communalFields
                    Ok <| communalFields.ToList()
            do! db.SaveChangesAsyncI()
            return
                r |> Result.map (fun r ->
                    r.Where(fun x -> nonClozeCommunals.Select(fun x -> x.FieldName).Contains x.FieldName)
                        .Select(fun x -> x.FieldName, x.Id)
                        .Distinct()
                        .ToList()
                )
        }

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
          MatureCardsHardIntervalFactor = 1.2
          MatureCardsBuryRelated = true
          LapsedCardsSteps = [ TimeSpan.FromMinutes 10. ]
          LapsedCardsNewIntervalFactor = 0.
          LapsedCardsMinimumInterval = TimeSpan.FromDays 1.
          LapsedCardsLeechThreshold = byte 8
          ShowAnswerTimer = false
          AutomaticallyPlayAudio = false
          ReplayQuestionAudioOnAnswer = false }
    let defaultCardOptionsEntity =
        defaultCardOptions.CopyToNew
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
    let getAll (db: CardOverflowDb) userId = task {
            let! user = db.User.SingleAsync(fun x -> x.Id = userId)
            let! r = db.CardOption.Where(fun x -> x.UserId = userId).ToListAsync()
            return r |> Seq.map (fun o -> CardOption.load (o.Id = user.DefaultCardOptionId.Value) o)
        }

module UserRepository =
    let add (db: CardOverflowDb) name email = task {
        let cardOption = CardOptionsRepository.defaultCardOptions.CopyToNew 0
        let user =
            UserEntity(
                DisplayName = name,
                Email = email,
                CardOptions = [cardOption].ToList()
            )
        db.User.AddI user
        do! db.SaveChangesAsyncI ()
        user.DefaultCardOption <- cardOption
        return! db.SaveChangesAsyncI () }
    let Get (db: CardOverflowDb) email =
        db.User.FirstOrDefault(fun x -> x.Email = email)

module TagRepository =
    let tagEntities (db: CardOverflowDb) (newTags: string seq) =
        let newTags = newTags.Distinct().ToList() // https://stackoverflow.com/a/18113534
        db.Tag
            .Select(fun x -> x.Name)
            .Where(fun x -> newTags.Contains x)
            .ToList()
            .Contains >> not
        |> newTags.Where
        |> Seq.map (fun x -> TagEntity(Name = x))
    let Add (db: CardOverflowDb) userId newTags =
        tagEntities db newTags
        |> db.Tag.AddRange
        db.SaveChangesI ()

    let AddTo (db: CardOverflowDb) newTag acquiredCardId =
        defaultArg
            (db.Tag.SingleOrDefault(fun x -> x.Name = newTag) |> Option.ofObj)
            (TagEntity(Name = newTag))
        |> fun x -> Tag_AcquiredCardEntity(AcquiredCardId = acquiredCardId, Tag = x)
        |> db.Tag_AcquiredCard.AddI
        db.SaveChangesI ()
    
    let DeleteFrom (db: CardOverflowDb) tagName acquiredCardId = task {
        let! tag = db.Tag.SingleOrDefaultAsync(fun x -> x.Name = tagName)
        return!
            tag
            |> function
            | null -> Task.FromResult()
            | tag ->
                db.Tag_AcquiredCard.Single(fun x -> x.AcquiredCardId = acquiredCardId && x.TagId = tag.Id)
                |> db.Tag_AcquiredCard.RemoveI
                db.SaveChangesAsyncI()
        }

    let Search (db: CardOverflowDb) (input: string) =
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
    let Create (db: CardOverflowDb) userId name query =
        DeckEntity(
            UserId = userId,
            Name = name,
            Query = query
        ) |> db.Deck.AddI
        db.SaveChangesAsyncI ()

    let Get (db: CardOverflowDb) userId =
        db.Deck.Where(fun d -> d.UserId = userId).ToListAsync()
        
    let Update (db: CardOverflowDb) deck =
        db.Deck.UpdateI deck
        db.SaveChangesI ()

    let Delete (db: CardOverflowDb) deck =
        db.Deck.RemoveI deck
        db.SaveChangesAsyncI ()

namespace CardOverflow.Api

open System.Threading.Tasks
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
open NeoSmart.Utils
open System.IO
open System

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
    let addAndSaveAsync (db: CardOverflowDb) (j: Relationship_AcquiredCardEntity) =
        db.Relationship_AcquiredCard.AddI j
        db.SaveChangesAsyncI ()
    let removeAndSaveAsync (db: CardOverflowDb) sourceCardId targetCardId userId name =
        db.Relationship_AcquiredCard.SingleOrDefault(fun x ->
            x.SourceAcquiredCard.CardInstance.CardId = sourceCardId &&
            x.TargetAcquiredCard.CardInstance.CardId = targetCardId &&
            x.SourceAcquiredCard.UserId = userId &&
            x.TargetAcquiredCard.UserId = userId &&
            x.Relationship.Name = name
        ) |> function
        | null ->
            sprintf "Relationship not found between source Card #%i and target Card #%i with name \"%s\"." sourceCardId targetCardId name |> Error |> Task.FromResult
        | x ->
            db.Relationship_AcquiredCard.RemoveI x
            db.SaveChangesAsyncI ()
            |> Task.map(fun () -> Ok())

module CommentRepository =
    let addAndSaveAsync (db: CardOverflowDb) (comment: CommentCardEntity) =
        db.CommentCard.AddI comment
        db.SaveChangesAsyncI ()

module TemplateRepository =
    let latest (db: CardOverflowDb) templateId =
        db.LatestTemplateInstance
            .SingleOrDefaultAsync(fun x -> x.TemplateId = templateId)
        |> Task.map (Result.requireNotNull <| sprintf "Template #%i not found" templateId)
        |> TaskResult.map TemplateInstance.loadLatest
    let instance (db: CardOverflowDb) instanceId =
        db.TemplateInstance
            .SingleOrDefaultAsync(fun x -> x.Id = instanceId)
        |> Task.map (Result.requireNotNull <| sprintf "Template Instance #%i not found" instanceId)
        |> TaskResult.map TemplateInstance.load
    let UpdateFieldsToNewInstance (db: CardOverflowDb) userId (instance: TemplateInstance) = task {
        let template =
            if instance.TemplateId = 0 then
                Entity <| TemplateEntity(AuthorId = userId)
            else    
                Id <| instance.TemplateId
        let newTemplateInstance = instance.CopyToNewInstance template
        db.TemplateInstance.AddI newTemplateInstance
        db  
            .AcquiredCard
            .Include(fun x -> x.CardInstance)
            .Where(fun x -> x.CardInstance.TemplateInstanceId = instance.Id)
            |> Seq.iter(fun ac ->
                db.Entry(ac.CardInstance).State <- EntityState.Added
                ac.CardInstance.Id <- ac.CardInstance.GetHashCode()
                db.Entry(ac.CardInstance).Property(Core.nameof <@ any<CardInstanceEntity>.Id @>).IsTemporary <- true
                ac.CardInstance.Created <- DateTime.UtcNow
                ac.CardInstance.Modified <- Nullable()
                ac.CardInstance.TemplateInstance <- newTemplateInstance
            )
        let! existing = db.User_TemplateInstance.Where(fun x -> x.UserId = userId && x.TemplateInstance.TemplateId = newTemplateInstance.TemplateId).ToListAsync()
        db.User_TemplateInstance.RemoveRange existing
        User_TemplateInstanceEntity(UserId = userId, TemplateInstance = newTemplateInstance, DefaultCardSettingId = 1) // lowTODO do we ever use the card setting here?
        |> db.User_TemplateInstance.AddI
        return! db.SaveChangesAsyncI()
        }

module HistoryRepository =
    let getHeatmap (db: CardOverflowDb) userId = task {
        let oneYearishAgo = DateTime.UtcNow - TimeSpan.FromDays (53. * 7. - 1.) // always show full weeks of slightly more than a year; -1 is from allDateCounts being inclusive
        let! dateCounts =
            (query {
                for h in db.History do
                where (h.Timestamp >= oneYearishAgo && h.AcquiredCard.UserId = userId)
                groupValBy h h.Timestamp.Date into g
                select { Date = g.Key; Count = g.Count() }
            }).ToListAsync()
        return Heatmap.get oneYearishAgo DateTime.UtcNow (dateCounts |> List.ofSeq) }

module ExploreCardRepository =
    let get (db: CardOverflowDb) userId cardId = taskResult {
        let! r =
            db.LatestCardInstance
                .Include(fun x -> x.Card.Author)
                .Include(fun x -> x.Card.CommentCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentCardEntity) -> x.User)
                .Include(fun x -> x.CommunalFieldInstance_CardInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_CardInstanceEntity) -> x.CommunalFieldInstance)
                .Include(fun x -> x.TemplateInstance)
                .Where(fun x -> x.CardId = cardId)
                .Select(fun x ->
                    x,
                    x.CardInstance.AcquiredCards.Single(fun x -> x.UserId = userId).Tag_AcquiredCards.Select(fun x -> x.Tag.Name).ToList(),
                    x.CardInstance.AcquiredCards.Single(fun x -> x.UserId = userId).Relationship_AcquiredCardSourceAcquiredCards.Select(fun x -> x.Relationship.Name).ToList(),
                    x.CardInstance.AcquiredCards.Single(fun x -> x.UserId = userId).Relationship_AcquiredCardTargetAcquiredCards.Select(fun x -> x.Relationship.Name).ToList()
                ).SingleOrDefaultAsync()
        let! e, t, rs, rt = r |> Result.ofNullable (sprintf "Card #%i not found" cardId)
        let! tc = db.CardTagCount.Where(fun x -> x.CardId = cardId).ToListAsync()
        let! rc = db.CardRelationshipCount.Where(fun x -> x.CardId = cardId).ToListAsync()
        let! isAcquired = db.AcquiredCard.AnyAsync(fun x -> x.UserId = userId && x.CardInstance.CardId = cardId)
        return
            CardInstanceMeta.loadLatest isAcquired e (Set.ofSeq t) tc (Seq.append rs rt |> Set.ofSeq) rc
            |> ExploreCard.load e.Card
        }
    let instance (db: CardOverflowDb) userId instanceId = taskResult {
        let! (r: CardInstanceEntity * string ResizeArray * string ResizeArray * string ResizeArray) =
            db.CardInstance
                .Include(fun x -> x.Card.Author)
                .Include(fun x -> x.Card.CommentCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentCardEntity) -> x.User)
                .Include(fun x -> x.CommunalFieldInstance_CardInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_CardInstanceEntity) -> x.CommunalFieldInstance)
                .Include(fun x -> x.TemplateInstance)
                .Where(fun x -> x.Id = instanceId)
                .Select(fun x ->
                    x,
                    x.AcquiredCards.Single(fun x -> x.UserId = userId).Tag_AcquiredCards.Select(fun x -> x.Tag.Name).ToList(),
                    x.AcquiredCards.Single(fun x -> x.UserId = userId).Relationship_AcquiredCardSourceAcquiredCards.Select(fun x -> x.Relationship.Name).ToList(),
                    x.AcquiredCards.Single(fun x -> x.UserId = userId).Relationship_AcquiredCardTargetAcquiredCards.Select(fun x -> x.Relationship.Name).ToList()
                ).SingleOrDefaultAsync()
        let! e, t, rs, rt = r |> Result.ofNullable (sprintf "Card Instance #%i not found" instanceId)
        let! tc = db.CardTagCount.Where(fun x -> x.CardId = e.CardId).ToListAsync()
        let! rc = db.CardRelationshipCount.Where(fun x -> x.CardId = e.CardId).ToListAsync()
        let! isAcquired = db.AcquiredCard.AnyAsync(fun x -> x.UserId = userId && x.CardInstance.CardId = e.CardId)
        let! isLatest = db.LatestCardInstance.AnyAsync(fun x -> x.CardInstanceId = instanceId)
        return
            CardInstanceMeta.load isAcquired isLatest e (Set.ofSeq t) tc (Seq.append rs rt |> Set.ofSeq) rc
            |> ExploreCard.load e.Card
        }

module FileRepository =
    let get (db: CardOverflowDb) hash =
        let sha256 = UrlBase64.Decode hash
        db.File.SingleOrDefaultAsync(fun x -> x.Sha256 = sha256)
        |> Task.map (Result.requireNotNull ())
        |> TaskResult.map (fun x -> x.Data)

module CardViewRepository =
    let private getAcquiredInstanceIds (db: CardOverflowDb) userId aId bId =
        if userId = 0 then
            [].ToListAsync()
        else 
            db.AcquiredCard
                .Where(fun x -> x.UserId = userId)
                .Where(fun x -> x.CardInstanceId = aId || x.CardInstanceId = bId)
                .Select(fun x -> x.CardInstanceId)
                .ToListAsync()
    let instanceWithLatest (db: CardOverflowDb) aId userId = taskResult {
        let! (a: CardInstanceEntity) =
            db.CardInstance
                .Include(fun x -> x.TemplateInstance)
                .SingleOrDefaultAsync(fun x -> x.Id = aId)
            |> Task.map (Result.requireNotNull (sprintf "Card instance #%i not found" aId))
        let! (b: LatestCardInstanceEntity) = // verylowTODO optimization try to get this from `a` above
            db.LatestCardInstance
                .Include(fun x -> x.TemplateInstance)
                .SingleAsync(fun x -> x.CardId = a.CardId)
        let! (acquiredInstanceIds: int ResizeArray) = getAcquiredInstanceIds db userId aId b.CardInstanceId
        return
            CardInstanceView.load a,
            acquiredInstanceIds.Contains a.Id,
            CardInstanceView.loadLatest b,
            acquiredInstanceIds.Contains b.CardInstanceId,
            b.CardInstanceId
    }
    let instancePair (db: CardOverflowDb) aId bId userId = taskResult {
        let! (instances: CardInstanceEntity ResizeArray) =
            db.CardInstance
                .Include(fun x -> x.TemplateInstance)
                .Where(fun x -> x.Id = aId || x.Id = bId)
                .ToListAsync()
        let! a = Result.requireNotNull (sprintf "Card instance #%i not found" aId) <| instances.SingleOrDefault(fun x -> x.Id = aId)
        let! b = Result.requireNotNull (sprintf "Card instance #%i not found" bId) <| instances.SingleOrDefault(fun x -> x.Id = bId)
        let! (acquiredInstanceIds: int ResizeArray) = getAcquiredInstanceIds db userId aId bId
        return
            CardInstanceView.load a,
            acquiredInstanceIds.Contains a.Id,
            CardInstanceView.load b,
            acquiredInstanceIds.Contains b.Id
    }
    let instance (db: CardOverflowDb) instanceId = task {
        match!
            db.CardInstance
            .Include(fun x -> x.TemplateInstance)
            .SingleOrDefaultAsync(fun x -> x.Id = instanceId) with
        | null -> return Error <| sprintf "Card instance %i not found" instanceId
        | x -> return Ok <| CardInstanceView.load x
    }
    let get (db: CardOverflowDb) cardId =
        db.LatestCardInstance
            .Include(fun x -> x.TemplateInstance)
            .SingleOrDefaultAsync(fun x -> x.CardId = cardId)
        |> Task.map Ok
        |> TaskResult.bind (fun x -> Result.requireNotNull (sprintf "Card #%i not found" cardId) x |> Task.FromResult)
        |> TaskResult.map CardInstanceView.loadLatest

module AcquiredCardRepository =
    let getAcquired (db: CardOverflowDb) userId (testCardInstanceIds: int ResizeArray) =
        db.AcquiredCard.Where(fun x -> testCardInstanceIds.Contains(x.CardInstanceId) && x.UserId = userId).Select(fun x -> x.CardInstanceId).ToListAsync()
    let getAcquiredInstanceFromInstance (db: CardOverflowDb) userId (cardInstanceId: int) =
        db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.UserId = userId && x.CardInstance.Card.CardInstances.Any(fun x -> x.Id = cardInstanceId))
        |> Task.map (Result.requireNotNull (sprintf "You don't have any cards with the id #%i" cardInstanceId))
        |> TaskResult.map (fun x -> x.CardInstanceId)

module CardRepository =
    let deleteAcquired (db: CardOverflowDb) userId acquiredCardId = taskResult {
            do! db.Relationship_AcquiredCard
                    .Where(fun x -> x.SourceAcquiredCardId = acquiredCardId || x.TargetAcquiredCardId = acquiredCardId)
                    .ToListAsync()
                |> Task.map db.Relationship_AcquiredCard.RemoveRange
            do! db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.Id = acquiredCardId && x.UserId = userId)
                |> Task.map (Result.ofNullable "You don't own that card.")
                |> TaskResult.map db.AcquiredCard.RemoveI
            return! db.SaveChangesAsyncI()
        }
    let editState (db: CardOverflowDb) userId acquiredCardId (state: CardState) = taskResult {
            let! (ac: AcquiredCardEntity) =
                db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.Id = acquiredCardId && x.UserId = userId)
                |> Task.map (Result.ofNullable "You don't own that card.")
            ac.CardState <- CardState.toDb state
            return! db.SaveChangesAsyncI()
        }
    let Revisions (db: CardOverflowDb) userId cardId = task {
        let! r =
            db.Card
                .Include(fun x -> x.Author)
                .Include(fun x -> x.CardInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CardInstanceEntity) -> x.TemplateInstance)
                .SingleAsync(fun x -> x.Id = cardId)
        let! isAcquired = db.AcquiredCard.AnyAsync(fun x -> x.UserId = userId && x.CardInstance.CardId = cardId)
        return CardRevision.load isAcquired r
    }
    let AcquireCardAsync (db: CardOverflowDb) userId cardInstanceId = task {
        let! user = db.User.SingleAsync(fun x -> x.Id = userId)
        let! cardInstance = db.CardInstance.SingleAsync(fun x -> x.Id = cardInstanceId)
        match! db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.UserId = userId && x.CardInstance.CardId = cardInstance.CardId) with
        | null ->
            let card =
                AcquiredCard.initialize
                    userId
                    user.DefaultCardSettingId.Value // medTODO handle the null case
                    []
                |> fun x -> x.copyToNew [] // medTODO get tags from template
            card.CardInstanceId <- cardInstanceId
            card.CardId <- cardInstance.CardId
            card |> db.AcquiredCard.AddI
        | card ->
            card.CardInstanceId <- cardInstanceId
            card.CardId <- cardInstance.CardId
        return! db.SaveChangesAsyncI ()
        }
    let UnacquireCardAsync (db: CardOverflowDb) acquiredCardId =
        db.AcquiredCard.Single(fun x -> x.Id = acquiredCardId)
        |> db.AcquiredCard.RemoveI
        db.SaveChangesAsyncI ()
    let GetAcquired (db: CardOverflowDb) (userId: int) (cardId: int) = task {
        let! tc = db.CardTagCount.Where(fun x -> x.CardId = cardId).ToListAsync()
        let! rc = db.CardRelationshipCount.Where(fun x -> x.CardId = cardId).ToListAsync()
        let! r =
            db.AcquiredCardIsLatest
                .Include(fun x -> x.CardInstance.TemplateInstance)
                .Include(fun x -> x.CardInstance.CommunalFieldInstance_CardInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_CardInstanceEntity) -> x.CommunalFieldInstance)
                .Include(fun x -> x.CardInstance.AcquiredCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: AcquiredCardEntity) -> x.Tag_AcquiredCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
                .Where(fun x -> x.CardInstance.CardId = cardId && x.UserId = userId)
                .Select(fun x ->
                    x,
                    x.CardInstance.AcquiredCards.Single(fun x -> x.UserId = userId).Tag_AcquiredCards.Select(fun x -> x.Tag.Name).ToList(),
                    x.CardInstance.AcquiredCards.Single(fun x -> x.UserId = userId).Relationship_AcquiredCardSourceAcquiredCards.Select(fun x -> x.Relationship.Name),
                    x.CardInstance.AcquiredCards.Single(fun x -> x.UserId = userId).Relationship_AcquiredCardTargetAcquiredCards.Select(fun x -> x.Relationship.Name)
                ).SingleOrDefaultAsync()
        return
            match r |> Core.toOption with
            | None -> Error (sprintf "Card #%i not found for User #%i" cardId userId)
            | Some (e, t, rs, rt) -> AcquiredCard.load (Set.ofSeq t) tc (Seq.append rs rt |> Set.ofSeq) rc e true
        }
    let getNew (db: CardOverflowDb) userId = task {
        let! user = db.User.SingleAsync(fun x -> x.Id = userId)
        return AcquiredCard.initialize userId user.DefaultCardSettingId.Value [] // lowTODO handle the null
        }
    let private searchAcquired (db: CardOverflowDb) userId (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.AcquiredCard
            .Include(fun x -> x.CardInstance.TemplateInstance)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                String.IsNullOrWhiteSpace searchTerm ||
                x.Tag_AcquiredCards.Any(fun x ->
                    x.Tag.TsVector.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))) ||
                x.CardInstance.TsVector.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
            )
    let private searchAcquiredIsLatest (db: CardOverflowDb) userId (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.AcquiredCardIsLatest
            .Include(fun x -> x.CardInstance.TemplateInstance)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                String.IsNullOrWhiteSpace searchTerm ||
                x.Tag_AcquiredCards.Any(fun x ->
                    x.Tag.TsVector.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))) ||
                x.CardInstance.TsVector.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
            )
    let GetAcquiredPages (db: CardOverflowDb) (userId: int) (pageNumber: int) (searchTerm: string) =
        task {
            let! r =
                (searchAcquiredIsLatest db userId searchTerm)
                    .Include(fun x -> x.Tag_AcquiredCards :> IEnumerable<_>)
                        .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
                    .ToPagedListAsync(pageNumber, 15)
            return {
                Results = r |> Seq.map (fun x -> AcquiredCard.load (x.Tag_AcquiredCards.Select(fun x -> x.Tag.Name) |> Set.ofSeq) ResizeArray.empty Set.empty ResizeArray.empty x true)
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
                    .Include(fun x -> x.CardSetting)
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
    let SearchAsync (db: CardOverflowDb) userId (pageNumber: int) order (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        task {
            let! r =
                db.SearchLatestCardInstance(searchTerm, plain, wildcard, order).Select(fun x ->
                    x,
                    x.CardInstance.AcquiredCards.Any(fun x -> x.UserId = userId),
                    x.TemplateInstance, // .Include fails for some reason, so we have to manually select
                    x.Author
                ).ToPagedListAsync(pageNumber, 15)
            let squashed =
                r |> List.ofSeq |> List.map (fun (c, isAcquired, template, author) ->
                    c.Author <- author
                    c.TemplateInstance <- template
                    c, isAcquired
                )
            return {
                Results =
                    squashed |> List.map (fun (c, isAcquired) ->
                        {   Id = c.CardId
                            Author = c.Author.DisplayName
                            AuthorId = c.AuthorId
                            Users = c.CardUsers
                            Instance = CardInstanceMeta.loadLatest isAcquired c Set.empty ResizeArray.empty Set.empty ResizeArray.empty
                        }
                    )
                Details = {
                    CurrentPage = r.PageNumber
                    PageCount = r.PageCount
                }
            }
        }

module UpdateRepository =
    let card (db: CardOverflowDb) (acquiredCard: AcquiredCard) (command: EditCardCommand) =
        taskResult {
            let! card =
                if acquiredCard.CardId = 0 then taskResult {
                    do! match command.Source with
                        | CopySourceInstanceId instanceId -> task {
                            let! userDoesntOwnInstance = db.CardInstance.AnyAsync(fun x -> x.Id = instanceId && x.Card.AuthorId <> acquiredCard.UserId)
                            return
                                if userDoesntOwnInstance then Ok ()
                                else Error "You can't copy your own cards. Yet. Contact us if you really want this feature."
                            }
                        | BranchSourceCardId
                        | Original -> Ok () |> Task.FromResult
                    return Entity <| fun () ->
                        match command.Source with
                        | CopySourceInstanceId instanceId ->
                            CardEntity(AuthorId = acquiredCard.UserId, CopySourceId = Nullable instanceId)
                        | BranchSourceCardId cardId ->
                            CardEntity(AuthorId = acquiredCard.UserId, BranchSourceId = Nullable cardId)
                        | Original ->
                            CardEntity(AuthorId = acquiredCard.UserId)
                    }
                else
                    Id acquiredCard.CardId
                    |> Ok
                    |> Task.FromResult
            let! (acquiredCardEntity: AcquiredCardEntity) =
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
            let! (acs: AcquiredCardEntity list) =
                match acquiredCardEntity with
                | null -> task {
                    let getsertTagId (db: CardOverflowDb) (input: string) =
                        match db.Tag.SingleOrDefault(fun x -> x.Name = input) with
                        | null ->
                            let e = TagEntity(Name = input)
                            db.Tag.AddI e
                            e
                        | x -> x
                    let tags = acquiredCard.Tags |> List.map (getsertTagId db) // lowTODO could optimize. This is single threaded cause async saving causes issues, so try batch saving
                    do! db.SaveChangesAsyncI()
                    let tagIds = tags.Select(fun x -> x.Id)
                    return
                        if command.TemplateInstance.IsCloze then
                            let valueByFieldName = command.FieldValues.Select(fun x -> x.EditField.Name, x.Value) |> Map.ofSeq
                            AnkiImportLogic.maxClozeIndex "Something's wrong with your cloze indexes." valueByFieldName command.TemplateInstance.QuestionTemplate
                            |> Result.map (fun max ->
                            [1s .. int16 max] |> List.map (fun clozeIndex ->
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
                            let e = acquiredCard.copyToNew tagIds
                            e.CardInstance <- c.CardView.CopyFieldsToNewInstance card c.EditSummary communalInstances
                            match card with
                            | Id x ->
                                e.CardId <- x
                            | Entity _ ->
                                e.Card <- e.CardInstance.Card
                            db.AcquiredCard.AddI e
                            e))
                    }
                | e ->
                    task {
                        let getsertTagId (db: CardOverflowDb) (input: string) =
                            match db.Tag.SingleOrDefault(fun x -> x.Name = input) with
                            | null ->
                                let e = TagEntity(Name = input)
                                db.Tag.AddI e
                                e
                            | x -> x
                        let tags = acquiredCard.Tags |> List.map (getsertTagId db) // lowTODO could optimize. This is single threaded cause async saving causes issues, so try batch saving
                        do! db.SaveChangesAsyncI()
                        let tagIds = tags.Select(fun x -> x.Id)
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
                        return
                            if command.TemplateInstance.IsCloze then
                                let valueByFieldName = command.FieldValues.Select(fun x -> x.EditField.Name, x.Value) |> Map.ofSeq
                                AnkiImportLogic.maxClozeIndex "Something's wrong with your cloze indexes." valueByFieldName command.TemplateInstance.QuestionTemplate
                                |> Result.map (fun max ->
                                [1s .. int16 max] |> List.map (fun clozeIndex ->
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
                            |> Result.map (List.collect (fun c ->
                                let associatedEntities =
                                    instanceIds |> List.collect id |> List.distinct |> List.map (fun instanceId ->
                                        db.AcquiredCard
                                            .Include(fun x -> x.CardInstance)
                                            .SingleOrDefault(fun x -> x.CardInstanceId = instanceId && x.UserId = acquiredCard.UserId)
                                        |> function
                                        | null -> None // null when it's an instance that isn't acquired, veryLowTODO filter out the unacquired instances
                                        | ac ->
                                            ac.CardInstance <- c.CardView.CopyFieldsToNewInstance (Id ac.CardInstance.CardId) command.EditSummary communalFields
                                            Some ac
                                    ) |> List.choose id
                                let e =
                                    if command.TemplateInstance.IsCloze then
                                        let entityIndex = ClozeRegex().TypedMatches(e.CardInstance.FieldValues).Single().clozeIndex.Value
                                        let commandIndex = ClozeRegex().TypedMatches(c.ClozeFieldValues.Select(fun x -> x.Value) |> String.concat " ").Single().clozeIndex.Value
                                        if entityIndex = commandIndex then
                                            e.CardInstance <- c.CardView.CopyFieldsToNewInstance card c.EditSummary communalFields
                                            e
                                        else
                                            if associatedEntities.Select(fun x -> ClozeRegex().TypedMatches(x.CardInstance.FieldValues).Single().clozeIndex.Value).Contains commandIndex then
                                                e
                                            else
                                                let card =
                                                    match command.Source with
                                                    | CopySourceInstanceId instanceId ->
                                                        CardEntity(AuthorId = acquiredCard.UserId, CopySourceId = Nullable instanceId)
                                                    | BranchSourceCardId cardId ->
                                                        CardEntity(AuthorId = acquiredCard.UserId, BranchSourceId = Nullable cardId)
                                                    | Original ->
                                                        CardEntity(AuthorId = acquiredCard.UserId)
                                                    |> fun x -> fun () -> x
                                                    |> Entity
                                                let ciEntity = c.CardView.CopyFieldsToNewInstance card c.EditSummary communalFields
                                                let ac = acquiredCard.copyToNew tagIds
                                                ac.Card <- ciEntity.Card
                                                ac.CardInstance <- ciEntity
                                                db.AcquiredCard.AddI ac
                                                ac
                                    else
                                        e.CardInstance <- c.CardView.CopyFieldsToNewInstance card c.EditSummary communalFields
                                        e
                                e :: associatedEntities
                            ))
                        }
            do! db.SaveChangesAsyncI()
            return
                acs.Select(fun x -> x.CardInstanceId).Distinct().ToList(),
                acs.SelectMany(fun x -> x.CardInstance.CommunalFieldInstance_CardInstances.Select(fun x -> x.CommunalFieldInstance))
                    .Where(fun x -> nonClozeCommunals.Select(fun x -> x.FieldName).Contains x.FieldName)
                    .Select(fun x -> x.FieldName, x.Id)
                    .Distinct()
                    .ToList()
        }

module CardSettingsRepository =
    let defaultCardSettings =
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
          LapsedCardsLeechThreshold = int16 8
          ShowAnswerTimer = false
          AutomaticallyPlayAudio = false
          ReplayQuestionAudioOnAnswer = false }
    let defaultCardSettingsEntity =
        defaultCardSettings.CopyToNew
    //let defaultAnkiCardSettings =
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
            let! r = db.CardSetting.Where(fun x -> x.UserId = userId).ToListAsync()
            return r |> Seq.map (fun o -> CardSetting.load (o.Id = user.DefaultCardSettingId.Value) o)
        }

module UserRepository =
    let add (db: CardOverflowDb) id name = task {
        let cardSetting = CardSettingsRepository.defaultCardSettings.CopyToNew 0
        let user =
            UserEntity(
                Id = id,
                DisplayName = name,
                CardSettings = [cardSetting].ToList()
            )
        db.User.AddI user
        do! db.SaveChangesAsyncI ()
        user.DefaultCardSetting <- cardSetting
        return! db.SaveChangesAsyncI () }
    let Get (db: CardOverflowDb) id =
        db.User.SingleAsync(fun x -> x.Id = id)

module TagRepository =
    let AddTo (db: CardOverflowDb) newTag acquiredCardId = task {
        defaultArg
            (db.Tag.SingleOrDefault(fun x -> x.Name = newTag) |> Option.ofObj)
            (TagEntity(Name = newTag))
        |> fun x -> Tag_AcquiredCardEntity(AcquiredCardId = acquiredCardId, Tag = x)
        |> db.Tag_AcquiredCard.AddI
        return! db.SaveChangesAsyncI ()
        }
    
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
        db.Tag.Where(fun t -> EF.Functions.ILike(t.Name, "%" + input + "%")).ToList()

module FilterRepository =
    let Create (db: CardOverflowDb) userId name query =
        FilterEntity(
            UserId = userId,
            Name = name,
            Query = query
        ) |> db.Filter.AddI
        db.SaveChangesAsyncI ()

    let Get (db: CardOverflowDb) userId =
        db.Filter.Where(fun d -> d.UserId = userId).ToListAsync()
        
    let Delete (db: CardOverflowDb) deck =
        db.Filter.RemoveI deck
        db.SaveChangesAsyncI ()

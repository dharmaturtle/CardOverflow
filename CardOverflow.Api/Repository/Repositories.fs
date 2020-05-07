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
        return x |> Seq.map CommunalFieldInstance.load |> toResizeArray
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
            x.SourceAcquiredCard.BranchInstance.CardId = sourceCardId &&
            x.TargetAcquiredCard.BranchInstance.CardId = targetCardId &&
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
        |> TaskResult.map TemplateInstance.load
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
            .Include(fun x -> x.BranchInstance)
            .Where(fun x -> x.BranchInstance.TemplateInstanceId = instance.Id)
            |> Seq.iter(fun ac ->
                db.Entry(ac.BranchInstance).State <- EntityState.Added
                ac.BranchInstance.Id <- ac.BranchInstance.GetHashCode()
                db.Entry(ac.BranchInstance).Property(Core.nameof <@ any<BranchInstanceEntity>.Id @>).IsTemporary <- true
                ac.BranchInstance.Created <- DateTime.UtcNow
                ac.BranchInstance.Modified <- Nullable()
                ac.BranchInstance.TemplateInstance <- newTemplateInstance
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
    let getAcquiredStatus (db: CardOverflowDb) userId (rootInstance: BranchInstanceEntity) = task {
        let! ac =
            db.AcquiredCard
                .Include(fun x -> x.Card.BranchChildren :> IEnumerable<_>)
                    .ThenInclude(fun (x: CardEntity) -> x.LatestInstance)
                .SingleOrDefaultAsync(fun x -> x.UserId = userId && x.BranchSourceIdOrCardId = rootInstance.CardId)
                |> Task.map Option.ofObj
        return
            match ac with
            | None -> NotAcquired
            | Some ac ->
                if   ac.BranchInstanceId = rootInstance.Id then
                    ExactInstanceAcquired ac.BranchInstanceId
                elif ac.CardId = rootInstance.CardId then
                    OtherInstanceAcquired ac.BranchInstanceId
                elif rootInstance.Card.BranchChildren.Select(fun x -> x.LatestInstanceId).Contains ac.BranchInstanceId then
                    LatestBranchAcquired ac.BranchInstanceId
                elif rootInstance.Card.BranchChildren.Select(fun x -> x.Id).Contains ac.CardId then
                    OtherBranchAcquired ac.BranchInstanceId
                else failwith "impossible"
    }
    let get (db: CardOverflowDb) userId cardId = taskResult {
        let! rootCardId =
            db.Card.SingleOrDefaultAsync(fun x -> x.Id = cardId)
            |> Task.map (Result.requireNotNull <| sprintf "Card #%i not found" cardId)
            |> TaskResult.map (fun x -> x.BranchSourceId |> Option.ofNullable |> Option.defaultValue x.Id)
        let! (r: BranchInstanceEntity * List<string> * List<string> * List<string>) =
            db.LatestBranchInstance
                .Include(fun x -> x.Card.Author)
                .Include(fun x -> x.Card.BranchChildren :> IEnumerable<_>)
                    .ThenInclude(fun (x: CardEntity) -> x.LatestInstance.TemplateInstance)
                .Include(fun x -> x.Card.BranchChildren :> IEnumerable<_>)
                    .ThenInclude(fun (x: CardEntity) -> x.Author)
                .Include(fun x -> x.Card.CommentCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentCardEntity) -> x.User)
                .Include(fun x -> x.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance)
                .Include(fun x -> x.TemplateInstance)
                .Where(fun x -> x.CardId = rootCardId)
                .Select(fun x ->
                    x,
                    x.AcquiredCards.Single(fun x -> x.UserId = userId).Tag_AcquiredCards.Select(fun x -> x.Tag.Name).ToList(),
                    x.AcquiredCards.Single(fun x -> x.UserId = userId).Relationship_AcquiredCardSourceAcquiredCards.Select(fun x -> x.Relationship.Name).ToList(),
                    x.AcquiredCards.Single(fun x -> x.UserId = userId).Relationship_AcquiredCardTargetAcquiredCards.Select(fun x -> x.Relationship.Name).ToList()
                ).SingleOrDefaultAsync()
        let! rootInstance, t, rs, rt = r |> Result.ofNullable (sprintf "Card #%i not found" rootCardId)
        let! (tc: List<CardTagCountEntity>) = db.CardTagCount.Where(fun x -> x.CardId = rootCardId).ToListAsync()
        let! (rc: List<CardRelationshipCountEntity>) = db.CardRelationshipCount.Where(fun x -> x.CardId = rootCardId).ToListAsync()
        let! acquiredStatus = getAcquiredStatus db userId rootInstance
        return
            BranchInstanceMeta.load (acquiredStatus = ExactInstanceAcquired rootInstance.Id ) true rootInstance
            |> ExploreCard.load rootInstance.Card acquiredStatus (Set.ofSeq t) tc (Seq.append rs rt |> Set.ofSeq) rc
        }
    let instance (db: CardOverflowDb) userId instanceId = taskResult {
        let! (e: BranchInstanceEntity) =
            db.BranchInstance
                .Include(fun x -> x.Card.Author)
                .Include(fun x -> x.Card.CommentCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentCardEntity) -> x.User)
                .Include(fun x -> x.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance)
                .Include(fun x -> x.TemplateInstance)
                .SingleOrDefaultAsync(fun x -> x.Id = instanceId)
            |> Task.map (Result.requireNotNull (sprintf "Card Instance #%i not found" instanceId))
        let! isAcquired = db.AcquiredCard.AnyAsync(fun x -> x.UserId = userId && x.BranchInstanceId = instanceId)
        let! latest = get db userId e.CardId
        return BranchInstanceMeta.load isAcquired (e.Card.LatestInstanceId = e.Id) e, latest // lowTODO optimization, only send the old instance - the latest instance isn't used
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
                .Where(fun x -> x.BranchInstanceId = aId || x.BranchInstanceId = bId)
                .Select(fun x -> x.BranchInstanceId)
                .ToListAsync()
    let instanceWithLatest (db: CardOverflowDb) aId userId = taskResult {
        let! (a: BranchInstanceEntity) =
            db.BranchInstance
                .Include(fun x -> x.TemplateInstance)
                .SingleOrDefaultAsync(fun x -> x.Id = aId)
            |> Task.map (Result.requireNotNull (sprintf "Card instance #%i not found" aId))
        let! (b: BranchInstanceEntity) = // verylowTODO optimization try to get this from `a` above
            db.LatestBranchInstance
                .Include(fun x -> x.TemplateInstance)
                .SingleAsync(fun x -> x.CardId = a.CardId)
        let! (acquiredInstanceIds: int ResizeArray) = getAcquiredInstanceIds db userId aId b.Id
        return
            BranchInstanceView.load a,
            acquiredInstanceIds.Contains a.Id,
            BranchInstanceView.load b,
            acquiredInstanceIds.Contains b.Id,
            b.Id
    }
    let instancePair (db: CardOverflowDb) aId bId userId = taskResult {
        let! (instances: BranchInstanceEntity ResizeArray) =
            db.BranchInstance
                .Include(fun x -> x.TemplateInstance)
                .Where(fun x -> x.Id = aId || x.Id = bId)
                .ToListAsync()
        let! a = Result.requireNotNull (sprintf "Card instance #%i not found" aId) <| instances.SingleOrDefault(fun x -> x.Id = aId)
        let! b = Result.requireNotNull (sprintf "Card instance #%i not found" bId) <| instances.SingleOrDefault(fun x -> x.Id = bId)
        let! (acquiredInstanceIds: int ResizeArray) = getAcquiredInstanceIds db userId aId bId
        return
            BranchInstanceView.load a,
            acquiredInstanceIds.Contains a.Id,
            BranchInstanceView.load b,
            acquiredInstanceIds.Contains b.Id
    }
    let instance (db: CardOverflowDb) instanceId = task {
        match!
            db.BranchInstance
            .Include(fun x -> x.TemplateInstance)
            .SingleOrDefaultAsync(fun x -> x.Id = instanceId) with
        | null -> return Error <| sprintf "Card instance %i not found" instanceId
        | x -> return Ok <| BranchInstanceView.load x
    }
    let get (db: CardOverflowDb) cardId =
        db.LatestBranchInstance
            .Include(fun x -> x.TemplateInstance)
            .SingleOrDefaultAsync(fun x -> x.CardId = cardId)
        |> Task.map Ok
        |> TaskResult.bind (fun x -> Result.requireNotNull (sprintf "Card #%i not found" cardId) x |> Task.FromResult)
        |> TaskResult.map BranchInstanceView.load

module AcquiredCardRepository =
    let getAcquired (db: CardOverflowDb) userId (testBranchInstanceIds: int ResizeArray) =
        db.AcquiredCard.Where(fun x -> testBranchInstanceIds.Contains(x.BranchInstanceId) && x.UserId = userId).Select(fun x -> x.BranchInstanceId).ToListAsync()
    let getAcquiredInstanceFromInstance (db: CardOverflowDb) userId (cardInstanceId: int) =
        db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.UserId = userId && x.BranchInstance.Card.BranchInstances.Any(fun x -> x.Id = cardInstanceId))
        |> Task.map (Result.requireNotNull (sprintf "You don't have any cards with the id #%i" cardInstanceId))
        |> TaskResult.map (fun x -> x.BranchInstanceId)

module CardRepository =
    let deleteAcquired (db: CardOverflowDb) userId acquiredCardId = taskResult {
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
                .Include(fun x -> x.BranchInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: BranchInstanceEntity) -> x.TemplateInstance)
                .SingleAsync(fun x -> x.Id = cardId)
        let! isAcquired = db.AcquiredCard.AnyAsync(fun x -> x.UserId = userId && x.BranchInstance.CardId = cardId)
        return CardRevision.load isAcquired r
    }
    let AcquireCardAsync (db: CardOverflowDb) userId cardInstanceId = taskResult {
        let! (defaultCardSettingId: Nullable<int>) = db.User.Where(fun x -> x.Id = userId).Select(fun x -> x.DefaultCardSettingId).SingleAsync()
        let! (cardInstance: BranchInstanceEntity) =
            db.BranchInstance
                .Include(fun x -> x.Card)
                .SingleOrDefaultAsync(fun x -> x.Id = cardInstanceId)
            |> Task.map (Result.requireNotNull <| sprintf "Card not found for Instance #%i" cardInstanceId)
        let! (ac: AcquiredCardEntity) =
            db.AcquiredCard.SingleOrDefaultAsync(fun x ->
                x.UserId = userId && 
                (x.BranchSourceIdOrCardId = cardInstance.CardId || Nullable x.BranchSourceIdOrCardId = cardInstance.Card.BranchSourceId)
            )
        match ac with
        | null ->
            let card =
                AcquiredCard.initialize
                    userId
                    defaultCardSettingId.Value // medTODO handle the null case
                    []
                |> fun x -> x.copyToNew [] // medTODO get tags from template
            card.BranchInstanceId <- cardInstanceId
            card.CardId <- cardInstance.CardId
            card |> db.AcquiredCard.AddI
        | card ->
            card.BranchInstanceId <- cardInstanceId
            card.CardId <- cardInstance.CardId
        return! db.SaveChangesAsyncI ()
        }
    let UnacquireCardAsync (db: CardOverflowDb) acquiredCardId = // medTODO needs userId for validation
        db.AcquiredCard.Single(fun x -> x.Id = acquiredCardId)
        |> db.AcquiredCard.RemoveI
        db.SaveChangesAsyncI ()
    let GetAcquired (db: CardOverflowDb) (userId: int) (cardId: int) = taskResult {
        let! e, t =
            db.AcquiredCardIsLatest
                .Include(fun x -> x.BranchInstance.TemplateInstance)
                .Include(fun x -> x.BranchInstance.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance)
                .Include(fun x -> x.BranchInstance.AcquiredCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: AcquiredCardEntity) -> x.Tag_AcquiredCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
                .Where(fun x -> (x.CardId = cardId || x.BranchSourceIdOrCardId = cardId) && x.UserId = userId) // add CardId
                .Select(fun x ->
                    x,
                    x.BranchInstance.AcquiredCards.Single(fun x -> x.UserId = userId).Tag_AcquiredCards.Select(fun x -> x.Tag.Name).ToList()
                ).SingleOrDefaultAsync()
            |> Task.map Core.toOption
            |> Task.map (Result.requireSome (sprintf "Card #%i not found for User #%i" cardId userId))
        return! AcquiredCard.load (Set.ofSeq t) e true
        }
    let getNew (db: CardOverflowDb) userId = task {
        let! user = db.User.SingleAsync(fun x -> x.Id = userId)
        return AcquiredCard.initialize userId user.DefaultCardSettingId.Value [] // lowTODO handle the null
        }
    let private searchAcquired (db: CardOverflowDb) userId (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.AcquiredCard
            .Include(fun x -> x.BranchInstance.TemplateInstance)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                String.IsNullOrWhiteSpace searchTerm ||
                x.Tag_AcquiredCards.Any(fun x ->
                    x.Tag.TsVector.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))) ||
                x.BranchInstance.TsVector.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
            )
    let private searchAcquiredIsLatest (db: CardOverflowDb) userId (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.AcquiredCardIsLatest
            .Include(fun x -> x.BranchInstance.TemplateInstance)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                String.IsNullOrWhiteSpace searchTerm ||
                x.Tag_AcquiredCards.Any(fun x ->
                    x.Tag.TsVector.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))) ||
                x.BranchInstance.TsVector.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
            )
    let GetAcquiredPages (db: CardOverflowDb) (userId: int) (pageNumber: int) (searchTerm: string) =
        task {
            let! r =
                (searchAcquiredIsLatest db userId searchTerm)
                    .Include(fun x -> x.Tag_AcquiredCards :> IEnumerable<_>)
                        .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
                    .ToPagedListAsync(pageNumber, 15)
            return {
                Results = r |> Seq.map (fun x -> AcquiredCard.load (x.Tag_AcquiredCards.Select(fun x -> x.Tag.Name) |> Set.ofSeq) x true)
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
                db.SearchLatestBranchInstance(searchTerm, plain, wildcard, order).Select(fun x ->
                    x,
                    x.AcquiredCards.Any(fun x -> x.UserId = userId),
                    x.TemplateInstance, // .Include fails for some reason, so we have to manually select
                    x.Card,
                    x.Card.Author
                ).ToPagedListAsync(pageNumber, 15)
            let squashed =
                r |> List.ofSeq |> List.map (fun (c, isAcquired, template, card, author) ->
                    c.Card <- card
                    c.Card.Author <- author
                    c.TemplateInstance <- template
                    c, isAcquired
                )
            return {
                Results =
                    squashed |> List.map (fun (c, isAcquired) ->
                        {   Id = c.CardId
                            Author = c.Card.Author.DisplayName
                            AuthorId = c.Card.AuthorId
                            Users = c.Card.Users
                            Instance = BranchInstanceMeta.load isAcquired true c
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
        let newCardEntity =
            Entity <| fun () ->
                match command.Source with
                | CopySourceInstanceId instanceId ->
                    CardEntity(AuthorId = acquiredCard.UserId, CopySourceId = Nullable instanceId)
                | BranchSourceCardId cardId ->
                    CardEntity(AuthorId = acquiredCard.UserId, BranchSourceId = Nullable cardId)
                | Original ->
                    CardEntity(AuthorId = acquiredCard.UserId)
        let card =
            if acquiredCard.CardId = 0 then
                newCardEntity
            else
                Id acquiredCard.CardId
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
        let updateCommunalField (old: CommunalFieldInstanceEntity) newValue =
            CommunalFieldInstanceEntity(
                CommunalField = old.CommunalField,
                FieldName = old.FieldName,
                Value = newValue,
                Created = DateTime.UtcNow,
                EditSummary = command.EditSummary)
        let nonClozeCommunals =
            command.CommunalNonClozeFieldValues
                .Select(fun x -> createCommunalFieldInstanceEntity command x.EditField.Name)
                .ToList()
        let getClozeIndex fieldValues = ClozeRegex().TypedMatches(fieldValues).Single().clozeIndex.Value
        let getClozeIndexes fieldValues = ClozeRegex().TypedMatches(fieldValues).Select(fun x -> x.clozeIndex.Value).ToList()
        let commandClozeIndexes = command.ClozeFieldValues |> Seq.collect (fun x -> getClozeIndexes x.Value)
        taskResult {
            let! splitCommands =
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
            let! (tagIds: int ResizeArray) = task {
                let getsertTagId (input: string) =
                    match db.Tag.SingleOrDefault(fun x -> x.Name = input) with
                    | null ->
                        let e = TagEntity(Name = input)
                        db.Tag.AddI e
                        e
                    | x -> x
                let tags = acquiredCard.Tags |> List.map getsertTagId // lowTODO optimize
                do! db.SaveChangesAsyncI()
                return tags.Select(fun x -> x.Id).ToList()
            }
            let branchSourceCardId =
                match command.Source with
                | BranchSourceCardId x -> Nullable x
                | _ -> Nullable()
            let! (acquiredCardEntity: AcquiredCardEntity) =
                db.AcquiredCard
                    .Include(fun x -> x.BranchInstance.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance.CommunalField)
                    .Include(fun x -> x.BranchInstance.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                        .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance.CommunalFieldInstance_BranchInstances)
                    .SingleOrDefaultAsync(fun x ->
                        (x.Id = acquiredCard.AcquiredCardId)
                        || (x.UserId = acquiredCard.UserId && Nullable x.BranchSourceIdOrCardId = branchSourceCardId))
            let acs =
                match acquiredCardEntity with
                | null ->
                    splitCommands
                    |> List.map (fun c ->
                        let communalInstances =
                            c.FieldValues.Select(fun edit ->
                                let fieldName = edit.EditField.Name
                                edit.Communal |> Option.bind(fun value ->
                                    if value.CommunalBranchInstanceIds.Contains 0 then
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
                        e.BranchInstance <- c.CardView.CopyFieldsToNewInstance card c.EditSummary communalInstances
                        match card with
                        | Id x ->
                            e.CardId <- x
                        | Entity _ ->
                            e.Card <- e.BranchInstance.Card
                        db.AcquiredCard.AddI e
                        e)
                | e ->
                    let communalInstances, instanceIds =
                        e.BranchInstance.CommunalFieldInstance_BranchInstances.Select(fun x -> x.CommunalFieldInstance)
                        |> List.ofSeq
                        |> List.map (fun old ->
                            let newValue = command.FieldValues.Single(fun x -> x.EditField.Name = old.FieldName).Value
                            if old.Value = newValue then
                                old, []
                            else
                                updateCommunalField old newValue,
                                old.CommunalFieldInstance_BranchInstances
                                    .Select(fun x -> x.BranchInstanceId)
                                    .Where(fun x -> x <> acquiredCard.BranchInstanceMeta.Id)
                                |> List.ofSeq)
                        |> List.unzip
                    splitCommands
                    |> List.collect (fun c ->
                        let associatedEntities =
                            instanceIds |> List.collect id |> List.distinct |> List.map (fun instanceId ->
                                db.AcquiredCard
                                    .Include(fun x -> x.BranchInstance)
                                    .SingleOrDefault(fun x -> x.BranchInstanceId = instanceId && x.UserId = acquiredCard.UserId)
                                |> function
                                | null -> None // null when it's an instance that isn't acquired, veryLowTODO filter out the unacquired instances
                                | ac ->
                                    if commandClozeIndexes.Contains <| getClozeIndex ac.BranchInstance.FieldValues then
                                        ac.BranchInstance <- c.CardView.CopyFieldsToNewInstance (Id ac.BranchInstance.CardId) command.EditSummary communalInstances
                                        Some ac
                                    else
                                        ac.CardState <- CardState.toDb Suspended
                                        None
                            ) |> List.choose id
                        let e =
                            if command.TemplateInstance.IsCloze then
                                let entityIndex = e.BranchInstance.FieldValues |> getClozeIndex
                                let commandIndex = c.ClozeFieldValues.Select(fun x -> x.Value) |> String.concat " " |> getClozeIndexes |> Seq.distinct |> Seq.exactlyOne
                                if entityIndex = commandIndex then
                                    e.BranchInstance <- c.CardView.CopyFieldsToNewInstance card c.EditSummary communalInstances
                                    e
                                else
                                    if associatedEntities.Select(fun x -> getClozeIndex x.BranchInstance.FieldValues).Contains commandIndex then
                                        e
                                    else
                                        let ac = acquiredCard.copyToNew tagIds
                                        ac.BranchInstance <- c.CardView.CopyFieldsToNewInstance newCardEntity c.EditSummary communalInstances
                                        db.AcquiredCard.AddI ac
                                        ac
                            else
                                e.BranchInstance <- c.CardView.CopyFieldsToNewInstance card c.EditSummary communalInstances
                                e
                        e :: associatedEntities
                    )
            do! db.SaveChangesAsyncI()
            return
                acs.Select(fun x -> x.BranchInstanceId).Distinct().ToList(),
                acs.SelectMany(fun x -> x.BranchInstance.CommunalFieldInstance_BranchInstances.Select(fun x -> x.CommunalFieldInstance))
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
    let AddTo (db: CardOverflowDb) acquiredCardId newTag =
        Tag_AcquiredCardEntity(AcquiredCardId = acquiredCardId, Tag = newTag)
        |> db.Tag_AcquiredCard.AddI
        db.SaveChangesAsyncI ()
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

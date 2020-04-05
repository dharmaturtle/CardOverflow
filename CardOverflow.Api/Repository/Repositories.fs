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
    let removeAndSaveAsync (db: CardOverflowDb) sourceCardId targetCardId userId name = task {
        return!
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
                task {
                    do! db.SaveChangesAsyncI ()
                    return Ok ()
                }
        }

module CommentRepository =
    let addAndSaveAsync (db: CardOverflowDb) (comment: CommentCardEntity) =
        db.CommentCard.AddI comment
        db.SaveChangesAsyncI ()

module TemplateRepository =
    let getFront (db: CardOverflowDb) templateId = task {
        let! r =
            db.LatestTemplateInstance
                .SingleOrDefaultAsync(fun x -> x.TemplateId = templateId)
        return
            match r with
            | null -> ""
            | r ->
                let front, _, _, _ = r |> TemplateInstance.loadLatest |> fun x -> x.FrontBackFrontSynthBackSynth
                front
    }
    let getBack (db: CardOverflowDb) templateId = task {
        let! r =
            db.LatestTemplateInstance
                .SingleOrDefaultAsync(fun x -> x.TemplateId = templateId)
        return
            match r with
            | null -> ""
            | r ->
                let _, back, _, _ = r |> TemplateInstance.loadLatest |> fun x -> x.FrontBackFrontSynthBackSynth
                back
    }
    let getFrontInstance (db: CardOverflowDb) instanceId = task {
        let! r =
            db.TemplateInstance
                .SingleOrDefaultAsync(fun x -> x.Id = instanceId)
        return
            match r with
            | null -> ""
            | r ->
                let front, _, _, _ = r |> TemplateInstance.load |> fun x -> x.FrontBackFrontSynthBackSynth
                front
    }
    let getBackInstance (db: CardOverflowDb) instanceId = task {
        let! r =
            db.TemplateInstance
                .SingleOrDefaultAsync(fun x -> x.Id = instanceId)
        return
            match r with
            | null -> ""
            | r ->
                let _, back, _, _ = r |> TemplateInstance.load |> fun x -> x.FrontBackFrontSynthBackSynth
                back
    }
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

module CardRepository =
    let deleteAcquired (db: CardOverflowDb) userId acquiredCardId = taskResult {
            let! (rs: Relationship_AcquiredCardEntity ResizeArray) = db.Relationship_AcquiredCard.Where(fun x -> x.SourceAcquiredCardId = acquiredCardId || x.TargetAcquiredCardId = acquiredCardId).ToListAsync()
            db.Relationship_AcquiredCard.RemoveRange rs
            let! (ac: AcquiredCardEntity) = db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.Id = acquiredCardId && x.UserId = userId)
            let! ac = ac |> Result.ofNullable "You don't own that card."
            db.AcquiredCard.RemoveI ac
            return! db.SaveChangesAsyncI()
        }
    let editState (db: CardOverflowDb) userId acquiredCardId (state: CardState) = taskResult {
            let! (ac: AcquiredCardEntity) = db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.Id = acquiredCardId && x.UserId = userId)
            let! ac = ac |> Result.ofNullable "You don't own that card."
            ac.CardState <- CardState.toDb state
            return! db.SaveChangesAsyncI()
        }
    let instance (db: CardOverflowDb) instanceId = task {
        match!
            db.CardInstance
            .Include(fun x -> x.TemplateInstance)
            .SingleOrDefaultAsync(fun x -> x.Id = instanceId) with
        | null -> return Error <| sprintf "Card instance %i not found" instanceId
        | x -> return Ok <| CardInstanceView.load x
    }
    let getView (db: CardOverflowDb) cardId = task {
        let! r =
            db.LatestCardInstance
                .Include(fun x -> x.TemplateInstance)
                .SingleAsync(fun x -> x.CardId = cardId)
        return CardInstanceView.loadLatest r
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
                    user.DefaultCardSettingId
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
        return AcquiredCard.initialize userId user.DefaultCardSettingId []
        }
    let private containsSearchTerm (searchTerm: string) = "\"" + searchTerm.Replace('"', ' ') + "\""
    let private searchAcquired (db: CardOverflowDb) userId (searchTerm: string) =
        let containsSearchTerm = containsSearchTerm searchTerm
        db.AcquiredCard
            .Include(fun x -> x.CardInstance.TemplateInstance)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                String.IsNullOrWhiteSpace searchTerm ||
                x.Tag_AcquiredCards.Any(fun x ->
                    EF.Functions.Contains(x.Tag.Name, containsSearchTerm) ||
                    EF.Functions.FreeText(x.Tag.Name, searchTerm)) ||
                EF.Functions.Contains(x.CardInstance.FieldValues, containsSearchTerm) ||
                EF.Functions.FreeText(x.CardInstance.FieldValues, searchTerm)
            )
    let private searchAcquiredIsLatest (db: CardOverflowDb) userId (searchTerm: string) =
        let containsSearchTerm = containsSearchTerm searchTerm
        db.AcquiredCardIsLatest
            .Include(fun x -> x.CardInstance.TemplateInstance)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                String.IsNullOrWhiteSpace searchTerm ||
                x.Tag_AcquiredCards.Any(fun x ->
                    EF.Functions.Contains(x.Tag.Name, containsSearchTerm) ||
                    EF.Functions.FreeText(x.Tag.Name, searchTerm)) ||
                EF.Functions.Contains(x.CardInstance.FieldValues, containsSearchTerm) ||
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
    let SearchAsync (db: CardOverflowDb) userId (pageNumber: int) (searchTerm: string) =
        let containsSearchTerm = containsSearchTerm searchTerm
        task {
            let! r =
                db.LatestCardInstance
                    .Where(fun x ->
                        String.IsNullOrWhiteSpace searchTerm ||
                        x.CardInstance.AcquiredCards.Any(fun x -> x.Tag_AcquiredCards.Any(fun x ->
                            EF.Functions.Contains(x.Tag.Name, containsSearchTerm) ||
                            EF.Functions.FreeText(x.Tag.Name, searchTerm))) ||
                        EF.Functions.Contains(x.CardInstance.FieldValues, containsSearchTerm) ||
                        EF.Functions.FreeText(x.CardInstance.FieldValues, searchTerm)
                    )
                    .Include(fun x -> x.TemplateInstance)
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
                            Instance = CardInstanceMeta.loadLatest isAcquired c Set.empty ResizeArray.empty Set.empty ResizeArray.empty
                        }
                    ) |> List.ofSeq // medTODO optimize
                Details = {
                    CurrentPage = r.PageNumber
                    PageCount = r.PageCount
                }
            }
        }
    let UpdateFieldsToNewInstance (db: CardOverflowDb) (acquiredCard: AcquiredCard) (command: EditCardCommand) =
        let getsertTagId (db: CardOverflowDb) (input: string) =
            let r =
                match db.Tag.SingleOrDefault(fun x -> x.Name = input) with
                | null ->
                    let e = TagEntity(Name = input)
                    db.Tag.AddI e
                    e
                | x -> x
            db.SaveChangesI ()
            r.Id
        taskResult {
            let! card =
                if acquiredCard.CardId = 0 then taskResult {
                    do! match command.ParentId with
                        | Some instanceId -> task {
                            let! userDoesntOwnInstance = db.CardInstance.AnyAsync(fun x -> x.Id = instanceId && x.Card.AuthorId <> acquiredCard.UserId)
                            return
                                if userDoesntOwnInstance then Ok ()
                                else Error "You can't fork your own cards."
                            }
                        | None -> Ok () |> Task.FromResult
                    return Entity <| fun () -> CardEntity(AuthorId = acquiredCard.UserId, ParentId = Option.toNullable command.ParentId)
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
            let! r =
                match acquiredCardEntity with
                | null ->
                    let tagIds = acquiredCard.Tags |> List.map (getsertTagId db) // lowTODO could optimize. This is single threaded cause async saving causes issues, so try batch saving
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
                        let e = acquiredCard.copyToNew tagIds
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
                r.Where(fun x -> nonClozeCommunals.Select(fun x -> x.FieldName).Contains x.FieldName)
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
          LapsedCardsLeechThreshold = byte 8
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
            return r |> Seq.map (fun o -> CardSetting.load (o.Id = user.DefaultCardSettingId) o)
        }

module UserRepository =
    let add (db: CardOverflowDb) name email = task {
        let cardSetting = CardSettingsRepository.defaultCardSettings.CopyToNew 0
        let user =
            UserEntity(
                DisplayName = name,
                Email = email,
                CardSettings = [cardSetting].ToList()
            )
        db.User.AddI user
        do! db.SaveChangesAsyncI ()
        user.DefaultCardSetting <- cardSetting
        return! db.SaveChangesAsyncI () }
    let Get (db: CardOverflowDb) email =
        db.User.FirstOrDefault(fun x -> x.Email = email)

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
        db.Tag.Where(fun t -> t.Name.ToLower().Contains(input.ToLower())).ToList()

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
        
    let Update (db: CardOverflowDb) deck =
        db.Filter.UpdateI deck
        db.SaveChangesI ()

    let Delete (db: CardOverflowDb) deck =
        db.Filter.RemoveI deck
        db.SaveChangesAsyncI ()

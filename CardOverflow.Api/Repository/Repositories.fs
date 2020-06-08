namespace CardOverflow.Api

open System.Threading.Tasks
open FsToolkit.ErrorHandling
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
    let removeAndSaveAsync (db: CardOverflowDb) sourceStackId targetStackId userId name =
        db.Relationship_AcquiredCard.SingleOrDefault(fun x ->
            x.SourceAcquiredCard.StackId = sourceStackId &&
            x.TargetAcquiredCard.StackId = targetStackId &&
            x.SourceAcquiredCard.UserId = userId &&
            x.TargetAcquiredCard.UserId = userId &&
            x.Relationship.Name = name
        ) |> function
        | null ->
            sprintf "Relationship not found between source Stack #%i and target Stack #%i with name \"%s\"." sourceStackId targetStackId name |> Error |> Task.FromResult
        | x ->
            db.Relationship_AcquiredCard.RemoveI x
            db.SaveChangesAsyncI ()
            |> Task.map(fun () -> Ok())

module CommentRepository =
    let addAndSaveAsync (db: CardOverflowDb) (comment: CommentStackEntity) =
        db.CommentStack.AddI comment
        db.SaveChangesAsyncI ()

module CollateRepository =
    let latest (db: CardOverflowDb) collateId =
        db.LatestCollateInstance
            .SingleOrDefaultAsync(fun x -> x.CollateId = collateId)
        |> Task.map (Result.requireNotNull <| sprintf "Collate #%i not found" collateId)
        |> TaskResult.map CollateInstance.load
    let instance (db: CardOverflowDb) instanceId =
        db.CollateInstance
            .SingleOrDefaultAsync(fun x -> x.Id = instanceId)
        |> Task.map (Result.requireNotNull <| sprintf "Collate Instance #%i not found" instanceId)
        |> TaskResult.map CollateInstance.load
    let UpdateFieldsToNewInstance (db: CardOverflowDb) userId (instance: CollateInstance) = task {
        let collate =
            if instance.CollateId = 0 then
                IdOrEntity.Entity <| CollateEntity(AuthorId = userId)
            else    
                Id <| instance.CollateId
        let newCollateInstance = instance.CopyToNewInstance collate
        db.CollateInstance.AddI newCollateInstance
        db  
            .AcquiredCard
            .Include(fun x -> x.BranchInstance)
            .Where(fun x -> x.BranchInstance.CollateInstanceId = instance.Id)
            |> Seq.iter(fun ac ->
                db.Entry(ac.BranchInstance).State <- EntityState.Added
                ac.BranchInstance.Id <- ac.BranchInstance.GetHashCode()
                db.Entry(ac.BranchInstance).Property(nameofInstance <@ any<BranchInstanceEntity>.Id @>).IsTemporary <- true
                ac.BranchInstance.Created <- DateTime.UtcNow
                ac.BranchInstance.Modified <- Nullable()
                ac.BranchInstance.CollateInstance <- newCollateInstance
            )
        let! existing = db.User_CollateInstance.Where(fun x -> x.UserId = userId && x.CollateInstance.CollateId = newCollateInstance.CollateId).ToListAsync()
        db.User_CollateInstance.RemoveRange existing
        User_CollateInstanceEntity(UserId = userId, CollateInstance = newCollateInstance, DefaultCardSettingId = 1) // lowTODO do we ever use the card setting here?
        |> db.User_CollateInstance.AddI
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

module ExploreStackRepository =
    let getAcquiredIds (db: CardOverflowDb) userId stackId =
        db.AcquiredCard
            .Include(fun x -> x.Stack.Branches :> IEnumerable<_>)
                .ThenInclude(fun (x: BranchEntity) -> x.LatestInstance)
            .Where(fun x -> x.UserId = userId && x.StackId = stackId)
            .Select(fun x -> x.StackId, x.BranchId, x.BranchInstanceId)
            .Distinct()
            .SingleOrDefaultAsync()
            |> Task.map Core.toOption
        |> TaskOption.map (fun (stackId, branchId, branchInstanceId) ->
            { StackId = stackId; BranchId = branchId; BranchInstanceId = branchInstanceId})
    let get (db: CardOverflowDb) userId stackId = taskResult {
        let! (r: StackEntity * List<string> * List<string> * List<string>) =
            db.LatestDefaultBranchInstance
                .Include(fun x -> x.Stack.Author)
                .Include(fun x -> x.Stack.Branches :> IEnumerable<_>)
                    .ThenInclude(fun (x: BranchEntity) -> x.LatestInstance.CollateInstance)
                .Include(fun x -> x.Stack.Branches :> IEnumerable<_>)
                    .ThenInclude(fun (x: BranchEntity) -> x.Author)
                .Include(fun x -> x.Stack.CommentStacks :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentStackEntity) -> x.User)
                .Include(fun x -> x.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance)
                .Include(fun x -> x.CollateInstance)
                .Where(fun x -> x.StackId = stackId)
                .Select(fun x ->
                    x.Stack,
                    x.Stack.AcquiredCards.Single(fun x -> x.UserId = userId).Tag_AcquiredCards.Select(fun x -> x.Tag.Name).ToList(),
                    x.Stack.AcquiredCards.Single(fun x -> x.UserId = userId).Relationship_AcquiredCardSourceAcquiredCards.Select(fun x -> x.Relationship.Name).ToList(),
                    x.Stack.AcquiredCards.Single(fun x -> x.UserId = userId).Relationship_AcquiredCardTargetAcquiredCards.Select(fun x -> x.Relationship.Name).ToList()
                ).SingleOrDefaultAsync()
        let! stack, t, rs, rt = r |> Result.ofNullable (sprintf "Stack #%i not found" stackId)
        let! (tc: List<StackTagCountEntity>) = db.StackTagCount.Where(fun x -> x.StackId = stackId).ToListAsync()
        let! (rc: List<StackRelationshipCountEntity>) = db.StackRelationshipCount.Where(fun x -> x.StackId = stackId).ToListAsync()
        let! acquiredIds = getAcquiredIds db userId stackId
        return ExploreStack.load stack acquiredIds (Set.ofSeq t) tc (Seq.append rs rt |> Set.ofSeq) rc
        }
    let instance (db: CardOverflowDb) userId instanceId = taskResult {
        let! (e: BranchInstanceEntity) =
            db.BranchInstance
                .Include(fun x -> x.Branch.Stack.Author)
                .Include(fun x -> x.Branch.Stack.CommentStacks :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentStackEntity) -> x.User)
                .Include(fun x -> x.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance)
                .Include(fun x -> x.CollateInstance)
                .SingleOrDefaultAsync(fun x -> x.Id = instanceId)
            |> Task.map (Result.requireNotNull (sprintf "Branch Instance #%i not found" instanceId))
        let! isAcquired = db.AcquiredCard.AnyAsync(fun x -> x.UserId = userId && x.BranchInstanceId = instanceId)
        let! latest = get db userId e.StackId
        return BranchInstanceMeta.load isAcquired (e.Branch.LatestInstanceId = e.Id) e, latest // lowTODO optimization, only send the old instance - the latest instance isn't used
    }
    let branch (db: CardOverflowDb) userId branchId = taskResult {
        let! (e: BranchInstanceEntity) =
            db.LatestBranchInstance
                .Include(fun x -> x.Branch.Stack.Author)
                .Include(fun x -> x.Branch.Stack.CommentStacks :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentStackEntity) -> x.User)
                .Include(fun x -> x.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance)
                .Include(fun x -> x.CollateInstance)
                .SingleOrDefaultAsync(fun x -> x.BranchId = branchId)
            |> Task.map (Result.requireNotNull (sprintf "Branch #%i not found" branchId))
        let! isAcquired = db.AcquiredCard.AnyAsync(fun x -> x.UserId = userId && x.BranchId = branchId)
        let! latest = get db userId e.StackId
        return BranchInstanceMeta.load isAcquired (e.Branch.LatestInstanceId = e.Id) e, latest // lowTODO optimization, only send the old instance - the latest instance isn't used
    }

module FileRepository =
    let get (db: CardOverflowDb) hash =
        let sha256 = UrlBase64.Decode hash
        db.File.SingleOrDefaultAsync(fun x -> x.Sha256 = sha256)
        |> Task.map (Result.requireNotNull ())
        |> TaskResult.map (fun x -> x.Data)

module StackViewRepository =
    let private getAcquiredInstanceIds (db: CardOverflowDb) userId aId bId =
        if userId = 0 then
            [].ToListAsync()
        else 
            db.AcquiredCard
                .Where(fun x -> x.UserId = userId)
                .Where(fun x -> x.BranchInstanceId = aId || x.BranchInstanceId = bId)
                .Select(fun x -> x.BranchInstanceId)
                .ToListAsync()
    let instanceWithLatest (db: CardOverflowDb) a_branchInstanceId userId = taskResult {
        let! (a: BranchInstanceEntity) =
            db.BranchInstance
                .Include(fun x -> x.CollateInstance)
                .SingleOrDefaultAsync(fun x -> x.Id = a_branchInstanceId)
            |> Task.map (Result.requireNotNull (sprintf "Branch instance #%i not found" a_branchInstanceId))
        let! (b: BranchInstanceEntity) = // verylowTODO optimization try to get this from `a` above
            db.LatestDefaultBranchInstance
                .Include(fun x -> x.CollateInstance)
                .SingleAsync(fun x -> x.StackId = a.StackId)
        let! (acquiredInstanceIds: int ResizeArray) = getAcquiredInstanceIds db userId a_branchInstanceId b.Id
        return
            BranchInstanceView.load a,
            acquiredInstanceIds.Contains a.Id,
            BranchInstanceView.load b,
            acquiredInstanceIds.Contains b.Id,
            b.Id
    }
    let instancePair (db: CardOverflowDb) a_branchInstanceId b_branchInstanceId userId = taskResult {
        let! (instances: BranchInstanceEntity ResizeArray) =
            db.BranchInstance
                .Include(fun x -> x.CollateInstance)
                .Where(fun x -> x.Id = a_branchInstanceId || x.Id = b_branchInstanceId)
                .ToListAsync()
        let! a = Result.requireNotNull (sprintf "Branch instance #%i not found" a_branchInstanceId) <| instances.SingleOrDefault(fun x -> x.Id = a_branchInstanceId)
        let! b = Result.requireNotNull (sprintf "Branch instance #%i not found" b_branchInstanceId) <| instances.SingleOrDefault(fun x -> x.Id = b_branchInstanceId)
        let! (acquiredInstanceIds: int ResizeArray) = getAcquiredInstanceIds db userId a_branchInstanceId b_branchInstanceId
        return
            BranchInstanceView.load a,
            acquiredInstanceIds.Contains a.Id,
            BranchInstanceView.load b,
            acquiredInstanceIds.Contains b.Id
    }
    let instance (db: CardOverflowDb) instanceId = task {
        match!
            db.BranchInstance
            .Include(fun x -> x.CollateInstance)
            .SingleOrDefaultAsync(fun x -> x.Id = instanceId) with
        | null -> return Error <| sprintf "Branch instance %i not found" instanceId
        | x -> return Ok <| BranchInstanceView.load x
    }
    let get (db: CardOverflowDb) stackId =
        db.LatestDefaultBranchInstance
            .Include(fun x -> x.CollateInstance)
            .SingleOrDefaultAsync(fun x -> x.StackId = stackId)
        |> Task.map Ok
        |> TaskResult.bind (fun x -> Result.requireNotNull (sprintf "Stack #%i not found" stackId) x |> Task.FromResult)
        |> TaskResult.map BranchInstanceView.load

module AcquiredCardRepository =
    let getAcquired (db: CardOverflowDb) userId (testBranchInstanceIds: int ResizeArray) =
        db.AcquiredCard.Where(fun x -> testBranchInstanceIds.Contains(x.BranchInstanceId) && x.UserId = userId).Select(fun x -> x.BranchInstanceId).ToListAsync()
    let getAcquiredInstanceFromInstance (db: CardOverflowDb) userId (branchInstanceId: int) =
        db.AcquiredCard
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                x.BranchInstanceId = branchInstanceId ||
                x.Branch.BranchInstances.Any(fun x -> x.Id = branchInstanceId)
            )
            .Select(fun x -> x.BranchInstanceId)
            .Distinct()
            .SingleOrDefaultAsync()
        |> Task.map (Result.requireNotEqualTo 0 <| sprintf "You don't have any cards with Branch Instance #%i" branchInstanceId)

module StackRepository =
    let unacquireStack (db: CardOverflowDb) userId stackId = taskResult {
        do! db.AcquiredCard.Where(fun x -> x.StackId = stackId && x.UserId = userId).ToListAsync()
            |> Task.map (Result.requireNotEmptyX <| sprintf "You don't have any cards with Stack #%i" stackId)
            |> TaskResult.map db.AcquiredCard.RemoveRange
        return! db.SaveChangesAsyncI()
    }
    let editState (db: CardOverflowDb) userId acquiredCardId (state: CardState) = taskResult {
        let! (ac: AcquiredCardEntity) =
            db.AcquiredCard.SingleOrDefaultAsync(fun x -> x.Id = acquiredCardId && x.UserId = userId)
            |> Task.map (Result.ofNullable "You don't own that card.")
        ac.CardState <- CardState.toDb state
        return! db.SaveChangesAsyncI()
    }
    let Revisions (db: CardOverflowDb) userId branchId = taskResult {
        let! r =
            db.Branch
                .Include(fun x -> x.Author)
                .Include(fun x -> x.BranchInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: BranchInstanceEntity) -> x.CollateInstance)
                .SingleOrDefaultAsync(fun x -> x.Id = branchId)
            |> Task.map (Result.requireNotNull <| sprintf "BranchId #%i not found" branchId)
        let! acquiredInstanceId =
            db.AcquiredCard
                .Where(fun x -> x.UserId = userId && x.BranchId = branchId)
                .Select(fun x -> x.BranchInstanceId)
                .Distinct()
                .SingleOrDefaultAsync()
        return BranchRevision.load acquiredInstanceId r
    }
    let acquireCardNoSave (db: CardOverflowDb) userId (branchInstance: BranchInstanceEntity) = taskResult {
        let! ((defaultCardSettingId, deckId): int * int) =
            db.User.Where(fun x -> x.Id = userId).Select(fun x ->
                x.DefaultCardSettingId,
                x.DefaultDeckId
            ).SingleAsync()
        let cardSansIndex =
            AcquiredCard.initialize
                userId
                defaultCardSettingId
                deckId
                []
            |> fun x -> x.copyToNew [] // medTODO get tags from collate
        let new' =
            [0s .. branchInstance.MaxIndexInclusive]
            |> List.map cardSansIndex
        let! (old': AcquiredCardEntity list) = db.AcquiredCard.Where(fun x -> x.UserId = userId && x.StackId = branchInstance.StackId).ToListAsync() |> Task.map Seq.toList
        return
            List.zipOn
                new'
                old'
                (fun new' old' -> new'.Index = old'.Index)
            |> List.map(
                function
                | (None, Some old') ->
                    db.AcquiredCard.RemoveI old' // highTODO add a warning on the UI that data will be lost
                    None
                | (Some new', None) ->
                    new'.BranchInstance <- branchInstance
                    new'.Branch <- branchInstance.Branch
                    new'.Stack <- branchInstance.Stack
                    new'.StackId <- branchInstance.StackId
                    db.AcquiredCard.AddI new'
                    Some new'
                | (Some _, Some old') ->
                    old'.BranchInstance <- branchInstance
                    old'.Branch <- branchInstance.Branch
                    old'.Stack <- branchInstance.Stack
                    old'.StackId <- branchInstance.StackId
                    Some old'
                | (None, None) -> failwith "impossible"
            ) |> ListOption.somes
    }
    let AcquireCardAsync (db: CardOverflowDb) userId branchInstanceId = taskResult {
        let! (branchInstance: BranchInstanceEntity) =
            db.BranchInstance
                .Include(fun x -> x.Branch.Stack)
                .SingleOrDefaultAsync(fun x -> x.Id = branchInstanceId)
            |> Task.map (Result.requireNotNull <| sprintf "Branch Instance #%i not found" branchInstanceId)
        do! acquireCardNoSave db userId branchInstance
        return! db.SaveChangesAsyncI ()
        }
    let GetAcquired (db: CardOverflowDb) (userId: int) (stackId: int) = taskResult {
        let! (e: _ ResizeArray) =
            db.AcquiredCardIsLatest
                .Include(fun x -> x.BranchInstance.CollateInstance)
                .Include(fun x -> x.BranchInstance.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance)
                .Include(fun x -> x.BranchInstance.AcquiredCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: AcquiredCardEntity) -> x.Tag_AcquiredCards :> IEnumerable<_>)
                    .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
                .Where(fun x -> x.StackId = stackId && x.UserId = userId)
                .Select(fun x ->
                    x,
                    x.BranchInstance.AcquiredCards.Single(fun x -> x.UserId = userId).Tag_AcquiredCards.Select(fun x -> x.Tag.Name).ToList()
                ).ToListAsync()
        return!
            e.Select(fun (e, t) ->
                AcquiredCard.load (Set.ofSeq t) e true
            ) |> Result.consolidate
            |> Result.map toResizeArray
        }
    let getNew (db: CardOverflowDb) userId = task {
        let! user = db.User.SingleAsync(fun x -> x.Id = userId)
        return AcquiredCard.initialize userId user.DefaultCardSettingId user.DefaultDeckId []
        }
    let private searchAcquired (db: CardOverflowDb) userId (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.AcquiredCard
            .Include(fun x -> x.BranchInstance.CollateInstance)
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
            .Include(fun x -> x.BranchInstance.CollateInstance)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                String.IsNullOrWhiteSpace searchTerm ||
                x.Tag_AcquiredCards.Any(fun x ->
                    x.Tag.TsVector.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))) ||
                x.BranchInstance.TsVector.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
            )
    let private acquiredByDeck (db: CardOverflowDb) deckId =
        db.AcquiredCard
            .Include(fun x -> x.BranchInstance.CollateInstance)
            .Where(fun x -> x.DeckId = deckId)
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
    let GetQuizBatchDeck (db: CardOverflowDb) deckId =
        let tomorrow = DateTime.UtcNow.AddDays 1.
        task {
            let! cards =
                (acquiredByDeck db deckId)
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
                db.SearchLatestDefaultBranchInstance(searchTerm, plain, wildcard, order).Select(fun x ->
                    x,
                    x.AcquiredCards.Any(fun x -> x.UserId = userId),
                    x.CollateInstance, // .Include fails for some reason, so we have to manually select
                    x.Stack,
                    x.Stack.Author
                ).ToPagedListAsync(pageNumber, 15)
            let squashed =
                r |> List.ofSeq |> List.map (fun (c, isAcquired, collate, stack, author) ->
                    c.Stack <- stack
                    c.Stack.Author <- author
                    c.CollateInstance <- collate
                    c, isAcquired
                )
            return {
                Results =
                    squashed |> List.map (fun (c, isAcquired) ->
                        {   ExploreStackSummary.Id = c.StackId
                            Author = c.Stack.Author.DisplayName
                            AuthorId = c.Stack.AuthorId
                            Users = c.Stack.Users
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
    let stack (db: CardOverflowDb) userId (command: EditStackCommand) =
        let branchNameCheck branchId name =
            db.Branch.AnyAsync(fun b -> b.Id = branchId && b.Stack.Branches.Any(fun b -> b.Name = name && b.Id <> branchId)) // veryLowTODO make case insensitive
            |> Task.map (Result.requireFalse <| sprintf "The stack with Branch #%i already has a Branch named '%s'." branchId name)
        let branchNameCheckStackId stackId name =
            db.Stack.AnyAsync(fun s -> s.Id = stackId && s.Branches.Any(fun b -> b.Name = name)) // veryLowTODO make case insensitive
            |> Task.map (Result.requireFalse <| sprintf "Stack #%i already has a Branch named '%s'." stackId name)
        taskResult {
            let! (branch: BranchEntity) =
                match command.Kind with
                    | Update_BranchId_Title (branchId, name) ->
                        branchNameCheck branchId name
                        |> TaskResult.bind (fun () ->
                            db.Branch.Include(fun x -> x.Stack).SingleOrDefaultAsync(fun x -> x.Id = branchId && x.AuthorId = userId)
                            |> Task.map (Result.requireNotNull <| sprintf "Either Branch #%i doesn't exist or you aren't its author" branchId)
                        )
                    | NewCopy_SourceInstanceId_TagIds (instanceId, _) ->
                        BranchEntity(
                            AuthorId = userId,
                            Stack =
                                StackEntity(
                                    AuthorId = userId,
                                    CopySourceId = Nullable instanceId
                                )) |> Ok |> Task.FromResult
                    | NewBranch_SourceStackId_Title (stackId, name) ->
                        branchNameCheckStackId stackId name
                        |> TaskResult.map(fun () ->
                            BranchEntity(
                                AuthorId = userId,
                                Name = name,
                                StackId = stackId))
                    | NewOriginal_TagIds ->
                        BranchEntity(
                            AuthorId = userId,
                            Stack =
                                StackEntity(
                                    AuthorId = userId
                                )) |> Ok |> Task.FromResult
            let branchInstance = command.CardView.CopyFieldsToNewInstance branch command.EditSummary []
            let! (acs: AcquiredCardEntity list) = StackRepository.acquireCardNoSave db userId branchInstance
            for ac in acs do
                ac.CardSettingId <- command.EditAcquiredCard.CardSettingId
                ac.DeckId <- command.EditAcquiredCard.DeckId
            match command.Kind with
            | Update_BranchId_Title
            | NewBranch_SourceStackId_Title -> ()
            | NewOriginal_TagIds tagIds
            | NewCopy_SourceInstanceId_TagIds (_, tagIds) ->
                for tagId in tagIds do
                    acs.First().Tag_AcquiredCards.Add(Tag_AcquiredCardEntity(TagId = tagId))
            do! db.SaveChangesAsyncI()
            return branchInstance.BranchId
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
    let theCollectiveId = 2
    let defaultCloze = "Cloze"
    let create (db: CardOverflowDb) id displayName = task {
        let defaultSetting = CardSettingsRepository.defaultCardSettings.CopyToNew 0
        defaultSetting |> db.CardSetting.AddI
        DeckEntity(Name = "Default Deck") |> db.Deck.AddI
        
        let! (nonClozeIds: int list) =
            db.LatestCollateInstance
                .Where(fun x -> x.Name <> defaultCloze && x.Collate.AuthorId = theCollectiveId)
                .Select(fun x -> x.Id)
                .ToListAsync() |> Task.map List.ofSeq
        let! oldestClozeId =
            db.CollateInstance
                .Where(fun x -> x.Name = defaultCloze && x.Collate.AuthorId = theCollectiveId)
                .OrderBy(fun x -> x.Created)
                .Select(fun x -> x.Id)
                .FirstAsync()
        UserEntity(
            Id = id,
            DisplayName = displayName,
            //Filters = [ FilterEntity ( Name = "All", Query = "" )].ToList(),
            User_CollateInstances =
                (oldestClozeId :: nonClozeIds)
                .Select(fun id -> User_CollateInstanceEntity (CollateInstanceId = id, DefaultCardSetting = defaultSetting ))
                .ToList()) |> db.User.AddI
        return! db.SaveChangesAsyncI () }
    let Get (db: CardOverflowDb) id =
        db.User.SingleAsync(fun x -> x.Id = id)

module TagRepository =
    let searchMany (db: CardOverflowDb) (input: string list) =
        let input = input |> List.map (fun x -> x.ToLower())
        db.Tag.Where(fun t -> input.Contains(t.Name.ToLower()))
    let search (db: CardOverflowDb) (input: string) =
        db.Tag.Where(fun t -> EF.Functions.ILike(t.Name, input + "%"))

module DeckRepository =
    let searchMany (db: CardOverflowDb) userId (input: string list) =
        let input = input |> List.map (fun x -> x.ToLower())
        db.Deck.Where(fun t -> input.Contains(t.Name.ToLower()) && t.UserId = userId)

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

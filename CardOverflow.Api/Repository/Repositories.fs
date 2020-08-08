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
open System.Runtime.ExceptionServices
open System.Runtime.CompilerServices

module CommieldRepository =
    let get (db: CardOverflowDb) fieldId = task {
        let! x = db.LatestCommeaf.SingleAsync(fun x -> x.CommieldId = fieldId)
        return x.Value
    }
    let getLeaf (db: CardOverflowDb) leafId = task {
        let! x = db.Commeaf.SingleAsync(fun x -> x.Id = leafId)
        return x.Value
    }
    let Search (db: CardOverflowDb) (query: string) = task {
        let! x =
            db.LatestCommeaf
                .Where(fun x -> x.Value.Contains query)
                .ToListAsync()
        return x |> Seq.map Commeaf.load |> toResizeArray
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
    let addAndSaveAsync (db: CardOverflowDb) (j: Relationship_CardEntity) =
        db.Relationship_Card.AddI j
        db.SaveChangesAsyncI ()
    let removeAndSaveAsync (db: CardOverflowDb) sourceStackId targetStackId userId name =
        db.Relationship_Card.SingleOrDefault(fun x ->
            x.SourceCard.StackId = sourceStackId &&
            x.TargetCard.StackId = targetStackId &&
            x.SourceCard.UserId = userId &&
            x.TargetCard.UserId = userId &&
            x.Relationship.Name = name
        ) |> function
        | null ->
            sprintf "Relationship not found between source Stack #%i and target Stack #%i with name \"%s\"." sourceStackId targetStackId name |> Error |> Task.FromResult
        | x ->
            db.Relationship_Card.RemoveI x
            db.SaveChangesAsyncI ()
            |> Task.map(fun () -> Ok())

module CommentRepository =
    let addAndSaveAsync (db: CardOverflowDb) (comment: CommentStackEntity) =
        db.CommentStack.AddI comment
        db.SaveChangesAsyncI ()

module GromplateRepository =
    let latest (db: CardOverflowDb) gromplateId =
        db.LatestGrompleaf
            .SingleOrDefaultAsync(fun x -> x.GromplateId = gromplateId)
        |> Task.map (Result.requireNotNull <| sprintf "Gromplate #%i not found" gromplateId)
        |> TaskResult.map Grompleaf.load
    let leaf (db: CardOverflowDb) leafId =
        db.Grompleaf
            .SingleOrDefaultAsync(fun x -> x.Id = leafId)
        |> Task.map (Result.requireNotNull <| sprintf "Gromplate Leaf #%i not found" leafId)
        |> TaskResult.map Grompleaf.load
    let UpdateFieldsToNewLeaf (db: CardOverflowDb) userId (leaf: Grompleaf) = task {
        let gromplate =
            if leaf.GromplateId = 0 then
                IdOrEntity.Entity <| GromplateEntity(AuthorId = userId)
            else    
                Id <| leaf.GromplateId
        let newGrompleaf = leaf.CopyToNewLeaf gromplate
        db.Grompleaf.AddI newGrompleaf
        db  
            .Card
            .Include(fun x -> x.Leaf)
            .Where(fun x -> x.Leaf.GrompleafId = leaf.Id)
            |> Seq.iter(fun cc ->
                db.Entry(cc.Leaf).State <- EntityState.Added
                cc.Leaf.Id <- cc.Leaf.GetHashCode()
                db.Entry(cc.Leaf).Property(nameofLeaf <@ any<LeafEntity>.Id @>).IsTemporary <- true
                cc.Leaf.Created <- DateTime.UtcNow
                cc.Leaf.Modified <- Nullable()
                cc.Leaf.Grompleaf <- newGrompleaf
            )
        let! existing = db.User_Grompleaf.Where(fun x -> x.UserId = userId && x.Grompleaf.GromplateId = newGrompleaf.GromplateId).ToListAsync()
        db.User_Grompleaf.RemoveRange existing
        User_GrompleafEntity(UserId = userId, Grompleaf = newGrompleaf, DefaultCardSettingId = 1) // lowTODO do we ever use the card setting here?
        |> db.User_Grompleaf.AddI
        return! db.SaveChangesAsyncI()
        }

module HistoryRepository =
    let getHeatmap (db: CardOverflowDb) userId = task {
        let oneYearishAgo = DateTime.UtcNow - TimeSpan.FromDays (53. * 7. - 1.) // always show full weeks of slightly more than a year; -1 is from allDateCounts being inclusive
        let! dateCounts =
            (query {
                for h in db.History do
                where (h.Created >= oneYearishAgo && h.Card.UserId = userId)
                groupValBy h h.Created.Date into g
                select { Date = g.Key; Count = g.Count() }
            }).ToListAsync()
        return Heatmap.get oneYearishAgo DateTime.UtcNow (dateCounts |> List.ofSeq) }

module ExploreStackRepository =
    let getCollectedIds (db: CardOverflowDb) userId stackId =
        db.Card
            .Include(fun x -> x.Stack.Branches :> IEnumerable<_>)
                .ThenInclude(fun (x: BranchEntity) -> x.Latest)
            .Where(fun x -> x.UserId = userId && x.StackId = stackId)
            .Select(fun x -> x.StackId, x.BranchId, x.LeafId)
            .Distinct()
            .SingleOrDefaultAsync()
            |> Task.map Core.toOption
        |> TaskOption.map (fun (stackId, branchId, leafId) ->
            { StackId = stackId; BranchId = branchId; LeafId = leafId})
    let get (db: CardOverflowDb) userId stackId = taskResult {
        let! (r: StackEntity * List<string> * List<string> * List<string>) =
            db.LatestDefaultLeaf
                .Include(fun x -> x.Stack.Author)
                .Include(fun x -> x.Stack.Branches :> IEnumerable<_>)
                    .ThenInclude(fun (x: BranchEntity) -> x.Latest.Grompleaf)
                .Include(fun x -> x.Stack.Branches :> IEnumerable<_>)
                    .ThenInclude(fun (x: BranchEntity) -> x.Author)
                .Include(fun x -> x.Stack.CommentStacks :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentStackEntity) -> x.User)
                .Include(fun x -> x.Commeaf_Leafs :> IEnumerable<_>)
                    .ThenInclude(fun (x: Commeaf_LeafEntity) -> x.Commeaf)
                .Include(fun x -> x.Grompleaf)
                .Where(fun x -> x.StackId = stackId)
                .Select(fun x ->
                    x.Stack,
                    x.Stack.Cards.Single(fun x -> x.UserId = userId).Tag_Cards.Select(fun x -> x.Tag.Name).ToList(),
                    x.Stack.Cards.Single(fun x -> x.UserId = userId).Relationship_CardSourceCards.Select(fun x -> x.Relationship.Name).ToList(),
                    x.Stack.Cards.Single(fun x -> x.UserId = userId).Relationship_CardTargetCards.Select(fun x -> x.Relationship.Name).ToList()
                ).SingleOrDefaultAsync()
        let! stack, t, rs, rt = r |> Result.ofNullable (sprintf "Stack #%i not found" stackId)
        let! (tc: List<StackTagCountEntity>) = db.StackTagCount.Where(fun x -> x.StackId = stackId).ToListAsync()
        let! (rc: List<StackRelationshipCountEntity>) = db.StackRelationshipCount.Where(fun x -> x.StackId = stackId).ToListAsync()
        let! collectedIds = getCollectedIds db userId stackId
        return ExploreStack.load stack collectedIds (Set.ofSeq t) tc (Seq.append rs rt |> Set.ofSeq) rc
        }
    let leaf (db: CardOverflowDb) userId leafId = taskResult {
        let! (e: LeafEntity) =
            db.Leaf
                .Include(fun x -> x.Branch.Stack.Author)
                .Include(fun x -> x.Branch.Stack.CommentStacks :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentStackEntity) -> x.User)
                .Include(fun x -> x.Commeaf_Leafs :> IEnumerable<_>)
                    .ThenInclude(fun (x: Commeaf_LeafEntity) -> x.Commeaf)
                .Include(fun x -> x.Grompleaf)
                .SingleOrDefaultAsync(fun x -> x.Id = leafId)
            |> Task.map (Result.requireNotNull (sprintf "Branch Leaf #%i not found" leafId))
        let! isCollected = db.Card.AnyAsync(fun x -> x.UserId = userId && x.LeafId = leafId)
        let! latest = get db userId e.StackId
        return LeafMeta.load isCollected (e.Branch.LatestId = e.Id) e, latest // lowTODO optimization, only send the old leaf - the latest leaf isn't used
    }
    let branch (db: CardOverflowDb) userId branchId = taskResult {
        let! (e: LeafEntity) =
            db.LatestLeaf
                .Include(fun x -> x.Branch.Stack.Author)
                .Include(fun x -> x.Branch.Stack.CommentStacks :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommentStackEntity) -> x.User)
                .Include(fun x -> x.Commeaf_Leafs :> IEnumerable<_>)
                    .ThenInclude(fun (x: Commeaf_LeafEntity) -> x.Commeaf)
                .Include(fun x -> x.Grompleaf)
                .SingleOrDefaultAsync(fun x -> x.BranchId = branchId)
            |> Task.map (Result.requireNotNull (sprintf "Branch #%i not found" branchId))
        let! isCollected = db.Card.AnyAsync(fun x -> x.UserId = userId && x.BranchId = branchId)
        let! latest = get db userId e.StackId
        return LeafMeta.load isCollected (e.Branch.LatestId = e.Id) e, latest // lowTODO optimization, only send the old leaf - the latest leaf isn't used
    }

module FileRepository =
    let get (db: CardOverflowDb) hash =
        let sha256 = UrlBase64.Decode hash
        db.File.SingleOrDefaultAsync(fun x -> x.Sha256 = sha256)
        |> Task.map (Result.requireNotNull ())
        |> TaskResult.map (fun x -> x.Data)

module StackViewRepository =
    let private getCollectedLeafIds (db: CardOverflowDb) userId aId bId =
        if userId = 0 then
            [].ToListAsync()
        else 
            db.Card
                .Where(fun x -> x.UserId = userId)
                .Where(fun x -> x.LeafId = aId || x.LeafId = bId)
                .Select(fun x -> x.LeafId)
                .ToListAsync()
    let leafWithLatest (db: CardOverflowDb) a_leafId userId = taskResult {
        let! (a: LeafEntity) =
            db.Leaf
                .Include(fun x -> x.Grompleaf)
                .SingleOrDefaultAsync(fun x -> x.Id = a_leafId)
            |> Task.map (Result.requireNotNull (sprintf "Branch leaf #%i not found" a_leafId))
        let! (b: LeafEntity) = // verylowTODO optimization try to get this from `a` above
            db.LatestDefaultLeaf
                .Include(fun x -> x.Grompleaf)
                .SingleAsync(fun x -> x.StackId = a.StackId)
        let! (collectedLeafIds: int ResizeArray) = getCollectedLeafIds db userId a_leafId b.Id
        return
            LeafView.load a,
            collectedLeafIds.Contains a.Id,
            LeafView.load b,
            collectedLeafIds.Contains b.Id,
            b.Id
    }
    let leafPair (db: CardOverflowDb) a_leafId b_leafId userId = taskResult {
        let! (leafs: LeafEntity ResizeArray) =
            db.Leaf
                .Include(fun x -> x.Grompleaf)
                .Where(fun x -> x.Id = a_leafId || x.Id = b_leafId)
                .ToListAsync()
        let! a = Result.requireNotNull (sprintf "Branch leaf #%i not found" a_leafId) <| leafs.SingleOrDefault(fun x -> x.Id = a_leafId)
        let! b = Result.requireNotNull (sprintf "Branch leaf #%i not found" b_leafId) <| leafs.SingleOrDefault(fun x -> x.Id = b_leafId)
        let! (collectedLeafIds: int ResizeArray) = getCollectedLeafIds db userId a_leafId b_leafId
        return
            LeafView.load a,
            collectedLeafIds.Contains a.Id,
            LeafView.load b,
            collectedLeafIds.Contains b.Id
    }
    let leaf (db: CardOverflowDb) leafId = task {
        match!
            db.Leaf
            .Include(fun x -> x.Grompleaf)
            .SingleOrDefaultAsync(fun x -> x.Id = leafId) with
        | null -> return Error <| sprintf "Branch leaf %i not found" leafId
        | x -> return Ok <| LeafView.load x
    }
    let get (db: CardOverflowDb) stackId =
        db.LatestDefaultLeaf
            .Include(fun x -> x.Grompleaf)
            .SingleOrDefaultAsync(fun x -> x.StackId = stackId)
        |> Task.map Ok
        |> TaskResult.bind (fun x -> Result.requireNotNull (sprintf "Stack #%i not found" stackId) x |> Task.FromResult)
        |> TaskResult.map LeafView.load

module CardRepository =
    let getCollected (db: CardOverflowDb) userId (testLeafIds: int ResizeArray) =
        db.Card.Where(fun x -> testLeafIds.Contains(x.LeafId) && x.UserId = userId).Select(fun x -> x.LeafId).ToListAsync()
    let getCollectedLeafFromLeaf (db: CardOverflowDb) userId (leafId: int) =
        db.Card
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                x.LeafId = leafId ||
                x.Branch.Leafs.Any(fun x -> x.Id = leafId)
            )
            .Select(fun x -> x.LeafId)
            .Distinct()
            .SingleOrDefaultAsync()
        |> Task.map (Result.requireNotEqualTo 0 <| sprintf "You don't have any cards with Branch Leaf #%i" leafId)

module StackRepository =
    let uncollectStack (db: CardOverflowDb) userId stackId = taskResult {
        do! db.Card.Where(fun x -> x.StackId = stackId && x.UserId = userId).ToListAsync()
            |> Task.map (Result.requireNotEmptyX <| sprintf "You don't have any cards with Stack #%i" stackId)
            |> TaskResult.map db.Card.RemoveRange
        return! db.SaveChangesAsyncI()
    }
    let editState (db: CardOverflowDb) userId cardId (state: CardState) = taskResult {
        let! (cc: CardEntity) =
            db.Card.SingleOrDefaultAsync(fun x -> x.Id = cardId && x.UserId = userId)
            |> Task.map (Result.ofNullable "You don't own that card.")
        cc.CardState <- CardState.toDb state
        return! db.SaveChangesAsyncI()
    }
    let Revisions (db: CardOverflowDb) userId branchId = taskResult {
        let! r =
            db.Branch
                .Include(fun x -> x.Author)
                .Include(fun x -> x.Leafs :> IEnumerable<_>)
                    .ThenInclude(fun (x: LeafEntity) -> x.Grompleaf)
                .SingleOrDefaultAsync(fun x -> x.Id = branchId)
            |> Task.map (Result.requireNotNull <| sprintf "BranchId #%i not found" branchId)
        let! collectedLeafId =
            db.Card
                .Where(fun x -> x.UserId = userId && x.BranchId = branchId)
                .Select(fun x -> x.LeafId)
                .Distinct()
                .SingleOrDefaultAsync()
        return BranchRevision.load collectedLeafId r
    }
    let collectStackNoSave (db: CardOverflowDb) userId (leaf: LeafEntity) mayUpdate = task {
        let! ((defaultCardSettingId, defaultDeckId): int * int) =
            db.User.Where(fun x -> x.Id = userId).Select(fun x ->
                x.DefaultCardSettingId,
                x.DefaultDeckId
            ).SingleAsync()
        let cardSansIndex =
            Card.initialize
                userId
                defaultCardSettingId
                defaultDeckId
                []
            |> fun x -> x.copyToNew [] // medTODO get tags from gromplate
        let new' =
            [0s .. leaf.MaxIndexInclusive]
            |> List.map cardSansIndex
        let! (old': CardEntity list) = db.Card.Where(fun x -> x.UserId = userId && x.StackId = leaf.StackId).ToListAsync() |>% Seq.toList
        return
            List.zipOn new' old' (fun new' old' -> new'.Index = old'.Index)
            |> List.map(
                function
                | None, Some old' ->
                    db.Card.RemoveI old' // highTODO add a warning on the UI that data will be lost
                    None
                | Some new', None ->
                    new'.Leaf <- leaf
                    new'.Branch <- leaf.Branch
                    new'.Stack <- leaf.Stack
                    new'.StackId <- leaf.StackId
                    new'.LeafId <- leaf.Id
                    db.Card.AddI new'
                    Some new'
                | Some _, Some old' ->
                    if leaf.BranchId = old'.BranchId || mayUpdate then
                        old'.Leaf <- leaf
                        old'.Branch <- leaf.Branch
                        old'.Stack <- leaf.Stack
                        old'.StackId <- leaf.StackId
                        old'.LeafId <- leaf.Id
                        Some old'
                    else None
                | None, None -> failwith "impossible"
            ) |> ListOption.somes
    }
    let collect (db: CardOverflowDb) userId leafId deckId = taskResult {
        let! (leaf: LeafEntity) =
            db.Leaf
                .Include(fun x -> x.Branch.Stack)
                .SingleOrDefaultAsync(fun x -> x.Id = leafId)
            |> Task.map (Result.requireNotNull <| sprintf "Branch Leaf #%i not found" leafId)
        let! (ccs: CardEntity list) = collectStackNoSave db userId leaf true
        match deckId with
        | Some deckId ->
            do! db.Deck.AnyAsync(fun x -> x.Id = deckId && x.UserId = userId)
                |>% Result.requireTrue (sprintf "Either Deck #%i doesn't exist or it doesn't belong to you." deckId)
            ccs |> List.iter (fun cc -> cc.DeckId <- deckId)
        | None -> ()
        do! db.SaveChangesAsyncI ()
        return ccs |> List.map (fun x -> x.Id)
        }
    let CollectCard (db: CardOverflowDb) userId leafId =
        collect db userId leafId None
    let GetCollected (db: CardOverflowDb) (userId: int) (stackId: int) = taskResult {
        let! (e: _ ResizeArray) =
            db.CardIsLatest
                .Include(fun x -> x.Leaf.Grompleaf)
                .Include(fun x -> x.Leaf.Commeaf_Leafs :> IEnumerable<_>)
                    .ThenInclude(fun (x: Commeaf_LeafEntity) -> x.Commeaf)
                .Include(fun x -> x.Leaf.Cards :> IEnumerable<_>)
                    .ThenInclude(fun (x: CardEntity) -> x.Tag_Cards :> IEnumerable<_>)
                    .ThenInclude(fun (x: Tag_CardEntity) -> x.Tag)
                .Where(fun x -> x.StackId = stackId && x.UserId = userId)
                .Select(fun x ->
                    x,
                    x.Leaf.Cards.Single(fun x -> x.UserId = userId).Tag_Cards.Select(fun x -> x.Tag.Name).ToList()
                ).ToListAsync()
        return!
            e.Select(fun (e, t) ->
                Card.load (Set.ofSeq t) e true
            ) |> Result.consolidate
            |> Result.map toResizeArray
        }
    let getNew (db: CardOverflowDb) userId = task {
        let! user = db.User.SingleAsync(fun x -> x.Id = userId)
        return Card.initialize userId user.DefaultCardSettingId user.DefaultDeckId []
        }
    let private searchCollected (db: CardOverflowDb) userId (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.Card
            .Include(fun x -> x.Leaf.Grompleaf)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                String.IsNullOrWhiteSpace searchTerm ||
                x.Tag_Cards.Any(fun x ->
                    x.Tag.Tsv.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))) ||
                x.Leaf.Tsv.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
            )
    let private searchCollectedIsLatest (db: CardOverflowDb) userId (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.CardIsLatest
            .Include(fun x -> x.Leaf.Grompleaf)
            .Where(fun x -> x.UserId = userId)
            .Where(fun x ->
                String.IsNullOrWhiteSpace searchTerm ||
                x.Tag_Cards.Any(fun x ->
                    x.Tag.Tsv.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))) ||
                x.Leaf.Tsv.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
            )
    let private collectedByDeck (db: CardOverflowDb) deckId =
        db.Card
            .Include(fun x -> x.Leaf.Grompleaf)
            .Where(fun x -> x.DeckId = deckId)
    let GetCollectedPages (db: CardOverflowDb) (userId: int) (pageNumber: int) (searchTerm: string) =
        task {
            let! r =
                (searchCollectedIsLatest db userId searchTerm)
                    .Include(fun x -> x.Tag_Cards :> IEnumerable<_>)
                        .ThenInclude(fun (x: Tag_CardEntity) -> x.Tag)
                    .ToPagedListAsync(pageNumber, 15)
            return {
                Results = r |> Seq.map (fun x -> Card.load (x.Tag_Cards.Select(fun x -> x.Tag.Name) |> Set.ofSeq) x true)
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
                (searchCollected db userId query)
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
                (collectedByDeck db deckId)
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
        (searchCollected db userId query)
            .Where(fun x -> x.Due < tomorrow && x.CardState = CardState.toDb Normal)
            .Count()
    let private searchExplore userId (pageNumber: int) (filteredLeafs: LeafEntity IOrderedQueryable)=
        task {
            let! r =
                filteredLeafs.Select(fun x ->
                    x,
                    x.Cards.Any(fun x -> x.UserId = userId),
                    x.Grompleaf, // .Include fails for some reason, so we have to manually select
                    x.Stack,
                    x.Stack.Author
                ).ToPagedListAsync(pageNumber, 15)
            let squashed =
                r |> List.ofSeq |> List.map (fun (c, isCollected, gromplate, stack, author) ->
                    c.Stack <- stack
                    c.Stack.Author <- author
                    c.Grompleaf <- gromplate
                    c, isCollected
                )
            return {
                Results =
                    squashed |> List.map (fun (c, isCollected) ->
                        {   ExploreStackSummary.Id = c.StackId
                            Author = c.Stack.Author.DisplayName
                            AuthorId = c.Stack.AuthorId
                            Users = c.Stack.Users
                            Leaf = LeafMeta.load isCollected true c
                        }
                    )
                Details = {
                    CurrentPage = r.PageNumber
                    PageCount = r.PageCount
                }
            }
        }
    let search (db: CardOverflowDb) userId (pageNumber: int) order (searchTerm: string) =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.LatestDefaultLeaf.Search(searchTerm, plain, wildcard, order)
        |> searchExplore userId pageNumber
    let searchDeck (db: CardOverflowDb) userId (pageNumber: int) order (searchTerm: string) deckId =
        let plain, wildcard = FullTextSearch.parse searchTerm
        db.Deck
            .Where(fun x -> x.Id = deckId && (x.IsPublic || x.UserId = userId))
            .SelectMany(fun x -> x.Cards.Select(fun x -> x.Leaf))
            .Search(searchTerm, plain, wildcard, order)
        |> searchExplore userId pageNumber

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
                    | NewCopy_SourceLeafId_TagIds (leafId, _) ->
                        BranchEntity(
                            AuthorId = userId,
                            Stack =
                                StackEntity(
                                    AuthorId = userId,
                                    CopySourceId = Nullable leafId
                                )) |> Ok |> Task.FromResult
                    | NewBranch_SourceStackId_Title (stackId, name) ->
                        branchNameCheckStackId stackId name
                        |> TaskResult.map(fun () ->
                            BranchEntity(
                                AuthorId = userId,
                                Name = name,
                                StackId = stackId))
                    | NewOriginal_TagIds _ ->
                        BranchEntity(
                            AuthorId = userId,
                            Stack =
                                StackEntity(
                                    AuthorId = userId
                                )) |> Ok |> Task.FromResult
            return command.CardView.CopyFieldsToNewLeaf branch command.EditSummary []
        }

module NotificationRepository =
    let get (db: CardOverflowDb) userId (pageNumber: int) = task {
        let! ns =
            db.ReceivedNotification
                .Where(fun x -> x.ReceiverId = userId)
                .Select(fun x ->
                    x.Notification,
                    x.Notification.Sender.DisplayName,
                    x.Notification.Stack.Cards.FirstOrDefault(fun x -> x.UserId = userId),
                    x.Notification.Deck.Name,
                    x.Notification.Deck.DerivedDecks.SingleOrDefault(fun x -> x.UserId = userId)
                ).ToPagedListAsync(pageNumber, 30)
        return {
            Results = ns |> Seq.map Notification.load
            Details = {
                CurrentPage = ns.PageNumber
                PageCount = ns.PageCount
            }
        }
    }
    let remove (db: CardOverflowDb) userId notificationId =
        FormattableStringFactory.Create("""SELECT public.fn_delete_received_notification({0},{1});""", notificationId, userId)
        |> db.Database.ExecuteSqlInterpolatedAsync
        |>% ignore

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

type Profile = {
    DisplayName: string
}

module UserRepository =
    let theCollectiveId = 2
    let defaultCloze = "Cloze"
    let create (db: CardOverflowDb) id displayName = task {
        let defaultSetting = CardSettingsRepository.defaultCardSettings.CopyToNew 0
        defaultSetting |> db.CardSetting.AddI
        DeckEntity(Name = "Default Deck") |> db.Deck.AddI
        
        let! (nonClozeIds: int list) =
            db.LatestGrompleaf
                .Where(fun x -> x.Name <> defaultCloze && x.Gromplate.AuthorId = theCollectiveId)
                .Select(fun x -> x.Id)
                .ToListAsync() |> Task.map List.ofSeq
        let! oldestClozeId =
            db.Grompleaf
                .Where(fun x -> x.Name = defaultCloze && x.Gromplate.AuthorId = theCollectiveId)
                .OrderBy(fun x -> x.Created)
                .Select(fun x -> x.Id)
                .FirstAsync()
        UserEntity(
            Id = id,
            DisplayName = displayName,
            //Filters = [ FilterEntity ( Name = "All", Query = "" )].ToList(),
            User_Grompleafs =
                (oldestClozeId :: nonClozeIds)
                .Select(fun id -> User_GrompleafEntity (GrompleafId = id, DefaultCardSetting = defaultSetting ))
                .ToList()) |> db.User.AddI
        return! db.SaveChangesAsyncI () }
    let profile (db: CardOverflowDb) userId =
        db.User
            .Where(fun x -> x.Id = userId)
            .Select(fun x -> x.DisplayName)
            .SingleOrDefaultAsync()
        |>% Result.requireNotNull (sprintf "User %i doesn't exist" userId)
        |>%% fun x -> { DisplayName = x }

type TreeTag = {
    Id: string
    ParentId: string
    Name: string
    IsExpanded: bool
    HasChildren: bool
}

module TagRepository =
    let delimiter = '/'
    let (+/+) a b =
        match String.IsNullOrWhiteSpace a, String.IsNullOrWhiteSpace b with
        | true, true -> ""
        | false, true -> a
        | true, false -> b
        | false, false -> sprintf "%s%c%s" a delimiter b
    let splitRawtag (rawTag: string) = // returns parent, name
        let i = rawTag.LastIndexOf delimiter
        if i = -1 then
            "", rawTag
        else
            rawTag.Substring(0, i),
            rawTag.Substring(i + 1)
    let unfold rawTag =
        (rawTag, false) |> List.unfold (fun (rawTag, hasChildren) ->
            let parent, name = splitRawtag rawTag
            match name with
            | "" -> None
            | _ ->
                ((rawTag, hasChildren), (parent, true))
                |> Some
          )
    let parse rawTags =
        rawTags
        |> Seq.toList
        |> List.collect unfold
        |> List.groupBy fst
        |> ListPair.map2 (List.exists snd)
        |> List.sortBy fst
        |> List.map(fun (rawTag, hasChildren) ->
            let parent, name = splitRawtag rawTag
            {   Id = rawTag
                ParentId = parent
                Name = name
                IsExpanded = false
                HasChildren = hasChildren
            })
    let getAll (db: CardOverflowDb) userId =
        db.Tag_Card
            .Where(fun x -> x.UserId = userId)
            .Select(fun x -> x.Tag.Name)
            .Distinct()
            .ToListAsync()
        |> Task.map parse
    let searchMany (db: CardOverflowDb) (input: string list) =
        let input = input |> List.map (fun x -> x.ToLower())
        db.Tag.Where(fun t -> input.Contains(t.Name.ToLower()))
    let search (db: CardOverflowDb) (input: string) =
        db.Tag.Where(fun t -> EF.Functions.ILike(t.Name, input + "%"))

[<CLIMutable>]
type DeckWithFollowMeta = {
    Id: int
    Name: string
    AuthorId: int
    AuthorName: string
    IsFollowed: bool
    FollowCount: int
}
type Follower = {
    Id: int
    DisplayName: string
}
module DeckRepository =
    let searchMany (db: CardOverflowDb) userId (input: string list) =
        let input = input |> List.map (fun x -> x.ToLower())
        db.Deck.Where(fun t -> input.Contains(t.Name.ToLower()) && t.UserId = userId)
    let getFollowers (db: CardOverflowDb) deckId =
        db.DeckFollower.Where(fun x -> x.DeckId = deckId && x.Deck.IsPublic).Select(fun x ->
            x.FollowerId,
            x.Follower.DisplayName
        ).ToListAsync()
        |>% Seq.map (fun (followerId, displayName) -> {
            Id = followerId
            DisplayName = displayName
        })
        |>% toResizeArray
    let getPublic (db: CardOverflowDb) userId authorId = task {
        let! r =
            db.Deck
                .Where(fun x -> x.IsPublic && x.UserId = authorId)
                .Select(fun x ->
                    x,
                    x.DeckFollowers.Any(fun x -> x.FollowerId = userId),
                    x.Followers,
                    x.UserId,
                    x.User.DisplayName
                )
                .ToListAsync()
        return
            r.Select(fun (deck, isFollowed, count, authorId, authorName) -> {
                Id = deck.Id
                Name = deck.Name
                AuthorId = authorId
                AuthorName = authorName
                IsFollowed = isFollowed
                FollowCount = count
            }).ToList()
    }

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

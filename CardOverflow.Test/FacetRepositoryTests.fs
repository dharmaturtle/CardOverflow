module FacetRepositoryTests

open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Debug
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open CardOverflow.Test
open System
open System.Linq
open Xunit
open CardOverflow.Pure
open System.Collections.Generic
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Sanitation
open FsToolkit.ErrorHandling

let normalCommand fieldValues grompleaf tagIds ids =
    let fieldValues =
        match fieldValues with
        | [] -> ["Front"; "Back"]
        | _ -> fieldValues
    {   Grompleaf = grompleaf
        FieldValues =
            grompleaf.Fields
            |> Seq.mapi (fun i field -> {
                EditField = ViewField.copyTo field
                Value = fieldValues.[i]
            }) |> toResizeArray
        EditSummary = "Initial creation"
        Kind = NewOriginal_TagIds tagIds
        Title = null
        Ids = UpsertIds.fromTuple ids
    }

let clozeCommand clozeText (clozeGromplate: ViewGrompleaf) tagIds ids = {
    EditSummary = "Initial creation"
    FieldValues =
        clozeGromplate.Fields.Select(fun f -> {
            EditField = ViewField.copyTo f
            Value =
                if f.Name = "Text" then
                    clozeText
                else
                    "extra"
        }).ToList()
    Grompleaf = clozeGromplate
    Kind = NewOriginal_TagIds tagIds
    Title = null
    Ids = UpsertIds.fromTuple ids }

let add gromplateName createCommand (db: CardOverflowDb) userId tags (ids: Guid * Guid * Guid * Guid list) = taskResult {
    let tagIds = ResizeArray.empty
    for tag in tags do
        let! tagId = SanitizeTagRepository.upsert db tag |> TaskResult.getOk
        tagIds.Add tagId
    let! gromplate = TestGromplateRepo.SearchEarliest db gromplateName
    return!
        createCommand gromplate (tagIds |> List.ofSeq) ids
        |> SanitizeStackRepository.Update db userId []
    }

let addReversedBasicStack: CardOverflowDb -> Guid -> string list -> (Guid * Guid * Guid * Guid list) -> Task<Result<Guid, string>> =
    add "Basic (and reversed card)" <| normalCommand []

let addBasicStack =
    add "Basic" <| normalCommand []

let addBasicCustomStack fieldValues =
    add "Basic" <| normalCommand fieldValues

let addCloze fieldValues =
    add "Cloze" <| clozeCommand fieldValues

let reversedBasicGromplate db =
    TestGromplateRepo.SearchEarliest db "Basic (and reversed card)"

let basicGromplate db =
    TestGromplateRepo.SearchEarliest db "Basic"

let update (c: TestContainer) authorId kind commandTransformer updateIds expectedBranchId = taskResult {
    let! (upsert: ViewEditStackCommand) = SanitizeStackRepository.getUpsert c.Db kind updateIds // using |>!! is *extremely* inconsistent and unstable for some reason
    return!
        upsert
        |> commandTransformer
        |> SanitizeStackRepository.Update c.Db authorId []
        |>%% Assert.equal expectedBranchId
}

[<Fact>]
let ``StackRepository.CreateCard on a basic facet collects 1 card/facet``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let aTag = Guid.NewGuid().ToString() |> SanitizeTagRepository.sanitize
    let bTag = Guid.NewGuid().ToString() |> SanitizeTagRepository.sanitize
    
    let! _ = addBasicStack c.Db userId [aTag; bTag] (stack_1, branch_1, leaf_1, [card_1])

    Assert.SingleI <| c.Db.Stack
    Assert.SingleI <| c.Db.Stack
    Assert.SingleI <| c.Db.Card
    let! cards = StackRepository.GetQuizBatch c.Db userId ""
    Assert.SingleI cards
    Assert.Equal(
        """<!DOCTYPE html>
    <head>
        <style>
            .cloze-brackets-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-filler-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-brackets-back {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: red;
            }
        </style>
        <style>
            .card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}

        </style>
    </head>
    <body>
        Front
        <script type="text/javascript" src="/js/iframeResizer.contentWindow.min.js"></script> 
    </body>
</html>""",
        cards
        |> Seq.head
        |> Result.getOk
        |> fun x -> x.Front
        , false, true
    )
    Assert.Equal(
        """<!DOCTYPE html>
    <head>
        <style>
            .cloze-brackets-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-filler-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-brackets-back {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: red;
            }
        </style>
        <style>
            .card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}

        </style>
    </head>
    <body>
        Front

<hr id=answer>

Back
        <script type="text/javascript" src="/js/iframeResizer.contentWindow.min.js"></script> 
    </body>
</html>""",
        cards
        |> Seq.head
        |> Result.getOk
        |> fun x -> x.Back
        , false, true
    )
    let! view = StackViewRepository.get c.Db stack_1
    Assert.Equal<FieldAndValue seq>(
        [{  Field = {
                Name = "Front"
                IsRightToLeft = false
                IsSticky = false }
            Value = "Front" }
         {  Field = {
                Name = "Back"
                IsRightToLeft = false
                IsSticky = false }
            Value = "Back"}],
        view.Value.FieldValues
            |> Seq.sortByDescending (fun x -> x.Field.Name)
    )
    Assert.Equal<string seq>(
        [aTag; bTag] |> List.sort,
        (StackRepository.GetCollectedPages c.Db userId 1 "")
            .GetAwaiter()
            .GetResult()
            .Results
            .Single()
            |> Result.getOk
            |> fun x -> x.Tags
            |> List.sort
    )}

[<Fact>]
let ``ExploreStackRepository.leaf works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    let cardId = card_1
    let stackId = stack_1
    let branchId = branch_1
    let oldLeafId = leaf_1
    let newLeafId = leaf_2
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeStackRepository.getUpsert c.Db (VUpdate_BranchId branchId) ((stackId, branchId, newLeafId, [card_2]) |> UpsertIds.fromTuple)
    let updated = {
        old with
            ViewEditStackCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db userId [] updated
    Assert.Equal(branchId, actualBranchId)

    let! (branch1: LeafMeta)    = ExploreStackRepository.get      c.Db userId stackId |> TaskResult.map(fun x -> x.Default.Leaf)
    let! (branch2: LeafMeta), _ = ExploreStackRepository.leaf c.Db userId newLeafId
    Assert.Equal(branch1.InC(), branch2.InC())
    Assert.Equal(newValue                 , branch2.StrippedFront)
    Assert.Equal(newValue + " " + newValue, branch2.StrippedBack)
    let! (card3: LeafMeta), _ = ExploreStackRepository.leaf c.Db userId oldLeafId
    Assert.Equal("Front",      card3.StrippedFront)
    Assert.Equal("Front Back", card3.StrippedBack)

    // nonexistant id
    let nonexistant = Ulid.create
    
    let! (missingCard: Result<_, _>) = ExploreStackRepository.leaf c.Db userId nonexistant
    
    Assert.Equal(sprintf "Branch Leaf #%A not found" nonexistant, missingCard.error)

    // update on branch that's in a nondefault deck with 0 editCardCommands doesn't change the deck
    let newDeckId = Ulid.create
    do! SanitizeDeckRepository.create c.Db userId (Guid.NewGuid().ToString()) newDeckId
    do! SanitizeDeckRepository.switch c.Db userId newDeckId cardId
    let! stackCommand = SanitizeStackRepository.getUpsert c.Db (VUpdate_BranchId branchId) ids_2
    
    do! SanitizeStackRepository.Update c.Db userId [] stackCommand

    let! (card: Card) = StackRepository.GetCollected c.Db userId stackId |>%% Assert.Single
    Assert.equal newDeckId card.DeckId
    } |> TaskResult.getOk)

[<Fact>]
let ``ExploreStackRepository.branch works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    let stackId = stack_1
    let branchId = branch_1
    let oldLeafId = leaf_1
    let newLeafId = leaf_2
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeStackRepository.getUpsert c.Db (VUpdate_BranchId branchId) ((stack_1, branch_1, newLeafId, [card_1]) |> UpsertIds.fromTuple)
    let updated = {
        old with
            ViewEditStackCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db userId [] updated
    Assert.Equal(branchId, actualBranchId)

    let! (branch1: LeafMeta)    = ExploreStackRepository.get      c.Db userId stackId |> TaskResult.map(fun x -> x.Default.Leaf)
    let! (branch2: LeafMeta), _ = ExploreStackRepository.branch   c.Db userId branchId
    Assert.Equal(branch1.InC(), branch2.InC())
    Assert.Equal(newValue                 , branch2.StrippedFront)
    Assert.Equal(newValue + " " + newValue, branch2.StrippedBack)
    Assert.Equal(branchId, branch2.BranchId)
    Assert.Equal(newLeafId, branch2.Id)

    // nonexistant id
    let nonexistant = Ulid.create
    
    let! (missingCard: Result<_, _>) = ExploreStackRepository.branch c.Db userId nonexistant
    
    Assert.Equal(sprintf "Branch #%A not found" nonexistant, missingCard.error)
    } |> TaskResult.getOk)

[<Fact>]
let ``StackViewRepository.leafPair works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let otherUserId = user_2
    let! _ = addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    let! _ = addBasicStack c.Db otherUserId [] (stack_2, branch_2, leaf_2, [card_2])
    
    let! a, (a_: bool), b, (b_:bool) = StackViewRepository.leafPair c.Db leaf_1 leaf_2 userId
    
    Assert.Equal(a.InC(), b.InC())
    Assert.True(a_)
    Assert.False(b_)

    // missing leafId
    let nonexistant = Ulid.create
    let! (x: Result<_, _>) = StackViewRepository.leafPair c.Db leaf_1 nonexistant userId
    
    Assert.equal (sprintf "Branch leaf #%A not found" nonexistant) x.error
    
    let! (x: Result<_, _>) = StackViewRepository.leafPair c.Db nonexistant leaf_1 userId
    
    Assert.equal (sprintf "Branch leaf #%A not found" nonexistant) x.error
    } |> TaskResult.getOk)

[<Fact>]
let ``StackViewRepository.leafWithLatest works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    let stackId = stack_1
    let branchId = branch_1
    let secondVersion = Guid.NewGuid().ToString()
    let updatedLeafId = leaf_2
    do! update c userId
            (VUpdate_BranchId branchId) (fun x -> { x with EditSummary = secondVersion; FieldValues = [].ToList() }) ((stackId, branchId, updatedLeafId, [Ulid.create]) |> UpsertIds.fromTuple) branchId
    let oldLeafId = leaf_1
    do! c.Db.Leaf.SingleAsync(fun x -> x.Id = updatedLeafId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))
    
    let! (a: LeafView), (a_: bool), (b: LeafView), (b_: bool), bId = StackViewRepository.leafWithLatest c.Db oldLeafId userId
    
    do! StackViewRepository.leaf c.Db oldLeafId
        |> TaskResult.map (fun expected -> Assert.Equal(expected.InC(), a.InC()))
    Assert.False a_
    Assert.True b_
    Assert.Empty b.FieldValues
    Assert.Equal(updatedLeafId, bId)

    // works on a new branch
    let newBranchId = branch_2
    let branchVersion = Guid.NewGuid().ToString()
    let leafId = leaf_3
    do! update c userId
            (VNewBranch_SourceStackId stackId) (fun x -> { x with EditSummary = branchVersion }) ((stackId, newBranchId, leafId, [Ulid.create]) |> UpsertIds.fromTuple) newBranchId
    do! c.Db.Leaf.SingleAsync(fun x -> x.Id = leafId)
        |> Task.map (fun x -> Assert.Equal(branchVersion, x.EditSummary))
    
    let! (a: LeafView), (a_: bool), (b: LeafView), (b_: bool), bId = StackViewRepository.leafWithLatest c.Db leafId userId
    
    do! StackViewRepository.leaf c.Db leafId
        |> TaskResult.map (fun expected -> Assert.Equal(expected.InC(), a.InC()))
    Assert.True a_
    Assert.False b_
    Assert.Empty b.FieldValues
    Assert.Equal(updatedLeafId, bId)
    } |> TaskResult.getOk)

[<Fact>]
let ``Leaf with "" as FieldValues is parsed to empty`` (): unit =
    let view =
        LeafEntity(
            FieldValues = "",
            Grompleaf = GrompleafEntity(
                Fields = "FrontArial20False0FalseBackArial20False1False"
            ))
        |> LeafView.load

    Assert.Empty view.FieldValues

[<Fact>]
let ``UpdateRepository.card edit/copy/branch works``() : Task<unit> = task {
    let og_s = stack_1
    let copy_s = stack_2
    let copy2x_s = stack_3
    let copyOfBranch_s = stack_ 4
    
    let og_b = branch_1
    let copy_b = branch_2
    let og_b_2 = branch_3
    let copy2x_b = branch_ 4
    let copyOfBranch_b = branch_ 5
    let branchOfCopy_b = branch_ 6

    let og_i = leaf_1
    let ogEdit_i = leaf_2
    let copy_i = leaf_3
    let branch_i = leaf_ 4 // branch of og_s and og_b_2
    let copy2x_i = leaf_ 5
    let copyOfBranch_i = leaf_ 6
    let branchOfCopy_i = leaf_ 7

    use c = new TestContainer()
    let assertCount (cardsIdsAndCounts: _ list) (branchIdsAndCounts: _ list) (leafIdsAndCounts: _ list) = task {
        //"XXXXXX Stack Count".D()
        do! c.Db.Stack.CountAsync()
            |> Task.map(fun i -> Assert.Equal(cardsIdsAndCounts.Length, i))
        //"XXXXXX Branch Count".D()
        do! c.Db.Branch.CountAsync()
            |> Task.map(fun i -> Assert.Equal(branchIdsAndCounts.Length, i))
        //"XXXXXX Branch Leaf Count".D()
        do! c.Db.Leaf.CountAsync()
            |> Task.map(fun i -> Assert.Equal(leafIdsAndCounts.Length, i))
        for id, count in cardsIdsAndCounts do
            //"XXXXXX".D(sprintf "Stack #%i should have count #%i" id count)
            do! c.Db.Stack.SingleAsync(fun x -> x.Id = id)
                |> Task.map (fun c -> Assert.Equal(count, c.Users))
        for id, count in branchIdsAndCounts do
            //"XXXXXX".D(sprintf "Branch #%i should have count #%i" id count)
            do! c.Db.Branch.SingleAsync(fun x -> x.Id = id)
                |> Task.map (fun c -> Assert.Equal(count, c.Users))
        for id, count in leafIdsAndCounts do
            //"XXXXXX".D(sprintf "Branch leaf #%i should have count #%i" id count)
            do! c.Db.Leaf.SingleAsync(fun x -> x.Id = id)
                |> Task.map (fun c -> Assert.Equal(count, c.Users))}
    let! _ = addBasicStack c.Db user_1 ["A"; "B"] (stack_1, branch_1, leaf_1, [card_1])
    do! assertCount
            [og_s, 1]
            [og_b, 1]
            [og_i, 1]

    // updated by user1
    let newValue = Guid.NewGuid().ToString()
    let! old = (og_s, og_b, ogEdit_i, [Ulid.create]) |> UpsertIds.fromTuple |> SanitizeStackRepository.getUpsert c.Db (VUpdate_BranchId og_b)
    let updated = {
        old.Value with
            FieldValues =
                old.Value.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db user_1 [] updated |> TaskResult.getOk
    Assert.Equal(og_b, actualBranchId)
    do! assertCount
            [og_s, 1]
            [og_b, 1]
            [og_i, 0; ogEdit_i, 1]
    
    let asserts userId stackId branchId leafId newValue leafCountForStack revisionCount tags = task {
        let! leaf = StackViewRepository.leaf c.Db leafId
        Assert.Equal<string seq>(
            [newValue; newValue],
            leaf.Value.FieldValues.Select(fun x -> x.Value))
        Assert.Equal(
            leafCountForStack,
            c.Db.Leaf.Count(fun x -> x.StackId = stackId))
        let! stack = ExploreStackRepository.get c.Db userId stackId
        Assert.areEquivalent
            tags
            stack.Value.Tags
        Assert.areEquivalent
            [newValue; newValue]
            (leaf.Value.FieldValues.Select(fun x -> x.Value))
        let createds = c.Db.Leaf.Select(fun x -> x.Created) |> Seq.toList
        Assert.NotEqual(createds.[0], createds.[1])
        let! revisions = StackRepository.Revisions c.Db userId branchId |> TaskResult.getOk
        Assert.Equal(revisionCount, revisions.SortedMeta.Count())
        let! leaf = StackViewRepository.leaf c.Db revisions.SortedMeta.[0].Id
        let revision, _, _, _ = leaf |> Result.getOk |> fun x -> x.FrontBackFrontSynthBackSynth.[0]
        Assert.Contains(newValue, revision)
    }
    
    do! asserts user_1 og_s og_b ogEdit_i newValue 2 2
            [ { Name = "A"
                Count = 1
                IsCollected = true }
              { Name = "B"
                Count = 1
                IsCollected = true }]
    let! revisions = StackRepository.Revisions c.Db user_1 og_b  |> TaskResult.getOk
    let! leaf = StackViewRepository.leaf c.Db revisions.SortedMeta.[1].Id
    let original, _, _, _ = leaf |> Result.getOk |> fun x -> x.FrontBackFrontSynthBackSynth.[0]
    Assert.Contains("Front", original)
    Assert.True(revisions.SortedMeta.Single(fun x -> x.IsLatest).Id > revisions.SortedMeta.Single(fun x -> not x.IsLatest).Id) // tests that Latest really came after NotLatest
            
    // copy by user2
    let newValue = Guid.NewGuid().ToString()
    let! old = (copy_s, copy_b, copy_i, [Ulid.create]) |> UpsertIds.fromTuple |> SanitizeStackRepository.getUpsert c.Db (VNewCopySource_LeafId ogEdit_i)
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db user_2 [] updated |> TaskResult.getOk
    Assert.Equal(copy_b, actualBranchId)
    do! assertCount
            [og_s, 1;              copy_s, 1]
            [og_b, 1;              copy_b, 1]
            [og_i, 0; ogEdit_i, 1; copy_i, 1]

    do! asserts user_2 copy_s copy_b copy_i newValue 1 1 []

    // missing copy
    let missingLeafId = Ulid.create
    let missingCardId = Ulid.create
    
    let! old = SanitizeStackRepository.getUpsert c.Db (VNewCopySource_LeafId missingLeafId) ids_1
    
    Assert.Equal(sprintf "Branch Leaf #%A not found." missingLeafId, old.error)
    do! assertCount
            [og_s, 1;              copy_s, 1]
            [og_b, 1;              copy_b, 1]
            [og_i, 0; ogEdit_i, 1; copy_i, 1]

    // user2 branchs og_s
    let newValue = Guid.NewGuid().ToString()
    let! old = ((og_s, og_b_2, branch_i, [Ulid.create]) |> UpsertIds.fromTuple) |> SanitizeStackRepository.getUpsert c.Db (VNewBranch_SourceStackId og_s)
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db user_2 [] updated |> TaskResult.getOk
    Assert.Equal(og_b_2, actualBranchId)
    let! x, _ = ExploreStackRepository.leaf c.Db user_2 branch_i |> TaskResult.getOk
    do! asserts user_2 x.StackId x.BranchId x.Id newValue 3 1
            [ { Name = "A"
                Count = 1
                IsCollected = false }
              { Name = "B"
                Count = 1
                IsCollected = false }]
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // user2 branchs missing stack
    let missingStackId = Ulid.create
    let! old = SanitizeStackRepository.getUpsert c.Db (VNewBranch_SourceStackId missingStackId) { ids_1 with StackId = missingStackId }
    Assert.Equal(sprintf "Stack #%A not found." missingStackId, old.error)
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // user2 copies their copy
    let! x = ((copy2x_s, copy2x_b, copy2x_i, [Ulid.create]) |> UpsertIds.fromTuple) |> SanitizeStackRepository.getUpsert c.Db (VNewCopySource_LeafId copy_i)
    let old = x.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db user_2 [] updated |> TaskResult.getOk
    Assert.Equal(copy2x_b, actualBranchId)
    let! x, _ = ExploreStackRepository.leaf c.Db user_2 copy2x_i |> TaskResult.getOk
    do! asserts user_2 x.StackId x.BranchId x.Id newValue 1 1 []
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ;    copy2x_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;    copy2x_b, 1 ;
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;    copy2x_i, 1 ;
             ogEdit_i, 1 ;
             branch_i, 1 ]
    
    // user2 copies their branch
    let! old = ((copyOfBranch_s, copyOfBranch_b, copyOfBranch_i, [Ulid.create]) |> UpsertIds.fromTuple) |> SanitizeStackRepository.getUpsert c.Db (VNewCopySource_LeafId branch_i)
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = SanitizeStackRepository.Update c.Db user_2 [] updated |> TaskResult.getOk
    Assert.Equal(copyOfBranch_b, actualBranchId)
    let! x, _ = ExploreStackRepository.leaf c.Db user_2 copyOfBranch_i |> TaskResult.getOk
    do! asserts user_2 x.StackId x.BranchId x.Id newValue 1 1 []
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ;    copy2x_s, 1 ;    copyOfBranch_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;    copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;    copy2x_i, 1 ;    copyOfBranch_i, 1
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // user2 branches their copy
    let! old = ((copy_s, branchOfCopy_b, branchOfCopy_i, [Ulid.create]) |> UpsertIds.fromTuple) |> SanitizeStackRepository.getUpsert c.Db (VNewBranch_SourceStackId copy_s)
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = user_2))
    let! actualBranchId = SanitizeStackRepository.Update c.Db user_2 [] updated |> TaskResult.getOk
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = user_2))
    Assert.Equal(branchOfCopy_b, actualBranchId)
    let! x, _ = ExploreStackRepository.leaf c.Db user_2 branchOfCopy_i |> TaskResult.getOk
    do! asserts user_2 x.StackId x.BranchId x.Id newValue 2 1 []
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfBranch_s, 1 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 1
             branch_i, 1 ]

    // adventures in collecting cards
    let adventurerId = user_3
    let! _ = StackRepository.CollectCard c.Db adventurerId og_i [ Ulid.create ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfBranch_s, 1 ]
            [og_b,     2 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     1 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    let! _ = StackRepository.CollectCard c.Db adventurerId ogEdit_i [ Ulid.create ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfBranch_s, 1 ]
            [og_b,     2 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    let! _ = StackRepository.CollectCard c.Db adventurerId copy_i [ Ulid.create ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 1 ;    copyOfBranch_s, 1 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    let! _ = StackRepository.CollectCard c.Db adventurerId copy2x_i [ Ulid.create ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 1 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    let! _ = StackRepository.CollectCard c.Db adventurerId copyOfBranch_i [ Ulid.create ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = adventurerId))
    let! _ = StackRepository.CollectCard c.Db adventurerId branch_i [ Ulid.create ] |> TaskResult.getOk
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     1 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = adventurerId))
    let! _ = StackRepository.CollectCard c.Db adventurerId branchOfCopy_i [ Ulid.create ] |> TaskResult.getOk
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    // adventures in implicit uncollecting
    let adventurerId = user_1 // changing the adventurer!
    let! cc = c.Db.Card.SingleAsync(fun x -> x.LeafId = ogEdit_i && x.UserId = adventurerId)
    do! StackRepository.uncollectStack c.Db adventurerId cc.StackId |> TaskResult.getOk
    Assert.Equal(0, c.Db.Card.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_s,     2 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    let! _ = StackRepository.CollectCard c.Db adventurerId ogEdit_i [ Ulid.create ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    let! _ = StackRepository.CollectCard c.Db adventurerId og_i [ Ulid.create ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     1 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    let! _ = StackRepository.CollectCard c.Db adventurerId branch_i [ Ulid.create ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   3 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 3 ]
    // adventures in uncollecting and suspending
    let adventurerId = user_2 // changing the adventurer, again!
    let! cc = c.Db.Card.SingleAsync(fun x -> x.StackId = og_s && x.UserId = adventurerId)
    do! StackRepository.editState c.Db adventurerId cc.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_s,     2 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    do! StackRepository.uncollectStack c.Db adventurerId cc.StackId |> TaskResult.getOk
    do! assertCount
            [og_s,     2 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    let! cc = c.Db.Card.SingleAsync(fun x -> x.StackId = copy_s && x.UserId = adventurerId)
    do! StackRepository.uncollectStack c.Db adventurerId cc.StackId |> TaskResult.getOk
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    let! cc = c.Db.Card.SingleAsync(fun x -> x.StackId = copy2x_s && x.UserId = adventurerId)
    do! StackRepository.editState c.Db adventurerId cc.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    do! StackRepository.uncollectStack c.Db adventurerId cc.StackId |> TaskResult.getOk
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    let adventurerId = user_3 // and another change!
    let! cc = c.Db.Card.SingleAsync(fun x -> x.StackId = copy2x_s && x.UserId = adventurerId)
    do! StackRepository.editState c.Db adventurerId cc.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 0 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    do! StackRepository.uncollectStack c.Db adventurerId cc.StackId |> TaskResult.getOk
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 0 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    let! cc = c.Db.Card.SingleAsync(fun x -> x.StackId = copyOfBranch_s && x.UserId = adventurerId)
    do! StackRepository.uncollectStack c.Db adventurerId cc.StackId |> TaskResult.getOk
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 0 ;    copyOfBranch_s, 1 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfBranch_b, 1
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    }

[<Fact>]
let ``ExploreStackRepository.get works for all ExploreStackCollectedStatus``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let testGetCollected stackId leafId =
        StackRepository.GetCollected c.Db userId stackId
        |> TaskResult.map (fun cc -> Assert.Equal(leafId, cc.Single().LeafMeta.Id))

    let! _ = addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    let og_s = stack_1
    let og_b = branch_1
    let og_i = leaf_1

    // tests ExactLeafCollected
    do! ExploreStackRepository.get c.Db userId og_s
        |> TaskResult.map (fun card -> Assert.Equal({ StackId = og_s; BranchId = og_b; LeafId = og_i }, card.CollectedIds.Value))
    do! testGetCollected og_s og_i
    
    // update card
    let update_i = leaf_2
    let! (command: ViewEditStackCommand) = SanitizeStackRepository.getUpsert c.Db (VUpdate_BranchId og_b) ((og_s, og_b, update_i, [Ulid.create]) |> UpsertIds.fromTuple)
    let! actualBranchId = SanitizeStackRepository.Update c.Db userId [] command
    Assert.Equal(og_b, actualBranchId)

    // tests ExactLeafCollected
    do! ExploreStackRepository.get c.Db userId og_s
        |> TaskResult.map (fun card -> Assert.Equal({ StackId = og_s; BranchId = og_b; LeafId = update_i }, card.CollectedIds.Value))
    do! testGetCollected og_s update_i

    // collecting old leaf doesn't change LatestId
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single().DefaultBranch.LatestId)
    do! StackRepository.CollectCard c.Db userId og_i [ Ulid.create ]
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single().DefaultBranch.LatestId)

    // tests OtherLeafCollected
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.CollectedIds with
    | Some x -> Assert.Equal({ StackId = og_s; BranchId = og_b; LeafId = og_i }, x)
    | _ -> failwith "impossible"
    do! testGetCollected og_s og_i

    // branch card
    let branch_i = leaf_3
    let branch_b = branch_2
    let! (command: ViewEditStackCommand) = SanitizeStackRepository.getUpsert c.Db (VNewBranch_SourceStackId og_s) ((og_s, branch_b, branch_i, [Ulid.create]) |> UpsertIds.fromTuple)
    let! actualBranchId = SanitizeStackRepository.Update c.Db userId [] command
    Assert.Equal(branch_b, actualBranchId)
    
    // tests LatestBranchCollected
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.CollectedIds with
    | Some x -> Assert.Equal({ StackId = og_s; BranchId = branch_b; LeafId = branch_i }, x)
    | _ -> failwith "impossible"
    do! testGetCollected og_s branch_i

    // update branch
    let updateBranch_i = leaf_ 4
    let! (command: ViewEditStackCommand) = SanitizeStackRepository.getUpsert c.Db (VUpdate_BranchId branch_b) ((og_s, branch_2, updateBranch_i, [Ulid.create]) |> UpsertIds.fromTuple)
    let! actualBranchId = SanitizeStackRepository.Update c.Db userId [] command
    Assert.Equal(branch_b, actualBranchId)

    // tests LatestBranchCollected
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.CollectedIds with
    | Some x -> Assert.Equal({ StackId = og_s; BranchId = branch_b; LeafId = updateBranch_i }, x)
    | _ -> failwith "impossible"
    do! testGetCollected og_s updateBranch_i

    // collecting old leaf doesn't change LatestId
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_s).Branches.Single().LatestId)
    Assert.Equal(updateBranch_i, c.Db.Stack.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_s).Branches.Single(fun x -> x.Id = branch_b).LatestId)
    do! StackRepository.CollectCard c.Db userId branch_i [ Ulid.create ]
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_s).Branches.Single().LatestId)
    Assert.Equal(updateBranch_i, c.Db.Stack.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_s).Branches.Single(fun x -> x.Id = branch_b).LatestId)

    // tests OtherBranchCollected
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.CollectedIds with
    | Some x -> Assert.Equal({ StackId = og_s; BranchId = branch_b; LeafId = branch_i }, x)
    | _ -> failwith "impossible"
    do! testGetCollected og_s branch_i

    // try to branch card again, but fail
    let! (command: ViewEditStackCommand) = SanitizeStackRepository.getUpsert c.Db (VNewBranch_SourceStackId og_s) ids_1
    let! (error: Result<_,_>) = SanitizeStackRepository.Update c.Db userId [] command
    Assert.equal (sprintf "Stack #%A already has a Branch named 'New Branch'." og_s) error.error

    // branch card again
    let branch_i2 = leaf_ 5
    let branch_b2 = branch_3
    let! (command: ViewEditStackCommand) = SanitizeStackRepository.getUpsert c.Db (VNewBranch_SourceStackId og_s) ((og_s, branch_b2, branch_i2, [Ulid.create]) |> UpsertIds.fromTuple)
    let command = { command with Title = Guid.NewGuid().ToString() }
    let! actualBranchId = SanitizeStackRepository.Update c.Db userId [] command
    Assert.Equal(branch_b2, actualBranchId)

    // tests LatestBranchCollected
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.CollectedIds with
    | Some x -> Assert.Equal({ StackId = og_s; BranchId = branch_b2; LeafId = branch_i2 }, x)
    | _ -> failwith "impossible"
    do! testGetCollected og_s branch_i2

    // tests LatestBranchCollected with og_s
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.CollectedIds with
    | Some x -> Assert.Equal({ StackId = og_s; BranchId = branch_b2; LeafId = branch_i2 }, x)
    | _ -> failwith "impossible"
    do! testGetCollected og_s branch_i2

    // collecting old leaf doesn't change LatestId; can also collect old branch
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_s).Branches.Single().LatestId)
    Assert.Equal(updateBranch_i, c.Db.Stack.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_s).Branches.Single(fun x -> x.Id = branch_b).LatestId)
    do! StackRepository.CollectCard c.Db userId branch_i [ Ulid.create ]
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_s).Branches.Single().LatestId)
    Assert.Equal(updateBranch_i, c.Db.Stack.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_s).Branches.Single(fun x -> x.Id = branch_b).LatestId)

    // can't collect missing id
    let missingId = Ulid.create
    let! (error: Result<_,_>) = StackRepository.CollectCard c.Db userId missingId [ Ulid.create ]
    Assert.Equal(sprintf "Branch Leaf #%A not found" missingId, error.error)

    // tests NotCollected
    let otherUser = user_1
    let! stack = ExploreStackRepository.get c.Db otherUser og_s
    Assert.Equal(None, stack.CollectedIds)
    } |> TaskResult.getOk)

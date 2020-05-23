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
open CardOverflow.Pure.Core
open System.Collections.Generic
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Sanitation
open FsToolkit.ErrorHandling

let normalCommand fieldValues collateInstance tagIds =
    let fieldValues =
        match fieldValues with
        | [] -> ["Front"; "Back"]
        | _ -> fieldValues
    {   CollateInstance = collateInstance
        FieldValues =
            collateInstance.Fields
            |> Seq.mapi (fun i field -> {
                EditField = ViewField.copyTo field
                Value = fieldValues.[i]
            }) |> toResizeArray
        EditSummary = "Initial creation"
        Kind = NewOriginal_TagIds tagIds
        Title = null
    }

let clozeCommand clozeText (clozeCollate: ViewCollateInstance) tagIds = {
    EditSummary = "Initial creation"
    FieldValues =
        clozeCollate.Fields.Select(fun f -> {
            EditField = ViewField.copyTo f
            Value =
                if f.Name = "Text" then
                    clozeText
                else
                    "extra"
        }).ToList()
    CollateInstance = clozeCollate
    Kind = NewOriginal_TagIds tagIds
    Title = null }

let add collateName createCommand (db: CardOverflowDb) userId tags = task {
    let tagIds = ResizeArray.empty
    for tag in tags do
        let! tagId = SanitizeTagRepository.upsert db tag |> TaskResult.getOk
        tagIds.Add tagId
    let! collate = TestCollateRepo.SearchEarliest db collateName
    let! r =
        createCommand collate (tagIds |> List.ofSeq)
        |> SanitizeCardRepository.Update db userId
    return r.Value
    }

let addReversedBasicCard: CardOverflowDb -> int -> string list -> Task<int> =
    add "Basic (and reversed card)" <| normalCommand []

let addBasicCard =
    add "Basic" <| normalCommand []

let addBasicCustomCard fieldValues =
    add "Basic" <| normalCommand fieldValues

let addCloze fieldValues =
    add "Cloze" <| clozeCommand fieldValues

[<Fact>]
let ``StackRepository.CreateCard on a basic facet acquires 1 card/facet``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let aTag = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    let bTag = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    
    let! _ = addBasicCard c.Db userId [aTag; bTag]

    Assert.SingleI <| c.Db.Stack
    Assert.SingleI <| c.Db.Stack
    Assert.SingleI <| c.Db.AcquiredCard
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
    let! view = StackViewRepository.get c.Db 1
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
        (StackRepository.GetAcquiredPages c.Db userId 1 "")
            .GetAwaiter()
            .GetResult()
            .Results
            .Single()
            |> Result.getOk
            |> fun x -> x.Tags
            |> List.sort
    )}

[<Fact>]
let ``ExploreStackRepository.getInstance works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let! _ = addBasicCard c.Db userId []
    let stackId = 1
    let branchId = 1
    let oldBranchInstanceId = 1001
    let newBranchInstanceId = 1002
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeCardRepository.getUpsert c.Db (VUpdateBranchId branchId)
    let updated = {
        old with
            ViewEditCardCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = UpdateRepository.card c.Db userId updated.load
    Assert.Equal(branchId, actualBranchId)

    let! (branch1: BranchInstanceMeta)    = ExploreStackRepository.get      c.Db userId stackId |> TaskResult.map(fun x -> x.Instance)
    let! (branch2: BranchInstanceMeta), _ = ExploreStackRepository.instance c.Db userId newBranchInstanceId
    Assert.Equal(branch1.InC(), branch2.InC())
    Assert.Equal(newValue                 , branch2.StrippedFront)
    Assert.Equal(newValue + " " + newValue, branch2.StrippedBack)
    let! (card3: BranchInstanceMeta), _ = ExploreStackRepository.instance c.Db userId oldBranchInstanceId
    Assert.Equal("Front",      card3.StrippedFront)
    Assert.Equal("Front Back", card3.StrippedBack)

    // nonexistant id
    let nonexistant = 1337
    
    let! (missingCard: Result<_, _>) = ExploreStackRepository.instance c.Db userId nonexistant
    
    Assert.Equal(sprintf "Branch Instance #%i not found" nonexistant, missingCard.error)
    } |> TaskResult.getOk)

[<Fact>]
let ``StackViewRepository.instancePair works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let otherUserId = 2
    let! _ = addBasicCard c.Db userId []
    let! _ = addBasicCard c.Db otherUserId []
    
    let! a, (a_: bool), b, (b_:bool) = StackViewRepository.instancePair c.Db 1001 1002 userId
    
    Assert.Equal(a.InC(), b.InC())
    Assert.True(a_)
    Assert.False(b_)

    // missing instanceId
    let! (x: Result<_, _>) = StackViewRepository.instancePair c.Db 1001 -1 userId
    
    Assert.Equal("Branch instance #-1 not found", x.error)
    
    let! (x: Result<_, _>) = StackViewRepository.instancePair c.Db -1 1001 userId
    
    Assert.Equal("Branch instance #-1 not found", x.error)
    } |> TaskResult.getOk)

[<Fact>]
let ``StackViewRepository.instanceWithLatest works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let! _ = addBasicCard c.Db userId []
    let branchId = 1
    let! collate =
        TestCollateRepo.Search c.Db "Basic"
        |> Task.map (fun x -> x.Single(fun x -> x.Name = "Basic"))
    let secondVersion = Guid.NewGuid().ToString()
    let! _ =
        {   EditStackCommand.EditSummary = secondVersion
            FieldValues = [].ToList()
            CollateInstance = collate |> ViewCollateInstance.copyTo
            Kind = Update_BranchId_Title (branchId, null)
        } |> UpdateRepository.card c.Db userId
    let oldInstanceId = 1001
    let updatedInstanceId = 1002
    do! c.Db.BranchInstance.SingleAsync(fun x -> x.Id = updatedInstanceId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))
    
    let! (a: BranchInstanceView), (a_: bool), (b: BranchInstanceView), (b_: bool), bId = StackViewRepository.instanceWithLatest c.Db 1001 userId
    
    do! StackViewRepository.instance c.Db oldInstanceId
        |> TaskResult.map (fun expected -> Assert.Equal(expected.InC(), a.InC()))
    Assert.False a_
    Assert.True b_
    Assert.Empty b.FieldValues
    Assert.Equal(updatedInstanceId, bId)
    } |> TaskResult.getOk)

[<Fact>]
let ``BranchInstance with "" as FieldValues is parsed to empty`` (): unit =
    let view =
        BranchInstanceEntity(
            FieldValues = "",
            CollateInstance = CollateInstanceEntity(
                Fields = "FrontArial20False0FalseBackArial20False1False"
            ))
        |> BranchInstanceView.load

    Assert.Empty view.FieldValues

[<Fact>]
let ``UpdateRepository.card edit/copy/branch works``() : Task<unit> = task {
    let og_s = 1
    let copy_s = 2
    let copy2x_s = 3
    let copyOfBranch_s = 4
    
    let og_b = 1
    let copy_b = 2
    let og_b_2 = 3
    let copy2x_b = 4
    let copyOfBranch_b = 5
    let branchOfCopy_b = 6

    let og_i = 1001
    let ogEdit_i = 1002
    let copy_i = 1003
    let branch_i = 1004 // branch of og_s and og_b_2
    let copy2x_i = 1005
    let copyOfBranch_i = 1006
    let branchOfCopy_i = 1007

    let user1 = 1
    let user2 = 2
    
    use c = new TestContainer()
    let assertCount (cardsIdsAndCounts: _ list) (branchIdsAndCounts: _ list) (branchInstanceIdsAndCounts: _ list) = task {
        //"XXXXXX Stack Count".D()
        do! c.Db.Stack.CountAsync()
            |> Task.map(fun i -> Assert.Equal(cardsIdsAndCounts.Length, i))
        //"XXXXXX Branch Count".D()
        do! c.Db.Branch.CountAsync()
            |> Task.map(fun i -> Assert.Equal(branchIdsAndCounts.Length, i))
        //"XXXXXX Branch Instance Count".D()
        do! c.Db.BranchInstance.CountAsync()
            |> Task.map(fun i -> Assert.Equal(branchInstanceIdsAndCounts.Length, i))
        for id, count in cardsIdsAndCounts do
            //"XXXXXX".D(sprintf "Stack #%i should have count #%i" id count)
            do! c.Db.Stack.SingleAsync(fun x -> x.Id = id)
                |> Task.map (fun c -> Assert.Equal(count, c.Users))
        for id, count in branchIdsAndCounts do
            //"XXXXXX".D(sprintf "Branch #%i should have count #%i" id count)
            do! c.Db.Branch.SingleAsync(fun x -> x.Id = id)
                |> Task.map (fun c -> Assert.Equal(count, c.Users))
        for id, count in branchInstanceIdsAndCounts do
            //"XXXXXX".D(sprintf "Branch instance #%i should have count #%i" id count)
            do! c.Db.BranchInstance.SingleAsync(fun x -> x.Id = id)
                |> Task.map (fun c -> Assert.Equal(count, c.Users))}
    let! _ = addBasicCard c.Db user1 ["A"; "B"]
    do! assertCount
            [og_s, 1]
            [og_b, 1]
            [og_i, 1]

    // updated by user1
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeCardRepository.getUpsert c.Db <| VUpdateBranchId og_b
    let updated = {
        old.Value with
            FieldValues =
                old.Value.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = UpdateRepository.card c.Db user1 updated.load |> TaskResult.getOk
    Assert.Equal(og_b, actualBranchId)
    do! assertCount
            [og_s, 1]
            [og_b, 1]
            [og_i, 0; ogEdit_i, 1]
    
    let asserts userId stackId branchId instanceId newValue instanceCountForStack revisionCount tags = task {
        let! instance = StackViewRepository.instance c.Db instanceId
        Assert.Equal<string seq>(
            [newValue; newValue],
            instance.Value.FieldValues.Select(fun x -> x.Value))
        Assert.Equal(
            instanceCountForStack,
            c.Db.BranchInstance.Count(fun x -> x.StackId = stackId))
        let! stack = ExploreStackRepository.get c.Db userId stackId
        Assert.Equal<ViewTag seq>(
            tags,
            stack.Value.Tags)
        Assert.Equal<string seq>(
            [newValue; newValue],
            instance.Value.FieldValues.Select(fun x -> x.Value)
        )
        let createds = c.Db.BranchInstance.Select(fun x -> x.Created) |> Seq.toList
        Assert.NotEqual(createds.[0], createds.[1])
        let! revisions = StackRepository.Revisions c.Db userId branchId
        Assert.Equal(revisionCount, revisions.SortedMeta.Count())
        let! instance = StackViewRepository.instance c.Db revisions.SortedMeta.[0].Id
        let revision, _, _, _ = instance |> Result.getOk |> fun x -> x.FrontBackFrontSynthBackSynth.[0]
        Assert.Contains(newValue, revision)
    }
    
    do! asserts user1 og_s og_b ogEdit_i newValue 2 2
            [ { Name = "A"
                Count = 1
                IsAcquired = true }
              { Name = "B"
                Count = 1
                IsAcquired = true }]
    let! revisions = StackRepository.Revisions c.Db user1 og_b
    let! instance = StackViewRepository.instance c.Db revisions.SortedMeta.[1].Id
    let original, _, _, _ = instance |> Result.getOk |> fun x -> x.FrontBackFrontSynthBackSynth.[0]
    Assert.Contains("Front", original)
    Assert.True(revisions.SortedMeta.Single(fun x -> x.IsLatest).Id > revisions.SortedMeta.Single(fun x -> not x.IsLatest).Id) // tests that Latest really came after NotLatest
            
    // copy by user2
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeCardRepository.getUpsert c.Db <| VNewCopySourceInstanceId ogEdit_i
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = UpdateRepository.card c.Db user2 updated.load |> TaskResult.getOk
    Assert.Equal(copy_b, actualBranchId)
    do! assertCount
            [og_s, 1;              copy_s, 1]
            [og_b, 1;              copy_b, 1]
            [og_i, 0; ogEdit_i, 1; copy_i, 1]

    do! asserts user2 copy_s copy_b copy_i newValue 1 1 []

    // missing copy
    let missingInstanceId = 1337
    let missingCardId = 1337
    
    let! old = SanitizeCardRepository.getUpsert c.Db <| VNewCopySourceInstanceId missingInstanceId
    
    Assert.Equal(sprintf "Branch Instance #%i not found." missingInstanceId, old.error)
    do! assertCount
            [og_s, 1;              copy_s, 1]
            [og_b, 1;              copy_b, 1]
            [og_i, 0; ogEdit_i, 1; copy_i, 1]

    // user2 branchs og_s
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeCardRepository.getUpsert c.Db <| VNewBranchSourceStackId og_s
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = UpdateRepository.card c.Db user2 updated.load |> TaskResult.getOk
    Assert.Equal(og_b_2, actualBranchId)
    let! x, _ = ExploreStackRepository.instance c.Db user2 branch_i |> TaskResult.getOk
    do! asserts user2 x.StackId x.BranchId x.Id newValue 3 1
            [ { Name = "A"
                Count = 1
                IsAcquired = false }
              { Name = "B"
                Count = 1
                IsAcquired = false }]
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // user2 branchs missing card
    let! old = SanitizeCardRepository.getUpsert c.Db <| VNewBranchSourceStackId missingCardId
    Assert.Equal(sprintf "Stack #%i not found." missingInstanceId, old.error)
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // user2 copies their copy
    let! x = SanitizeCardRepository.getUpsert c.Db <| VNewCopySourceInstanceId copy_i
    let old = x.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = UpdateRepository.card c.Db user2 updated.load |> TaskResult.getOk
    Assert.Equal(copy2x_b, actualBranchId)
    let! x, _ = ExploreStackRepository.instance c.Db user2 copy2x_i |> TaskResult.getOk
    do! asserts user2 x.StackId x.BranchId x.Id newValue 1 1 []
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ;    copy2x_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;    copy2x_b, 1 ;
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;    copy2x_i, 1 ;
             ogEdit_i, 1 ;
             branch_i, 1 ]
    
    // user2 copies their branch
    let! old = SanitizeCardRepository.getUpsert c.Db <| VNewCopySourceInstanceId branch_i
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualBranchId = UpdateRepository.card c.Db user2 updated.load |> TaskResult.getOk
    Assert.Equal(copyOfBranch_b, actualBranchId)
    let! x, _ = ExploreStackRepository.instance c.Db user2 copyOfBranch_i |> TaskResult.getOk
    do! asserts user2 x.StackId x.BranchId x.Id newValue 1 1 []
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ;    copy2x_s, 1 ;    copyOfBranch_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;    copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;    copy2x_i, 1 ;    copyOfBranch_i, 1
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // user2 branches their copy
    let! old = SanitizeCardRepository.getUpsert c.Db <| VNewBranchSourceStackId copy_s
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    Assert.Equal(4, c.Db.AcquiredCard.Count(fun x -> x.UserId = user2))
    let! actualBranchId = UpdateRepository.card c.Db user2 updated.load |> TaskResult.getOk
    Assert.Equal(4, c.Db.AcquiredCard.Count(fun x -> x.UserId = user2))
    Assert.Equal(branchOfCopy_b, actualBranchId)
    let! x, _ = ExploreStackRepository.instance c.Db user2 branchOfCopy_i |> TaskResult.getOk
    do! asserts user2 x.StackId x.BranchId x.Id newValue 2 1 []
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfBranch_s, 1 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 1
             branch_i, 1 ]

    // adventures in acquiring cards
    let adventurerId = 3
    do! StackRepository.AcquireCardAsync c.Db adventurerId og_i |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfBranch_s, 1 ]
            [og_b,     2 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     1 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    do! StackRepository.AcquireCardAsync c.Db adventurerId ogEdit_i |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfBranch_s, 1 ]
            [og_b,     2 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    do! StackRepository.AcquireCardAsync c.Db adventurerId copy_i |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 1 ;    copyOfBranch_s, 1 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    do! StackRepository.AcquireCardAsync c.Db adventurerId copy2x_i |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 1 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    do! StackRepository.AcquireCardAsync c.Db adventurerId copyOfBranch_i |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    Assert.Equal(4, c.Db.AcquiredCard.Count(fun x -> x.UserId = adventurerId))
    do! StackRepository.AcquireCardAsync c.Db adventurerId branch_i |> TaskResult.getOk
    Assert.Equal(4, c.Db.AcquiredCard.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     1 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    Assert.Equal(4, c.Db.AcquiredCard.Count(fun x -> x.UserId = adventurerId))
    do! StackRepository.AcquireCardAsync c.Db adventurerId branchOfCopy_i |> TaskResult.getOk
    Assert.Equal(4, c.Db.AcquiredCard.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    // adventures in implicit unacquiring
    let adventurerId = 1 // changing the adventurer!
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.BranchInstanceId = ogEdit_i && x.UserId = adventurerId)
    do! StackRepository.UnacquireCardAsync c.Db ac.Id
    Assert.Equal(0, c.Db.AcquiredCard.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_s,     2 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    do! StackRepository.AcquireCardAsync c.Db adventurerId ogEdit_i |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    do! StackRepository.AcquireCardAsync c.Db adventurerId og_i |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     1 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    do! StackRepository.AcquireCardAsync c.Db adventurerId branch_i |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   3 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 3 ]
    // adventures in unacquiring and suspending
    let adventurerId = 2 // changing the adventurer, again!
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.StackId = og_s && x.UserId = adventurerId)
    do! StackRepository.editState c.Db adventurerId ac.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_s,     2 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    do! StackRepository.UnacquireCardAsync c.Db ac.Id
    do! assertCount
            [og_s,     2 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.StackId = copy_s && x.UserId = adventurerId)
    do! StackRepository.UnacquireCardAsync c.Db ac.Id
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 2 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.StackId = copy2x_s && x.UserId = adventurerId)
    do! StackRepository.editState c.Db adventurerId ac.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    do! StackRepository.UnacquireCardAsync c.Db ac.Id
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    let adventurerId = 3 // and another change!
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.StackId = copy2x_s && x.UserId = adventurerId)
    do! StackRepository.editState c.Db adventurerId ac.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 0 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    do! StackRepository.UnacquireCardAsync c.Db ac.Id
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 0 ;    copyOfBranch_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.StackId = copyOfBranch_s && x.UserId = adventurerId)
    do! StackRepository.UnacquireCardAsync c.Db ac.Id
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 0 ;    copyOfBranch_s, 1 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfBranch_b, 1
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    }

[<Fact>]
let ``ExploreStackRepository.get works for all ExploreCardAcquiredStatus``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let testGetAcquired stackId instanceId =
        StackRepository.GetAcquired c.Db userId stackId
        |> TaskResult.map (fun ac -> Assert.Equal(instanceId, ac.Single().BranchInstanceMeta.Id))

    let! _ = addBasicCard c.Db userId []
    let og_s = 1
    let og_b = 1
    let og_i = 1001

    // tests ExactInstanceAcquired
    do! ExploreStackRepository.get c.Db userId og_s
        |> TaskResult.map (fun card -> Assert.Equal(ExactInstanceAcquired og_i, card.AcquiredStatus))
    do! testGetAcquired og_s og_i
    
    // update card
    let update_i = 1002
    let! (command: ViewEditCardCommand) = SanitizeCardRepository.getUpsert c.Db <| VUpdateBranchId og_b
    let! actualBranchId = UpdateRepository.card c.Db userId command.load
    Assert.Equal(og_b, actualBranchId)

    // tests ExactInstanceAcquired
    do! ExploreStackRepository.get c.Db userId og_s
        |> TaskResult.map (fun card -> Assert.Equal(ExactInstanceAcquired update_i, card.AcquiredStatus))
    do! testGetAcquired og_s update_i

    // acquiring old instance doesn't change LatestInstanceId
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single().DefaultBranch.LatestInstanceId)
    do! StackRepository.AcquireCardAsync c.Db userId og_i
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single().DefaultBranch.LatestInstanceId)

    // tests OtherInstanceAcquired
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.AcquiredStatus with
    | OtherInstanceAcquired x -> Assert.Equal(og_i, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_s og_i

    // branch card
    let branch_i = 1003
    let branch_b = 2
    let! (command: ViewEditCardCommand) = SanitizeCardRepository.getUpsert c.Db <| VNewBranchSourceStackId og_s
    let! actualBranchId = UpdateRepository.card c.Db userId command.load
    Assert.Equal(branch_b, actualBranchId)
    
    // tests LatestBranchAcquired
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.AcquiredStatus with
    | LatestBranchAcquired x -> Assert.Equal(branch_i, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_s branch_i

    // update branch
    let updateBranch_i = 1004
    let! (command: ViewEditCardCommand) = SanitizeCardRepository.getUpsert c.Db <| VUpdateBranchId branch_b
    let! actualBranchId = UpdateRepository.card c.Db userId command.load
    Assert.Equal(branch_b, actualBranchId)

    // tests LatestBranchAcquired
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.AcquiredStatus with
    | LatestBranchAcquired x -> Assert.Equal(updateBranch_i, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_s updateBranch_i

    // acquiring old instance doesn't change LatestInstanceId
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_s).Branches.Single().LatestInstanceId)
    Assert.Equal(updateBranch_i, c.Db.Stack.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_s).Branches.Single(fun x -> x.Id = branch_b).LatestInstanceId)
    do! StackRepository.AcquireCardAsync c.Db userId branch_i
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_s).Branches.Single().LatestInstanceId)
    Assert.Equal(updateBranch_i, c.Db.Stack.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_s).Branches.Single(fun x -> x.Id = branch_b).LatestInstanceId)

    // tests OtherBranchAcquired
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.AcquiredStatus with
    | OtherBranchAcquired x -> Assert.Equal(branch_i, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_s branch_i

    // try to branch card again, but fail
    let! (command: ViewEditCardCommand) = SanitizeCardRepository.getUpsert c.Db <| VNewBranchSourceStackId og_s
    let! (error: Result<_,_>) = UpdateRepository.card c.Db userId command.load
    Assert.Equal(sprintf "Stack #1 already has a Branch named 'New Branch'.", error.error);

    // branch card again
    let branch_i2 = 1005
    let branch_b2 = 3
    let! (command: ViewEditCardCommand) = SanitizeCardRepository.getUpsert c.Db <| VNewBranchSourceStackId og_s
    let command =
        { command.load with
            Kind =
                match command.load.Kind with
                | NewBranch_SourceStackId_Title (id, name) -> NewBranch_SourceStackId_Title (id, name + Guid.NewGuid().ToString())
                | _ -> failwith "impossible"
        }
    let! actualBranchId = UpdateRepository.card c.Db userId command
    Assert.Equal(branch_b2, actualBranchId)

    // tests LatestBranchAcquired
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.AcquiredStatus with
    | LatestBranchAcquired x -> Assert.Equal(branch_i2, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_s branch_i2

    // tests LatestBranchAcquired with og_s
    let! stack = ExploreStackRepository.get c.Db userId og_s
    match stack.AcquiredStatus with
    | LatestBranchAcquired x -> Assert.Equal(branch_i2, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_s branch_i2

    // acquiring old instance doesn't change LatestInstanceId; can also acquire old branch
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_s).Branches.Single().LatestInstanceId)
    Assert.Equal(updateBranch_i, c.Db.Stack.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_s).Branches.Single(fun x -> x.Id = branch_b).LatestInstanceId)
    do! StackRepository.AcquireCardAsync c.Db userId branch_i
    Assert.Equal(update_i, c.Db.Stack.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_s).Branches.Single().LatestInstanceId)
    Assert.Equal(updateBranch_i, c.Db.Stack.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_s).Branches.Single(fun x -> x.Id = branch_b).LatestInstanceId)

    // can't acquire missing id
    let missingId = 9001
    let! (error: Result<_,_>) = StackRepository.AcquireCardAsync c.Db userId missingId
    Assert.Equal(sprintf "Branch Instance #%i not found" missingId, error.error)

    // tests NotAcquired
    let otherUser = 1
    let! stack = ExploreStackRepository.get c.Db otherUser og_s
    Assert.Equal(NotAcquired, stack.AcquiredStatus)
    } |> TaskResult.getOk)
    
// fuck merge
//[<Fact>]
//let ``StackRepository's SaveCards updates a Card``() =
//    use c = new TestContainer()
//    let facet = 
//        StackEntity(
//            Title = "",
//            Description = "",
//            Fields = "",
//            CollateId = 1,
//            Modified = DateTime.UtcNow)
    
//    StackRepository.AddCard c.Db facet

//    let updatedCard = StackRepository.GetCards c.Db |> Seq.head
//    let updatedTitle = Guid.NewGuid().ToString()
//    updatedCard.Title <- updatedTitle

//    updatedCard 
//    |> Seq.singleton 
//    |> ResizeArray<StackEntity>
//    |> StackRepository.SaveCards c.Db

//    StackRepository.GetCards c.Db
//    |> Seq.filter(fun x -> x.Title = updatedTitle)
//    |> Assert.Single

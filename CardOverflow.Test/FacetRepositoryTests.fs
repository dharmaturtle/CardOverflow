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
            |> Seq.sortBy (fun x -> x.Ordinal)
            |> Seq.mapi (fun i field -> {
                EditField = ViewField.copyTo field
                Value = fieldValues.[i]
            }) |> toResizeArray
        EditSummary = "Initial creation"
        Kind = NewOriginal_TagIds tagIds
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
    Kind = NewOriginal_TagIds tagIds }

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
    add "Basic (and reversed card) - Card 1" <| normalCommand []

let addBasicCard =
    add "Basic" <| normalCommand []

let addBasicCustomCard fieldValues =
    add "Basic" <| normalCommand fieldValues

let addCloze fieldValues =
    add "Cloze" <| clozeCommand fieldValues

[<Fact>]
let ``CardRepository.CreateCard on a basic facet acquires 1 card/facet``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let aTag = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    let bTag = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    
    let! _ = addBasicCard c.Db userId [aTag; bTag]

    Assert.SingleI <| c.Db.Card
    Assert.SingleI <| c.Db.Card
    Assert.SingleI <| c.Db.AcquiredCard
    let! cards = CardRepository.GetQuizBatch c.Db userId ""
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
    let! view = CardViewRepository.get c.Db 1
    Assert.Equal<FieldAndValue seq>(
        [{  Field = {
                Name = "Front"
                IsRightToLeft = false
                Ordinal = 0uy
                IsSticky = false }
            Value = "Front" }
         {  Field = {
                Name = "Back"
                IsRightToLeft = false
                Ordinal = 1uy
                IsSticky = false }
            Value = "Back"}],
        view.Value.FieldValues
            |> Seq.sortByDescending (fun x -> x.Field.Name)
    )
    Assert.Equal<string seq>(
        [aTag; bTag],
        (CardRepository.GetAcquiredPages c.Db userId 1 "")
            .GetAwaiter()
            .GetResult()
            .Results
            .Single()
            |> Result.getOk
            |> fun x -> x.Tags
    )}

[<Fact>]
let ``ExploreCardRepository.getInstance works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let! _ = addBasicCard c.Db userId []
    let cardId = 1
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

    let! (card1: BranchInstanceMeta)    = ExploreCardRepository.get      c.Db userId cardId |> TaskResult.map(fun x -> x.Instance)
    let! (card2: BranchInstanceMeta), _ = ExploreCardRepository.instance c.Db userId newBranchInstanceId
    Assert.Equal(card1.InC(), card2.InC())
    Assert.Equal(newValue                 , card2.StrippedFront)
    Assert.Equal(newValue + " " + newValue, card2.StrippedBack)
    let! (card3: BranchInstanceMeta), _ = ExploreCardRepository.instance c.Db userId oldBranchInstanceId
    Assert.Equal("Front",      card3.StrippedFront)
    Assert.Equal("Front Back", card3.StrippedBack)

    // nonexistant id
    let nonexistant = 1337
    
    let! (missingCard: Result<_, _>) = ExploreCardRepository.instance c.Db userId nonexistant
    
    Assert.Equal(sprintf "Branch Instance #%i not found" nonexistant, missingCard.error)
    } |> TaskResult.getOk)

[<Fact>]
let ``CardViewRepository.instancePair works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let otherUserId = 2
    let! _ = addBasicCard c.Db userId []
    let! _ = addBasicCard c.Db otherUserId []
    
    let! a, (a_: bool), b, (b_:bool) = CardViewRepository.instancePair c.Db 1001 1002 userId
    
    Assert.Equal(a.InC(), b.InC())
    Assert.True(a_)
    Assert.False(b_)

    // missing instanceId
    let! (x: Result<_, _>) = CardViewRepository.instancePair c.Db 1001 -1 userId
    
    Assert.Equal("Card instance #-1 not found", x.error)
    
    let! (x: Result<_, _>) = CardViewRepository.instancePair c.Db -1 1001 userId
    
    Assert.Equal("Card instance #-1 not found", x.error)
    } |> TaskResult.getOk)

[<Fact>]
let ``CardViewRepository.instanceWithLatest works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let! _ = addBasicCard c.Db userId []
    let branchId = 1
    let! collate =
        TestCollateRepo.Search c.Db "Basic"
        |> Task.map (fun x -> x.Single(fun x -> x.Name = "Basic"))
    let secondVersion = Guid.NewGuid().ToString()
    let! _ =
        {   EditCardCommand.EditSummary = secondVersion
            FieldValues = [].ToList()
            CollateInstance = collate |> ViewCollateInstance.copyTo
            Kind = Update_BranchId_Title (branchId, null)
        } |> UpdateRepository.card c.Db userId
    let oldInstanceId = 1001
    let updatedInstanceId = 1002
    do! c.Db.BranchInstance.SingleAsync(fun x -> x.Id = updatedInstanceId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))
    
    let! (a: BranchInstanceView), (a_: bool), (b: BranchInstanceView), (b_: bool), bId = CardViewRepository.instanceWithLatest c.Db 1001 userId
    
    do! CardViewRepository.instance c.Db oldInstanceId
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
    let og_c = 1
    let copy_c = 2
    let copy2x_c = 3
    let copyOfBranch_c = 4
    
    let og_b = 1
    let copy_b = 2
    let og_b_2 = 3
    let copy2x_b = 4
    let copyOfBranch_b = 5
    let branchOfCopy_b = 6

    let og_i = 1001
    let ogEdit_i = 1002
    let copy_i = 1003
    let branch_i = 1004 // branch of og_c and og_b_2
    let copy2x_i = 1005
    let copyOfBranch_i = 1006
    let branchOfCopy_i = 1007

    let user1 = 1
    let user2 = 2
    
    use c = new TestContainer()
    let assertCount (cardsIdsAndCounts: _ list) (branchIdsAndCounts: _ list) (branchInstanceIdsAndCounts: _ list) = task {
        //"XXXXXX Card Count".D()
        do! c.Db.Card.CountAsync()
            |> Task.map(fun i -> Assert.Equal(cardsIdsAndCounts.Length, i))
        //"XXXXXX Branch Count".D()
        do! c.Db.Branch.CountAsync()
            |> Task.map(fun i -> Assert.Equal(branchIdsAndCounts.Length, i))
        //"XXXXXX Branch Instance Count".D()
        do! c.Db.BranchInstance.CountAsync()
            |> Task.map(fun i -> Assert.Equal(branchInstanceIdsAndCounts.Length, i))
        for id, count in cardsIdsAndCounts do
            //"XXXXXX".D(sprintf "Card #%i should have count #%i" id count)
            do! c.Db.Card.SingleAsync(fun x -> x.Id = id)
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
            [og_c, 1]
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
            [og_c, 1]
            [og_b, 1]
            [og_i, 0; ogEdit_i, 1]
    
    let asserts userId cardId branchId instanceId newValue instanceCountForCard revisionCount tags = task {
        let! instance = CardViewRepository.instance c.Db instanceId
        Assert.Equal<string seq>(
            [newValue; newValue],
            instance.Value.FieldValues.Select(fun x -> x.Value))
        Assert.Equal(
            instanceCountForCard,
            c.Db.BranchInstance.Count(fun x -> x.CardId = cardId))
        let! card = ExploreCardRepository.get c.Db userId cardId
        Assert.Equal<ViewTag seq>(
            tags,
            card.Value.Tags)
        Assert.Equal<string seq>(
            [newValue; newValue],
            instance.Value.FieldValues.OrderBy(fun x -> x.Field.Ordinal).Select(fun x -> x.Value)
        )
        let createds = c.Db.BranchInstance.Select(fun x -> x.Created) |> Seq.toList
        Assert.NotEqual(createds.[0], createds.[1])
        let! revisions = CardRepository.Revisions c.Db userId branchId
        Assert.Equal(revisionCount, revisions.SortedMeta.Count())
        let! instance = CardViewRepository.instance c.Db revisions.SortedMeta.[0].Id
        let revision, _, _, _ = instance |> Result.getOk |> fun x -> x.FrontBackFrontSynthBackSynth.[0]
        Assert.Contains(newValue, revision)
    }
    
    do! asserts user1 og_c og_b ogEdit_i newValue 2 2
            [ { Name = "A"
                Count = 1
                IsAcquired = true }
              { Name = "B"
                Count = 1
                IsAcquired = true }]
    let! revisions = CardRepository.Revisions c.Db user1 og_b
    let! instance = CardViewRepository.instance c.Db revisions.SortedMeta.[1].Id
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
            [og_c, 1;              copy_c, 1]
            [og_b, 1;              copy_b, 1]
            [og_i, 0; ogEdit_i, 1; copy_i, 1]

    do! asserts user2 copy_c copy_b copy_i newValue 1 1 []

    // missing copy
    let missingInstanceId = 1337
    let missingCardId = 1337
    
    let! old = SanitizeCardRepository.getUpsert c.Db <| VNewCopySourceInstanceId missingInstanceId
    
    Assert.Equal(sprintf "Branch Instance #%i not found." missingInstanceId, old.error)
    do! assertCount
            [og_c, 1;              copy_c, 1]
            [og_b, 1;              copy_b, 1]
            [og_i, 0; ogEdit_i, 1; copy_i, 1]

    // user2 branchs og_c
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeCardRepository.getUpsert c.Db <| VNewBranchSourceCardId og_c
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
    let! x, _ = ExploreCardRepository.instance c.Db user2 branch_i |> TaskResult.getOk
    do! asserts user2 x.CardId x.BranchId x.Id newValue 3 1
            [ { Name = "A"
                Count = 1
                IsAcquired = false }
              { Name = "B"
                Count = 1
                IsAcquired = false }]
    do! assertCount
            [og_c,     2 ;    copy_c, 1 ]
            [og_b,     1 ;    copy_b, 1 ;
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // user2 branchs missing card
    let! old = SanitizeCardRepository.getUpsert c.Db <| VNewBranchSourceCardId missingCardId
    Assert.Equal(sprintf "Card #%i not found." missingInstanceId, old.error)
    do! assertCount
            [og_c,     2 ;    copy_c, 1 ]
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
    let! x, _ = ExploreCardRepository.instance c.Db user2 copy2x_i |> TaskResult.getOk
    do! asserts user2 x.CardId x.BranchId x.Id newValue 1 1 []
    do! assertCount
            [og_c,     2 ;    copy_c, 1 ;    copy2x_c, 1 ]
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
    let! x, _ = ExploreCardRepository.instance c.Db user2 copyOfBranch_i |> TaskResult.getOk
    do! asserts user2 x.CardId x.BranchId x.Id newValue 1 1 []
    do! assertCount
            [og_c,     2 ;    copy_c, 1 ;    copy2x_c, 1 ;    copyOfBranch_c, 1 ]
            [og_b,     1 ;    copy_b, 1 ;    copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;    copy2x_i, 1 ;    copyOfBranch_i, 1
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // user2 branches their copy
    let! old = SanitizeCardRepository.getUpsert c.Db <| VNewBranchSourceCardId copy_c
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
    let! x, _ = ExploreCardRepository.instance c.Db user2 branchOfCopy_i |> TaskResult.getOk
    do! asserts user2 x.CardId x.BranchId x.Id newValue 2 1 []
    do! assertCount
            [og_c,     2 ;    copy_c, 1         ; copy2x_c, 1 ;    copyOfBranch_c, 1 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 1
             branch_i, 1 ]

    // adventures in acquiring cards
    let adventurerId = 3
    do! CardRepository.AcquireCardAsync c.Db adventurerId og_i |> TaskResult.getOk
    do! assertCount
            [og_c,     3 ;    copy_c, 1         ; copy2x_c, 1 ;    copyOfBranch_c, 1 ]
            [og_b,     2 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     1 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    do! CardRepository.AcquireCardAsync c.Db adventurerId ogEdit_i |> TaskResult.getOk
    do! assertCount
            [og_c,     3 ;    copy_c, 1         ; copy2x_c, 1 ;    copyOfBranch_c, 1 ]
            [og_b,     2 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    do! CardRepository.AcquireCardAsync c.Db adventurerId copy_i |> TaskResult.getOk
    do! assertCount
            [og_c,     3 ;    copy_c, 2         ; copy2x_c, 1 ;    copyOfBranch_c, 1 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 1 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 1 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    do! CardRepository.AcquireCardAsync c.Db adventurerId copy2x_i |> TaskResult.getOk
    do! assertCount
            [og_c,     3 ;    copy_c, 2         ; copy2x_c, 2 ;    copyOfBranch_c, 1 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfBranch_b, 1
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    do! CardRepository.AcquireCardAsync c.Db adventurerId copyOfBranch_i |> TaskResult.getOk
    do! assertCount
            [og_c,     3 ;    copy_c, 2         ; copy2x_c, 2 ;    copyOfBranch_c, 2 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   1 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 2 ;    branchOfCopy_i, 1
             branch_i, 1 ]
    Assert.Equal(4, c.Db.AcquiredCard.Count(fun x -> x.UserId = adventurerId))
    do! CardRepository.AcquireCardAsync c.Db adventurerId branch_i |> TaskResult.getOk
    Assert.Equal(4, c.Db.AcquiredCard.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_c,     3 ;    copy_c, 2         ; copy2x_c, 2 ;    copyOfBranch_c, 2 ]
            [og_b,     1 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    Assert.Equal(4, c.Db.AcquiredCard.Count(fun x -> x.UserId = adventurerId))
    do! CardRepository.AcquireCardAsync c.Db adventurerId branchOfCopy_i |> TaskResult.getOk
    Assert.Equal(4, c.Db.AcquiredCard.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_c,     3 ;    copy_c, 2         ; copy2x_c, 2 ;    copyOfBranch_c, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    // adventures in implicit unacquiring
    let adventurerId = 1 // changing the adventurer!
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.BranchInstanceId = ogEdit_i && x.UserId = adventurerId)
    do! CardRepository.UnacquireCardAsync c.Db ac.Id
    Assert.Equal(0, c.Db.AcquiredCard.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_c,     2 ;    copy_c, 2         ; copy2x_c, 2 ;    copyOfBranch_c, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    do! CardRepository.AcquireCardAsync c.Db adventurerId ogEdit_i |> TaskResult.getOk
    do! assertCount
            [og_c,     3 ;    copy_c, 2         ; copy2x_c, 2 ;    copyOfBranch_c, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 1 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    do! CardRepository.AcquireCardAsync c.Db adventurerId og_i |> TaskResult.getOk
    do! assertCount
            [og_c,     3 ;    copy_c, 2         ; copy2x_c, 2 ;    copyOfBranch_c, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     1 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    do! CardRepository.AcquireCardAsync c.Db adventurerId branch_i |> TaskResult.getOk
    do! assertCount
            [og_c,     3 ;    copy_c, 2         ; copy2x_c, 2 ;    copyOfBranch_c, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   3 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 3 ]
    // adventures in unacquiring and suspending
    let adventurerId = 2 // changing the adventurer, again!
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.CardId = og_c && x.UserId = adventurerId)
    do! CardRepository.editState c.Db adventurerId ac.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_c,     2 ;    copy_c, 2         ; copy2x_c, 2 ;    copyOfBranch_c, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    do! CardRepository.UnacquireCardAsync c.Db ac.Id
    do! assertCount
            [og_c,     2 ;    copy_c, 2         ; copy2x_c, 2 ;    copyOfBranch_c, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 2
             branch_i, 2 ]
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.CardId = copy_c && x.UserId = adventurerId)
    do! CardRepository.UnacquireCardAsync c.Db ac.Id
    do! assertCount
            [og_c,     2 ;    copy_c, 1         ; copy2x_c, 2 ;    copyOfBranch_c, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.CardId = copy2x_c && x.UserId = adventurerId)
    do! CardRepository.editState c.Db adventurerId ac.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_c,     2 ;    copy_c, 1         ; copy2x_c, 1 ;    copyOfBranch_c, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    do! CardRepository.UnacquireCardAsync c.Db ac.Id
    do! assertCount
            [og_c,     2 ;    copy_c, 1         ; copy2x_c, 1 ;    copyOfBranch_c, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    let adventurerId = 3 // and another change!
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.CardId = copy2x_c && x.UserId = adventurerId)
    do! CardRepository.editState c.Db adventurerId ac.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_c,     2 ;    copy_c, 1         ; copy2x_c, 0 ;    copyOfBranch_c, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    do! CardRepository.UnacquireCardAsync c.Db ac.Id
    do! assertCount
            [og_c,     2 ;    copy_c, 1         ; copy2x_c, 0 ;    copyOfBranch_c, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfBranch_b, 2
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfBranch_i, 2 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.CardId = copyOfBranch_c && x.UserId = adventurerId)
    do! CardRepository.UnacquireCardAsync c.Db ac.Id
    do! assertCount
            [og_c,     2 ;    copy_c, 1         ; copy2x_c, 0 ;    copyOfBranch_c, 1 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfBranch_b, 1
             og_b_2,   2 ;    branchOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfBranch_i, 1 ;
             ogEdit_i, 0 ;    branchOfCopy_i, 1
             branch_i, 2 ]
    }

[<Fact>]
let ``ExploreCardRepository.get works for all ExploreCardAcquiredStatus``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let testGetAcquired cardId instanceId =
        CardRepository.GetAcquired c.Db userId cardId
        |> TaskResult.map (fun ac -> Assert.Equal(instanceId, ac.Single().BranchInstanceMeta.Id))

    let! _ = addBasicCard c.Db userId []
    let og_c = 1
    let og_b = 1
    let og_i = 1001

    // tests ExactInstanceAcquired
    do! ExploreCardRepository.get c.Db userId og_c
        |> TaskResult.map (fun card -> Assert.Equal(ExactInstanceAcquired og_i, card.AcquiredStatus))
    do! testGetAcquired og_c og_i
    
    // update card
    let update_i = 1002
    let! (command: ViewEditCardCommand) = SanitizeCardRepository.getUpsert c.Db <| VUpdateBranchId og_b
    let! instanceIds = UpdateRepository.card c.Db userId command.load
    Assert.Equal(update_i, instanceIds)

    // tests ExactInstanceAcquired
    do! ExploreCardRepository.get c.Db userId og_c
        |> TaskResult.map (fun card -> Assert.Equal(ExactInstanceAcquired update_i, card.AcquiredStatus))
    do! testGetAcquired og_c update_i

    // acquiring old instance doesn't change LatestInstanceId
    Assert.Equal(update_i, c.Db.Card.Include(fun x -> x.DefaultBranch).Single().DefaultBranch.LatestInstanceId)
    do! CardRepository.AcquireCardAsync c.Db userId og_i
    Assert.Equal(update_i, c.Db.Card.Include(fun x -> x.DefaultBranch).Single().DefaultBranch.LatestInstanceId)

    // tests OtherInstanceAcquired
    let! card = ExploreCardRepository.get c.Db userId og_c
    match card.AcquiredStatus with
    | OtherInstanceAcquired x -> Assert.Equal(og_i, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_c og_i

    // branch card
    let branch_i = 1003
    let branch_b = 2
    let! (command: ViewEditCardCommand) = SanitizeCardRepository.getUpsert c.Db <| VNewBranchSourceCardId og_c
    let! instanceIds = UpdateRepository.card c.Db userId command.load
    Assert.Equal(branch_i, instanceIds)
    
    // tests LatestBranchAcquired
    let! card = ExploreCardRepository.get c.Db userId og_c
    match card.AcquiredStatus with
    | LatestBranchAcquired x -> Assert.Equal(branch_i, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_c branch_i

    // update branch
    let updateBranch_i = 1004
    let! (command: ViewEditCardCommand) = SanitizeCardRepository.getUpsert c.Db <| VUpdateBranchId og_b
    let! instanceIds = UpdateRepository.card c.Db userId command.load
    Assert.Equal(updateBranch_i, instanceIds)

    // tests LatestBranchAcquired
    let! card = ExploreCardRepository.get c.Db userId og_c
    match card.AcquiredStatus with
    | LatestBranchAcquired x -> Assert.Equal(updateBranch_i, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_c updateBranch_i

    // acquiring old instance doesn't change LatestInstanceId
    Assert.Equal(update_i, c.Db.Card.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_c).Branches.Single().LatestInstanceId)
    Assert.Equal(updateBranch_i, c.Db.Card.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_c).Branches.Single(fun x -> x.Id = branch_b).LatestInstanceId)
    do! CardRepository.AcquireCardAsync c.Db userId branch_i
    Assert.Equal(update_i, c.Db.Card.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_c).Branches.Single().LatestInstanceId)
    Assert.Equal(updateBranch_i, c.Db.Card.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_c).Branches.Single(fun x -> x.Id = branch_b).LatestInstanceId)

    // tests OtherBranchAcquired
    let! card = ExploreCardRepository.get c.Db userId og_c
    match card.AcquiredStatus with
    | OtherBranchAcquired x -> Assert.Equal(branch_i, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_c branch_i

    // try to branch card again, but fail
    let! (command: ViewEditCardCommand) = SanitizeCardRepository.getUpsert c.Db <| VUpdateBranchId og_b
    let! (error: Result<_,_>) = UpdateRepository.card c.Db userId command.load
    Assert.Equal(sprintf "Card #1 already has a Branch named 'New Branch'.", error.error);

    // branch card again
    let branch_i2 = 1005
    let branch_c2 = 3
    let! (command: ViewEditCardCommand) = SanitizeCardRepository.getUpsert c.Db <| VUpdateBranchId og_b
    let command =
        { command.load with
            Kind =
                match command.load.Kind with
                | NewBranch_SourceCardId_Title (id, name) -> NewBranch_SourceCardId_Title (id, name + Guid.NewGuid().ToString())
                | _ -> failwith "impossible"
        }
    let! instanceIds = UpdateRepository.card c.Db userId command
    Assert.Equal(branch_i2, instanceIds)

    // tests LatestBranchAcquired
    let! card = ExploreCardRepository.get c.Db userId og_c
    match card.AcquiredStatus with
    | LatestBranchAcquired x -> Assert.Equal(branch_i2, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_c branch_i2

    // tests LatestBranchAcquired with og_c
    let! card = ExploreCardRepository.get c.Db userId og_c
    match card.AcquiredStatus with
    | LatestBranchAcquired x -> Assert.Equal(branch_i2, x)
    | _ -> failwith "impossible"
    do! testGetAcquired og_c branch_i2

    // acquiring old instance doesn't change LatestInstanceId; can also acquire old branch
    Assert.Equal(update_i, c.Db.Card.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_c).Branches.Single().LatestInstanceId)
    Assert.Equal(updateBranch_i, c.Db.Card.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_c).Branches.Single(fun x -> x.Id = branch_b).LatestInstanceId)
    do! CardRepository.AcquireCardAsync c.Db userId branch_i
    Assert.Equal(update_i, c.Db.Card.Include(fun x -> x.DefaultBranch).Single(fun x -> x.Id = og_c).Branches.Single().LatestInstanceId)
    Assert.Equal(updateBranch_i, c.Db.Card.Include(fun x -> x.Branches).Single(fun x -> x.Id = og_c).Branches.Single(fun x -> x.Id = branch_b).LatestInstanceId)

    // can't acquire missing id
    let missingId = 9001
    let! (error: Result<_,_>) = CardRepository.AcquireCardAsync c.Db userId missingId
    Assert.Equal(error.error, sprintf "Card not found for Instance #%i" missingId)

    // tests NotAcquired
    let otherUser = 1
    let! card = ExploreCardRepository.get c.Db otherUser og_c
    Assert.Equal(NotAcquired, card.AcquiredStatus)
    } |> TaskResult.getOk)
    
// fuck merge
//[<Fact>]
//let ``CardRepository's SaveCards updates a Card``() =
//    use c = new TestContainer()
//    let facet = 
//        CardEntity(
//            Title = "",
//            Description = "",
//            Fields = "",
//            CollateId = 1,
//            Modified = DateTime.UtcNow)
    
//    CardRepository.AddCard c.Db facet

//    let updatedCard = CardRepository.GetCards c.Db |> Seq.head
//    let updatedTitle = Guid.NewGuid().ToString()
//    updatedCard.Title <- updatedTitle

//    updatedCard 
//    |> Seq.singleton 
//    |> ResizeArray<CardEntity>
//    |> CardRepository.SaveCards c.Db

//    CardRepository.GetCards c.Db
//    |> Seq.filter(fun x -> x.Title = updatedTitle)
//    |> Assert.Single

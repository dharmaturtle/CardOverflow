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

let normalCommand fieldValues templateInstance =
    let fieldValues =
        match fieldValues with
        | [] -> ["Front"; "Back"]
        | _ -> fieldValues
    {   TemplateInstance = templateInstance
        FieldValues =
            templateInstance.Fields
            |> Seq.sortBy (fun x -> x.Ordinal)
            |> Seq.mapi (fun i field -> {
                EditField = ViewField.copyTo field
                Value = fieldValues.[i]
                Communal = None
            }) |> toResizeArray
        EditSummary = "Initial creation"
        Source = Original
    }

let clozeCommand clozeText (clozeTemplate: ViewTemplateInstance) = {
    EditSummary = "Initial creation"
    FieldValues =
        clozeTemplate.Fields.Select(fun f -> {
            EditField = ViewField.copyTo f
            Value =
                if f.Name = "Text" then
                    clozeText
                else
                    "extra"
            Communal =
                if f.Name = "Text" then
                    {   InstanceId = None
                        CommunalCardInstanceIds = [0].ToList()
                    } |> Some
                else None
        }).ToList()
    TemplateInstance = clozeTemplate
    Source = Original }

let clozeCommandWithSharedExtra clozeText clozeTemplate = {
    clozeCommand clozeText clozeTemplate with
        FieldValues =
            clozeTemplate.Fields.Select(fun f -> {
                EditField = ViewField.copyTo f
                Value =
                    if f.Name = "Text" then
                        clozeText
                    else
                        "extra"
                Communal =
                    if f.Name = "Text" then
                        {   InstanceId = None
                            CommunalCardInstanceIds = [0].ToList()
                        } |> Some
                    else
                        {   InstanceId = None
                            CommunalCardInstanceIds = [0].ToList()
                        } |> Some
            }).ToList() }

let add templateName createCommand (db: CardOverflowDb) userId tags = task {
    let! template = TestTemplateRepo.SearchEarliest db templateName
    let! ac = CardRepository.getNew db userId
    let ac = { ac with Tags = tags }
    let! r =
        createCommand template
        |> SanitizeCardRepository.Update db userId ac
    return r.Value
    }

let addReversedBasicCard: CardOverflowDb -> int -> string list -> Task<ResizeArray<int> * ResizeArray<string * int>> =
    add "Basic (and reversed card) - Card 1" <| normalCommand []

let addBasicCard =
    add "Basic" <| normalCommand []

let addBasicCustomCard fieldValues =
    add "Basic" <| normalCommand fieldValues

let addCloze fieldValues =
    add "Cloze" <| clozeCommand fieldValues

let addClozeWithSharedExtra fieldValues =
    add "Cloze" <| clozeCommandWithSharedExtra fieldValues

[<Fact>]
let ``CardRepository.CreateCard on a basic facet acquires 1 card/facet``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let tags = ["a"; "b"]
    
    let! _ = addBasicCard c.Db userId tags

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
                Font = "Arial"
                FontSize = 20uy
                IsRightToLeft = false
                Ordinal = 0uy
                IsSticky = false }
            Value = "Front" }
         {  Field = {
                Name = "Back"
                Font = "Arial"
                FontSize = 20uy
                IsRightToLeft = false
                Ordinal = 1uy
                IsSticky = false }
            Value = "Back"}],
        view.Value.FieldValues
            |> Seq.sortByDescending (fun x -> x.Field.Name)
    )
    Assert.Equal<string seq>(
        tags,
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
    let oldCardInstanceId = 1001
    let newCardInstanceId = 1002
    let newValue = Guid.NewGuid().ToString()
    let! old, ac = SanitizeCardRepository.getEdit c.Db userId cardId
    let updated = {
        old with
            ViewEditCardCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! (instanceId, x) = UpdateRepository.card c.Db ac updated.load
    Assert.Equal<int seq>([newCardInstanceId], instanceId)
    Assert.Empty x

    let! (card1: ExploreCard) = ExploreCardRepository.get      c.Db userId cardId
    let! (card2: ExploreCard) = ExploreCardRepository.instance c.Db userId newCardInstanceId
    Assert.Equal(card1.InC(), card2.InC())
    Assert.Equal(newValue                 , card2.Summary.Instance.StrippedFront)
    Assert.Equal(newValue + " " + newValue, card2.Summary.Instance.StrippedBack)
    let! (card3: ExploreCard) = ExploreCardRepository.instance c.Db userId oldCardInstanceId
    Assert.Equal("Front",      card3.Summary.Instance.StrippedFront)
    Assert.Equal("Front Back", card3.Summary.Instance.StrippedBack)

    // nonexistant id
    let nonexistant = 1337
    
    let! (missingCard: Result<ExploreCard, string>) = ExploreCardRepository.instance c.Db userId nonexistant
    
    Assert.Equal(sprintf "Card Instance #%i not found" nonexistant, missingCard.error)
    } |> TaskResult.assertOk)

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
    } |> TaskResult.assertOk)

[<Fact>]
let ``CardViewRepository.instanceWithLatest works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let! _ = addBasicCard c.Db userId []
    let! ac = CardRepository.GetAcquired c.Db userId 1
    let! template = SanitizeTemplate.AllInstances c.Db 1
    let secondVersion = Guid.NewGuid().ToString()
    let! _ =
        {   EditCardCommand.EditSummary = secondVersion
            FieldValues = [].ToList()
            TemplateInstance = template.Instances.Single() |> ViewTemplateInstance.copyTo
            Source = Original
        } |> UpdateRepository.card c.Db ac
    let oldInstanceId = 1001
    let updatedInstanceId = 1002
    do! c.Db.CardInstance.SingleAsync(fun x -> x.Id = updatedInstanceId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))
    
    let! (a: CardInstanceView), (a_: bool), (b: CardInstanceView), (b_: bool), bId = CardViewRepository.instanceWithLatest c.Db 1001 userId
    
    do! CardViewRepository.instance c.Db oldInstanceId
        |> TaskResult.map (fun expected -> Assert.Equal(expected.InC(), a.InC()))
    Assert.False a_
    Assert.True b_
    Assert.Empty b.FieldValues
    Assert.Equal(updatedInstanceId, bId)
    } |> TaskResult.assertOk)

[<Fact>]
let ``CardInstance with "" as FieldValues is parsed to empty`` (): unit =
    let view =
        CardInstanceEntity(
            FieldValues = "",
            TemplateInstance = TemplateInstanceEntity(
                Fields = "FrontArial20False0FalseBackArial20False1False"
            ))
        |> CardInstanceView.load

    Assert.Empty view.FieldValues

[<Fact>]
let ``UpdateRepository.card edit/copy/branch works``() : Task<unit> = task {
    let og_c = 1
    let copy_c = 2
    let branch_c = 3
    let copy2x_c = 4
    let copyBranch_c = 5
    let og_i = 1001
    let ogEdit_i = 1002
    let copy_i = 1003
    let branch_i = 1004
    let copy2x_i = 1005
    let copyBranch_i = 1006

    let user1 = 1
    let user2 = 2
    
    use c = new TestContainer()
    let assertCount (cardsIdsAndCounts: _ list) (cardInstanceIdsAndCounts: _ list) = task {
        do! c.Db.Card.CountAsync()
            |> Task.map(fun i -> Assert.Equal(cardsIdsAndCounts.Length, i))
        do! c.Db.CardInstance.CountAsync()
            |> Task.map(fun i -> Assert.Equal(cardInstanceIdsAndCounts.Length, i))
        for id, count in cardsIdsAndCounts do
            do! c.Db.Card.SingleAsync(fun x -> x.Id = id)
                |> Task.map (fun c -> Assert.Equal(count, c.Users))
        for id, count in cardInstanceIdsAndCounts do
            do! c.Db.CardInstance.SingleAsync(fun x -> x.Id = id)
                |> Task.map (fun c -> Assert.Equal(count, c.Users))}
    let tags = ["a"; "b"]
    let! _ = addBasicCard c.Db user1 tags
    do! assertCount
            [og_c, 1]
            [og_i, 1]

    // updated by user1
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeCardRepository.getEdit c.Db user1 og_c
    let old, ac = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! x = UpdateRepository.card c.Db ac updated.load
    let instanceId, x = x.Value
    Assert.Equal<int seq>([ogEdit_i], instanceId)
    Assert.Empty x
    do! assertCount
            [og_c, 1]
            [og_i, 0; ogEdit_i, 1]
    
    let asserts userId cardId newValue instanceCountForCard revisionCount tags = task {
        let! refreshed = CardViewRepository.get c.Db cardId
        Assert.Equal<string seq>(
            [newValue; newValue],
            refreshed.Value.FieldValues.Select(fun x -> x.Value))
        Assert.Equal(
            instanceCountForCard,
            c.Db.CardInstance.Count(fun x -> x.CardId = cardId))
        let! card = ExploreCardRepository.get c.Db userId cardId
        Assert.Equal<ViewTag seq>(
            tags,
            card.Value.Tags)
        Assert.Equal<string seq>(
            [newValue; newValue],
            refreshed.Value.FieldValues.OrderBy(fun x -> x.Field.Ordinal).Select(fun x -> x.Value)
        )
        let createds = c.Db.CardInstance.Select(fun x -> x.Created) |> Seq.toList
        Assert.NotEqual(createds.[0], createds.[1])
        let! revisions = CardRepository.Revisions c.Db userId cardId
        Assert.Equal(revisionCount, revisions.SortedMeta.Count())
        let! instance = CardViewRepository.instance c.Db revisions.SortedMeta.[0].Id
        let revision, _, _, _ = instance |> Result.getOk |> fun x -> x.FrontBackFrontSynthBackSynth
        Assert.Contains(newValue, revision)
        if instanceCountForCard > 1 then
            let! instance = CardViewRepository.instance c.Db revisions.SortedMeta.[1].Id
            let original, _, _, _ = instance |> Result.getOk |> fun x -> x.FrontBackFrontSynthBackSynth
            Assert.Contains("Front", original)
            Assert.True(revisions.SortedMeta.Single(fun x -> x.IsLatest).Id > revisions.SortedMeta.Single(fun x -> not x.IsLatest).Id) // tests that Latest really came after NotLatest
    }
    
    do! asserts user1 og_c newValue 2 2
            [ { Name = "a"
                Count = 1
                IsAcquired = true }
              { Name = "b"
                Count = 1
                IsAcquired = true }]
            
    // copy by user2
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeCardRepository.getCopy c.Db user2 ogEdit_i
    let old, ac = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! x = UpdateRepository.card c.Db ac updated.load
    let (instanceIds, communals) = x.Value
    Assert.Equal<int seq>([copy_i], instanceIds)
    Assert.Empty communals
    do! assertCount
            [og_c, 1;              copy_c, 1]
            [og_i, 0; ogEdit_i, 1; copy_i, 1]

    do! asserts user2 copy_c newValue 1 1 []

    // missing copy
    let missingInstanceId = 1337
    
    let! old = SanitizeCardRepository.getCopy c.Db user2 missingInstanceId
    
    Assert.Equal(sprintf "Card instance %i not found" missingInstanceId, old.error)
    do! assertCount
            [og_c, 1;              copy_c, 1]
            [og_i, 0; ogEdit_i, 1; copy_i, 1]

    // user2 branchs og_c
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeCardRepository.getBranch c.Db user2 og_c
    let old, ac = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! x = UpdateRepository.card c.Db ac updated.load
    let instanceIds, communals = x.Value
    Assert.Equal<int seq>([branch_i], instanceIds)
    Assert.Empty communals
    let! x = ExploreCardRepository.instance c.Db user2 branch_i
    do! asserts user2 x.Value.Id newValue 1 1 []
    do! assertCount
            [og_c,     2 ;    copy_c, 1 ;
             branch_c, 1 ]
            [og_i,     0 ;    copy_i, 1 ;
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // user2 tries to branch their branch
    let! old = SanitizeCardRepository.getBranch c.Db user2 branch_c
    Assert.Equal("You can't branch a branch", old.error)
    do! assertCount
            [og_c,     2 ;    copy_c, 1 ;
             branch_c, 1 ]
            [og_i,     0 ;    copy_i, 1 ;
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // user2 branchs missing card
    let! old = SanitizeCardRepository.getBranch c.Db user2 missingInstanceId
    Assert.Equal(sprintf "Card #%i doesn't exist" missingInstanceId, old.error)
    do! assertCount
            [og_c,     2 ;    copy_c, 1 ;
             branch_c, 1 ]
            [og_i,     0 ;    copy_i, 1 ;
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // user2 copies their copy
    let! x = SanitizeCardRepository.getCopy c.Db user2 copy_i
    let old, ac = x.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! x = UpdateRepository.card c.Db ac updated.load
    let instanceIds, communals = x.Value
    Assert.Equal<int seq>([copy2x_i], instanceIds)
    Assert.Empty communals
    let! x = ExploreCardRepository.instance c.Db user2 copy2x_i
    do! asserts user2 x.Value.Id newValue 1 1 []
    do! assertCount
            [og_c,     2 ;    copy_c, 1 ;    copy2x_c, 1 ;
             branch_c, 1 ]
            [og_i,     0 ;    copy_i, 1 ;    copy2x_i, 1 ;
             ogEdit_i, 1 ;
             branch_i, 1 ]
    
    // user2 copies their branch
    let! old = SanitizeCardRepository.getCopy c.Db user2 branch_i
    let old, ac = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! x = UpdateRepository.card c.Db ac updated.load
    let instanceIds, communals = x.Value
    Assert.Equal<int seq>([copyBranch_i], instanceIds)
    Assert.Empty communals
    let! x = ExploreCardRepository.instance c.Db user2 copyBranch_i
    do! asserts user2 x.Value.Id newValue 1 1 []
    do! assertCount
            [og_c,     2 ;    copy_c, 1 ;    copy2x_c, 1 ;    copyBranch_c, 1
             branch_c, 1 ]
            [og_i,     0 ;    copy_i, 1 ;    copy2x_i, 1 ;    copyBranch_i, 1
             ogEdit_i, 1 ;
             branch_i, 1 ]

    // adventures in acquiring and unacquiring cards
    let adventurerId = 3
    do! CardRepository.AcquireCardAsync c.Db adventurerId og_i
    do! assertCount
            [og_c,     3 ;    copy_c, 1 ;    copy2x_c, 1 ;    copyBranch_c, 1
             branch_c, 1 ]
            [og_i,     1 ;    copy_i, 1 ;    copy2x_i, 1 ;    copyBranch_i, 1
             ogEdit_i, 1 ;
             branch_i, 1 ]
    do! CardRepository.AcquireCardAsync c.Db adventurerId ogEdit_i
    do! assertCount
            [og_c,     3 ;    copy_c, 1 ;    copy2x_c, 1 ;    copyBranch_c, 1
             branch_c, 1 ]
            [og_i,     0 ;    copy_i, 1 ;    copy2x_i, 1 ;    copyBranch_i, 1
             ogEdit_i, 2 ;
             branch_i, 1 ]
    do! CardRepository.AcquireCardAsync c.Db adventurerId copy_i
    do! assertCount
            [og_c,     3 ;    copy_c, 2 ;    copy2x_c, 1 ;    copyBranch_c, 1
             branch_c, 1 ]
            [og_i,     0 ;    copy_i, 2 ;    copy2x_i, 1 ;    copyBranch_i, 1
             ogEdit_i, 2 ;
             branch_i, 1 ]
    do! CardRepository.AcquireCardAsync c.Db adventurerId copy2x_i
    do! assertCount
            [og_c,     3 ;    copy_c, 2 ;    copy2x_c, 2 ;    copyBranch_c, 1
             branch_c, 1 ]
            [og_i,     0 ;    copy_i, 2 ;    copy2x_i, 2 ;    copyBranch_i, 1
             ogEdit_i, 2 ;
             branch_i, 1 ]
    do! CardRepository.AcquireCardAsync c.Db adventurerId copyBranch_i
    do! assertCount
            [og_c,     3 ;    copy_c, 2 ;    copy2x_c, 2 ;    copyBranch_c, 2
             branch_c, 1 ]
            [og_i,     0 ;    copy_i, 2 ;    copy2x_i, 2 ;    copyBranch_i, 2
             ogEdit_i, 2 ;
             branch_i, 1 ]
    }
    
// fuck merge
//[<Fact>]
//let ``CardRepository's SaveCards updates a Card``() =
//    use c = new TestContainer()
//    let facet = 
//        CardEntity(
//            Title = "",
//            Description = "",
//            Fields = "",
//            TemplateId = 1,
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

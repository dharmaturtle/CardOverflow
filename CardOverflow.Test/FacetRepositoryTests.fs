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
        CopySourceId = None
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
    CopySourceId = None }

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
    return Result.getOk r
    }

let addReversedBasicCard: CardOverflowDb -> int -> string list -> Task<ResizeArray<string * int>> =
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
    let! acquiredCard = 
        (CardRepository.GetAcquired c.Db userId cardId)
            .ContinueWith(fun (x: Task<Result<AcquiredCard, string>>) -> x.Result.Value)
    let! old = SanitizeCardRepository.getEdit c.Db oldCardInstanceId
    let updated = {
        old with
            ViewEditCardCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! x = CardRepository.UpdateFieldsToNewInstance c.Db acquiredCard updated.load
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
            CopySourceId = None
        } |> CardRepository.UpdateFieldsToNewInstance c.Db ac
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
let ``CardRepository.UpdateFieldsToNewInstance on basic card updates the fields, also copying``() : Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let tags = ["a"; "b"]
    let! _ = addBasicCard c.Db userId tags
    let cardId = 1
    let cardInstanceId = 1001
    let newValue = Guid.NewGuid().ToString()
    let! acquiredCard = 
        (CardRepository.GetAcquired c.Db userId cardId)
            .ContinueWith(fun (x: Task<Result<AcquiredCard, string>>) -> x.Result.Value)
    let! old = SanitizeCardRepository.getEdit c.Db cardInstanceId
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! x = CardRepository.UpdateFieldsToNewInstance c.Db acquiredCard updated.load
    Assert.Empty x.Value
    
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
    
    do! asserts userId cardId newValue 2 2
            [ { Name = "a"
                Count = 1
                IsAcquired = true }
              { Name = "b"
                Count = 1
                IsAcquired = true }]
            
    // copy
    let userId = 2
    let cardId = 2
    let cardInstanceId = 1002
    let newValue = Guid.NewGuid().ToString()
    let! acquiredCard =  CardRepository.getNew c.Db userId
    let! old = SanitizeCardRepository.getCopy c.Db cardInstanceId
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! x = CardRepository.UpdateFieldsToNewInstance c.Db acquiredCard updated.load
    Assert.Empty x.Value

    do! asserts userId cardId newValue 1 1 []

    // already copied
    let cardInstanceId = 1003
    Assert.Equal(userId, c.Db.CardInstance.Include(fun x -> x.Card).Single(fun x -> x.Id = cardInstanceId).Card.AuthorId)
    let! acquiredCard =  CardRepository.getNew c.Db userId
    let! copy = SanitizeCardRepository.getCopy c.Db cardInstanceId
    
    let! x = CardRepository.UpdateFieldsToNewInstance c.Db acquiredCard copy.Value.load
    
    Assert.Equal("You can't copy your own cards. Yet. Contact us if you really want this feature.", x.error)

    // missing copy
    let cardInstanceId = 1337
    
    let! old = SanitizeCardRepository.getCopy c.Db cardInstanceId
    
    Assert.Equal(sprintf "Card instance %i not found" cardInstanceId, old.error)
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

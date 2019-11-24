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

let add templateName fieldValues (db: CardOverflowDb) userId tags = task {
    let cardTemplateInstance =
        db.CardTemplateInstance
            .Include(fun x -> x.CardTemplate)
            .Include(fun x -> x.User_CardTemplateInstances)
            .First(fun x -> x.CardTemplate.CardTemplateInstances.Single().Name = templateName)
            |> AcquiredCardTemplateInstance.load
    let fieldValues =
        match fieldValues with
        | [] -> ["Front"; "Back"]
        | _ -> fieldValues
    let! ac = CardRepository.getNew db userId
    let ac = { ac with Tags = tags }
    let! r =
        {   TemplateInstance = cardTemplateInstance.CardTemplateInstance
            FieldValues =
                cardTemplateInstance.CardTemplateInstance.Fields
                |> Seq.sortBy (fun x -> x.Ordinal)
                |> Seq.mapi (fun i field -> {
                    Field = field
                    Value = fieldValues.[i]
                    CommunalCardInstanceIds = [].ToList()
                }) |> toResizeArray
            EditSummary = "Initial creation"
        } |> CardRepository.UpdateFieldsToNewInstance db ac
    return Result.getOk r
    }

let addReversedBasicCard: CardOverflowDb -> int -> string list -> Task<unit> =
    add "Basic (and reversed card) - Card 1" []

let addBasicCard =
    add "Basic" []

let addBasicCustomCard x =
    add "Basic" x

[<Fact>]
let ``CardRepository.CreateCard on a basic facet acquires 1 card/facet``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let tags = ["a"; "b"]
    
    do! addBasicCard c.Db userId tags

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
    )
    let! view = CardRepository.getView c.Db 1
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
        view.FieldValues
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
    )
    }

open CardOverflow.Sanitation
[<Fact>]
let ``CardRepository.UpdateFieldsToNewInstance on a basic card updates the fields``() : Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let tags = ["a"; "b"]
    do! addBasicCard c.Db userId tags
    let cardId = 1
    let newValue = Guid.NewGuid().ToString()
    let! acquiredCard = 
        (CardRepository.GetAcquired c.Db userId cardId)
            .ContinueWith(fun (x: Task<Result<AcquiredCard, string>>) -> Result.getOk x.Result)
    let! old = SanitizeCardRepository.getEdit c.Db cardId
    let old = Result.getOk old
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! x = CardRepository.UpdateFieldsToNewInstance c.Db acquiredCard updated.load
    Result.getOk x
    
    let! refreshed = CardRepository.getView c.Db cardId
    Assert.Equal<string seq>(
        [newValue; newValue],
        refreshed.FieldValues.Select(fun x -> x.Value))
    Assert.Equal(
        2,
        c.Db.CardInstance.Count(fun x -> x.CardId = cardId))
    let! card = CardRepository.Get c.Db userId cardId
    Assert.Equal<ViewTag seq>(
        [{  Name = "a"
            Count = 1
            IsAcquired = true }
         {  Name = "b"
            Count = 1
            IsAcquired = true }],
        card.Tags)
    Assert.Equal<string seq>(
        [newValue; newValue],
        refreshed.FieldValues.OrderBy(fun x -> x.Field.Ordinal).Select(fun x -> x.Value)
    )
    let createds = c.Db.CardInstance.Select(fun x -> x.Created) |> Seq.toList
    Assert.NotEqual(createds.[0], createds.[1])

    let! revisions = CardRepository.Revisions c.Db userId cardId
    Assert.Equal(2, revisions.SortedMeta.Count())
    let! instance = CardRepository.instance c.Db revisions.SortedMeta.[0].Id
    let revision, _, _, _ = instance |> Result.getOk |> fun x -> x.FrontBackFrontSynthBackSynth
    Assert.Contains(newValue, revision)
    let! instance = CardRepository.instance c.Db revisions.SortedMeta.[1].Id
    let original, _, _, _ = instance |> Result.getOk |> fun x -> x.FrontBackFrontSynthBackSynth
    Assert.Contains("Front", original)
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
//            CardTemplateId = 1,
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

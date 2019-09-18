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

let add templateName (db: CardOverflowDb) userId tags =
    let cardTemplateInstance =
        db.CardTemplateInstance
            .Include(fun x -> x.Fields)
            .Include(fun x -> x.CardTemplate)
            .Include(fun x -> x.User_CardTemplateInstances)
            .First(fun x -> x.CardTemplate.Name = templateName)
            |> AcquiredCardTemplateInstance.load
    TagRepository.Add db userId tags
    let tags =
        TagRepository.GetAll db userId
        |> Seq.map (fun x -> x.Id)
    let initialCard = {
        CardTemplateHash = cardTemplateInstance.CardTemplateInstance.AcquireHash
        AuthorId = userId
        Description = templateName
        DefaultCardOptionId = cardTemplateInstance.DefaultCardOptionId
        CardTemplateIdsAndTags = cardTemplateInstance.CardTemplateInstance |> fun x -> x.Id, tags
        FieldValues =
            cardTemplateInstance.CardTemplateInstance.Fields
            |> Seq.sortBy (fun x -> x.Ordinal)
            |> Seq.map (fun x -> { Field = x; Value = x.Name })
    }
    CardRepository.CreateCard db initialCard <| Seq.empty.ToList()

let addReversedBasicCard: CardOverflowDb -> int -> seq<string> -> unit =
    add "Basic (and reversed card) - Card 2"

let addBasicCard =
    add "Basic"

[<Fact>]
let ``CardRepository.CreateCard on a basic facet acquires 1 card/facet``() =
    use c = new TestContainer()
    let userId = 3
    let tags = ["a"; "b"]
    
    addBasicCard c.Db userId tags

    Assert.SingleI <| c.Db.Card
    Assert.SingleI <| c.Db.Card
    Assert.SingleI <| c.Db.AcquiredCard
    Assert.SingleI <| CardRepository.GetAllCards c.Db userId
    Assert.Equal(
        "<html>\r\n    <head>\r\n        <style>\r\n            .card {\r\n font-family: arial;\r\n font-size: 20px;\r\n text-align: center;\r\n color: black;\r\n background-color: white;\r\n}\r\n\r\n        </style>\r\n    </head>\r\n    <body>\r\n        Front\r\n    </body>\r\n</html>",
        (CardRepository.GetTodaysCards c.Db userId).GetAwaiter().GetResult()
        |> Seq.head
        |> Result.getOk
        |> fun x -> x.Front
    )
    Assert.Equal(
        "<html>\r\n    <head>\r\n        <style>\r\n            .card {\r\n font-family: arial;\r\n font-size: 20px;\r\n text-align: center;\r\n color: black;\r\n background-color: white;\r\n}\r\n\r\n        </style>\r\n    </head>\r\n    <body>\r\n        Front\r\n\r\n<hr id=answer>\r\n\r\nBack\r\n    </body>\r\n</html>",
        (CardRepository.GetTodaysCards c.Db userId).GetAwaiter().GetResult()
        |> Seq.head
        |> Result.getOk
        |> fun x -> x.Back
    )
    Assert.Equal<FieldAndValue seq>(
        [{  Field = {
                Id = 1
                Name = "Front"
                Font = "Arial"
                FontSize = 20uy
                IsRightToLeft = false
                Ordinal = 0uy
                IsSticky = false }
            Value = "Front" }
         {  Field = {
                Id = 2
                Name = "Back"
                Font = "Arial"
                FontSize = 20uy
                IsRightToLeft = false
                Ordinal = 1uy
                IsSticky = false }
            Value = "Back"}],
        (CardRepository.GetAcquiredPages c.Db userId 1)
            .GetAwaiter()
            .GetResult()
            .Results
            .Single()
            |> Result.getOk
            |> fun x -> x.CardInstance.FieldValues
            |> Seq.sortByDescending (fun x -> x.Field.Name)
    )
    Assert.Equal<string seq>(
        tags,
        (CardRepository.GetAcquiredPages c.Db userId 1)
            .GetAwaiter()
            .GetResult()
            .Results
            .Single()
            |> Result.getOk
            |> fun x -> x.Tags
    )

[<Fact>]
let ``CardRepository.UpdateFieldsToNewInstance on a basic card updates the fields``() : Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let tags = ["a"; "b"]
    addBasicCard c.Db userId tags
    let cardId = 1
    let newValue = ""
    let! old = 
        (CardRepository.GetAcquired c.Db userId cardId)
            .ContinueWith(fun (x: Task<Result<AcquiredCard, string>>) -> Result.getOk x.Result)
    let updated = {
        old with
            CardInstance = {
                old.CardInstance with
                    FieldValues =
                        old.CardInstance.FieldValues
                        |> Seq.map (fun x ->
                            { x with Value = newValue}
                        )
            }
        }
    
    do! CardRepository.UpdateFieldsToNewInstance c.Db updated
    
    let! updated =
        (CardRepository.GetAcquired c.Db userId cardId)
            .ContinueWith(fun (x: Task<Result<AcquiredCard, string>>) -> Result.getOk x.Result)
    Assert.Equal<string seq>(
        [newValue; newValue],
        updated.CardInstance.FieldValues.Select(fun x -> x.Value)
    )
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

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

let add templateName (db: CardOverflowDb) userId tags =
    let facetTemplate =
        db.FacetTemplateInstance
            .Include(fun x -> x.CardTemplates)
            .Include(fun x -> x.Fields)
            .Include(fun x -> x.FacetTemplate)
            .Include(fun x -> x.User_FacetTemplateInstances)
            .First(fun x -> x.FacetTemplate.Name = templateName)
            |> AcquiredFacetTemplateInstance.load
    PrivateTagRepository.Add db userId tags
    let privateTags =
        PrivateTagRepository.GetAll db userId
        |> Seq.map (fun x -> x.Id)
    let initialConcept = {
        FacetTemplateHash = facetTemplate.Instance.AcquireHash
        MaintainerId = userId
        Description = templateName
        DefaultCardOptionId = facetTemplate.DefaultCardOptionId
        CardTemplateIdsAndTags = facetTemplate.Instance.CardTemplates |> Seq.map (fun x -> x.Id, privateTags)
        FieldValues =
            facetTemplate.Instance.Fields
            |> Seq.sortBy (fun x -> x.Ordinal)
            |> Seq.map (fun x -> { FieldId = x.Id; Value = x.Name })
    }
    ConceptRepository.CreateConcept db initialConcept <| Seq.empty.ToList()

let addBasicAndReversedConcept: CardOverflowDb -> int -> seq<string> -> unit =
    add "Basic (and reversed card)"

let addBasicConcept =
    add "Basic"

[<Fact>]
let ``FacetRepository.CreateFacet on a basic facet acquires 1 card/facet``() =
    use c = new TestContainer()
    let userId = 3
    let tags = ["a"; "b"]
    
    addBasicConcept c.Db userId tags

    Assert.SingleI <| c.Db.Facet
    Assert.SingleI <| c.Db.Card
    Assert.SingleI <| c.Db.AcquiredCard
    Assert.SingleI <| CardRepository.GetAllCards c.Db userId
    Assert.Equal(
        "<html>\r\n    <head>\r\n        <style>\r\n            .card {\r\n font-family: arial;\r\n font-size: 20px;\r\n text-align: center;\r\n color: black;\r\n background-color: white;\r\n}\r\n\r\n        </style>\r\n    </head>\r\n    <body>\r\n        Front\r\n    </body>\r\n</html>",
        (CardRepository.GetTodaysCards c.Db userId).GetAwaiter().GetResult()
        |> Seq.head
        |> Result.getOk
        |> fun x -> x.Question
    )
    Assert.Equal(
        "<html>\r\n    <head>\r\n        <style>\r\n            .card {\r\n font-family: arial;\r\n font-size: 20px;\r\n text-align: center;\r\n color: black;\r\n background-color: white;\r\n}\r\n\r\n        </style>\r\n    </head>\r\n    <body>\r\n        Front\r\n\r\n<hr id=answer>\r\n\r\nBack\r\n    </body>\r\n</html>",
        (CardRepository.GetTodaysCards c.Db userId).GetAwaiter().GetResult()
        |> Seq.head
        |> Result.getOk
        |> fun x -> x.Answer
    )
    Assert.Equal<FieldAndValue seq>(
        [{  Field = {
                Id = 10
                Name = "Front"
                Font = "Arial"
                FontSize = 20uy
                IsRightToLeft = false
                Ordinal = 0uy
                IsSticky = false }
            Value = "Front" }
         {  Field = {
                Id = 9
                Name = "Back"
                Font = "Arial"
                FontSize = 20uy
                IsRightToLeft = false
                Ordinal = 1uy
                IsSticky = false }
            Value = "Back"}],
        (ConceptRepository.GetAcquiredAsync c.Db userId 1)
            .GetAwaiter()
            .GetResult()
            .Results
            .Single().AcquiredFacets.Single().FacetFields.OrderByDescending(fun x -> x)
    )
    Assert.Equal<string seq>(
        tags,
        (ConceptRepository.GetAcquiredAsync c.Db userId 1)
            .GetAwaiter()
            .GetResult()
            .Results
            .Single().AcquiredFacets.Single().Cards.SelectMany(fun x -> x.Tags)
    )

// fuck merge
//[<Fact>]
//let ``FacetRepository's SaveFacets updates a Facet``() =
//    use c = new TestContainer()
//    let facet = 
//        FacetEntity(
//            Title = "",
//            Description = "",
//            Fields = "",
//            FacetTemplateId = 1,
//            Modified = DateTime.UtcNow)
    
//    FacetRepository.AddFacet c.Db facet

//    let updatedFacet = FacetRepository.GetFacets c.Db |> Seq.head
//    let updatedTitle = Guid.NewGuid().ToString()
//    updatedFacet.Title <- updatedTitle

//    updatedFacet 
//    |> Seq.singleton 
//    |> ResizeArray<FacetEntity>
//    |> FacetRepository.SaveFacets c.Db

//    FacetRepository.GetFacets c.Db
//    |> Seq.filter(fun x -> x.Title = updatedTitle)
//    |> Assert.Single

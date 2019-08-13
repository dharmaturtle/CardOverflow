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

[<Fact>]
let ``FacetRepository.CreateFacet on a basic facet acquires 1 card/facet``() =
    use c = new TestContainer()
    let userId = 3
    let facetTemplate =
        c.Db.FacetTemplateInstance
            .Include(fun x -> x.CardTemplates)
            .Include(fun x -> x.Fields)
            .Include(fun x -> x.FacetTemplate)
            .Include(fun x -> x.User_FacetTemplateInstances)
            .First(fun x -> x.FacetTemplate.Name = "Basic")
            |> FacetTemplateInstance.Load
    let initialConcept = {
        FacetTemplateHash = facetTemplate.AcquireHash
        MaintainerId = userId
        Description = "Basic"
        DefaultCardOptionId = facetTemplate.DefaultCardOptionId
        CardTemplateIds = facetTemplate.CardTemplates |> Seq.map (fun x -> x.Id)
        FieldValues =
            facetTemplate.Fields
            |> Seq.sortBy (fun x -> x.Ordinal)
            |> Seq.map (fun x -> { FieldId = x.Id; Value = x.Name })
    }
    
    ConceptRepository.CreateConcept c.Db initialConcept <| Seq.empty.ToList()

    Assert.SingleI <| c.Db.Facet
    Assert.SingleI <| c.Db.Card
    Assert.SingleI <| c.Db.AcquiredCard
    Assert.SingleI <| CardRepository.GetQuizCards c.Db userId
    Assert.Equal(
        "Front",
        (CardRepository.GetNextCard c.Db userId).GetAwaiter().GetResult()
        |> Result.getOk
        |> fun x -> x.Question
    )
    Assert.Equal<string seq>(
        ["Front"; "Back"],
        (ConceptRepository.GetConcepts c.Db userId)
            .Single().Facets.Single().FacetInstances.Single().Fields.OrderByDescending(fun x -> x)
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

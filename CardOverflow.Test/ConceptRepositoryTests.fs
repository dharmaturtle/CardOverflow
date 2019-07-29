module ConceptRepositoryTests

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
let ``ConceptRepository.CreateConcept on a basic concept acquires 1 card/concept``() =
    use c = new TestContainer()
    let userId = 3
    let conceptTemplate =
        c.Db.ConceptTemplateInstances
            .Include(fun x -> x.CardTemplates)
            .Include(fun x -> x.ConceptTemplate.ConceptTemplateDefaultConceptTemplateUsers :> IEnumerable<_>)
                .ThenInclude(fun (x: ConceptTemplateDefaultConceptTemplateUserEntity) -> x.ConceptTemplateDefault)
            .First(fun x -> x.ConceptTemplate.Name = "Basic")
    let basicConcept = {
        ConceptTemplateHash = conceptTemplate.AcquireHash
        MaintainerId = userId
        Name = "Basic"
        DefaultCardOptionId = conceptTemplate.DefaultCardOptionId
        CardTemplateIds = conceptTemplate.CardTemplates |> Seq.map (fun x -> x.Id)
        FieldValues =
            conceptTemplate.Fields
            |> Seq.map (fun x -> { FieldId = x.Id; Value = x.Name })
        IsPublic = true
    }
    
    ConceptRepository.CreateConcept c.Db basicConcept

    Assert.SingleI <| c.Db.Concepts
    Assert.SingleI <| c.Db.Cards
    Assert.SingleI <| c.Db.AcquiredCards
    Assert.SingleI <| CardRepository.GetQuizCards c.Db userId

// fuck merge
//[<Fact>]
//let ``ConceptRepository's SaveConcepts updates a Concept``() =
//    use c = new TestContainer()
//    let concept = 
//        ConceptEntity(
//            Title = "",
//            Description = "",
//            Fields = "",
//            ConceptTemplateId = 1,
//            Modified = DateTime.UtcNow)
    
//    ConceptRepository.AddConcept c.Db concept

//    let updatedConcept = ConceptRepository.GetConcepts c.Db |> Seq.head
//    let updatedTitle = Guid.NewGuid().ToString()
//    updatedConcept.Title <- updatedTitle

//    updatedConcept 
//    |> Seq.singleton 
//    |> ResizeArray<ConceptEntity>
//    |> ConceptRepository.SaveConcepts c.Db

//    ConceptRepository.GetConcepts c.Db
//    |> Seq.filter(fun x -> x.Title = updatedTitle)
//    |> Assert.Single

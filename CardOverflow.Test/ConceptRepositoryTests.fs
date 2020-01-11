module ConceptRepositoryTests

open CardOverflow.Pure.Core
open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Api
open ContainerExtensions
open LoadersAndCopiers
open Helpers
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Test
open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Quotations
open System.Linq
open Xunit
open System
open SimpleInjector
open SimpleInjector.Lifestyles
open System.Diagnostics
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Sanitation
open System.Collections

[<Fact>]
let ``Getting 10 pages of GetAcquiredConceptsAsync takes less than 1 minute``(): Task<unit> = task {
    use c = new Container()
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    use __ = AsyncScopedLifestyle.BeginScope c
    let db = c.GetInstance<CardOverflowDb>()
    let userId = 3

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        let! _ = CardRepository.GetAcquiredPages db userId i ""
        ()
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)
    }

[<Fact>]
let ``GetForUser isn't empty``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    do! FacetRepositoryTests.addBasicCard c.Db userId ["a"; "b"]
    do! CommentCardEntity (
            CardId = 1,
            UserId = userId,
            Text = "text",
            Created = DateTime.UtcNow
        ) |> CommentRepository.addAndSaveAsync c.Db
    let cardId = 1
        
    let! card = CardRepository.Get c.Db userId cardId
    let! view = CardRepository.getView c.Db cardId
        
    let front, _, _, _ = view.FrontBackFrontSynthBackSynth
    Assert.DoesNotContain("{{Front}}", front)
    Assert.NotEmpty <| card.Comments
    Assert.True card.IsAcquired
    Assert.Equal<ViewTag seq>(
        [{  Name = "a"
            Count = 1
            IsAcquired = true }
         {  Name = "b"
            Count = 1
            IsAcquired = true }],
        card.Tags
    )}

[<Fact>]
let ``Getting 10 pages of GetAsync takes less than 1 minute, and has users``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    do! FacetRepositoryTests.addBasicCard c.Db userId ["a"; "b"]

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        let! _ = CardRepository.SearchAsync c.Db userId i ""
        ()
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)
    
    let! card = CardRepository.Get c.Db userId 1
    Assert.Equal(1, card.Summary.Users)
    Assert.Equal<ViewTag seq>(
        [{  Name = "a"
            Count = 1
            IsAcquired = true }
         {  Name = "b"
            Count = 1
            IsAcquired = true }],
        card.Tags
    )}

let testGetAcquired (cardIds: int list) addCards name = task {
    use c = new TestContainer(name)
    
    let userId = 1 // this user creates the card
    for addCard in addCards do
        do! addCard c.Db userId ["a"]
    let! acquiredCards = CardRepository.GetAcquiredPages c.Db userId 1 ""
    Assert.Equal(
        cardIds.Count(),
        acquiredCards.Results.Count()
    )
    let! card = CardRepository.GetAcquired c.Db userId 1
    let card = card |> Result.getOk
    Assert.Equal(userId, card.UserId)
    let relationshipName = "test relationship"
    let addRelationshipCommand1 =
        {   Name = relationshipName
            SourceId = 1
            TargetLink = "2"
        }
    if cardIds.Length <> 1 then
        do! SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1 |> Result.getOk
        let! card = CardRepository.Get c.Db userId 1
        Assert.Equal(1, card.Relationships.Single().Users)
        let! card = CardRepository.Get c.Db userId 2
        Assert.Equal(1, card.Relationships.Single().Users)

        do! SanitizeRelationshipRepository.Remove c.Db 1 2 userId relationshipName
        let! card = CardRepository.Get c.Db userId 1
        Assert.Equal(0, card.Relationships.Count)
        let! card = CardRepository.Get c.Db userId 2
        Assert.Equal(0, card.Relationships.Count)

        do! SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1 |> Result.getOk
        do! SanitizeRelationshipRepository.Remove c.Db 2 1 userId relationshipName
        let! card = CardRepository.Get c.Db userId 1
        Assert.Equal(0, card.Relationships.Count)
        let! card = CardRepository.Get c.Db userId 2
        Assert.Equal(0, card.Relationships.Count)
    
    let userId = 2 // this user acquires the card
    if cardIds.Length = 1 then
        do! CardRepository.AcquireCardAsync c.Db userId cardIds.[0]
    else
        do! CardRepository.AcquireCardAsync c.Db userId cardIds.[0]
        do! CardRepository.AcquireCardAsync c.Db userId cardIds.[1]
    let! card = CardRepository.Get c.Db userId 1
    Assert.Equal<ViewTag seq>(
        [{  Name = "a"
            Count = 1
            IsAcquired = false }],
        card.Tags
    )
    let! card = CardRepository.GetAcquired c.Db userId 1
    let card = card |> Result.getOk
    TagRepository.AddTo c.Db "a" card.AcquiredCardId
    let! card = CardRepository.Get c.Db userId 1
    Assert.Equal<ViewTag seq>(
        [{  Name = "a"
            Count = 2
            IsAcquired = true }],
        card.Tags
    )
    if cardIds.Length <> 1 then
        let addRelationshipCommand2 =
            {   Name = relationshipName
                SourceId = 2
                TargetLink = "1"
            }
        let commands = [
            addRelationshipCommand1, addRelationshipCommand1
            addRelationshipCommand2, addRelationshipCommand2
            addRelationshipCommand1, addRelationshipCommand2
            addRelationshipCommand2, addRelationshipCommand1 ]
        let testRelationships (creator, acquirer) = task {
            do! SanitizeRelationshipRepository.Add c.Db 1 creator |> Result.getOk // card creator also acquires the relationship; .Single() below refers this this
        
            do! SanitizeRelationshipRepository.Add c.Db userId acquirer |> Result.getOk
            let! card = CardRepository.Get c.Db userId 1
            Assert.Equal(2, card.Relationships.Single().Users)
            Assert.True(card.Relationships.Single().IsAcquired)
            let! card = CardRepository.Get c.Db userId 2
            Assert.Equal(2, card.Relationships.Single().Users)
            Assert.True(card.Relationships.Single().IsAcquired)
        
            do! SanitizeRelationshipRepository.Remove c.Db 1 2 userId relationshipName
            let! card = CardRepository.Get c.Db userId 1
            Assert.Equal(1, card.Relationships.Count)
            Assert.False(card.Relationships.Single().IsAcquired)
            let! card = CardRepository.Get c.Db userId 2
            Assert.Equal(1, card.Relationships.Count)
            Assert.False(card.Relationships.Single().IsAcquired)

            do! SanitizeRelationshipRepository.Add c.Db userId acquirer |> Result.getOk
            do! SanitizeRelationshipRepository.Remove c.Db 2 1 userId relationshipName
            let! card = CardRepository.Get c.Db userId 1
            Assert.Equal(1, card.Relationships.Count)
            Assert.False(card.Relationships.Single().IsAcquired)
            let! card = CardRepository.Get c.Db userId 2
            Assert.Equal(1, card.Relationships.Count)
            Assert.False(card.Relationships.Single().IsAcquired)
            
            do! SanitizeRelationshipRepository.Remove c.Db 1 2 1 relationshipName // cleanup from do! SanitizeRelationshipRepository.Add c.Db 1 a |> Result.getOk
        }
        do! testRelationships commands.[0]
        do! testRelationships commands.[1]
        do! testRelationships commands.[2]
        do! testRelationships commands.[3]
    let userId = 3 // this user never acquires the card
    if cardIds.Length = 1 then
        let! cards = CardRepository.SearchAsync c.Db userId 1 ""
        Assert.Equal(
            cardIds.Count(),
            cards.Results.Count()
        )
        let! card = CardRepository.Get c.Db userId 1
        Assert.Equal<ViewTag seq>(
            [{  Name = "a"
                Count = 2
                IsAcquired = false }],
            card.Tags
        )
    else
        let! cards = CardRepository.SearchAsync c.Db userId 1 ""
        Assert.Equal(
            cardIds.Count(),
            cards.Results.Count()
        )
        let! card1 = CardRepository.Get c.Db userId 1
        Assert.Equal<ViewTag seq>(
            [{  Name = "a"
                Count = 2
                IsAcquired = false }],
            card1.Tags
        )
        let! card2 = CardRepository.Get c.Db userId 2
        Assert.Equal<ViewTag seq>(
            [{  Name = "a"
                Count = 1
                IsAcquired = false }],
            card2.Tags
        )
    }
    
[<Fact>]
let rec ``GetAcquired works when acquiring 1 basic card``(): Task<unit> =
    testGetAcquired
        [1]
        [ FacetRepositoryTests.addBasicCard ]
        <| nameof <@ ``GetAcquired works when acquiring 1 basic card`` @>

[<Fact>]
let rec ``GetAcquired works when acquiring 1 card of a pair``(): Task<unit> = 
    testGetAcquired
        [1]
        [ FacetRepositoryTests.addReversedBasicCard ]
        <| nameof <@ ``GetAcquired works when acquiring 1 card of a pair`` @>

[<Fact>]
let rec ``GetAcquired works when acquiring 2 cards of a pair; also a lot of relationship tests``(): Task<unit> =
    testGetAcquired
        [1; 2]
        [ FacetRepositoryTests.addBasicCard; FacetRepositoryTests.addReversedBasicCard ]
        <| nameof <@ ``GetAcquired works when acquiring 2 cards of a pair; also a lot of relationship tests`` @>

[<Fact>]
let ``Card search works`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let basicTag = "basic tag"
    do! FacetRepositoryTests.addBasicCard c.Db userId [basicTag]
    let front = Guid.NewGuid().ToString()
    let back = Guid.NewGuid().ToString()
    do! FacetRepositoryTests.addBasicCustomCard [front; back] c.Db userId ["custom tag"]
    do! Task.Delay 10000 // give the full text index time to rebuild

    let search = CardRepository.SearchAsync c.Db userId 1
    
    let! cards = search ""
    Assert.Equal(2, cards.Results.Count())
    let! cards = search basicTag
    Assert.Equal(1, cards.Results.Single().Id)
    let! cards = search "Front"
    Assert.Equal(1, cards.Results.Single().Id)
    let! cards = search <| Guid.NewGuid().ToString()
    Assert.Empty(cards.Results)
    let! cards = search front
    Assert.Equal(2, cards.Results.Single().Id)
    let! cards = search back
    Assert.Equal(2, cards.Results.Single().Id)
    }

[<Fact>]
let ``New user has TheCollective's card templates`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! myTemplates = SanitizeCardTemplate.GetMine c.Db userId
    let theCollectiveId = c.Db.User.Single(fun x -> x.DisplayName = "The Collective").Id
    for template in myTemplates do
        Assert.Equal(theCollectiveId, template.AuthorId)
    }

[<Fact>]
let ``Can create card template and insert a modified one`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let initialCardTemplate = ViewCardTemplateWithAllInstances.initialize userId

    do! SanitizeCardTemplate.Update c.Db userId initialCardTemplate.Editable |> Result.getOk
    let! myTemplates = SanitizeCardTemplate.GetMine c.Db userId

    Assert.True(myTemplates.Single(fun x -> x.AuthorId = userId).Editable.Fields.Any(fun x -> x.Name = initialCardTemplate.Editable.Fields.First().Name))
    
    // testing a brand new template, but slightly different
    let fieldName = Guid.NewGuid().ToString()
    let newEditable =
        let newField =
            {   Name = fieldName
                Font = ""
                FontSize = 0uy
                IsRightToLeft = false
                Ordinal = 0
                IsSticky = false
            }
        {   initialCardTemplate.Editable with
                Fields = initialCardTemplate.Editable.Fields.Append newField |> toResizeArray
        }
    do! SanitizeCardTemplate.Update c.Db userId newEditable |> Result.getOk
    
    Assert.Equal(2, c.Db.CardTemplate.Count(fun x -> x.AuthorId = userId))
    let! myTemplates = SanitizeCardTemplate.GetMine c.Db userId
    Assert.True(myTemplates.OrderBy(fun x -> x.Id).Last().Editable.Fields.Any(fun x -> x.Name = fieldName))
    }

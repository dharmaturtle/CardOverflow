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
open System.Security.Cryptography
open FsToolkit.ErrorHandling

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
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId ["a"; "b"]
    do! CommentCardEntity (
            CardId = 1,
            UserId = userId,
            Text = "text",
            Created = DateTime.UtcNow
        ) |> CommentRepository.addAndSaveAsync c.Db
    let cardId = 1
        
    let! card = CardRepository.Get c.Db userId cardId
    let card = card.Value
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
    )
    
    let! card = CardRepository.Get c.Db userId 9999
    Assert.Equal("Card #9999 not found", card.error) }

[<Fact>]
let ``Getting 10 pages of GetAsync takes less than 1 minute, and has users``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId ["a"; "b"]

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        let! _ = CardRepository.SearchAsync c.Db userId i ""
        ()
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)
    
    let! card = CardRepository.Get c.Db userId 1
    let card = card.Value
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
    use c = new TestContainer(false, name)
    
    let userId = 1 // this user creates the card
    for (addCard: CardOverflowDb -> int -> string list -> Task<ResizeArray<string * int>>) in addCards do
        let! x = addCard c.Db userId ["a"]
        Assert.Empty x
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
            SourceCardId = 1
            TargetCardLink = "2"
        }
    if cardIds.Length <> 1 then
        let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1
        Assert.Null x.Value
        let! card = CardRepository.Get c.Db userId 1
        Assert.Equal(1, card.Value.Relationships.Single().Users)
        let! card = CardRepository.Get c.Db userId 2
        Assert.Equal(1, card.Value.Relationships.Single().Users)

        let successfulRemove () = task {
            let! r = SanitizeRelationshipRepository.Remove c.Db 1 2 userId relationshipName
            Assert.Null r.Value
            let! card = CardRepository.Get c.Db userId 1
            Assert.Equal(0, card.Value.Relationships.Count)
            let! card = CardRepository.Get c.Db userId 2
            Assert.Equal(0, card.Value.Relationships.Count) }
        do! successfulRemove ()

        let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1
        Assert.Null x.Value
        let! r = SanitizeRelationshipRepository.Remove c.Db 2 1 userId relationshipName
        Assert.Equal("Relationship not found between source Card #2 and target Card #1 with name \"test relationship\".", r.error)
        let! card = CardRepository.Get c.Db userId 1
        Assert.Equal(1, card.Value.Relationships.Count)
        let! card = CardRepository.Get c.Db userId 2
        Assert.Equal(1, card.Value.Relationships.Count)

        do! successfulRemove ()
    
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
        card.Value.Tags
    )
    let! card = CardRepository.GetAcquired c.Db userId 1
    let card = card |> Result.getOk
    TagRepository.AddTo c.Db "a" card.AcquiredCardId
    let! card = CardRepository.Get c.Db userId 1
    Assert.Equal<ViewTag seq>(
        [{  Name = "a"
            Count = 2
            IsAcquired = true }],
        card.Value.Tags
    )

    let testRelationships userId = task {
        let addRelationshipCommand2 =
            {   Name = relationshipName
                SourceCardId = 2
                TargetCardLink = "1"
            }
        let commands = [
            addRelationshipCommand1, addRelationshipCommand1
            addRelationshipCommand2, addRelationshipCommand2
            addRelationshipCommand1, addRelationshipCommand2
            addRelationshipCommand2, addRelationshipCommand1 ]
        let testRelationships (creator, acquirer) = task {
            let! x = SanitizeRelationshipRepository.Add c.Db 1 creator // card creator also acquires the relationship; .Single() below refers this this
            Assert.Null x.Value
        
            let! x = SanitizeRelationshipRepository.Add c.Db userId acquirer
            Assert.Null x.Value
            let! card = CardRepository.Get c.Db userId 1
            let card = card.Value
            Assert.Equal(2, card.Relationships.Single().Users)
            Assert.True(card.Relationships.Single().IsAcquired)
            let! card = CardRepository.Get c.Db userId 2
            let card = card.Value
            Assert.Equal(2, card.Relationships.Single().Users)
            Assert.True(card.Relationships.Single().IsAcquired)

            let successfulRemove () = task {
                let! r = SanitizeRelationshipRepository.Remove c.Db 1 2 userId relationshipName
                Assert.Null r.Value
                let! card = CardRepository.Get c.Db userId 1
                let card = card.Value
                Assert.Equal(1, card.Relationships.Count)
                Assert.False(card.Relationships.Single().IsAcquired)
                let! card = CardRepository.Get c.Db userId 2
                let card = card.Value
                Assert.Equal(1, card.Relationships.Count)
                Assert.False(card.Relationships.Single().IsAcquired) }
            do! successfulRemove ()

            let! x = SanitizeRelationshipRepository.Add c.Db userId acquirer
            Assert.Null x.Value
            let! r = SanitizeRelationshipRepository.Remove c.Db 2 1 userId relationshipName
            Assert.Equal("Relationship not found between source Card #2 and target Card #1 with name \"test relationship\".", r.error)
            let! card = CardRepository.Get c.Db userId 1
            let card = card.Value
            Assert.Equal(1, card.Relationships.Count)
            Assert.True(card.Relationships.Single().IsAcquired)
            let! card = CardRepository.Get c.Db userId 2
            let card = card.Value
            Assert.Equal(1, card.Relationships.Count)
            Assert.True(card.Relationships.Single().IsAcquired)
            
            do! successfulRemove ()
            let! r = SanitizeRelationshipRepository.Remove c.Db 1 2 1 relationshipName // cleanup from do! SanitizeRelationshipRepository.Add c.Db 1 a |> Result.getOk
            Assert.Null r.Value }
        if cardIds.Length <> 1 then
            do! testRelationships commands.[0]
            do! testRelationships commands.[1]
            do! testRelationships commands.[2]
            do! testRelationships commands.[3]
        }

    let userId = 3 // this user hasn't acquired the card yet
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
            card.Value.Tags
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
            card1.Value.Tags
        )
        let! card2 = CardRepository.Get c.Db userId 2
        Assert.Equal<ViewTag seq>(
            [{  Name = "a"
                Count = 1
                IsAcquired = false }],
            card2.Value.Tags
        )
    if cardIds.Length <> 1 then // user3 acquires card in opposite order from user2
        do! CardRepository.AcquireCardAsync c.Db userId cardIds.[1]
        do! CardRepository.AcquireCardAsync c.Db userId cardIds.[0]
        do! testRelationships userId
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

[<NCrunch.Framework.Serial>]
[<Fact>]
let ``Card search works`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let basicTag = "basic"
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId [basicTag]
    let front = Guid.NewGuid().ToString()
    let back = Guid.NewGuid().ToString()
    let! _ = FacetRepositoryTests.addBasicCustomCard [front; back] c.Db userId ["custom"]
    let clozeText = "{{c1::" + Guid.NewGuid().ToString() + "}}"
    let! _ = FacetRepositoryTests.addCloze clozeText c.Db userId []
    do! Task.Delay 10000 // give the full text index time to rebuild
    let search = CardRepository.SearchAsync c.Db userId 1
    
    let! cards = search ""
    Assert.Equal(3, cards.Results.Count())
    let! cards = search basicTag
    Assert.Equal(1, cards.Results.Single().Id)
    let! cards = search "Front"
    Assert.Equal(1, cards.Results.Single().Id)
    let! cards = search "\"Front"
    Assert.Equal(1, cards.Results.Single().Id)
    let! cards = search "Fro*"
    Assert.Equal(1, cards.Results.Single().Id)
    let! cards = search <| Guid.NewGuid().ToString()
    Assert.Empty(cards.Results)
    let! cards = search front
    Assert.Equal(2, cards.Results.Single().Id)
    let! cards = search back
    Assert.Equal(2, cards.Results.Single().Id)
    let! cards = search clozeText
    Assert.Equal(3, cards.Results.Single().Id)

    let search = SanitizeTemplate.Search c.Db userId 1
    let! templates = search "Cloze"
    Assert.Equal("Cloze", templates.Results.Single().Name)
    Assert.Equal(3, templates.Results.Single().TemplateUsers)
    Assert.True(templates.Results.Single().IsAcquired)
    let! templates = search "type"
    Assert.Equal("Basic (type in the answer)", templates.Results.Single().Name)
    Assert.Equal(3, templates.Results.Single().TemplateUsers)
    Assert.True(templates.Results.Single().IsAcquired)
    let! templates = search "Basic"
    Assert.Equal(4, templates.Results.Count())
    Assert.True(templates.Results.All(fun x -> x.TemplateUsers = 3))
    Assert.True(templates.Results.All(fun x -> x.IsAcquired))
    }

[<Fact>]
let ``New user has TheCollective's card templates`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! myTemplates = SanitizeTemplate.GetMine c.Db userId
    let theCollectiveId = c.Db.User.Single(fun x -> x.DisplayName = "The Collective").Id
    for template in myTemplates do
        Assert.Equal(theCollectiveId, template.AuthorId)
    }

[<Fact>]
let ``Updating card template with duplicate field names yields error`` (): Task<unit> = task {
    let userId = 3
    let fieldName = Guid.NewGuid().ToString()
    let template = TemplateInstance.initialize |> ViewTemplateInstance.load
    let template = { template with Fields = template.Fields.Select(fun f -> { f with Name = fieldName }).ToList() }
    
    let! error = SanitizeTemplate.Update null userId template
    
    Assert.Equal("Field names must differ", error.error)
    }

[<Fact>]
let ``Can create card template and insert a modified one`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let name = Guid.NewGuid().ToString()
    let initialTemplate = ViewTemplateWithAllInstances.initialize userId

    let! x = SanitizeTemplate.Update c.Db userId { initialTemplate.Editable with Name = name }
    Assert.Null x.Value
    let! myTemplates = SanitizeTemplate.GetMine c.Db userId

    Assert.SingleI(myTemplates.Where(fun x -> x.Editable.Name = name))
    
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
        {   initialTemplate.Editable with
                Fields = initialTemplate.Editable.Fields.Append newField |> toResizeArray
        }
    let! x = SanitizeTemplate.Update c.Db userId newEditable
    Assert.Null x.Value
    
    Assert.Equal(2, c.Db.Template.Count(fun x -> x.AuthorId = userId))
    let! myTemplates = SanitizeTemplate.GetMine c.Db userId
    let latestTemplate = myTemplates.OrderBy(fun x -> x.Id).Last().Editable
    Assert.True(latestTemplate.Fields.Any(fun x -> x.Name = fieldName))

    // updating the slightly different template
    let name = Guid.NewGuid().ToString()
    let! x = SanitizeTemplate.Update c.Db userId { latestTemplate with Name = name }
    Assert.Null x.Value

    let! myTemplates = SanitizeTemplate.GetMine c.Db userId
    Assert.Equal(latestTemplate.TemplateId, myTemplates.Select(fun x -> x.Instances.Single()).Single(fun x -> x.Name = name).TemplateId)
    }

[<Fact>]
let ``New card template has correct hash`` (): Task<unit> = taskResult {
        use c = new TestContainer()
        let userId = 3
        let initialTemplate = ViewTemplateWithAllInstances.initialize userId
        use sha512 = SHA512.Create()
        do! SanitizeTemplate.Update c.Db userId initialTemplate.Editable
        let! (dbTemplate: TemplateInstanceEntity) = c.Db.TemplateInstance.SingleAsync(fun x -> x.Template.AuthorId = userId)
    
        let computedHash =
            initialTemplate.Editable
            |> ViewTemplateInstance.copyTo
            |> fun x -> TemplateEntity() |> Entity |> x.CopyToNewInstance
            |> TemplateInstanceEntity.hash sha512
    
        Assert.Equal<byte[]>(dbTemplate.Hash, computedHash) } |> TaskResult.assertOk

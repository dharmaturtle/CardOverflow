module ConceptRepositoryTests

open CardOverflow.Pure
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
let ``Getting 10 pages of GetAcquiredPages takes less than 1 minute``(): Task<unit> = task {
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
let ``GetAcquiredPages gets the acquired card if there's been an update``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId []
    let! collate = SanitizeCollate.AllInstances c.Db 1
    let secondVersion = Guid.NewGuid().ToString()
    let! _ =
        {   EditCardCommand.EditSummary = secondVersion
            FieldValues = [].ToList()
            CollateInstance = collate.Instances.Single() |> ViewCollateInstance.copyTo
            Kind = NewOriginal_TagIds []
        } |> UpdateRepository.card c.Db userId
    let oldInstanceId = 1001
    let updatedInstanceId = 1002
    do! c.Db.BranchInstance.SingleAsync(fun x -> x.Id = oldInstanceId)
        |> Task.map (fun x -> Assert.Equal("Initial creation", x.EditSummary))
    do! c.Db.BranchInstance.SingleAsync(fun x -> x.Id = updatedInstanceId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))

    let! (cards: PagedList<Result<AcquiredCard, string>>) = CardRepository.GetAcquiredPages c.Db userId 1 ""
    let cards = cards.Results |> Seq.map Result.getOk |> Seq.toList

    Assert.Equal(updatedInstanceId, cards.Single().BranchInstanceMeta.Id)

    // getAcquiredInstanceFromInstance gets the updatedInstanceId when given the oldInstanceId
    let! actual = AcquiredCardRepository.getAcquiredInstanceFromInstance c.Db userId oldInstanceId

    Assert.Equal(updatedInstanceId, actual)

    // getAcquiredInstanceFromInstance gets the updatedInstanceId when given the updatedInstanceId
    let! actual = AcquiredCardRepository.getAcquiredInstanceFromInstance c.Db userId updatedInstanceId

    Assert.Equal(updatedInstanceId, actual)
    } |> TaskResult.getOk)

[<Fact>]
let ``GetForUser isn't empty``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId ["A"; "B"]
    do! CommentCardEntity (
            CardId = 1,
            UserId = userId,
            Text = "text",
            Created = DateTime.UtcNow
        ) |> CommentRepository.addAndSaveAsync c.Db
    let cardId = 1
        
    let! card = ExploreCardRepository.get c.Db userId cardId
    let card = card.Value
    let! view = CardViewRepository.get c.Db cardId
        
    let front, _, _, _ = view.Value.FrontBackFrontSynthBackSynth.[0]
    Assert.DoesNotContain("{{Front}}", front)
    Assert.NotEmpty <| card.Comments
    Assert.True card.Instance.IsAcquired
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 1
            IsAcquired = true }
         {  Name = "B"
            Count = 1
            IsAcquired = true }],
        card.Tags
    )
    
    let! card = ExploreCardRepository.get c.Db userId 9999
    Assert.Equal("Card #9999 not found", card.error) }

[<Fact>]
let ``Getting 10 pages of GetAsync takes less than 1 minute, and has users``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId ["A"; "B"]

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        let! _ = CardRepository.SearchAsync c.Db userId i SearchOrder.Popularity ""
        ()
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)
    
    let! card = ExploreCardRepository.get c.Db userId 1
    let card = card.Value
    Assert.Equal(1, card.Summary.Users)
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 1
            IsAcquired = true }
         {  Name = "B"
            Count = 1
            IsAcquired = true }],
        card.Tags)
    
    let! ac = CardRepository.GetAcquired c.Db userId card.Id
    let ac = ac.Value.Single()
    let! x = CardRepository.editState c.Db userId ac.AcquiredCardId CardState.Suspended
    Assert.Null x.Value
    let! card = ExploreCardRepository.get c.Db userId 1
    Assert.Equal(0, card.Value.Summary.Users) // suspended cards don't count to User count
    }

let testGetAcquired (acCount: int) addCards name = task {
    use c = new TestContainer(false, name)
    
    let userId = 1 // this user creates the card
    for (addCard: CardOverflowDb -> int -> string list -> Task<int>) in addCards do
        let! _ = addCard c.Db userId ["A"]
        ()
    let! acquiredCards = CardRepository.GetAcquiredPages c.Db userId 1 ""
    Assert.Equal(acCount, acquiredCards.Results.Count())
    let! ac = CardRepository.GetAcquired c.Db userId 1
    let ac = ac.Value
    Assert.Equal(userId, ac.Select(fun x -> x.UserId).Distinct().Single())
    
    let userId = 2 // this user acquires the card
    do! CardRepository.AcquireCardAsync c.Db userId 1001 |> TaskResult.getOk
    let! card = ExploreCardRepository.get c.Db userId 1 |> TaskResult.getOk
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 1
            IsAcquired = false }],
        card.Tags
    )
    do! SanitizeTagRepository.AddTo c.Db userId "a" card.Id |> TaskResult.getOk
    let! card = ExploreCardRepository.get c.Db userId 1 |> TaskResult.getOk
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 2
            IsAcquired = true }],
        card.Tags
    )
    let! cards = CardRepository.SearchAsync c.Db userId 1 SearchOrder.Popularity ""
    Assert.Equal(1, cards.Results.Count())

    let userId = 3 // this user never acquires the card
    let! card = ExploreCardRepository.get c.Db userId 1 |> TaskResult.getOk
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 2
            IsAcquired = false }],
        card.Tags
    )}
    
[<Fact>]
let rec ``GetAcquired works when acquiring 1 basic card``(): Task<unit> =
    testGetAcquired
        1
        [ FacetRepositoryTests.addBasicCard ]
        <| nameof ``GetAcquired works when acquiring 1 basic card``

[<Fact>]
let rec ``GetAcquired works when acquiring a pair``(): Task<unit> = 
    testGetAcquired
        2
        [ FacetRepositoryTests.addReversedBasicCard ]
        <| nameof ``GetAcquired works when acquiring a pair``

let relationshipTestInit (c: TestContainer) relationshipName = task {
    let addRelationshipCommand1 =
        {   Name = relationshipName
            SourceCardId = 1
            TargetCardLink = "2"
        }
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

    let userId = 1 // this user creates the card
    for (addCard: CardOverflowDb -> int -> string list -> Task<int>) in [ FacetRepositoryTests.addBasicCard; FacetRepositoryTests.addReversedBasicCard ] do
        let! _ = addCard c.Db userId []
        ()

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1
    Assert.Null x.Value
    let! card = ExploreCardRepository.get c.Db userId 1
    Assert.Equal(1, card.Value.Relationships.Single().Users)
    let! card = ExploreCardRepository.get c.Db userId 2
    Assert.Equal(1, card.Value.Relationships.Single().Users)

    let successfulRemove () = task {
        let! r = SanitizeRelationshipRepository.Remove c.Db 1 2 userId relationshipName
        Assert.Null r.Value
        let! card = ExploreCardRepository.get c.Db userId 1
        Assert.Equal(0, card.Value.Relationships.Count)
        let! card = ExploreCardRepository.get c.Db userId 2
        Assert.Equal(0, card.Value.Relationships.Count) }
    do! successfulRemove ()

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1
    Assert.Null x.Value
    let! r = SanitizeRelationshipRepository.Remove c.Db 2 1 userId relationshipName
    Assert.Equal(sprintf "Relationship not found between source Card #2 and target Card #1 with name \"%s\"." relationshipName, r.error)
    let! card = ExploreCardRepository.get c.Db userId 1
    Assert.Equal(1, card.Value.Relationships.Count)
    let! card = ExploreCardRepository.get c.Db userId 2
    Assert.Equal(1, card.Value.Relationships.Count)
    do! successfulRemove ()

    return commands }

[<Fact>]
let ``Relationships can't be self related``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! actualBranchId = FacetRepositoryTests.addBasicCard c.Db userId []
    Assert.Equal(1, actualBranchId)
    let addRelationshipCommand =
        {   Name = ""
            SourceCardId = 1
            TargetCardLink = string 1
        }

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand
    
    Assert.Equal("A card can't be related to itself", x.error) }

[<Fact>]
let ``Directional relationship tests``(): Task<unit> = task {
    let branchInstanceIds = [1001; 1002]
    use c = new TestContainer()
    let relationshipName = "Test/Relationship"
    
    let! commands = relationshipTestInit c relationshipName
    let testRelationships userId (creator, acquirer) = task {
        let! x = SanitizeRelationshipRepository.Add c.Db 1 creator // card creator also acquires the relationship; .Single() below refers this this
        Assert.Null x.Value
        
        let! x = SanitizeRelationshipRepository.Add c.Db userId acquirer
        Assert.Null x.Value
        let! card = ExploreCardRepository.get c.Db userId 1
        let card = card.Value
        Assert.Equal(2, card.Relationships.Single().Users)
        Assert.True(card.Relationships.Single().IsAcquired)
        let! card = ExploreCardRepository.get c.Db userId 2
        let card = card.Value
        Assert.Equal(2, card.Relationships.Single().Users)
        Assert.True(card.Relationships.Single().IsAcquired)

        let successfulRemove () = task {
            let! r = SanitizeRelationshipRepository.Remove c.Db acquirer.SourceCardId (int acquirer.TargetCardLink) userId relationshipName
            Assert.Null r.Value
            let! card = ExploreCardRepository.get c.Db userId 1
            let card = card.Value
            Assert.Equal(1, card.Relationships.Count)
            Assert.False(card.Relationships.Single().IsAcquired)
            let! card = ExploreCardRepository.get c.Db userId 2
            let card = card.Value
            Assert.Equal(1, card.Relationships.Count)
            Assert.False(card.Relationships.Single().IsAcquired) }
        do! successfulRemove ()

        let! x = SanitizeRelationshipRepository.Add c.Db userId acquirer
        Assert.Null x.Value
        let! r = SanitizeRelationshipRepository.Remove c.Db (int acquirer.TargetCardLink) acquirer.SourceCardId userId relationshipName
        Assert.Equal(sprintf "Relationship not found between source Card #%i and target Card #%i with name \"%s\"." (int acquirer.TargetCardLink) acquirer.SourceCardId relationshipName, r.error)
        let! card = ExploreCardRepository.get c.Db userId 1
        let card = card.Value
        Assert.Equal(1, card.Relationships.Count)
        Assert.True(card.Relationships.Single().IsAcquired)
        let! card = ExploreCardRepository.get c.Db userId 2
        let card = card.Value
        Assert.Equal(1, card.Relationships.Count)
        Assert.True(card.Relationships.Single().IsAcquired)
            
        do! successfulRemove ()
        let! r = SanitizeRelationshipRepository.Remove c.Db acquirer.SourceCardId (int acquirer.TargetCardLink) 1 relationshipName // cleanup from do! SanitizeRelationshipRepository.Add c.Db 1 a |> Result.getOk
        Assert.Null r.Value }

    let userId = 2 // this user acquires the card
    do! CardRepository.AcquireCardAsync c.Db userId branchInstanceIds.[0] |> TaskResult.getOk
    do! CardRepository.AcquireCardAsync c.Db userId branchInstanceIds.[1] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    //do! testRelationships userId commands.[2]
    //do! testRelationships userId commands.[3]

    let userId = 3 // this user acquires card in opposite order from user2
    do! CardRepository.AcquireCardAsync c.Db userId branchInstanceIds.[1] |> TaskResult.getOk
    do! CardRepository.AcquireCardAsync c.Db userId branchInstanceIds.[0] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    //do! testRelationships userId commands.[2]
    //do! testRelationships userId commands.[3]
    }

[<Fact>]
let ``Nondirectional relationship tests``(): Task<unit> = task {
    let branchInstanceIds = [1001; 1002]
    use c = new TestContainer()
    let relationshipName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    
    let! commands = relationshipTestInit c relationshipName
    let testRelationships userId (creator, acquirer) = task {
        let! x = SanitizeRelationshipRepository.Add c.Db 1 creator // card creator also acquires the relationship; .Single() below refers this this
        Assert.Null x.Value
        
        let! x = SanitizeRelationshipRepository.Add c.Db userId acquirer
        Assert.Null x.Value
        let! card = ExploreCardRepository.get c.Db userId 1
        let card = card.Value
        Assert.Equal(2, card.Relationships.Single().Users)
        Assert.True(card.Relationships.Single().IsAcquired)
        let! card = ExploreCardRepository.get c.Db userId 2
        let card = card.Value
        Assert.Equal(2, card.Relationships.Single().Users)
        Assert.True(card.Relationships.Single().IsAcquired)

        let successfulRemove () = task {
            let! r = SanitizeRelationshipRepository.Remove c.Db 1 2 userId relationshipName
            Assert.Null r.Value
            let! card = ExploreCardRepository.get c.Db userId 1
            let card = card.Value
            Assert.Equal(1, card.Relationships.Count)
            Assert.False(card.Relationships.Single().IsAcquired)
            let! card = ExploreCardRepository.get c.Db userId 2
            let card = card.Value
            Assert.Equal(1, card.Relationships.Count)
            Assert.False(card.Relationships.Single().IsAcquired) }
        do! successfulRemove ()

        let! x = SanitizeRelationshipRepository.Add c.Db userId acquirer
        Assert.Null x.Value
        let! r = SanitizeRelationshipRepository.Remove c.Db 2 1 userId relationshipName
        Assert.Equal(sprintf "Relationship not found between source Card #2 and target Card #1 with name \"%s\"." relationshipName, r.error)
        let! card = ExploreCardRepository.get c.Db userId 1
        let card = card.Value
        Assert.Equal(1, card.Relationships.Count)
        Assert.True(card.Relationships.Single().IsAcquired)
        let! card = ExploreCardRepository.get c.Db userId 2
        let card = card.Value
        Assert.Equal(1, card.Relationships.Count)
        Assert.True(card.Relationships.Single().IsAcquired)
            
        do! successfulRemove ()
        let! r = SanitizeRelationshipRepository.Remove c.Db 1 2 1 relationshipName // cleanup from do! SanitizeRelationshipRepository.Add c.Db 1 a |> Result.getOk
        Assert.Null r.Value }

    let userId = 2 // this user acquires the card
    do! CardRepository.AcquireCardAsync c.Db userId branchInstanceIds.[0] |> TaskResult.getOk
    do! CardRepository.AcquireCardAsync c.Db userId branchInstanceIds.[1] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    do! testRelationships userId commands.[2]
    do! testRelationships userId commands.[3]

    let userId = 3 // this user acquires card in opposite order from user2
    do! CardRepository.AcquireCardAsync c.Db userId branchInstanceIds.[1] |> TaskResult.getOk
    do! CardRepository.AcquireCardAsync c.Db userId branchInstanceIds.[0] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    do! testRelationships userId commands.[2]
    do! testRelationships userId commands.[3] }

let sanitizeSearchData: Object [] [] = [|
        [| "{{c1::cloze deletion}}"
           "{{c1::cloze deletion}}"
           "" |]
        [| "wild*"
           ""
           "wild:*" |]
        [| "0wild*"
           "0"
           "wild:*" |]
        [| "0wild* word"
           "0 word"
           "wild:*" |]
        [| "zombie 0wild* word"
           "zombie 0 word"
           "wild:*" |]
        [| "wild*card"
           "wild*card"
           "" |]
        [| "*wild"
           "*wild"
           "" |]
        [| "wild*."
           "wild*."
           "" |]
        [| "wild* card"
           " card"
           "wild:*" |]
        [| "wild* card*"
           " "
           "wild:* card:*" |]
    |]
[<Theory>]
[<MemberData(nameof sanitizeSearchData)>]
let ``Sanitize search`` (input: string, expectedPlain: string, expectedWildcards: string): unit =
    let actualPlain, actualWildcards = FullTextSearch.parse input

    Assert.Equal(expectedPlain, actualPlain)
    Assert.Equal(expectedWildcards, actualWildcards)

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
    let search = CardRepository.SearchAsync c.Db userId 1 SearchOrder.Popularity
    
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

    let search = CardRepository.SearchAsync c.Db userId 1 SearchOrder.Relevance
    // testing relevance
    let term = "relevant "
    let less = String.replicate 1 term
    let more = String.replicate 3 term
    let! _ = FacetRepositoryTests.addBasicCustomCard [less; less] c.Db userId ["tag1"]
    let! _ = FacetRepositoryTests.addBasicCustomCard [more; more] c.Db userId ["tag2"]
    let! hits = search term
    Assert.Equal(more.Trim(), hits.Results.First().Instance.StrippedFront)

    // testing relevance sans tags
    let term = "nightwish "
    let less = String.replicate 1 term
    let more = String.replicate 3 term
    let! _ = FacetRepositoryTests.addBasicCustomCard [less; less] c.Db userId []
    let! _ = FacetRepositoryTests.addBasicCustomCard [more; more] c.Db userId []
    let! hits = search term
    Assert.Equal(more.Trim(), hits.Results.First().Instance.StrippedFront)
    
    // tags outweigh fields
    let lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."
    let tag = " batman"
    let! _ = FacetRepositoryTests.addBasicCustomCard [lorem      ; ""] c.Db userId [tag]
    let! _ = FacetRepositoryTests.addBasicCustomCard [lorem + tag; ""] c.Db userId []
    let! hits = search tag
    Assert.Equal(lorem, hits.Results.First().Instance.StrippedFront)

    // testing collate search
    let search = SanitizeCollate.Search c.Db userId 1
    let! collates = search "Cloze"
    Assert.Equal("Cloze", collates.Results.Single().Name)
    Assert.Equal(3, collates.Results.Single().CollateUsers)
    Assert.False(collates.Results.Single().IsAcquired) // most recent cloze is not acquired because it's missing Extra. Why Damien?
    let! collates = search "type"
    Assert.Equal("Basic (type in the answer)", collates.Results.Single().Name)
    Assert.Equal(3, collates.Results.Single().CollateUsers)
    Assert.True(collates.Results.Single().IsAcquired)
    let! collates = search "Basic"
    Assert.Equal(4, collates.Results.Count())
    Assert.True(collates.Results.All(fun x -> x.CollateUsers = 3))
    Assert.True(collates.Results.All(fun x -> x.IsAcquired))
    }

[<Fact>]
let ``New user has TheCollective's card collates`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! myCollates = SanitizeCollate.GetMine c.Db userId
    let theCollectiveId = c.Db.User.Single(fun x -> x.DisplayName = "The Collective").Id
    for collate in myCollates do
        Assert.Equal(theCollectiveId, collate.AuthorId)
    }

[<Fact>]
let ``Updating card collate with duplicate field names yields error`` (): Task<unit> = task {
    let userId = 3
    let fieldName = Guid.NewGuid().ToString()
    let collate = CollateInstance.initialize |> ViewCollateInstance.load
    let collate = { collate with Fields = collate.Fields.Select(fun f -> { f with Name = fieldName }).ToList() }
    
    let! error = SanitizeCollate.Update null userId collate
    
    Assert.Equal("Field names must differ", error.error)
    }

[<Fact>]
let ``Can create card collate and insert a modified one`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let name = Guid.NewGuid().ToString()
    let initialCollate = ViewCollateWithAllInstances.initialize userId

    let! x = SanitizeCollate.Update c.Db userId { initialCollate.Editable with Name = name }
    Assert.Null x.Value
    let! myCollates = SanitizeCollate.GetMine c.Db userId

    Assert.SingleI(myCollates.Where(fun x -> x.Editable.Name = name))
    
    // testing a brand new collate, but slightly different
    let fieldName = Guid.NewGuid().ToString()
    let newEditable =
        let newField =
            {   Name = fieldName
                IsRightToLeft = false
                Ordinal = 0
                IsSticky = false
            }
        {   initialCollate.Editable with
                Fields = initialCollate.Editable.Fields.Append newField |> Core.toResizeArray
        }
    let! x = SanitizeCollate.Update c.Db userId newEditable
    Assert.Null x.Value
    
    Assert.Equal(2, c.Db.Collate.Count(fun x -> x.AuthorId = userId))
    let! myCollates = SanitizeCollate.GetMine c.Db userId
    let latestCollate = myCollates.OrderBy(fun x -> x.Id).Last().Editable
    Assert.True(latestCollate.Fields.Any(fun x -> x.Name = fieldName))

    // updating the slightly different collate
    let name = Guid.NewGuid().ToString()
    let! x = SanitizeCollate.Update c.Db userId { latestCollate with Name = name }
    Assert.Null x.Value

    let! myCollates = SanitizeCollate.GetMine c.Db userId
    Assert.Equal(latestCollate.CollateId, myCollates.Select(fun x -> x.Instances.First()).Single(fun x -> x.Name = name).CollateId)
    }

[<Fact>]
let ``New card collate has correct hash`` (): Task<unit> = taskResult {
        use c = new TestContainer()
        let userId = 3
        let initialCollate = ViewCollateWithAllInstances.initialize userId
        use sha512 = SHA512.Create()
        do! SanitizeCollate.Update c.Db userId initialCollate.Editable
        let! (dbCollate: CollateInstanceEntity) = c.Db.CollateInstance.SingleAsync(fun x -> x.Collate.AuthorId = userId)
    
        let computedHash =
            initialCollate.Editable
            |> ViewCollateInstance.copyTo
            |> fun x -> CollateEntity() |> IdOrEntity.Entity |> x.CopyToNewInstance
            |> CollateInstanceEntity.hash sha512
    
        Assert.Equal<BitArray>(dbCollate.Hash, computedHash) } |> TaskResult.getOk

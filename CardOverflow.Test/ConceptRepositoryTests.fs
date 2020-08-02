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
let ``Getting 10 pages of GetCollectedPages takes less than 1 minute``(): Task<unit> = task {
    use c = new Container()
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    use __ = AsyncScopedLifestyle.BeginScope c
    let db = c.GetInstance<CardOverflowDb>()
    let userId = 3

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        let! _ = StackRepository.GetCollectedPages db userId i ""
        ()
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)
    }

[<Fact>]
let ``GetCollectedPages works if updated``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId []
    let branchId = 1
    let secondVersion = Guid.NewGuid().ToString()
    do! FacetRepositoryTests.update c userId
            (VUpdateBranchId branchId) (fun x -> { x with EditSummary = secondVersion }) branchId
    let oldInstanceId = 1001
    let updatedInstanceId = 1002
    do! c.Db.Leaf.SingleAsync(fun x -> x.Id = oldInstanceId)
        |> Task.map (fun x -> Assert.Equal("Initial creation", x.EditSummary))
    do! c.Db.Leaf.SingleAsync(fun x -> x.Id = updatedInstanceId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))

    let! (cards: PagedList<Result<Card, string>>) = StackRepository.GetCollectedPages c.Db userId 1 ""
    let cards = cards.Results |> Seq.map Result.getOk |> Seq.toList

    Assert.Equal(updatedInstanceId, cards.Select(fun x -> x.LeafMeta.Id).Distinct().Single())

    // getCollectedInstanceFromInstance gets the updatedInstanceId when given the oldInstanceId
    let! actual = CardRepository.getCollectedInstanceFromInstance c.Db userId oldInstanceId

    Assert.Equal(updatedInstanceId, actual)

    // getCollectedInstanceFromInstance gets the updatedInstanceId when given the updatedInstanceId
    let! actual = CardRepository.getCollectedInstanceFromInstance c.Db userId updatedInstanceId

    Assert.Equal(updatedInstanceId, actual)

    // getCollectedInstanceFromInstance fails gracefully on invalid instanceId
    let invalidInstanceId = 1337

    let! (actual: Result<_,_>) = CardRepository.getCollectedInstanceFromInstance c.Db userId invalidInstanceId

    Assert.Equal("You don't have any cards with Branch Instance #1337", actual.error)

    // StackRepository.Revisions says we collected the most recent leaf
    let! revision = StackRepository.Revisions c.Db userId branchId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldInstanceId, false); (updatedInstanceId, true)]

    // collect oldest instance, then StackRepository.Revisions says we collected the oldest leaf
    do! StackRepository.CollectCard c.Db userId oldInstanceId
    
    let! revision = StackRepository.Revisions c.Db userId branchId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldInstanceId, true); (updatedInstanceId, false)]
    } |> TaskResult.getOk)

[<Fact>]
let ``GetCollectedPages works if updated, but pair``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let! _ = FacetRepositoryTests.addReversedBasicStack c.Db userId []
    let branchId = 1
    let secondVersion = Guid.NewGuid().ToString()
    do! FacetRepositoryTests.update c userId
            (VUpdateBranchId branchId) (fun x -> { x with EditSummary = secondVersion }) branchId
    let oldInstanceId = 1001
    let updatedInstanceId = 1002
    do! c.Db.Leaf.SingleAsync(fun x -> x.Id = oldInstanceId)
        |> Task.map (fun x -> Assert.Equal("Initial creation", x.EditSummary))
    do! c.Db.Leaf.SingleAsync(fun x -> x.Id = updatedInstanceId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))

    let! (cards: PagedList<Result<Card, string>>) = StackRepository.GetCollectedPages c.Db userId 1 ""
    let cards = cards.Results |> Seq.map Result.getOk |> Seq.toList

    Assert.Equal(updatedInstanceId, cards.Select(fun x -> x.LeafMeta.Id).Distinct().Single())

    // getCollectedInstanceFromInstance gets the updatedInstanceId when given the oldInstanceId
    let! actual = CardRepository.getCollectedInstanceFromInstance c.Db userId oldInstanceId

    Assert.Equal(updatedInstanceId, actual)

    // getCollectedInstanceFromInstance gets the updatedInstanceId when given the updatedInstanceId
    let! actual = CardRepository.getCollectedInstanceFromInstance c.Db userId updatedInstanceId

    Assert.Equal(updatedInstanceId, actual)

    // getCollectedInstanceFromInstance fails gracefully on invalid instanceId
    let invalidInstanceId = 1337

    let! (actual: Result<_,_>) = CardRepository.getCollectedInstanceFromInstance c.Db userId invalidInstanceId

    Assert.Equal("You don't have any cards with Branch Instance #1337", actual.error)

    // StackRepository.Revisions says we collected the most recent leaf
    let! revision = StackRepository.Revisions c.Db userId branchId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldInstanceId, false); (updatedInstanceId, true)]

    // collect oldest instance, then StackRepository.Revisions says we collected the oldest leaf
    do! StackRepository.CollectCard c.Db userId oldInstanceId
    
    let! revision = StackRepository.Revisions c.Db userId branchId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldInstanceId, true); (updatedInstanceId, false)]
    } |> TaskResult.getOk)

[<Fact>]
let ``GetForUser isn't empty``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId ["A"; "B"]
    do! CommentStackEntity (
            StackId = 1,
            UserId = userId,
            Text = "text",
            Created = DateTime.UtcNow
        ) |> CommentRepository.addAndSaveAsync c.Db
    let stackId = 1
        
    let! stack = ExploreStackRepository.get c.Db userId stackId
    let stack = stack.Value
    let! view = StackViewRepository.get c.Db stackId
        
    let front, _, _, _ = view.Value.FrontBackFrontSynthBackSynth.[0]
    Assert.DoesNotContain("{{Front}}", front)
    Assert.NotEmpty <| stack.Comments
    Assert.True stack.Default.Instance.IsCollected
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 1
            IsCollected = true }
         {  Name = "B"
            Count = 1
            IsCollected = true }],
        stack.Tags
    )
    
    let! stack = ExploreStackRepository.get c.Db userId 9999
    Assert.Equal("Stack #9999 not found", stack.error) }

[<Fact>]
let ``Getting 10 pages of GetAsync takes less than 1 minute, and has users``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId ["A"; "B"]

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        let! _ = StackRepository.search c.Db userId i SearchOrder.Popularity ""
        ()
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)
    
    let! stack = ExploreStackRepository.get c.Db userId 1
    let stack = stack.Value
    Assert.Equal(1, stack.Default.Summary.Users)
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 1
            IsCollected = true }
         {  Name = "B"
            Count = 1
            IsCollected = true }],
        stack.Tags)
    
    let! cc = StackRepository.GetCollected c.Db userId stack.Id
    let cc = cc.Value.Single()
    let! x = StackRepository.editState c.Db userId cc.CardId CardState.Suspended
    Assert.Null x.Value
    let! stack = ExploreStackRepository.get c.Db userId 1
    Assert.Equal(0, stack.Value.Default.Instance.Users) // suspended cards don't count to User count
    }

let testGetCollected (acCount: int) addCard getGromplate name = task {
    use c = new TestContainer(false, name)
    
    let authorId = 1 // this user creates the card
    let! (_: int) = addCard c.Db authorId ["A"]
    let stackId = 1
    let branchId = 1
    let leafId = 1001
    let! cards = StackRepository.GetCollectedPages c.Db authorId 1 ""
    Assert.Equal(acCount, cards.Results.Count())
    let! cc = StackRepository.GetCollected c.Db authorId stackId
    let cc = cc.Value
    Assert.Equal(authorId, cc.Select(fun x -> x.UserId).Distinct().Single())

    let collectorId = 2  // this user collects the card
    let! _ = StackRepository.CollectCard c.Db collectorId leafId |> TaskResult.getOk
    let! stack = ExploreStackRepository.get c.Db collectorId stackId |> TaskResult.getOk
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 1
            IsCollected = false }],
        stack.Tags
    )
    do! SanitizeTagRepository.AddTo c.Db collectorId "a" stack.Id |> TaskResult.getOk
    let! stack = ExploreStackRepository.get c.Db collectorId stackId |> TaskResult.getOk
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 2
            IsCollected = true }],
        stack.Tags
    )
    let! stacks = StackRepository.search c.Db collectorId 1 SearchOrder.Popularity ""
    Assert.Equal(1, stacks.Results.Count())

    // author branching keeps tags
    let! gromplate = getGromplate c.Db
    let! _ =
        {   EditStackCommand.EditSummary = ""
            FieldValues = [].ToList()
            Grompleaf = gromplate |> ViewGrompleaf.copyTo
            Kind = NewBranch_SourceStackId_Title(stackId, "New Branch")
        } |> UpdateRepository.stack c.Db authorId

    let! stack = ExploreStackRepository.get c.Db authorId stackId |> TaskResult.getOk
    
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 2
            IsCollected = true }],
        stack.Tags
    )

    // search returns Default branches (not the new one created)
    let! stacks = StackRepository.search c.Db collectorId 1 SearchOrder.Popularity ""
    Assert.Equal(1, stacks.Results.Count())

    let nonCollectorId = 3 // this user never collects the card
    let! stack = ExploreStackRepository.get c.Db nonCollectorId 1 |> TaskResult.getOk
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 2
            IsCollected = false }],
        stack.Tags
    )}
    
[<Fact>]
let rec ``GetCollected works when collecting 1 basic card``(): Task<unit> =
    testGetCollected
        1
        FacetRepositoryTests.addBasicStack
        FacetRepositoryTests.basicGromplate
        <| nameof ``GetCollected works when collecting 1 basic card``

[<Fact>]
let rec ``GetCollected works when collecting a pair``(): Task<unit> = 
    testGetCollected
        2
        FacetRepositoryTests.addReversedBasicStack
        FacetRepositoryTests.reversedBasicGromplate
        <| nameof ``GetCollected works when collecting a pair``

let relationshipTestInit (c: TestContainer) relationshipName = task {
    let addRelationshipCommand1 =
        {   Name = relationshipName
            SourceStackId = 1
            TargetStackLink = "2"
        }
    let addRelationshipCommand2 =
        {   Name = relationshipName
            SourceStackId = 2
            TargetStackLink = "1"
        }
    let commands = [
        addRelationshipCommand1, addRelationshipCommand1
        addRelationshipCommand2, addRelationshipCommand2
        addRelationshipCommand1, addRelationshipCommand2
        addRelationshipCommand2, addRelationshipCommand1 ]

    let userId = 1 // this user creates the card
    for (addStack: CardOverflowDb -> int -> string list -> Task<int>) in [ FacetRepositoryTests.addBasicStack; FacetRepositoryTests.addReversedBasicStack ] do
        let! _ = addStack c.Db userId []
        ()

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1
    Assert.Null x.Value
    let! stack = ExploreStackRepository.get c.Db userId 1
    Assert.Equal(1, stack.Value.Relationships.Single().Users)
    let! stack = ExploreStackRepository.get c.Db userId 2
    Assert.Equal(1, stack.Value.Relationships.Single().Users)

    let successfulRemove () = task {
        let! r = SanitizeRelationshipRepository.Remove c.Db 1 2 userId relationshipName
        Assert.Null r.Value
        let! stack = ExploreStackRepository.get c.Db userId 1
        Assert.Equal(0, stack.Value.Relationships.Count)
        let! stack = ExploreStackRepository.get c.Db userId 2
        Assert.Equal(0, stack.Value.Relationships.Count) }
    do! successfulRemove ()

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1
    Assert.Null x.Value
    let! r = SanitizeRelationshipRepository.Remove c.Db 2 1 userId relationshipName
    Assert.Equal(sprintf "Relationship not found between source Stack #2 and target Stack #1 with name \"%s\"." relationshipName, r.error)
    let! stack = ExploreStackRepository.get c.Db userId 1
    Assert.Equal(1, stack.Value.Relationships.Count)
    let! stack = ExploreStackRepository.get c.Db userId 2
    Assert.Equal(1, stack.Value.Relationships.Count)
    do! successfulRemove ()

    return commands }

[<Fact>]
let ``Relationships can't be self related``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId []
    Assert.Equal(1, actualBranchId)
    let addRelationshipCommand =
        {   Name = ""
            SourceStackId = 1
            TargetStackLink = string 1
        }

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand
    
    Assert.Equal("A stack can't be related to itself", x.error) }

[<Fact>]
let ``Directional relationship tests``(): Task<unit> = task {
    let leafIds = [1001; 1002]
    use c = new TestContainer()
    let relationshipName = "Test/Relationship"
    
    let! commands = relationshipTestInit c relationshipName
    let testRelationships userId (creator, collector) = task {
        let! x = SanitizeRelationshipRepository.Add c.Db 1 creator // card creator also collects the relationship; .Single() below refers this this
        Assert.Null x.Value
        
        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! stack = ExploreStackRepository.get c.Db userId 1
        let stack = stack.Value
        Assert.Equal(2, stack.Relationships.Single().Users)
        Assert.True(stack.Relationships.Single().IsCollected)
        let! stack = ExploreStackRepository.get c.Db userId 2
        let stack = stack.Value
        Assert.Equal(2, stack.Relationships.Single().Users)
        Assert.True(stack.Relationships.Single().IsCollected)

        let successfulRemove () = task {
            let! r = SanitizeRelationshipRepository.Remove c.Db collector.SourceStackId (int collector.TargetStackLink) userId relationshipName
            Assert.Null r.Value
            let! stack = ExploreStackRepository.get c.Db userId 1
            let stack = stack.Value
            Assert.Equal(1, stack.Relationships.Count)
            Assert.False(stack.Relationships.Single().IsCollected)
            let! stack = ExploreStackRepository.get c.Db userId 2
            let stack = stack.Value
            Assert.Equal(1, stack.Relationships.Count)
            Assert.False(stack.Relationships.Single().IsCollected) }
        do! successfulRemove ()

        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! r = SanitizeRelationshipRepository.Remove c.Db (int collector.TargetStackLink) collector.SourceStackId userId relationshipName
        Assert.Equal(sprintf "Relationship not found between source Stack #%i and target Stack #%i with name \"%s\"." (int collector.TargetStackLink) collector.SourceStackId relationshipName, r.error)
        let! stack = ExploreStackRepository.get c.Db userId 1
        let stack = stack.Value
        Assert.Equal(1, stack.Relationships.Count)
        Assert.True(stack.Relationships.Single().IsCollected)
        let! stack = ExploreStackRepository.get c.Db userId 2
        let stack = stack.Value
        Assert.Equal(1, stack.Relationships.Count)
        Assert.True(stack.Relationships.Single().IsCollected)
            
        do! successfulRemove ()
        let! r = SanitizeRelationshipRepository.Remove c.Db collector.SourceStackId (int collector.TargetStackLink) 1 relationshipName // cleanup from do! SanitizeRelationshipRepository.Add c.Db 1 a |> Result.getOk
        Assert.Null r.Value }

    let userId = 2 // this user collects the card
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[0] |> TaskResult.getOk
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[1] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    //do! testRelationships userId commands.[2]
    //do! testRelationships userId commands.[3]

    let userId = 3 // this user collects card in opposite order from user2
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[1] |> TaskResult.getOk
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[0] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    //do! testRelationships userId commands.[2]
    //do! testRelationships userId commands.[3]
    }

[<Fact>]
let ``Nondirectional relationship tests``(): Task<unit> = task {
    let leafIds = [1001; 1002]
    use c = new TestContainer()
    let relationshipName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    
    let! commands = relationshipTestInit c relationshipName
    let testRelationships userId (creator, collector) = task {
        let! x = SanitizeRelationshipRepository.Add c.Db 1 creator // card creator also collects the relationship; .Single() below refers this this
        Assert.Null x.Value
        
        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! stack = ExploreStackRepository.get c.Db userId 1 |> TaskResult.getOk
        Assert.Equal(2, stack.Relationships.Single().Users)
        Assert.True(stack.Relationships.Single().IsCollected)
        let! stack = ExploreStackRepository.get c.Db userId 2 |> TaskResult.getOk
        Assert.Equal(2, stack.Relationships.Single().Users)
        Assert.True(stack.Relationships.Single().IsCollected)

        let successfulRemove () = task {
            let! r = SanitizeRelationshipRepository.Remove c.Db 1 2 userId relationshipName
            Assert.Null r.Value
            let! stack = ExploreStackRepository.get c.Db userId 1 |> TaskResult.getOk
            Assert.Equal(1, stack.Relationships.Count)
            Assert.False(stack.Relationships.Single().IsCollected)
            let! stack = ExploreStackRepository.get c.Db userId 2 |> TaskResult.getOk
            Assert.Equal(1, stack.Relationships.Count)
            Assert.False(stack.Relationships.Single().IsCollected) }
        do! successfulRemove ()

        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! r = SanitizeRelationshipRepository.Remove c.Db 2 1 userId relationshipName
        Assert.Equal(sprintf "Relationship not found between source Stack #2 and target Stack #1 with name \"%s\"." relationshipName, r.error)
        let! stack = ExploreStackRepository.get c.Db userId 1 |> TaskResult.getOk
        Assert.Equal(1, stack.Relationships.Count)
        Assert.True(stack.Relationships.Single().IsCollected)
        let! stack = ExploreStackRepository.get c.Db userId 2 |> TaskResult.getOk
        Assert.Equal(1, stack.Relationships.Count)
        Assert.True(stack.Relationships.Single().IsCollected)
            
        do! successfulRemove ()
        let! r = SanitizeRelationshipRepository.Remove c.Db 1 2 1 relationshipName // cleanup from do! SanitizeRelationshipRepository.Add c.Db 1 a |> Result.getOk
        Assert.Null r.Value }

    let userId = 2 // this user collects the card
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[0] |> TaskResult.getOk
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[1] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    do! testRelationships userId commands.[2]
    do! testRelationships userId commands.[3]

    let userId = 3 // this user collects card in opposite order from user2
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[1] |> TaskResult.getOk
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[0] |> TaskResult.getOk
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
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId [basicTag]
    let front = Guid.NewGuid().ToString()
    let back = Guid.NewGuid().ToString()
    let! _ = FacetRepositoryTests.addBasicCustomStack [front; back] c.Db userId ["custom"]
    let clozeText = "{{c1::" + Guid.NewGuid().ToString() + "}}"
    let! _ = FacetRepositoryTests.addCloze clozeText c.Db userId []
    
    // testing search
    let search = StackRepository.search c.Db userId 1 SearchOrder.Popularity
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

    // testing deckSearch
    do! SanitizeDeckRepository.setIsPublic c.Db userId userId true |> TaskResult.getOk
    let search searchTerm = StackRepository.searchDeck c.Db userId 1 SearchOrder.Popularity searchTerm userId
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

    let search = StackRepository.search c.Db userId 1 SearchOrder.Relevance
    // testing relevance
    let term = "relevant "
    let less = String.replicate 1 term
    let more = String.replicate 3 term
    let! _ = FacetRepositoryTests.addBasicCustomStack [less; less] c.Db userId ["tag1"]
    let! _ = FacetRepositoryTests.addBasicCustomStack [more; more] c.Db userId ["tag2"]
    let! hits = search term
    Assert.Equal(more.Trim(), hits.Results.First().Instance.StrippedFront)

    // testing relevance sans tags
    let term = "nightwish "
    let less = String.replicate 1 term
    let more = String.replicate 3 term
    let! _ = FacetRepositoryTests.addBasicCustomStack [less; less] c.Db userId []
    let! _ = FacetRepositoryTests.addBasicCustomStack [more; more] c.Db userId []
    let! hits = search term
    Assert.Equal(more.Trim(), hits.Results.First().Instance.StrippedFront)
    
    // tags outweigh fields
    let lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."
    let tag = " batman"
    let! _ = FacetRepositoryTests.addBasicCustomStack [lorem      ; ""] c.Db userId [tag]
    let! _ = FacetRepositoryTests.addBasicCustomStack [lorem + tag; ""] c.Db userId []
    let! hits = search tag
    Assert.Equal(lorem, hits.Results.First().Instance.StrippedFront)

    // testing gromplate search
    let search = SanitizeGromplate.Search c.Db userId 1
    let! gromplates = search "Cloze"
    Assert.Equal("Cloze", gromplates.Results.Single().Name)
    Assert.Equal(1, gromplates.Results.Single().GromplateUsers)
    Assert.False(gromplates.Results.Single().IsCollected) // most recent cloze is not collected because it's missing Extra. Why Damien?
    let! gromplates = search "type"
    Assert.Equal("Basic (type in the answer)", gromplates.Results.Single().Name)
    Assert.Equal(1, gromplates.Results.Single().GromplateUsers)
    Assert.True(gromplates.Results.Single().IsCollected)
    let! gromplates = search "Basic"
    Assert.Equal(4, gromplates.Results.Count())
    Assert.True(gromplates.Results.All(fun x -> x.GromplateUsers = 1))
    Assert.True(gromplates.Results.All(fun x -> x.IsCollected))
    }

[<Fact>]
let ``New user has TheCollective's card gromplates`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! myGromplates = SanitizeGromplate.GetMine c.Db userId
    let theCollectiveId = c.Db.User.Single(fun x -> x.DisplayName = "The Collective").Id
    for gromplate in myGromplates do
        Assert.Equal(theCollectiveId, gromplate.AuthorId)
    }

[<Fact>]
let ``Updating card gromplate with duplicate field names yields error`` (): Task<unit> = task {
    let userId = 3
    let fieldName = Guid.NewGuid().ToString()
    let gromplate = Grompleaf.initialize |> ViewGrompleaf.load
    let gromplate = { gromplate with Fields = gromplate.Fields.Select(fun f -> { f with Name = fieldName }).ToList() }
    
    let! error = SanitizeGromplate.Update null userId gromplate
    
    Assert.Equal("Field names must differ", error.error)
    }

[<Fact>]
let ``Can create card gromplate and insert a modified one`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let name = Guid.NewGuid().ToString()
    let initialGromplate = ViewGromplateWithAllInstances.initialize userId

    let! x = SanitizeGromplate.Update c.Db userId { initialGromplate.Editable with Name = name }
    Assert.Null x.Value
    let! myGromplates = SanitizeGromplate.GetMine c.Db userId

    Assert.SingleI(myGromplates.Where(fun x -> x.Editable.Name = name))
    
    // testing a brand new gromplate, but slightly different
    let fieldName = Guid.NewGuid().ToString()
    let newEditable =
        let newField =
            {   Name = fieldName
                IsRightToLeft = false
                IsSticky = false
            }
        {   initialGromplate.Editable with
                Fields = initialGromplate.Editable.Fields.Append newField |> Core.toResizeArray
        }
    let! x = SanitizeGromplate.Update c.Db userId newEditable
    Assert.Null x.Value
    
    Assert.Equal(2, c.Db.Gromplate.Count(fun x -> x.AuthorId = userId))
    let! myGromplates = SanitizeGromplate.GetMine c.Db userId
    let latestGromplate = myGromplates.OrderBy(fun x -> x.Id).Last().Editable
    Assert.True(latestGromplate.Fields.Any(fun x -> x.Name = fieldName))

    // updating the slightly different gromplate
    let name = Guid.NewGuid().ToString()
    let! x = SanitizeGromplate.Update c.Db userId { latestGromplate with Name = name }
    Assert.Null x.Value

    let! myGromplates = SanitizeGromplate.GetMine c.Db userId
    Assert.Equal(latestGromplate.GromplateId, myGromplates.Select(fun x -> x.Instances.First()).Single(fun x -> x.Name = name).GromplateId)

    // updating to cloze
    let name = Guid.NewGuid().ToString()
    let! x =
        SanitizeGromplate.Update c.Db userId
            { latestGromplate
                with
                    Name = name
                    Templates = Cloze <| latestGromplate.JustTemplates.First()
            }
    Assert.Null x.Value

    let! myGromplates = SanitizeGromplate.GetMine c.Db userId
    Assert.Equal(latestGromplate.GromplateId, myGromplates.Select(fun x -> x.Instances.First()).Single(fun x -> x.Name = name).GromplateId)
    Assert.True(myGromplates.Select(fun x -> x.Instances.First()).Single(fun x -> x.Name = name).IsCloze)

    // updating to multiple templates
    let name = Guid.NewGuid().ToString()
    let! x =
        SanitizeGromplate.Update c.Db userId
            { latestGromplate
                with
                    Name = name
                    Templates = Standard [ latestGromplate.JustTemplates.First() ; latestGromplate.JustTemplates.First() ]
            }
    Assert.Null x.Value

    let! myGromplates = SanitizeGromplate.GetMine c.Db userId
    Assert.Equal(latestGromplate.GromplateId, myGromplates.Select(fun x -> x.Instances.First()).Single(fun x -> x.Name = name).GromplateId)
    Assert.Equal(2, myGromplates.Select(fun x -> x.Instances.First()).Single(fun x -> x.Name = name).JustTemplates.Count())
    }

[<Fact>]
let ``New card gromplate has correct hash`` (): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    let initialGromplate = ViewGromplateWithAllInstances.initialize userId
    use sha512 = SHA512.Create()
    do! SanitizeGromplate.Update c.Db userId initialGromplate.Editable
    let! (dbGromplate: GrompleafEntity) = c.Db.Grompleaf.SingleAsync(fun x -> x.Gromplate.AuthorId = userId)
    
    let computedHash =
        initialGromplate.Editable
        |> ViewGrompleaf.copyTo
        |> fun x -> GromplateEntity() |> IdOrEntity.Entity |> x.CopyToNewInstance
        |> GrompleafEntity.hash sha512
    
    Assert.Equal<BitArray>(dbGromplate.Hash, computedHash)
    } |> TaskResult.getOk)

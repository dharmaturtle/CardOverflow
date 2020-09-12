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
    let userId = user_3

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        let! _ = StackRepository.GetCollectedPages db userId i ""
        ()
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)
    }

[<Fact>]
let ``GetCollectedPages works if updated``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    let branchId = branch_1
    let secondVersion = Guid.NewGuid().ToString()
    do! FacetRepositoryTests.update c userId
            (VUpdate_BranchId branchId) (fun x -> { x with EditSummary = secondVersion }) ((stack_1, branch_1, leaf_2, [card_1]) |> UpsertIds.fromTuple) branchId
    let oldLeafId = leaf_1
    let updatedLeafId = leaf_2
    do! c.Db.Leaf.SingleAsync(fun x -> x.Id = oldLeafId)
        |> Task.map (fun x -> Assert.Equal("Initial creation", x.EditSummary))
    do! c.Db.Leaf.SingleAsync(fun x -> x.Id = updatedLeafId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))

    let! (cards: PagedList<Result<Card, string>>) = StackRepository.GetCollectedPages c.Db userId 1 ""
    let cards = cards.Results |> Seq.map Result.getOk |> Seq.toList

    Assert.Equal(updatedLeafId, cards.Select(fun x -> x.LeafMeta.Id).Distinct().Single())

    // getCollectedLeafFromLeaf gets the updatedLeafId when given the oldLeafId
    let! actual = CardRepository.getCollectedLeafFromLeaf c.Db userId oldLeafId

    Assert.Equal(updatedLeafId, actual)

    // getCollectedLeafFromLeaf gets the updatedLeafId when given the updatedLeafId
    let! actual = CardRepository.getCollectedLeafFromLeaf c.Db userId updatedLeafId

    Assert.Equal(updatedLeafId, actual)

    // getCollectedLeafFromLeaf fails gracefully on invalid leafId
    let invalidLeafId = Ulid.create

    let! (actual: Result<_,_>) = CardRepository.getCollectedLeafFromLeaf c.Db userId invalidLeafId

    Assert.equal (sprintf "You don't have any cards with Branch Leaf #%A" invalidLeafId) actual.error

    // StackRepository.Revisions says we collected the most recent leaf
    let! revision = StackRepository.Revisions c.Db userId branchId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldLeafId, false); (updatedLeafId, true)]

    // collect oldest leaf, then StackRepository.Revisions says we collected the oldest leaf
    let! _ = StackRepository.CollectCard c.Db userId oldLeafId [ card_1 ]
    
    let! revision = StackRepository.Revisions c.Db userId branchId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldLeafId, true); (updatedLeafId, false)]
    } |> TaskResult.getOk)

[<Fact>]
let ``GetCollectedPages works if updated, but pair``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = FacetRepositoryTests.addReversedBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1; card_2])
    let branchId = branch_1
    let secondVersion = Guid.NewGuid().ToString()
    do! FacetRepositoryTests.update c userId
            (VUpdate_BranchId branchId) (fun x -> { x with EditSummary = secondVersion }) ((stack_1, branch_1, leaf_2, [card_3; card_ 4]) |> UpsertIds.fromTuple) branchId
    let oldLeafId = leaf_1
    let updatedLeafId = leaf_2
    do! c.Db.Leaf.SingleAsync(fun x -> x.Id = oldLeafId)
        |> Task.map (fun x -> Assert.Equal("Initial creation", x.EditSummary))
    do! c.Db.Leaf.SingleAsync(fun x -> x.Id = updatedLeafId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))

    let! (cards: PagedList<Result<Card, string>>) = StackRepository.GetCollectedPages c.Db userId 1 ""
    let cards = cards.Results |> Seq.map Result.getOk |> Seq.toList

    Assert.Equal(updatedLeafId, cards.Select(fun x -> x.LeafMeta.Id).Distinct().Single())

    // getCollectedLeafFromLeaf gets the updatedLeafId when given the oldLeafId
    let! actual = CardRepository.getCollectedLeafFromLeaf c.Db userId oldLeafId

    Assert.Equal(updatedLeafId, actual)

    // getCollectedLeafFromLeaf gets the updatedLeafId when given the updatedLeafId
    let! actual = CardRepository.getCollectedLeafFromLeaf c.Db userId updatedLeafId

    Assert.Equal(updatedLeafId, actual)

    // getCollectedLeafFromLeaf fails gracefully on invalid leafId
    let invalidLeafId = Ulid.create

    let! (actual: Result<_,_>) = CardRepository.getCollectedLeafFromLeaf c.Db userId invalidLeafId

    Assert.equal (sprintf "You don't have any cards with Branch Leaf #%A" invalidLeafId) actual.error

    // StackRepository.Revisions says we collected the most recent leaf
    let! revision = StackRepository.Revisions c.Db userId branchId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldLeafId, false); (updatedLeafId, true)]

    // collect oldest leaf, then StackRepository.Revisions says we collected the oldest leaf
    let! _ = StackRepository.CollectCard c.Db userId oldLeafId [ Ulid.create; Ulid.create ]
    
    let! revision = StackRepository.Revisions c.Db userId branchId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldLeafId, true); (updatedLeafId, false)]
    } |> TaskResult.getOk)

[<Fact>]
let ``GetForUser isn't empty``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId ["A"; "B"] (stack_1, branch_1, leaf_1, [card_1])
    do! CommentStackEntity (
            StackId = stack_1,
            UserId = userId,
            Text = "text",
            Created = DateTime.UtcNow
        ) |> CommentRepository.addAndSaveAsync c.Db
    let stackId = stack_1
        
    let! stack = ExploreStackRepository.get c.Db userId stackId
    let stack = stack.Value
    let! view = StackViewRepository.get c.Db stackId
        
    let front, _, _, _ = view.Value.FrontBackFrontSynthBackSynth.[0]
    Assert.DoesNotContain("{{Front}}", front)
    Assert.NotEmpty <| stack.Comments
    Assert.True stack.Default.Leaf.IsCollected
    Assert.areEquivalent
        [{  Name = "A"
            Count = 1
            IsCollected = true }
         {  Name = "B"
            Count = 1
            IsCollected = true }]
        stack.Tags
    
    let missingStack = Ulid.create
    let! stack = ExploreStackRepository.get c.Db userId missingStack
    Assert.equal (sprintf "Stack #%A not found" missingStack) stack.error }

[<Fact>]
let ``Getting 10 pages of GetAsync takes less than 1 minute, and has users``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId ["A"; "B"] (stack_1, branch_1, leaf_1, [card_1])

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        let! _ = StackRepository.search c.Db userId i SearchOrder.Popularity ""
        ()
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)
    
    let! stack = ExploreStackRepository.get c.Db userId stack_1
    let stack = stack.Value
    Assert.Equal(1, stack.Default.Summary.Users)
    Assert.areEquivalent
        [{  Name = "A"
            Count = 1
            IsCollected = true }
         {  Name = "B"
            Count = 1
            IsCollected = true }]
        stack.Tags
    
    let! cc = StackRepository.GetCollected c.Db userId stack.Id
    let cc = cc.Value.Single()
    let! x = StackRepository.editState c.Db userId cc.CardId CardState.Suspended
    Assert.Null x.Value
    let! stack = ExploreStackRepository.get c.Db userId stack_1
    Assert.Equal(0, stack.Value.Default.Leaf.Users) // suspended cards don't count to User count
    }

let testGetCollected (acCount: int) addCard getGromplate name = task {
    use c = new TestContainer(false, name)
    
    let authorId = user_1 // this user creates the card
    let! x = addCard c.Db authorId ["A"] (stack_1, branch_1, leaf_1, [1..acCount] |> List.map (fun _ -> Ulid.create)) |> TaskResult.getOk
    Assert.NotNull x
    let stackId = stack_1
    let branchId = branch_1
    let leafId = leaf_1
    let! cards = StackRepository.GetCollectedPages c.Db authorId 1 ""
    Assert.Equal(acCount, cards.Results.Count())
    let! cc = StackRepository.GetCollected c.Db authorId stackId
    let cc = cc.Value
    Assert.Equal(authorId, cc.Select(fun x -> x.UserId).Distinct().Single())

    let collectorId = user_2  // this user collects the card
    let cardIds = [1..acCount] |> List.map (fun _ -> Ulid.create)
    let! _ = StackRepository.CollectCard c.Db collectorId leafId cardIds |> TaskResult.getOk
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
            Kind = NewBranch_Title "New Branch"
            Ids = ids_1
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

    let nonCollectorId = user_3 // this user never collects the card
    let! stack = ExploreStackRepository.get c.Db nonCollectorId stack_1 |> TaskResult.getOk
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
            SourceStackId = stack_1
            TargetStackLink = stack_2.ToString()
        }
    let addRelationshipCommand2 =
        {   Name = relationshipName
            SourceStackId = stack_2
            TargetStackLink = stack_1.ToString()
        }
    let commands = [
        addRelationshipCommand1, addRelationshipCommand1
        addRelationshipCommand2, addRelationshipCommand2
        addRelationshipCommand1, addRelationshipCommand2
        addRelationshipCommand2, addRelationshipCommand1 ]

    let userId = user_1 // this user creates the card
    let! x = FacetRepositoryTests.addBasicStack          c.Db userId [] (stack_1, branch_1, leaf_1, [Ulid.create])
    Assert.NotNull x.Value
    let! x = FacetRepositoryTests.addReversedBasicStack  c.Db userId [] (stack_2, branch_2, leaf_2, [Ulid.create; Ulid.create])
    Assert.NotNull x.Value

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1
    Assert.Null x.Value
    let! stack = ExploreStackRepository.get c.Db userId stack_1
    Assert.Equal(1, stack.Value.Relationships.Single().Users)
    let! stack = ExploreStackRepository.get c.Db userId stack_2
    Assert.Equal(1, stack.Value.Relationships.Single().Users)

    let successfulRemove () = task {
        let! r = SanitizeRelationshipRepository.Remove c.Db stack_1 stack_2 userId relationshipName
        Assert.Null r.Value
        let! stack = ExploreStackRepository.get c.Db userId stack_1
        Assert.Equal(0, stack.Value.Relationships.Count)
        let! stack = ExploreStackRepository.get c.Db userId stack_2
        Assert.Equal(0, stack.Value.Relationships.Count) }
    do! successfulRemove ()

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1
    Assert.Null x.Value
    let! r = SanitizeRelationshipRepository.Remove c.Db stack_2 stack_1 userId relationshipName
    Assert.Equal(sprintf "Relationship not found between source Stack #%A and target Stack #%A with name \"%s\"." stack_2 stack_1 relationshipName, r.error)
    let! stack = ExploreStackRepository.get c.Db userId stack_1
    Assert.Equal(1, stack.Value.Relationships.Count)
    let! stack = ExploreStackRepository.get c.Db userId stack_2
    Assert.Equal(1, stack.Value.Relationships.Count)
    do! successfulRemove ()

    return commands }

[<Fact>]
let ``Relationships can't be self related``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    Assert.Equal(branch_ 1, actualBranchId.Value)
    let addRelationshipCommand =
        {   Name = ""
            SourceStackId = stack_1
            TargetStackLink = stack_1.ToString()
        }

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand
    
    Assert.equal "A stack can't be related to itself" x.error }

[<Fact>]
let ``Directional relationship tests``(): Task<unit> = task {
    let leafIds = [leaf_1; leaf_2]
    use c = new TestContainer()
    let relationshipName = "Test/Relationship"
    
    let! commands = relationshipTestInit c relationshipName
    let testRelationships userId (creator, collector) = task {
        let! x = SanitizeRelationshipRepository.Add c.Db user_1 creator // card creator also collects the relationship; .Single() below refers this this
        Assert.Null x.Value
        
        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! stack = ExploreStackRepository.get c.Db userId stack_1
        let stack = stack.Value
        Assert.Equal(2, stack.Relationships.Single().Users)
        Assert.True(stack.Relationships.Single().IsCollected)
        let! stack = ExploreStackRepository.get c.Db userId stack_2
        let stack = stack.Value
        Assert.Equal(2, stack.Relationships.Single().Users)
        Assert.True(stack.Relationships.Single().IsCollected)

        let successfulRemove () = task {
            let! r = SanitizeRelationshipRepository.Remove c.Db collector.SourceStackId (Guid.Parse collector.TargetStackLink) userId relationshipName
            Assert.Null r.Value
            let! stack = ExploreStackRepository.get c.Db userId stack_1
            let stack = stack.Value
            Assert.Equal(1, stack.Relationships.Count)
            Assert.False(stack.Relationships.Single().IsCollected)
            let! stack = ExploreStackRepository.get c.Db userId stack_2
            let stack = stack.Value
            Assert.Equal(1, stack.Relationships.Count)
            Assert.False(stack.Relationships.Single().IsCollected) }
        do! successfulRemove ()

        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! r = SanitizeRelationshipRepository.Remove c.Db (Guid.Parse collector.TargetStackLink) collector.SourceStackId userId relationshipName
        Assert.Equal(sprintf "Relationship not found between source Stack #%s and target Stack #%A with name \"%s\"." collector.TargetStackLink collector.SourceStackId relationshipName, r.error)
        let! stack = ExploreStackRepository.get c.Db userId stack_1
        let stack = stack.Value
        Assert.Equal(1, stack.Relationships.Count)
        Assert.True(stack.Relationships.Single().IsCollected)
        let! stack = ExploreStackRepository.get c.Db userId stack_2
        let stack = stack.Value
        Assert.Equal(1, stack.Relationships.Count)
        Assert.True(stack.Relationships.Single().IsCollected)
            
        do! successfulRemove ()
        let! r = SanitizeRelationshipRepository.Remove c.Db collector.SourceStackId (Guid.Parse collector.TargetStackLink) user_1 relationshipName // cleanup from do! SanitizeRelationshipRepository.Add c.Db 1 a |> Result.getOk
        Assert.Null r.Value }

    let userId = user_2 // this user collects the card
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[0] [ Ulid.create ] |> TaskResult.getOk
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[1] [ Ulid.create; Ulid.create ] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    //do! testRelationships userId commands.[2]
    //do! testRelationships userId commands.[3]

    let userId = user_3 // this user collects card in opposite order from user2
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[1] [ Ulid.create; Ulid.create ] |> TaskResult.getOk
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[0] [ Ulid.create ] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    //do! testRelationships userId commands.[2]
    //do! testRelationships userId commands.[3]
    }

[<Fact>]
let ``Nondirectional relationship tests``(): Task<unit> = task {
    let leafIds = [leaf_1; leaf_2]
    use c = new TestContainer()
    let relationshipName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    
    let! commands = relationshipTestInit c relationshipName
    let testRelationships userId (creator, collector) = task {
        let! x = SanitizeRelationshipRepository.Add c.Db user_1 creator // card creator also collects the relationship; .Single() below refers this this
        Assert.Null x.Value
        
        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! stack = ExploreStackRepository.get c.Db userId stack_1 |> TaskResult.getOk
        Assert.Equal(2, stack.Relationships.Single().Users)
        Assert.True(stack.Relationships.Single().IsCollected)
        let! stack = ExploreStackRepository.get c.Db userId stack_2 |> TaskResult.getOk
        Assert.Equal(2, stack.Relationships.Single().Users)
        Assert.True(stack.Relationships.Single().IsCollected)

        let successfulRemove () = task {
            let! r = SanitizeRelationshipRepository.Remove c.Db stack_1 stack_2 userId relationshipName
            Assert.Null r.Value
            let! stack = ExploreStackRepository.get c.Db userId stack_1 |> TaskResult.getOk
            Assert.Equal(1, stack.Relationships.Count)
            Assert.False(stack.Relationships.Single().IsCollected)
            let! stack = ExploreStackRepository.get c.Db userId stack_2 |> TaskResult.getOk
            Assert.Equal(1, stack.Relationships.Count)
            Assert.False(stack.Relationships.Single().IsCollected) }
        do! successfulRemove ()

        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! r = SanitizeRelationshipRepository.Remove c.Db setting_1 stack_1 userId relationshipName
        Assert.Equal(sprintf "Relationship not found between source Stack #%A and target Stack #%A with name \"%s\"." setting_1 stack_1 relationshipName, r.error)
        let! stack = ExploreStackRepository.get c.Db userId stack_1 |> TaskResult.getOk
        Assert.Equal(1, stack.Relationships.Count)
        Assert.True(stack.Relationships.Single().IsCollected)
        let! stack = ExploreStackRepository.get c.Db userId stack_2 |> TaskResult.getOk
        Assert.Equal(1, stack.Relationships.Count)
        Assert.True(stack.Relationships.Single().IsCollected)
            
        do! successfulRemove ()
        let! r = SanitizeRelationshipRepository.Remove c.Db stack_1 stack_2 user_1 relationshipName // cleanup from do! SanitizeRelationshipRepository.Add c.Db 1 a |> Result.getOk
        Assert.Null r.Value }

    let userId = user_2 // this user collects the card
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[0] [ Ulid.create ] |> TaskResult.getOk
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[1] [ Ulid.create; Ulid.create ] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    do! testRelationships userId commands.[2]
    do! testRelationships userId commands.[3]

    let userId = user_3 // this user collects card in opposite order from user2
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[1] [ Ulid.create; Ulid.create ] |> TaskResult.getOk
    let! _ = StackRepository.CollectCard c.Db userId leafIds.[0] [ Ulid.create ] |> TaskResult.getOk
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
    let userId = user_3
    let basicTag = "basic"
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId [basicTag] (stack_1, branch_1, leaf_1, [card_1])
    let front = Guid.NewGuid().ToString()
    let back = Guid.NewGuid().ToString()
    let! _ = FacetRepositoryTests.addBasicCustomStack [front; back] c.Db userId ["custom"] (stack_2, branch_2, leaf_2, [card_2])
    let clozeText = "{{c1::" + Guid.NewGuid().ToString() + "}}"
    let! _ = FacetRepositoryTests.addCloze clozeText c.Db userId [] (stack_3, branch_3, leaf_3, [card_3])
    
    // testing search
    let search = StackRepository.search c.Db userId 1 SearchOrder.Popularity
    let! cards = search ""
    Assert.Equal(3, cards.Results.Count())
    let! cards = search basicTag
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search "Front"
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search "\"Front"
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search "Fro*"
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search <| Guid.NewGuid().ToString()
    Assert.Empty(cards.Results)
    let! cards = search front
    Assert.Equal(stack_2, cards.Results.Single().Id)
    let! cards = search back
    Assert.Equal(stack_2, cards.Results.Single().Id)
    let! cards = search clozeText
    Assert.Equal(stack_3, cards.Results.Single().Id)

    // testing deckSearch
    let search searchTerm = StackRepository.searchDeck c.Db userId 1 SearchOrder.Popularity searchTerm deck_3
    let! cards = search ""
    Assert.Equal(3, cards.Results.Count())
    let! cards = search basicTag
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search "Front"
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search "\"Front"
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search "Fro*"
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search <| Guid.NewGuid().ToString()
    Assert.Empty(cards.Results)
    let! cards = search front
    Assert.Equal(stack_2, cards.Results.Single().Id)
    let! cards = search back
    Assert.Equal(stack_2, cards.Results.Single().Id)
    let! cards = search clozeText
    Assert.Equal(stack_3, cards.Results.Single().Id)
    
    // testing deckSearch from other user
    let otherUserId = user_1
    let search searchTerm = StackRepository.searchDeck c.Db otherUserId 1 SearchOrder.Popularity searchTerm deck_3
    let! cards = search ""
    Assert.Equal(0, cards.Results.Count())
    do! SanitizeDeckRepository.setIsPublic c.Db userId deck_3 true |> TaskResult.getOk
    let! cards = search ""
    Assert.Equal(3, cards.Results.Count())
    let! cards = search basicTag
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search "Front"
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search "\"Front"
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search "Fro*"
    Assert.Equal(stack_1, cards.Results.Single().Id)
    let! cards = search <| Guid.NewGuid().ToString()
    Assert.Empty(cards.Results)
    let! cards = search front
    Assert.Equal(stack_2, cards.Results.Single().Id)
    let! cards = search back
    Assert.Equal(stack_2, cards.Results.Single().Id)
    let! cards = search clozeText
    Assert.Equal(stack_3, cards.Results.Single().Id)

    let search = StackRepository.search c.Db userId 1 SearchOrder.Relevance
    // testing relevance
    let term = "relevant "
    let less = String.replicate 1 term
    let more = String.replicate 3 term
    let! _ = FacetRepositoryTests.addBasicCustomStack [less; less] c.Db userId ["tag1"] (stack_ 4, branch_ 4, leaf_ 4, [card_ 4])
    let! _ = FacetRepositoryTests.addBasicCustomStack [more; more] c.Db userId ["tag2"] (stack_ 5, branch_ 5, leaf_ 5, [card_ 5])
    let! hits = search term
    Assert.Equal(more.Trim(), hits.Results.First().Leaf.StrippedFront)

    // testing relevance sans tags
    let term = "nightwish "
    let less = String.replicate 1 term
    let more = String.replicate 3 term
    let! _ = FacetRepositoryTests.addBasicCustomStack [less; less] c.Db userId [] (stack_ 6, branch_ 6, leaf_ 6, [card_ 6])
    let! _ = FacetRepositoryTests.addBasicCustomStack [more; more] c.Db userId [] (stack_ 7, branch_ 7, leaf_ 7, [card_ 7])
    let! hits = search term
    Assert.Equal(more.Trim(), hits.Results.First().Leaf.StrippedFront)
    
    // tags outweigh fields
    let lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."
    let tag = " batman"
    let! _ = FacetRepositoryTests.addBasicCustomStack [lorem      ; ""] c.Db userId [tag] (stack_ 8, branch_ 8, leaf_ 8, [card_ 8])
    let! _ = FacetRepositoryTests.addBasicCustomStack [lorem + tag; ""] c.Db userId []    (stack_ 9, branch_ 9, leaf_ 9, [card_ 9])
    let! hits = search tag
    Assert.Equal(lorem, hits.Results.First().Leaf.StrippedFront)

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
    let userId = user_3
    let! myGromplates = SanitizeGromplate.GetMine c.Db userId
    let theCollectiveId = c.Db.User.Single(fun x -> x.DisplayName = "The Collective").Id
    for gromplate in myGromplates do
        Assert.Equal(theCollectiveId, gromplate.AuthorId)
    }

[<Fact>]
let ``Updating card gromplate with duplicate field names yields error`` (): Task<unit> = task {
    let userId = user_3
    let fieldName = Guid.NewGuid().ToString()
    let gromplate = (Grompleaf.initialize Ulid.create Ulid.create) |> ViewGrompleaf.load
    let gromplate = { gromplate with Fields = gromplate.Fields.Select(fun f -> { f with Name = fieldName }).ToList() }
    
    let! error = SanitizeGromplate.Update null userId gromplate
    
    Assert.Equal("Field names must differ", error.error)
    }

[<Fact>]
let ``Can create card gromplate and insert a modified one`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let name = Guid.NewGuid().ToString()
    let grompleafId1 = grompleaf_ 8
    let gromplateId1 = gromplate_ 8
    let initialGromplate = ViewGromplateWithAllLeafs.initialize userId grompleafId1 gromplateId1

    let! x = SanitizeGromplate.Update c.Db userId { initialGromplate.Editable with Name = name }
    Assert.Null x.Value
    let! myGromplates1 = SanitizeGromplate.GetMine c.Db userId

    Assert.equal name <| myGromplates1.Single(fun x -> x.Id = gromplateId1).Editable.Name
    
    // testing a brand new gromplate, but slightly different
    let fieldName = Guid.NewGuid().ToString()
    let newEditable =
        let newField =
            {   Name = fieldName
                IsRightToLeft = false
                IsSticky = false
            }
        {   initialGromplate.Editable with
                Id = grompleaf_ 9
                GromplateId = gromplate_ 9
                Fields = initialGromplate.Editable.Fields.Append newField |> Core.toResizeArray
        }
    let! x = SanitizeGromplate.Update c.Db userId newEditable
    Assert.Null x.Value
    
    Assert.Equal(2, c.Db.Gromplate.Count(fun x -> x.AuthorId = userId))
    let! myGromplates2 = SanitizeGromplate.GetMine c.Db userId
    let latestId = myGromplates2.Select(fun x -> x.Id).Except(myGromplates1.Select(fun x -> x.Id)).Single()
    Assert.equal (gromplate_ 9) latestId
    let latestGromplate = myGromplates2.Single(fun x -> x.Id = latestId).Editable
    Assert.True(latestGromplate.Fields.Any(fun x -> x.Name = fieldName))

    // updating the slightly different gromplate
    let name = Guid.NewGuid().ToString()
    let! x = SanitizeGromplate.Update c.Db userId { latestGromplate with Name = name; Id = Ulid.create }
    Assert.Null x.Value

    let! myGromplates = SanitizeGromplate.GetMine c.Db userId
    Assert.Equal(latestGromplate.GromplateId, myGromplates.Select(fun x -> x.Leafs.First()).Single(fun x -> x.Name = name).GromplateId)

    // updating to cloze
    let name = Guid.NewGuid().ToString()
    let! x =
        SanitizeGromplate.Update c.Db userId
            { latestGromplate
                with
                    Id = Ulid.create
                    Name = name
                    Templates = Cloze <| latestGromplate.JustTemplates.First()
            }
    Assert.Null x.Value

    let! myGromplates = SanitizeGromplate.GetMine c.Db userId
    Assert.Equal(latestGromplate.GromplateId, myGromplates.Select(fun x -> x.Leafs.First()).Single(fun x -> x.Name = name).GromplateId)
    Assert.True(myGromplates.Select(fun x -> x.Leafs.First()).Single(fun x -> x.Name = name).IsCloze)

    // updating to multiple templates
    let name = Guid.NewGuid().ToString()
    let! x =
        SanitizeGromplate.Update c.Db userId
            { latestGromplate
                with
                    Id = Ulid.create
                    Name = name
                    Templates = Standard [ latestGromplate.JustTemplates.First() ; latestGromplate.JustTemplates.First() ]
            }
    Assert.Null x.Value

    let! myGromplates = SanitizeGromplate.GetMine c.Db userId
    Assert.Equal(latestGromplate.GromplateId, myGromplates.Select(fun x -> x.Leafs.First()).Single(fun x -> x.Name = name).GromplateId)
    Assert.Equal(2, myGromplates.Select(fun x -> x.Leafs.First()).Single(fun x -> x.Name = name).JustTemplates.Count())
    }

[<Fact>]
let ``New card gromplate has correct hash`` (): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let initialGromplate = ViewGromplateWithAllLeafs.initialize userId Ulid.create Ulid.create
    use sha512 = SHA512.Create()
    do! SanitizeGromplate.Update c.Db userId initialGromplate.Editable
    let! (dbGromplate: GrompleafEntity) = c.Db.Grompleaf.SingleAsync(fun x -> x.Gromplate.AuthorId = userId)
    
    let computedHash =
        initialGromplate.Editable
        |> ViewGrompleaf.copyTo
        |> fun x -> x.CopyToNewLeaf
        |> GrompleafEntity.hash sha512
    
    Assert.Equal<BitArray>(dbGromplate.Hash, computedHash)
    } |> TaskResult.getOk)

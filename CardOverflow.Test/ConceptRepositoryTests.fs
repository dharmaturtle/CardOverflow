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
open FSharp.UMX

[<Fact(Skip=PgSkip.reason)>]
let ``Getting 10 pages of GetCollectedPages takes less than 1 minute``(): Task<unit> = task {
    use c = new Container()
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    use __ = AsyncScopedLifestyle.BeginScope c
    let db = c.GetInstance<CardOverflowDb>()
    let userId = user_3

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        let! _ = ConceptRepository.GetCollectedPages db userId i ""
        ()
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)
    }

[<Fact(Skip=PgSkip.reason)>]
let ``GetCollectedPages works if updated``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = FacetRepositoryTests.addBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1])
    let exampleId = example_1
    let secondVersion = Guid.NewGuid().ToString()
    do! FacetRepositoryTests.update c userId
            (VUpdate_ExampleId exampleId) (fun x -> { x with EditSummary = secondVersion }) ((concept_1, example_1, revision_2, [card_1]) |> UpsertIds.fromTuple) exampleId
    let oldRevisionId = revision_1
    let updatedRevisionId = revision_2
    do! c.Db.Revision.SingleAsync(fun x -> x.Id = oldRevisionId)
        |> Task.map (fun x -> Assert.Equal("Initial creation", x.EditSummary))
    do! c.Db.Revision.SingleAsync(fun x -> x.Id = updatedRevisionId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))

    let! (cards: PagedList<Result<Card, string>>) = ConceptRepository.GetCollectedPages c.Db userId 1 ""
    let cards = cards.Results |> Seq.map Result.getOk |> Seq.toList

    Assert.Equal(updatedRevisionId, cards.Select(fun x -> x.RevisionMeta.Id).Distinct().Single())

    // getCollectedRevisionFromRevision gets the updatedRevisionId when given the oldRevisionId
    let! actual = CardRepository.getCollectedRevisionFromRevision c.Db userId oldRevisionId

    Assert.Equal(updatedRevisionId, actual)

    // getCollectedRevisionFromRevision gets the updatedRevisionId when given the updatedRevisionId
    let! actual = CardRepository.getCollectedRevisionFromRevision c.Db userId updatedRevisionId

    Assert.Equal(updatedRevisionId, actual)

    // getCollectedRevisionFromRevision fails gracefully on invalid revisionId
    let invalidRevisionId = Ulid.create

    let! (actual: Result<_,_>) = CardRepository.getCollectedRevisionFromRevision c.Db userId invalidRevisionId

    Assert.equal (sprintf "You don't have any cards with Example Revision #%A" invalidRevisionId) actual.error

    // ConceptRepository.Revisions says we collected the most recent revision
    let! revision = ConceptRepository.Revisions c.Db userId exampleId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldRevisionId, false); (updatedRevisionId, true)]

    // collect oldest revision, then ConceptRepository.Revisions says we collected the oldest revision
    let! _ = ConceptRepository.CollectCard c.Db userId oldRevisionId [ card_1 ]
    
    let! revision = ConceptRepository.Revisions c.Db userId exampleId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldRevisionId, true); (updatedRevisionId, false)]
    } |> TaskResult.getOk)

[<Fact(Skip=PgSkip.reason)>]
let ``GetCollectedPages works if updated, but pair``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = FacetRepositoryTests.addReversedBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1; card_2])
    let exampleId = example_1
    let secondVersion = Guid.NewGuid().ToString()
    do! FacetRepositoryTests.update c userId
            (VUpdate_ExampleId exampleId) (fun x -> { x with EditSummary = secondVersion }) ((concept_1, example_1, revision_2, [card_1; card_2]) |> UpsertIds.fromTuple) exampleId
    let oldRevisionId = revision_1
    let updatedRevisionId = revision_2
    do! c.Db.Revision.SingleAsync(fun x -> x.Id = oldRevisionId)
        |> Task.map (fun x -> Assert.Equal("Initial creation", x.EditSummary))
    do! c.Db.Revision.SingleAsync(fun x -> x.Id = updatedRevisionId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))

    let! (cards: PagedList<Result<Card, string>>) = ConceptRepository.GetCollectedPages c.Db userId 1 ""
    let cards = cards.Results |> Seq.map Result.getOk |> Seq.toList

    Assert.Equal(updatedRevisionId, cards.Select(fun x -> x.RevisionMeta.Id).Distinct().Single())

    // getCollectedRevisionFromRevision gets the updatedRevisionId when given the oldRevisionId
    let! actual = CardRepository.getCollectedRevisionFromRevision c.Db userId oldRevisionId

    Assert.Equal(updatedRevisionId, actual)

    // getCollectedRevisionFromRevision gets the updatedRevisionId when given the updatedRevisionId
    let! actual = CardRepository.getCollectedRevisionFromRevision c.Db userId updatedRevisionId

    Assert.Equal(updatedRevisionId, actual)

    // getCollectedRevisionFromRevision fails gracefully on invalid revisionId
    let invalidRevisionId = Ulid.create

    let! (actual: Result<_,_>) = CardRepository.getCollectedRevisionFromRevision c.Db userId invalidRevisionId

    Assert.equal (sprintf "You don't have any cards with Example Revision #%A" invalidRevisionId) actual.error

    // ConceptRepository.Revisions says we collected the most recent revision
    let! revision = ConceptRepository.Revisions c.Db userId exampleId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldRevisionId, false); (updatedRevisionId, true)]

    // collect oldest revision, then ConceptRepository.Revisions says we collected the oldest revision
    let! _ = ConceptRepository.CollectCard c.Db userId oldRevisionId [card_1; card_2]
    
    let! revision = ConceptRepository.Revisions c.Db userId exampleId

    revision.SortedMeta.OrderBy(fun x -> x.Id).Select(fun x -> x.Id, x.IsCollected) |> List.ofSeq 
    |> Assert.equal [(oldRevisionId, true); (updatedRevisionId, false)]
    } |> TaskResult.getOk)

[<Fact(Skip=PgSkip.reason)>]
let ``GetForUser isn't empty``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let! _ = FacetRepositoryTests.addBasicConcept c.Db userId ["A"; "B"] (concept_1, example_1, revision_1, [card_1])
    do! CommentConceptEntity (
            ConceptId = concept_1,
            UserId = userId,
            Text = "text",
            Created = DateTimeX.UtcNow
        ) |> CommentRepository.addAndSaveAsync c.Db
    let conceptId = concept_1
        
    let! concept = ExploreConceptRepository.get c.Db userId conceptId
    let concept = concept.Value
    let! view = ConceptViewRepository.get c.Db conceptId
        
    let front, _, _, _ = view.Value.FrontBackFrontSynthBackSynth.[0]
    Assert.DoesNotContain("{{Front}}", front)
    Assert.NotEmpty <| concept.Comments
    Assert.True concept.Default.Revision.IsCollected
    Assert.areEquivalent
        [{  Name = "A"
            Count = 1
            IsCollected = true }
         {  Name = "B"
            Count = 1
            IsCollected = true }]
        concept.Tags
    
    let missingConcept = Ulid.create
    let! concept = ExploreConceptRepository.get c.Db userId missingConcept
    Assert.equal (sprintf "Concept #%A not found" missingConcept) concept.error }

[<Fact(Skip=PgSkip.reason)>]
let ``Getting 10 pages of GetAsync takes less than 1 minute, and has users``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let! _ = FacetRepositoryTests.addBasicConcept c.Db userId ["A"; "B"] (concept_1, example_1, revision_1, [card_1])

    let stopwatch = Stopwatch.StartNew()
    for i in 1 .. 10 do
        let! _ = ConceptRepository.search c.Db userId i SearchOrder.Popularity ""
        ()
    Assert.True(stopwatch.Elapsed <= TimeSpan.FromMinutes 1.)
    
    let! concept = ExploreConceptRepository.get c.Db userId concept_1
    let concept = concept.Value
    Assert.Equal(1, concept.Default.Summary.Users)
    Assert.areEquivalent
        [{  Name = "A"
            Count = 1
            IsCollected = true }
         {  Name = "B"
            Count = 1
            IsCollected = true }]
        concept.Tags
    
    let! cc = ConceptRepository.GetCollected c.Db userId concept.Id
    let cc = cc.Value.Single()
    let! x = ConceptRepository.editState c.Db userId cc.CardId CardState.Suspended
    Assert.Null x.Value
    let! concept = ExploreConceptRepository.get c.Db userId concept_1
    Assert.Equal(0, concept.Value.Default.Revision.Users) // suspended cards don't count to User count
    }

let testGetCollected (acCount: int) addCard getTemplate name = task {
    use c = new TestContainer(false, name)
    
    let authorId = user_1 // this user creates the card
    let! x = addCard c.Db authorId ["A"] (concept_1, example_1, revision_1, [1..acCount] |> List.map (fun _ -> Ulid.create)) |> TaskResult.getOk
    Assert.NotNull x
    let conceptId = concept_1
    let exampleId = example_1
    let revisionId = revision_1
    let! cards = ConceptRepository.GetCollectedPages c.Db authorId 1 ""
    Assert.Equal(acCount, cards.Results.Count())
    let! cc = ConceptRepository.GetCollected c.Db authorId conceptId
    let cc = cc.Value
    Assert.Equal(authorId, cc.Select(fun x -> x.UserId).Distinct().Single())

    let collectorId = user_2  // this user collects the card
    let cardIds = [1..acCount] |> List.map (fun _ -> Ulid.create)
    let! _ = ConceptRepository.CollectCard c.Db collectorId revisionId cardIds |> TaskResult.getOk
    let! concept = ExploreConceptRepository.get c.Db collectorId conceptId |> TaskResult.getOk
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 1
            IsCollected = false }],
        concept.Tags
    )
    do! SanitizeTagRepository.AddTo c.Db collectorId "a" concept.Id |> TaskResult.getOk
    let! concept = ExploreConceptRepository.get c.Db collectorId conceptId |> TaskResult.getOk
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 2
            IsCollected = true }],
        concept.Tags
    )
    let! concepts = ConceptRepository.search c.Db collectorId 1 SearchOrder.Popularity ""
    Assert.Equal(1, concepts.Results.Count())

    // author creating another example keeps tags
    let! (template: ViewTemplateRevision) = getTemplate c.Db
    let! _ =
        {   EditConceptCommand.EditSummary = ""
            FieldValues = [].ToList()
            TemplateRevisionId = % template.Id
            Kind = NewExample_Title "New Example"
            Ids = ids_1
        } |> UpdateRepository.concept c.Db authorId

    let! concept = ExploreConceptRepository.get c.Db authorId conceptId |> TaskResult.getOk
    
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 2
            IsCollected = true }],
        concept.Tags
    )

    // search returns Default examples (not the new one created)
    let! concepts = ConceptRepository.search c.Db collectorId 1 SearchOrder.Popularity ""
    Assert.Equal(1, concepts.Results.Count())

    let nonCollectorId = user_3 // this user never collects the card
    let! concept = ExploreConceptRepository.get c.Db nonCollectorId concept_1 |> TaskResult.getOk
    Assert.Equal<ViewTag seq>(
        [{  Name = "A"
            Count = 2
            IsCollected = false }],
        concept.Tags
    )}
    
[<Fact(Skip=PgSkip.reason)>]
let rec ``GetCollected works when collecting 1 basic card``(): Task<unit> =
    testGetCollected
        1
        FacetRepositoryTests.addBasicConcept
        FacetRepositoryTests.basicTemplate
        <| nameof ``GetCollected works when collecting 1 basic card``

[<Fact(Skip=PgSkip.reason)>]
let rec ``GetCollected works when collecting a pair``(): Task<unit> = 
    testGetCollected
        2
        FacetRepositoryTests.addReversedBasicConcept
        FacetRepositoryTests.reversedBasicTemplate
        <| nameof ``GetCollected works when collecting a pair``

let relationshipTestInit (c: TestContainer) relationshipName = task {
    let addRelationshipCommand1 =
        {   Name = relationshipName
            SourceConceptId = concept_1
            TargetConceptLink = concept_2.ToString()
        }
    let addRelationshipCommand2 =
        {   Name = relationshipName
            SourceConceptId = concept_2
            TargetConceptLink = concept_1.ToString()
        }
    let commands = [
        addRelationshipCommand1, addRelationshipCommand1
        addRelationshipCommand2, addRelationshipCommand2
        addRelationshipCommand1, addRelationshipCommand2
        addRelationshipCommand2, addRelationshipCommand1 ]

    let userId = user_1 // this user creates the card
    let! x = FacetRepositoryTests.addBasicConcept          c.Db userId [] (concept_1, example_1, revision_1, [Ulid.create])
    Assert.NotNull x.Value
    let! x = FacetRepositoryTests.addReversedBasicConcept  c.Db userId [] (concept_2, example_2, revision_2, [Ulid.create; Ulid.create])
    Assert.NotNull x.Value

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1
    Assert.Null x.Value
    let! concept = ExploreConceptRepository.get c.Db userId concept_1
    Assert.Equal(1, concept.Value.Relationships.Single().Users)
    let! concept = ExploreConceptRepository.get c.Db userId concept_2
    Assert.Equal(1, concept.Value.Relationships.Single().Users)

    let successfulRemove () = task {
        let! r = SanitizeRelationshipRepository.Remove c.Db concept_1 concept_2 userId relationshipName
        Assert.Null r.Value
        let! concept = ExploreConceptRepository.get c.Db userId concept_1
        Assert.Equal(0, concept.Value.Relationships.Count)
        let! concept = ExploreConceptRepository.get c.Db userId concept_2
        Assert.Equal(0, concept.Value.Relationships.Count) }
    do! successfulRemove ()

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand1
    Assert.Null x.Value
    let! r = SanitizeRelationshipRepository.Remove c.Db concept_2 concept_1 userId relationshipName
    Assert.Equal(sprintf "Relationship not found between source Concept #%A and target Concept #%A with name \"%s\"." concept_2 concept_1 relationshipName, r.error)
    let! concept = ExploreConceptRepository.get c.Db userId concept_1
    Assert.Equal(1, concept.Value.Relationships.Count)
    let! concept = ExploreConceptRepository.get c.Db userId concept_2
    Assert.Equal(1, concept.Value.Relationships.Count)
    do! successfulRemove ()

    return commands }

[<Fact(Skip=PgSkip.reason)>]
let ``Relationships can't be self related``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let! actualExampleId = FacetRepositoryTests.addBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1])
    Assert.Equal(example_ 1, actualExampleId.Value)
    let addRelationshipCommand =
        {   Name = ""
            SourceConceptId = concept_1
            TargetConceptLink = concept_1.ToString()
        }

    let! x = SanitizeRelationshipRepository.Add c.Db userId addRelationshipCommand
    
    Assert.equal "A concept can't be related to itself" x.error }

[<Fact(Skip=PgSkip.reason)>]
let ``Directional relationship tests``(): Task<unit> = task {
    let revisionIds = [revision_1; revision_2]
    use c = new TestContainer()
    let relationshipName = "Test/Relationship"
    
    let! commands = relationshipTestInit c relationshipName
    let testRelationships userId (creator, collector) = task {
        let! x = SanitizeRelationshipRepository.Add c.Db user_1 creator // card creator also collects the relationship; .Single() below refers this this
        Assert.Null x.Value
        
        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! concept = ExploreConceptRepository.get c.Db userId concept_1
        let concept = concept.Value
        Assert.Equal(2, concept.Relationships.Single().Users)
        Assert.True(concept.Relationships.Single().IsCollected)
        let! concept = ExploreConceptRepository.get c.Db userId concept_2
        let concept = concept.Value
        Assert.Equal(2, concept.Relationships.Single().Users)
        Assert.True(concept.Relationships.Single().IsCollected)

        let successfulRemove () = task {
            let! r = SanitizeRelationshipRepository.Remove c.Db collector.SourceConceptId (Guid.Parse collector.TargetConceptLink) userId relationshipName
            Assert.Null r.Value
            let! concept = ExploreConceptRepository.get c.Db userId concept_1
            let concept = concept.Value
            Assert.Equal(1, concept.Relationships.Count)
            Assert.False(concept.Relationships.Single().IsCollected)
            let! concept = ExploreConceptRepository.get c.Db userId concept_2
            let concept = concept.Value
            Assert.Equal(1, concept.Relationships.Count)
            Assert.False(concept.Relationships.Single().IsCollected) }
        do! successfulRemove ()

        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! r = SanitizeRelationshipRepository.Remove c.Db (Guid.Parse collector.TargetConceptLink) collector.SourceConceptId userId relationshipName
        Assert.Equal(sprintf "Relationship not found between source Concept #%s and target Concept #%A with name \"%s\"." collector.TargetConceptLink collector.SourceConceptId relationshipName, r.error)
        let! concept = ExploreConceptRepository.get c.Db userId concept_1
        let concept = concept.Value
        Assert.Equal(1, concept.Relationships.Count)
        Assert.True(concept.Relationships.Single().IsCollected)
        let! concept = ExploreConceptRepository.get c.Db userId concept_2
        let concept = concept.Value
        Assert.Equal(1, concept.Relationships.Count)
        Assert.True(concept.Relationships.Single().IsCollected)
            
        do! successfulRemove ()
        let! r = SanitizeRelationshipRepository.Remove c.Db collector.SourceConceptId (Guid.Parse collector.TargetConceptLink) user_1 relationshipName // cleanup from do! SanitizeRelationshipRepository.Add c.Db 1 a |> Result.getOk
        Assert.Null r.Value }

    let userId = user_2 // this user collects the card
    let! _ = ConceptRepository.CollectCard c.Db userId revisionIds.[0] [ Ulid.create ] |> TaskResult.getOk
    let! _ = ConceptRepository.CollectCard c.Db userId revisionIds.[1] [ Ulid.create; Ulid.create ] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    //do! testRelationships userId commands.[2]
    //do! testRelationships userId commands.[3]

    let userId = user_3 // this user collects card in opposite order from user2
    let! _ = ConceptRepository.CollectCard c.Db userId revisionIds.[1] [ Ulid.create; Ulid.create ] |> TaskResult.getOk
    let! _ = ConceptRepository.CollectCard c.Db userId revisionIds.[0] [ Ulid.create ] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    //do! testRelationships userId commands.[2]
    //do! testRelationships userId commands.[3]
    }

[<Fact(Skip=PgSkip.reason)>]
let ``Nondirectional relationship tests``(): Task<unit> = task {
    let revisionIds = [revision_1; revision_2]
    use c = new TestContainer()
    let relationshipName = Guid.NewGuid().ToString() |> MappingTools.toTitleCase
    
    let! commands = relationshipTestInit c relationshipName
    let testRelationships userId (creator, collector) = task {
        let! x = SanitizeRelationshipRepository.Add c.Db user_1 creator // card creator also collects the relationship; .Single() below refers this this
        Assert.Null x.Value
        
        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! concept = ExploreConceptRepository.get c.Db userId concept_1 |> TaskResult.getOk
        Assert.Equal(2, concept.Relationships.Single().Users)
        Assert.True(concept.Relationships.Single().IsCollected)
        let! concept = ExploreConceptRepository.get c.Db userId concept_2 |> TaskResult.getOk
        Assert.Equal(2, concept.Relationships.Single().Users)
        Assert.True(concept.Relationships.Single().IsCollected)

        let successfulRemove () = task {
            let! r = SanitizeRelationshipRepository.Remove c.Db concept_1 concept_2 userId relationshipName
            Assert.Null r.Value
            let! concept = ExploreConceptRepository.get c.Db userId concept_1 |> TaskResult.getOk
            Assert.Equal(1, concept.Relationships.Count)
            Assert.False(concept.Relationships.Single().IsCollected)
            let! concept = ExploreConceptRepository.get c.Db userId concept_2 |> TaskResult.getOk
            Assert.Equal(1, concept.Relationships.Count)
            Assert.False(concept.Relationships.Single().IsCollected) }
        do! successfulRemove ()

        let! x = SanitizeRelationshipRepository.Add c.Db userId collector
        Assert.Null x.Value
        let! r = SanitizeRelationshipRepository.Remove c.Db setting_1 concept_1 userId relationshipName
        Assert.Equal(sprintf "Relationship not found between source Concept #%A and target Concept #%A with name \"%s\"." setting_1 concept_1 relationshipName, r.error)
        let! concept = ExploreConceptRepository.get c.Db userId concept_1 |> TaskResult.getOk
        Assert.Equal(1, concept.Relationships.Count)
        Assert.True(concept.Relationships.Single().IsCollected)
        let! concept = ExploreConceptRepository.get c.Db userId concept_2 |> TaskResult.getOk
        Assert.Equal(1, concept.Relationships.Count)
        Assert.True(concept.Relationships.Single().IsCollected)
            
        do! successfulRemove ()
        let! r = SanitizeRelationshipRepository.Remove c.Db concept_1 concept_2 user_1 relationshipName // cleanup from do! SanitizeRelationshipRepository.Add c.Db 1 a |> Result.getOk
        Assert.Null r.Value }

    let userId = user_2 // this user collects the card
    let! _ = ConceptRepository.CollectCard c.Db userId revisionIds.[0] [ Ulid.create ] |> TaskResult.getOk
    let! _ = ConceptRepository.CollectCard c.Db userId revisionIds.[1] [ Ulid.create; Ulid.create ] |> TaskResult.getOk
    do! testRelationships userId commands.[0]
    do! testRelationships userId commands.[1]
    do! testRelationships userId commands.[2]
    do! testRelationships userId commands.[3]

    let userId = user_3 // this user collects card in opposite order from user2
    let! _ = ConceptRepository.CollectCard c.Db userId revisionIds.[1] [ Ulid.create; Ulid.create ] |> TaskResult.getOk
    let! _ = ConceptRepository.CollectCard c.Db userId revisionIds.[0] [ Ulid.create ] |> TaskResult.getOk
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

[<Fact(Skip=PgSkip.reason)>]
let ``Card search works`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let basicTag = "basic"
    let! _ = FacetRepositoryTests.addBasicConcept c.Db userId [basicTag] (concept_1, example_1, revision_1, [card_1])
    let front = Guid.NewGuid().ToString()
    let back = Guid.NewGuid().ToString()
    let! _ = FacetRepositoryTests.addBasicCustomConcept [front; back] c.Db userId ["custom"] (concept_2, example_2, revision_2, [card_2])
    let clozeText = "{{c1::" + Guid.NewGuid().ToString() + "}}"
    let! _ = FacetRepositoryTests.addCloze clozeText c.Db userId [] (concept_3, example_3, revision_3, [card_3])
    
    // testing search
    let search = ConceptRepository.search c.Db userId 1 SearchOrder.Popularity
    let! cards = search ""
    Assert.Equal(3, cards.Results.Count())
    let! cards = search basicTag
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search "Front"
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search "\"Front"
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search "Fro*"
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search <| Guid.NewGuid().ToString()
    Assert.Empty(cards.Results)
    let! cards = search front
    Assert.Equal(concept_2, cards.Results.Single().Id)
    let! cards = search back
    Assert.Equal(concept_2, cards.Results.Single().Id)
    let! cards = search clozeText
    Assert.Equal(concept_3, cards.Results.Single().Id)

    // testing deckSearch
    let search searchTerm = ConceptRepository.searchDeck c.Db userId 1 SearchOrder.Popularity searchTerm deck_3
    let! cards = search ""
    Assert.Equal(3, cards.Results.Count())
    let! cards = search basicTag
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search "Front"
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search "\"Front"
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search "Fro*"
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search <| Guid.NewGuid().ToString()
    Assert.Empty(cards.Results)
    let! cards = search front
    Assert.Equal(concept_2, cards.Results.Single().Id)
    let! cards = search back
    Assert.Equal(concept_2, cards.Results.Single().Id)
    let! cards = search clozeText
    Assert.Equal(concept_3, cards.Results.Single().Id)
    
    // testing deckSearch from other user
    let otherUserId = user_1
    let search searchTerm = ConceptRepository.searchDeck c.Db otherUserId 1 SearchOrder.Popularity searchTerm deck_3
    let! cards = search ""
    Assert.Equal(0, cards.Results.Count())
    do! SanitizeDeckRepository.setIsPublic c.Db userId deck_3 true |> TaskResult.getOk
    let! cards = search ""
    Assert.Equal(3, cards.Results.Count())
    let! cards = search basicTag
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search "Front"
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search "\"Front"
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search "Fro*"
    Assert.Equal(concept_1, cards.Results.Single().Id)
    let! cards = search <| Guid.NewGuid().ToString()
    Assert.Empty(cards.Results)
    let! cards = search front
    Assert.Equal(concept_2, cards.Results.Single().Id)
    let! cards = search back
    Assert.Equal(concept_2, cards.Results.Single().Id)
    let! cards = search clozeText
    Assert.Equal(concept_3, cards.Results.Single().Id)

    let search = ConceptRepository.search c.Db userId 1 SearchOrder.Relevance
    // testing relevance
    let term = "relevant "
    let less = String.replicate 1 term
    let more = String.replicate 3 term
    let! _ = FacetRepositoryTests.addBasicCustomConcept [less; less] c.Db userId ["tag1"] (concept_ 4, example_ 4, revision_ 4, [card_ 4])
    let! _ = FacetRepositoryTests.addBasicCustomConcept [more; more] c.Db userId ["tag2"] (concept_ 5, example_ 5, revision_ 5, [card_ 5])
    let! hits = search term
    Assert.Equal(more.Trim(), hits.Results.First().Revision.StrippedFront)

    // testing relevance sans tags
    let term = "nightwish "
    let less = String.replicate 1 term
    let more = String.replicate 3 term
    let! _ = FacetRepositoryTests.addBasicCustomConcept [less; less] c.Db userId [] (concept_ 6, example_ 6, revision_ 6, [card_ 6])
    let! _ = FacetRepositoryTests.addBasicCustomConcept [more; more] c.Db userId [] (concept_ 7, example_ 7, revision_ 7, [card_ 7])
    let! hits = search term
    Assert.Equal(more.Trim(), hits.Results.First().Revision.StrippedFront)
    
    // tags outweigh fields
    let lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."
    let tag = " batman"
    let! _ = FacetRepositoryTests.addBasicCustomConcept [lorem      ; ""] c.Db userId [tag] (concept_ 8, example_ 8, revision_ 8, [card_ 8])
    let! _ = FacetRepositoryTests.addBasicCustomConcept [lorem + tag; ""] c.Db userId []    (concept_ 9, example_ 9, revision_ 9, [card_ 9])
    let! hits = search tag
    Assert.Equal(lorem, hits.Results.First().Revision.StrippedFront)

    // testing template search
    let search = SanitizeTemplate.Search c.Db userId 1
    let! templates = search "Cloze"
    Assert.Equal("Cloze", templates.Results.Single().Name)
    Assert.Equal(1, templates.Results.Single().TemplateUsers)
    Assert.False(templates.Results.Single().IsCollected) // most recent cloze is not collected because it's missing Extra. Why Damien?
    let! templates = search "type"
    Assert.Equal("Basic (type in the answer)", templates.Results.Single().Name)
    Assert.Equal(1, templates.Results.Single().TemplateUsers)
    Assert.True(templates.Results.Single().IsCollected)
    let! templates = search "Basic"
    Assert.Equal(4, templates.Results.Count())
    Assert.True(templates.Results.All(fun x -> x.TemplateUsers = 1))
    Assert.True(templates.Results.All(fun x -> x.IsCollected))
    }

[<Fact(Skip=PgSkip.reason)>]
let ``New user has TheCollective's card templates`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let! myTemplates = SanitizeTemplate.GetMine c.Db userId
    let theCollectiveId = c.Db.User.Single(fun x -> x.DisplayName = "The Collective").Id
    for template in myTemplates do
        Assert.Equal(theCollectiveId, template.AuthorId)
    }

[<Fact>]
let ``Updating card template with duplicate field names yields error`` (): Task<unit> = task {
    let userId = user_3
    let fieldName = Guid.NewGuid().ToString()
    let template = (TemplateRevision.initialize Ulid.create Ulid.create) |> ViewTemplateRevision.load
    let template = { template with Fields = template.Fields.Select(fun f -> { f with Name = fieldName }).ToList() }
    
    let! error = SanitizeTemplate.Update null userId template
    
    Assert.Equal("Field names must differ", error.error)
    }

[<Fact(Skip=PgSkip.reason)>]
let ``Can create card template and insert a modified one`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let name = Guid.NewGuid().ToString()
    let templateRevisionId1 = templateRevision_ 8
    let templateId1 = template_ 8
    let initialTemplate = ViewTemplateWithAllRevisions.initialize userId templateRevisionId1 templateId1

    let! x = SanitizeTemplate.Update c.Db userId { initialTemplate.Editable with Name = name }
    Assert.Null x.Value
    let! myTemplates1 = SanitizeTemplate.GetMine c.Db userId

    Assert.equal name <| myTemplates1.Single(fun x -> x.Id = templateId1).Editable.Name
    
    // testing a brand new template, but slightly different
    let fieldName = Guid.NewGuid().ToString()
    let newEditable =
        let newField =
            {   Name = fieldName
                IsRightToLeft = false
                IsSticky = false
            }
        {   initialTemplate.Editable with
                Id = templateRevision_ 9
                TemplateId = template_ 9
                Fields = initialTemplate.Editable.Fields.Append newField |> Core.toResizeArray
        }
    let! x = SanitizeTemplate.Update c.Db userId newEditable
    Assert.Null x.Value
    
    Assert.Equal(2, c.Db.Template.Count(fun x -> x.AuthorId = userId))
    let! myTemplates2 = SanitizeTemplate.GetMine c.Db userId
    let latestId = myTemplates2.Select(fun x -> x.Id).Except(myTemplates1.Select(fun x -> x.Id)).Single()
    Assert.equal (template_ 9) latestId
    let latestTemplate = myTemplates2.Single(fun x -> x.Id = latestId).Editable
    Assert.True(latestTemplate.Fields.Any(fun x -> x.Name = fieldName))

    // updating the slightly different template
    let name = Guid.NewGuid().ToString()
    let! x = SanitizeTemplate.Update c.Db userId { latestTemplate with Name = name; Id = Ulid.create }
    Assert.Null x.Value

    let! myTemplates = SanitizeTemplate.GetMine c.Db userId
    Assert.Equal(latestTemplate.TemplateId, myTemplates.Select(fun x -> x.Revisions.First()).Single(fun x -> x.Name = name).TemplateId)

    // updating to cloze
    let name = Guid.NewGuid().ToString()
    let! x =
        SanitizeTemplate.Update c.Db userId
            { latestTemplate
                with
                    Id = Ulid.create
                    Name = name
                    CardTemplates = Cloze <| latestTemplate.JustCardTemplates.First()
            }
    Assert.Null x.Value

    let! myTemplates = SanitizeTemplate.GetMine c.Db userId
    Assert.Equal(latestTemplate.TemplateId, myTemplates.Select(fun x -> x.Revisions.First()).Single(fun x -> x.Name = name).TemplateId)
    Assert.True(myTemplates.Select(fun x -> x.Revisions.First()).Single(fun x -> x.Name = name).IsCloze)

    // updating to multiple card templates
    let name = Guid.NewGuid().ToString()
    let! x =
        SanitizeTemplate.Update c.Db userId
            { latestTemplate
                with
                    Id = Ulid.create
                    Name = name
                    CardTemplates = Standard [ latestTemplate.JustCardTemplates.First() ; latestTemplate.JustCardTemplates.First() ]
            }
    Assert.Null x.Value

    let! myTemplates = SanitizeTemplate.GetMine c.Db userId
    Assert.Equal(latestTemplate.TemplateId, myTemplates.Select(fun x -> x.Revisions.First()).Single(fun x -> x.Name = name).TemplateId)
    Assert.Equal(2, myTemplates.Select(fun x -> x.Revisions.First()).Single(fun x -> x.Name = name).JustCardTemplates.Count())
    }

[<Fact(Skip=PgSkip.reason)>]
let ``New card template has correct hash`` (): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let initialTemplate = ViewTemplateWithAllRevisions.initialize userId Ulid.create Ulid.create
    use sha512 = SHA512.Create()
    do! SanitizeTemplate.Update c.Db userId initialTemplate.Editable
    let! (dbTemplate: TemplateRevisionEntity) = c.Db.TemplateRevision.SingleAsync(fun x -> x.Template.AuthorId = userId)
    
    let computedHash =
        initialTemplate.Editable
        |> ViewTemplateRevision.copyTo
        |> fun x -> x.CopyToNewRevision
        |> TemplateRevisionEntity.hash sha512
    
    Assert.Equal<BitArray>(dbTemplate.Hash, computedHash)
    } |> TaskResult.getOk)

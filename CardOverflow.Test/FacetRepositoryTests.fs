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
open CardOverflow.Sanitation
open FsToolkit.ErrorHandling

let normalCommand fieldValues templateRevision tagIds ids =
    let fieldValues =
        match fieldValues with
        | [] -> ["Front"; "Back"]
        | _ -> fieldValues
    {   TemplateRevision = templateRevision
        FieldValues =
            templateRevision.Fields
            |> Seq.mapi (fun i field -> {
                EditField = ViewField.copyTo field
                Value = fieldValues.[i]
            }) |> toResizeArray
        EditSummary = "Initial creation"
        Kind = NewOriginal_TagIds (tagIds |> Set.ofList)
        Title = null
        Ids = UpsertIds.fromTuple ids
    }

let clozeCommand clozeText (clozeTemplate: ViewTemplateRevision) tagIds ids = {
    EditSummary = "Initial creation"
    FieldValues =
        clozeTemplate.Fields.Select(fun f -> {
            EditField = ViewField.copyTo f
            Value =
                if f.Name = "Text" then
                    clozeText
                else
                    "extra"
        }).ToList()
    TemplateRevision = clozeTemplate
    Kind = NewOriginal_TagIds (tagIds |> Set.ofList)
    Title = null
    Ids = UpsertIds.fromTuple ids }

let add templateName createCommand (db: CardOverflowDb) userId tags (ids: Guid * Guid * Guid * Guid list) = taskResult {
    let! template = TestTemplateRepo.SearchEarliest db templateName
    return!
        createCommand template tags ids
        |> SanitizeConceptRepository.Update db userId []
    }

let addReversedBasicConcept: CardOverflowDb -> Guid -> string list -> (Guid * Guid * Guid * Guid list) -> Task<Result<Guid, string>> =
    add "Basic (and reversed card)" <| normalCommand []

let addBasicConcept =
    add "Basic" <| normalCommand []

let addBasicCustomConcept fieldValues =
    add "Basic" <| normalCommand fieldValues

let addCloze fieldValues =
    add "Cloze" <| clozeCommand fieldValues

let reversedBasicTemplate db =
    TestTemplateRepo.SearchEarliest db "Basic (and reversed card)"

let basicTemplate db =
    TestTemplateRepo.SearchEarliest db "Basic"

let update (c: TestContainer) authorId kind commandTransformer updateIds expectedExampleId = taskResult {
    let! (upsert: ViewEditConceptCommand) = SanitizeConceptRepository.getUpsert c.Db authorId kind updateIds // using |>!! is *extremely* inconsistent and unstable for some reason
    return!
        upsert
        |> commandTransformer
        |> SanitizeConceptRepository.Update c.Db authorId []
        |>%% Assert.equal expectedExampleId
}

let setCardIds (command: ViewEditConceptCommand) cardIds =
    { command with Ids = { command.Ids with CardIds = cardIds } }

[<Fact>]
let ``ConceptRepository.CreateCard on a basic facet collects 1 card/facet``(): Task<unit> = task {
    use c = new TestContainer()
    let userId = user_3
    let aTag = Guid.NewGuid().ToString() |> SanitizeTagRepository.sanitize
    let bTag = Guid.NewGuid().ToString() |> SanitizeTagRepository.sanitize
    
    let! _ = addBasicConcept c.Db userId [aTag; bTag] (concept_1, example_1, revision_1, [card_1])

    Assert.SingleI <| c.Db.Concept
    Assert.SingleI <| c.Db.Concept
    Assert.SingleI <| c.Db.Card
    let! cards = ConceptRepository.GetQuizBatch c.Db userId ""
    Assert.SingleI cards
    Assert.Equal(
        """<!DOCTYPE html>
    <head>
        <style>
            .cloze-brackets-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-filler-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-brackets-back {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: red;
            }
        </style>
        <style>
            .card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}

        </style>
    </head>
    <body>
        Front
        <script type="text/javascript" src="/js/iframeResizer.contentWindow.min.js"></script> 
    </body>
</html>""",
        cards
        |> Seq.head
        |> Result.getOk
        |> fun x -> x.Front
        , false, true
    )
    Assert.Equal(
        """<!DOCTYPE html>
    <head>
        <style>
            .cloze-brackets-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-filler-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-brackets-back {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: red;
            }
        </style>
        <style>
            .card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}

        </style>
    </head>
    <body>
        Front

<hr id=answer>

Back
        <script type="text/javascript" src="/js/iframeResizer.contentWindow.min.js"></script> 
    </body>
</html>""",
        cards
        |> Seq.head
        |> Result.getOk
        |> fun x -> x.Back
        , false, true
    )
    let! view = ConceptViewRepository.get c.Db concept_1
    Assert.Equal<FieldAndValue seq>(
        [{  Field = {
                Name = "Front"
                IsRightToLeft = false
                IsSticky = false }
            Value = "Front" }
         {  Field = {
                Name = "Back"
                IsRightToLeft = false
                IsSticky = false }
            Value = "Back"}],
        view.Value.FieldValues
            |> Seq.sortByDescending (fun x -> x.Field.Name)
    )
    Assert.Equal<string seq>(
        [aTag; bTag] |> List.sort,
        (ConceptRepository.GetCollectedPages c.Db userId 1 "")
            .GetAwaiter()
            .GetResult()
            .Results
            .Single()
            |> Result.getOk
            |> fun x -> x.Tags
            |> List.sort
    )}

[<Fact>]
let ``ExploreConceptRepository.revision works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = addBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1])
    let cardId = card_1
    let conceptId = concept_1
    let exampleId = example_1
    let oldRevisionId = revision_1
    let newRevisionId = revision_2
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeConceptRepository.getUpsert c.Db userId (VUpdate_ExampleId exampleId) ((conceptId, exampleId, newRevisionId, [card_1]) |> UpsertIds.fromTuple)
    let updated = {
        old with
            ViewEditConceptCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualExampleId = SanitizeConceptRepository.Update c.Db userId [] updated
    Assert.Equal(exampleId, actualExampleId)

    let! (example1: RevisionMeta)    = ExploreConceptRepository.get      c.Db userId conceptId |> TaskResult.map(fun x -> x.Default.Revision)
    let! (example2: RevisionMeta), _ = ExploreConceptRepository.revision c.Db userId newRevisionId
    Assert.Equal(example1.InC(), example2.InC())
    Assert.Equal(newValue                 , example2.StrippedFront)
    Assert.Equal(newValue + " " + newValue, example2.StrippedBack)
    let! (card3: RevisionMeta), _ = ExploreConceptRepository.revision c.Db userId oldRevisionId
    Assert.Equal("Front",      card3.StrippedFront)
    Assert.Equal("Front Back", card3.StrippedBack)

    // nonexistant id
    let nonexistant = Ulid.create
    
    let! (missingCard: Result<_, _>) = ExploreConceptRepository.revision c.Db userId nonexistant
    
    Assert.Equal(sprintf "Example Revision #%A not found" nonexistant, missingCard.error)

    // update on example that's in a nondefault deck with 0 editCardCommands doesn't change the deck
    let newDeckId = Ulid.create
    do! SanitizeDeckRepository.create c.Db userId (Guid.NewGuid().ToString()) newDeckId
    do! SanitizeDeckRepository.switch c.Db userId newDeckId cardId
    let! conceptCommand = SanitizeConceptRepository.getUpsert c.Db userId (VUpdate_ExampleId exampleId) ids_2
    
    do! SanitizeConceptRepository.Update c.Db userId [] conceptCommand

    let! (card: Card) = ConceptRepository.GetCollected c.Db userId conceptId |>%% Assert.Single
    Assert.equal newDeckId card.DeckId
    } |> TaskResult.getOk)

[<Fact>]
let ``ExploreConceptRepository.example works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = addBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1])
    let conceptId = concept_1
    let exampleId = example_1
    let oldRevisionId = revision_1
    let newRevisionId = revision_2
    let newValue = Guid.NewGuid().ToString()
    let! old = SanitizeConceptRepository.getUpsert c.Db userId (VUpdate_ExampleId exampleId) ((concept_1, example_1, newRevisionId, [card_1]) |> UpsertIds.fromTuple)
    let updated = {
        old with
            ViewEditConceptCommand.FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualExampleId = SanitizeConceptRepository.Update c.Db userId [] updated
    Assert.Equal(exampleId, actualExampleId)

    let! (example1: RevisionMeta)    = ExploreConceptRepository.get      c.Db userId conceptId |> TaskResult.map(fun x -> x.Default.Revision)
    let! (example2: RevisionMeta), _ = ExploreConceptRepository.example   c.Db userId exampleId
    Assert.Equal(example1.InC(), example2.InC())
    Assert.Equal(newValue                 , example2.StrippedFront)
    Assert.Equal(newValue + " " + newValue, example2.StrippedBack)
    Assert.Equal(exampleId, example2.ExampleId)
    Assert.Equal(newRevisionId, example2.Id)

    // nonexistant id
    let nonexistant = Ulid.create
    
    let! (missingCard: Result<_, _>) = ExploreConceptRepository.example c.Db userId nonexistant
    
    Assert.Equal(sprintf "Example #%A not found" nonexistant, missingCard.error)
    } |> TaskResult.getOk)

[<Fact>]
let ``ConceptViewRepository.revisionPair works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let otherUserId = user_2
    let! _ = addBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1])
    let! _ = addBasicConcept c.Db otherUserId [] (concept_2, example_2, revision_2, [card_2])
    
    let! a, (a_: bool), b, (b_:bool) = ConceptViewRepository.revisionPair c.Db revision_1 revision_2 userId
    
    Assert.Equal(a.InC(), b.InC())
    Assert.True(a_)
    Assert.False(b_)

    // missing revisionId
    let nonexistant = Ulid.create
    let! (x: Result<_, _>) = ConceptViewRepository.revisionPair c.Db revision_1 nonexistant userId
    
    Assert.equal (sprintf "Example revision #%A not found" nonexistant) x.error
    
    let! (x: Result<_, _>) = ConceptViewRepository.revisionPair c.Db nonexistant revision_1 userId
    
    Assert.equal (sprintf "Example revision #%A not found" nonexistant) x.error
    } |> TaskResult.getOk)

[<Fact>]
let ``ConceptViewRepository.revisionWithLatest works``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let! _ = addBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1])
    let conceptId = concept_1
    let exampleId = example_1
    let secondVersion = Guid.NewGuid().ToString()
    let updatedRevisionId = revision_2
    do! update c userId
            (VUpdate_ExampleId exampleId) (fun x -> { x with EditSummary = secondVersion; FieldValues = [].ToList() }) ((conceptId, exampleId, updatedRevisionId, [card_1]) |> UpsertIds.fromTuple) exampleId
    let oldRevisionId = revision_1
    do! c.Db.Revision.SingleAsync(fun x -> x.Id = updatedRevisionId)
        |> Task.map (fun x -> Assert.Equal(secondVersion, x.EditSummary))
    
    let! (a: RevisionView), (a_: bool), (b: RevisionView), (b_: bool), bId = ConceptViewRepository.revisionWithLatest c.Db oldRevisionId userId
    
    do! ConceptViewRepository.revision c.Db oldRevisionId
        |> TaskResult.map (fun expected -> Assert.Equal(expected.InC(), a.InC()))
    Assert.False a_
    Assert.True b_
    Assert.Empty b.FieldValues
    Assert.Equal(updatedRevisionId, bId)

    // works on a new example
    let newExampleId = example_2
    let exampleVersion = Guid.NewGuid().ToString()
    let revisionId = revision_3
    do! update c userId
            (VNewExample_SourceConceptId conceptId) (fun x -> { x with EditSummary = exampleVersion }) ((conceptId, newExampleId, revisionId, [card_1]) |> UpsertIds.fromTuple) newExampleId
    do! c.Db.Revision.SingleAsync(fun x -> x.Id = revisionId)
        |> Task.map (fun x -> Assert.Equal(exampleVersion, x.EditSummary))
    
    let! (a: RevisionView), (a_: bool), (b: RevisionView), (b_: bool), bId = ConceptViewRepository.revisionWithLatest c.Db revisionId userId
    
    do! ConceptViewRepository.revision c.Db revisionId
        |> TaskResult.map (fun expected -> Assert.Equal(expected.InC(), a.InC()))
    Assert.True a_
    Assert.False b_
    Assert.Empty b.FieldValues
    Assert.Equal(updatedRevisionId, bId)
    } |> TaskResult.getOk)

[<Fact>]
let ``Revision with "" as FieldValues is parsed to empty`` (): unit =
    let view =
        RevisionEntity(
            FieldValues = "",
            TemplateRevision = TemplateRevisionEntity(
                Fields = "FrontArial20False0FalseBackArial20False1False"
            ))
        |> RevisionView.load

    Assert.Empty view.FieldValues

[<Fact(Skip=PgSkip.reason)>]
let ``UpdateRepository.card edit/copy/example works``() : Task<unit> = task {
    let og_s = concept_1
    let copy_s = concept_2
    let copy2x_s = concept_3
    let copyOfExample_s = concept_ 4
    
    let og_b = example_1
    let copy_b = example_2
    let og_b_2 = example_3
    let copy2x_b = example_ 4
    let copyOfExample_b = example_ 5
    let exampleOfCopy_b = example_ 6

    let og_i = revision_1
    let ogEdit_i = revision_2
    let copy_i = revision_3
    let example_i = revision_ 4 // example of og_s and og_b_2
    let copy2x_i = revision_ 5
    let copyOfExample_i = revision_ 6
    let exampleOfCopy_i = revision_ 7

    use c = new TestContainer()
    let assertCount (cardsIdsAndCounts: _ list) (exampleIdsAndCounts: _ list) (revisionIdsAndCounts: _ list) = task {
        //"XXXXXX Concept Count".D()
        do! c.Db.Concept.CountAsync()
            |> Task.map(fun i -> Assert.Equal(cardsIdsAndCounts.Length, i))
        //"XXXXXX Example Count".D()
        do! c.Db.Example.CountAsync()
            |> Task.map(fun i -> Assert.Equal(exampleIdsAndCounts.Length, i))
        //"XXXXXX Example Revision Count".D()
        do! c.Db.Revision.CountAsync()
            |> Task.map(fun i -> Assert.Equal(revisionIdsAndCounts.Length, i))
        for id, count in cardsIdsAndCounts do
            //"XXXXXX".D(sprintf "Concept #%A should have count #%i" id count)
            do! c.Db.Concept.SingleAsync(fun x -> x.Id = id)
                |> Task.map (fun c -> Assert.Equal(count, c.Users))
        for id, count in exampleIdsAndCounts do
            //"XXXXXX".D(sprintf "Example #%A should have count #%i" id count)
            do! c.Db.Example.SingleAsync(fun x -> x.Id = id)
                |> Task.map (fun c -> Assert.Equal(count, c.Users))
        for id, count in revisionIdsAndCounts do
            //"XXXXXX".D(sprintf "Example revision #%A should have count #%i" id count)
            do! c.Db.Revision.SingleAsync(fun x -> x.Id = id)
                |> Task.map (fun c -> Assert.Equal(count, c.Users))}
    let! _ = addBasicConcept c.Db user_1 ["A"; "B"] (concept_1, example_1, revision_1, [card_1])
    do! assertCount
            [og_s, 1]
            [og_b, 1]
            [og_i, 1]

    // updated by user1
    let newValue = Guid.NewGuid().ToString()
    let! old = (og_s, og_b, ogEdit_i, [card_1]) |> UpsertIds.fromTuple |> SanitizeConceptRepository.getUpsert c.Db user_1 (VUpdate_ExampleId og_b)
    let updated = {
        old.Value with
            FieldValues =
                old.Value.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualExampleId = SanitizeConceptRepository.Update c.Db user_1 [] updated |> TaskResult.getOk
    Assert.Equal(og_b, actualExampleId)
    do! assertCount
            [og_s, 1]
            [og_b, 1]
            [og_i, 0; ogEdit_i, 1]
    
    let asserts userId conceptId exampleId revisionId newValue revisionCountForConcept revisionCount tags = task {
        let! revision = ConceptViewRepository.revision c.Db revisionId
        Assert.Equal<string seq>(
            [newValue; newValue],
            revision.Value.FieldValues.Select(fun x -> x.Value))
        Assert.Equal(
            revisionCountForConcept,
            c.Db.Revision.Count(fun x -> x.ConceptId = conceptId))
        let! concept = ExploreConceptRepository.get c.Db userId conceptId
        Assert.areEquivalent
            tags
            concept.Value.Tags
        Assert.areEquivalent
            [newValue; newValue]
            (revision.Value.FieldValues.Select(fun x -> x.Value))
        let createds = c.Db.Revision.Select(fun x -> x.Created) |> Seq.toList
        Assert.NotEqual(createds.[0], createds.[1])
        let! revisions = ConceptRepository.Revisions c.Db userId exampleId |> TaskResult.getOk
        Assert.Equal(revisionCount, revisions.SortedMeta.Count())
        let! revision = ConceptViewRepository.revision c.Db revisions.SortedMeta.[0].Id
        let revision, _, _, _ = revision |> Result.getOk |> fun x -> x.FrontBackFrontSynthBackSynth.[0]
        Assert.Contains(newValue, revision)
    }
    
    do! asserts user_1 og_s og_b ogEdit_i newValue 2 2
            [ { Name = "A"
                Count = 1
                IsCollected = true }
              { Name = "B"
                Count = 1
                IsCollected = true }]
    let! revisions = ConceptRepository.Revisions c.Db user_1 og_b  |> TaskResult.getOk
    let! revision = ConceptViewRepository.revision c.Db revisions.SortedMeta.[1].Id
    let original, _, _, _ = revision |> Result.getOk |> fun x -> x.FrontBackFrontSynthBackSynth.[0]
    Assert.Contains("Front", original)
    Assert.True(revisions.SortedMeta.Single(fun x -> x.IsLatest).Id > revisions.SortedMeta.Single(fun x -> not x.IsLatest).Id) // tests that Latest really came after NotLatest
            
    // copy by user2
    let newValue = Guid.NewGuid().ToString()
    let! old = (copy_s, copy_b, copy_i, []) |> UpsertIds.fromTuple |> SanitizeConceptRepository.getUpsert c.Db user_2 (VNewCopySource_RevisionId ogEdit_i)
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualExampleId = SanitizeConceptRepository.Update c.Db user_2 [] (setCardIds updated [card_2]) |> TaskResult.getOk
    Assert.Equal(copy_b, actualExampleId)
    do! assertCount
            [og_s, 1;              copy_s, 1]
            [og_b, 1;              copy_b, 1]
            [og_i, 0; ogEdit_i, 1; copy_i, 1]

    do! asserts user_2 copy_s copy_b copy_i newValue 1 1 []

    // missing copy
    let missingRevisionId = Ulid.create
    let missingCardId = Ulid.create
    
    let! old = SanitizeConceptRepository.getUpsert c.Db user_1 (VNewCopySource_RevisionId missingRevisionId) ids_1
    
    Assert.Equal(sprintf "Example Revision #%A not found." missingRevisionId, old.error)
    do! assertCount
            [og_s, 1;              copy_s, 1]
            [og_b, 1;              copy_b, 1]
            [og_i, 0; ogEdit_i, 1; copy_i, 1]

    // user2 examples og_s
    let newValue = Guid.NewGuid().ToString()
    let! old = ((og_s, og_b_2, example_i, []) |> UpsertIds.fromTuple) |> SanitizeConceptRepository.getUpsert c.Db user_2 (VNewExample_SourceConceptId og_s)
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualExampleId = SanitizeConceptRepository.Update c.Db user_2 [] (setCardIds updated [Ulid.create]) |> TaskResult.getOk
    Assert.Equal(og_b_2, actualExampleId)
    let! x, _ = ExploreConceptRepository.revision c.Db user_2 example_i |> TaskResult.getOk
    do! asserts user_2 x.ConceptId x.ExampleId x.Id newValue 3 1
            [ { Name = "A"
                Count = 1
                IsCollected = false }
              { Name = "B"
                Count = 1
                IsCollected = false }]
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;
             ogEdit_i, 1 ;
             example_i, 1 ]

    // user2 creates example on missing concept
    let missingConceptId = Ulid.create
    let! old = SanitizeConceptRepository.getUpsert c.Db user_2 (VNewExample_SourceConceptId missingConceptId) { ids_1 with ConceptId = missingConceptId }
    Assert.Equal(sprintf "Concept #%A not found." missingConceptId, old.error)
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;
             ogEdit_i, 1 ;
             example_i, 1 ]

    // user2 copies their copy
    let! x = ((copy2x_s, copy2x_b, copy2x_i, []) |> UpsertIds.fromTuple) |> SanitizeConceptRepository.getUpsert c.Db user_2 (VNewCopySource_RevisionId copy_i)
    let old = x.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualExampleId = SanitizeConceptRepository.Update c.Db user_2 [] (setCardIds updated [Ulid.create]) |> TaskResult.getOk
    Assert.Equal(copy2x_b, actualExampleId)
    let! x, _ = ExploreConceptRepository.revision c.Db user_2 copy2x_i |> TaskResult.getOk
    do! asserts user_2 x.ConceptId x.ExampleId x.Id newValue 1 1 []
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ;    copy2x_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;    copy2x_b, 1 ;
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;    copy2x_i, 1 ;
             ogEdit_i, 1 ;
             example_i, 1 ]
    
    // user2 copies their example
    let! old = ((copyOfExample_s, copyOfExample_b, copyOfExample_i, []) |> UpsertIds.fromTuple) |> SanitizeConceptRepository.getUpsert c.Db user_2 (VNewCopySource_RevisionId example_i)
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    let! actualExampleId = SanitizeConceptRepository.Update c.Db user_2 [] (setCardIds updated [Ulid.create]) |> TaskResult.getOk
    Assert.Equal(copyOfExample_b, actualExampleId)
    let! x, _ = ExploreConceptRepository.revision c.Db user_2 copyOfExample_i |> TaskResult.getOk
    do! asserts user_2 x.ConceptId x.ExampleId x.Id newValue 1 1 []
    do! assertCount
            [og_s,     2 ;    copy_s, 1 ;    copy2x_s, 1 ;    copyOfExample_s, 1 ]
            [og_b,     1 ;    copy_b, 1 ;    copy2x_b, 1 ;    copyOfExample_b, 1
             og_b_2,   1 ]
            [og_i,     0 ;    copy_i, 1 ;    copy2x_i, 1 ;    copyOfExample_i, 1
             ogEdit_i, 1 ;
             example_i, 1 ]

    // user2 creates a new example from their copy
    let! old = ((copy_s, exampleOfCopy_b, exampleOfCopy_i, []) |> UpsertIds.fromTuple) |> SanitizeConceptRepository.getUpsert c.Db user_2 (VNewExample_SourceConceptId copy_s)
    let old = old.Value
    let updated = {
        old with
            FieldValues =
                old.FieldValues.Select(fun x ->
                    { x with Value = newValue }
                ).ToList()
    }
    
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = user_2))
    let! actualExampleId = SanitizeConceptRepository.Update c.Db user_2 [] updated |> TaskResult.getOk
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = user_2))
    Assert.Equal(exampleOfCopy_b, actualExampleId)
    let! x, _ = ExploreConceptRepository.revision c.Db user_2 exampleOfCopy_i |> TaskResult.getOk
    do! asserts user_2 x.ConceptId x.ExampleId x.Id newValue 2 1 []
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfExample_s, 1 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfExample_b, 1
             og_b_2,   1 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfExample_i, 1 ;
             ogEdit_i, 1 ;    exampleOfCopy_i, 1
             example_i, 1 ]

    // adventures in collecting cards
    let adventurerId = user_3
    let! _ = ConceptRepository.CollectCard c.Db adventurerId og_i [ card_3 ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfExample_s, 1 ]
            [og_b,     2 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfExample_b, 1
             og_b_2,   1 ;    exampleOfCopy_b, 1 ]
            [og_i,     1 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfExample_i, 1 ;
             ogEdit_i, 1 ;    exampleOfCopy_i, 1
             example_i, 1 ]
    let! _ = ConceptRepository.CollectCard c.Db adventurerId ogEdit_i [ card_3 ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfExample_s, 1 ]
            [og_b,     2 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfExample_b, 1
             og_b_2,   1 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfExample_i, 1 ;
             ogEdit_i, 2 ;    exampleOfCopy_i, 1
             example_i, 1 ]
    let! _ = ConceptRepository.CollectCard c.Db adventurerId copy_i [ card_ 4 ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 1 ;    copyOfExample_s, 1 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 1 ;    copyOfExample_b, 1
             og_b_2,   1 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 1 ;    copyOfExample_i, 1 ;
             ogEdit_i, 2 ;    exampleOfCopy_i, 1
             example_i, 1 ]
    let! _ = ConceptRepository.CollectCard c.Db adventurerId copy2x_i [ Ulid.create ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfExample_s, 1 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfExample_b, 1
             og_b_2,   1 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfExample_i, 1 ;
             ogEdit_i, 2 ;    exampleOfCopy_i, 1
             example_i, 1 ]
    let! _ = ConceptRepository.CollectCard c.Db adventurerId copyOfExample_i [ Ulid.create ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfExample_s, 2 ]
            [og_b,     2 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfExample_b, 2
             og_b_2,   1 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfExample_i, 2 ;
             ogEdit_i, 2 ;    exampleOfCopy_i, 1
             example_i, 1 ]
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = adventurerId))
    let! _ = ConceptRepository.CollectCard c.Db adventurerId example_i [ card_3 ] |> TaskResult.getOk
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfExample_s, 2 ]
            [og_b,     1 ;    copy_b, 1         ; copy2x_b, 2 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 1         ; copy2x_i, 2 ;    copyOfExample_i, 2 ;
             ogEdit_i, 1 ;    exampleOfCopy_i, 1
             example_i, 2 ]
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = adventurerId))
    let! _ = ConceptRepository.CollectCard c.Db adventurerId exampleOfCopy_i [ card_ 4 ] |> TaskResult.getOk
    Assert.Equal(4, c.Db.Card.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfExample_s, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfExample_i, 2 ;
             ogEdit_i, 1 ;    exampleOfCopy_i, 2
             example_i, 2 ]
    // adventures in implicit uncollecting
    let adventurerId = user_1 // changing the adventurer!
    let! cc = c.Db.Card.SingleAsync(fun x -> x.RevisionId = ogEdit_i && x.UserId = adventurerId)
    do! ConceptRepository.uncollectConcept c.Db adventurerId cc.ConceptId |> TaskResult.getOk
    Assert.Equal(0, c.Db.Card.Count(fun x -> x.UserId = adventurerId))
    do! assertCount
            [og_s,     2 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfExample_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfExample_i, 2 ;
             ogEdit_i, 0 ;    exampleOfCopy_i, 2
             example_i, 2 ]
    let! _ = ConceptRepository.CollectCard c.Db adventurerId ogEdit_i [ card_ 5 ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfExample_s, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfExample_i, 2 ;
             ogEdit_i, 1 ;    exampleOfCopy_i, 2
             example_i, 2 ]
    let! _ = ConceptRepository.CollectCard c.Db adventurerId og_i [ card_ 5 ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfExample_s, 2 ]
            [og_b,     1 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 2 ]
            [og_i,     1 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfExample_i, 2 ;
             ogEdit_i, 0 ;    exampleOfCopy_i, 2
             example_i, 2 ]
    let! _ = ConceptRepository.CollectCard c.Db adventurerId example_i [ card_ 5 ] |> TaskResult.getOk
    do! assertCount
            [og_s,     3 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfExample_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfExample_b, 2
             og_b_2,   3 ;    exampleOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfExample_i, 2 ;
             ogEdit_i, 0 ;    exampleOfCopy_i, 2
             example_i, 3 ]
    // adventures in uncollecting and suspending
    let adventurerId = user_2 // changing the adventurer, again!
    let! cc = c.Db.Card.SingleAsync(fun x -> x.ConceptId = og_s && x.UserId = adventurerId)
    do! ConceptRepository.editState c.Db adventurerId cc.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_s,     2 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfExample_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfExample_i, 2 ;
             ogEdit_i, 0 ;    exampleOfCopy_i, 2
             example_i, 2 ]
    do! ConceptRepository.uncollectConcept c.Db adventurerId cc.ConceptId |> TaskResult.getOk
    do! assertCount
            [og_s,     2 ;    copy_s, 2         ; copy2x_s, 2 ;    copyOfExample_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 2 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfExample_i, 2 ;
             ogEdit_i, 0 ;    exampleOfCopy_i, 2
             example_i, 2 ]
    let! cc = c.Db.Card.SingleAsync(fun x -> x.ConceptId = copy_s && x.UserId = adventurerId)
    do! ConceptRepository.uncollectConcept c.Db adventurerId cc.ConceptId |> TaskResult.getOk
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 2 ;    copyOfExample_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 2 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 2 ;    copyOfExample_i, 2 ;
             ogEdit_i, 0 ;    exampleOfCopy_i, 1
             example_i, 2 ]
    let! cc = c.Db.Card.SingleAsync(fun x -> x.ConceptId = copy2x_s && x.UserId = adventurerId)
    do! ConceptRepository.editState c.Db adventurerId cc.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfExample_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfExample_i, 2 ;
             ogEdit_i, 0 ;    exampleOfCopy_i, 1
             example_i, 2 ]
    do! ConceptRepository.uncollectConcept c.Db adventurerId cc.ConceptId |> TaskResult.getOk
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 1 ;    copyOfExample_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 1 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 1 ;    copyOfExample_i, 2 ;
             ogEdit_i, 0 ;    exampleOfCopy_i, 1
             example_i, 2 ]
    let adventurerId = user_3 // and another change!
    let! cc = c.Db.Card.SingleAsync(fun x -> x.ConceptId = copy2x_s && x.UserId = adventurerId)
    do! ConceptRepository.editState c.Db adventurerId cc.Id CardState.Suspended |> Task.map (fun x -> x.Value)
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 0 ;    copyOfExample_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfExample_i, 2 ;
             ogEdit_i, 0 ;    exampleOfCopy_i, 1
             example_i, 2 ]
    do! ConceptRepository.uncollectConcept c.Db adventurerId cc.ConceptId |> TaskResult.getOk
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 0 ;    copyOfExample_s, 2 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfExample_b, 2
             og_b_2,   2 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfExample_i, 2 ;
             ogEdit_i, 0 ;    exampleOfCopy_i, 1
             example_i, 2 ]
    let! cc = c.Db.Card.SingleAsync(fun x -> x.ConceptId = copyOfExample_s && x.UserId = adventurerId)
    do! ConceptRepository.uncollectConcept c.Db adventurerId cc.ConceptId |> TaskResult.getOk
    do! assertCount
            [og_s,     2 ;    copy_s, 1         ; copy2x_s, 0 ;    copyOfExample_s, 1 ]
            [og_b,     0 ;    copy_b, 0         ; copy2x_b, 0 ;    copyOfExample_b, 1
             og_b_2,   2 ;    exampleOfCopy_b, 1 ]
            [og_i,     0 ;    copy_i, 0         ; copy2x_i, 0 ;    copyOfExample_i, 1 ;
             ogEdit_i, 0 ;    exampleOfCopy_i, 1
             example_i, 2 ]
    }

[<Fact>]
let ``ExploreConceptRepository.get works for all ExploreConceptCollectedStatus``() : Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = user_3
    let testGetCollected conceptId revisionId =
        ConceptRepository.GetCollected c.Db userId conceptId
        |> TaskResult.map (fun cc -> Assert.Equal(revisionId, cc.Single().RevisionMeta.Id))

    let! _ = addBasicConcept c.Db userId [] (concept_1, example_1, revision_1, [card_1])
    let og_s = concept_1
    let og_b = example_1
    let og_i = revision_1

    // tests ExactRevisionCollected
    do! ExploreConceptRepository.get c.Db userId og_s
        |> TaskResult.map (fun card -> Assert.Equal({ ConceptId = og_s; ExampleId = og_b; RevisionId = og_i; CardIds = [card_1] }, card.CollectedIds.Value))
    do! testGetCollected og_s og_i
    
    // update card
    let update_i = revision_2
    let! (command: ViewEditConceptCommand) = SanitizeConceptRepository.getUpsert c.Db user_2 (VUpdate_ExampleId og_b) ((og_s, og_b, update_i, []) |> UpsertIds.fromTuple)
    let! actualExampleId = SanitizeConceptRepository.Update c.Db userId [] (setCardIds command [card_1])
    Assert.Equal(og_b, actualExampleId)

    // tests ExactRevisionCollected
    do! ExploreConceptRepository.get c.Db userId og_s
        |> TaskResult.map (fun card -> Assert.Equal({ ConceptId = og_s; ExampleId = og_b; RevisionId = update_i; CardIds = [card_1] }, card.CollectedIds.Value))
    do! testGetCollected og_s update_i

    // collecting old revision doesn't change LatestId
    Assert.Equal(update_i, c.Db.Concept.Include(fun x -> x.DefaultExample).Single().DefaultExample.LatestId)
    let! _ = ConceptRepository.CollectCard c.Db userId og_i [ card_1 ]
    Assert.Equal(update_i, c.Db.Concept.Include(fun x -> x.DefaultExample).Single().DefaultExample.LatestId)

    // tests OtherRevisionCollected
    let! concept = ExploreConceptRepository.get c.Db userId og_s
    Assert.equal
        concept.CollectedIds.Value
        { ConceptId = og_s; ExampleId = og_b; RevisionId = og_i; CardIds = [card_1] }
    do! testGetCollected og_s og_i

    // create new example
    let example_i = revision_3
    let example_b = example_2
    let! (command: ViewEditConceptCommand) = SanitizeConceptRepository.getUpsert c.Db user_2 (VNewExample_SourceConceptId og_s) ((og_s, example_b, example_i, []) |> UpsertIds.fromTuple)
    let! actualExampleId = SanitizeConceptRepository.Update c.Db userId [] (setCardIds command [card_1])
    Assert.Equal(example_b, actualExampleId)
    
    // tests LatestExampleCollected
    let! concept = ExploreConceptRepository.get c.Db userId og_s
    Assert.equal
        concept.CollectedIds.Value
        { ConceptId = og_s; ExampleId = example_b; RevisionId = example_i; CardIds = [card_1] }
    do! testGetCollected og_s example_i

    // update example
    let updateExample_i = revision_ 4
    let! (command: ViewEditConceptCommand) = SanitizeConceptRepository.getUpsert c.Db user_2 (VUpdate_ExampleId example_b) ((og_s, example_2, updateExample_i, []) |> UpsertIds.fromTuple)
    let! actualExampleId = SanitizeConceptRepository.Update c.Db userId [] (setCardIds command [card_1])
    Assert.Equal(example_b, actualExampleId)

    // tests LatestExampleCollected
    let! concept = ExploreConceptRepository.get c.Db userId og_s
    Assert.equal
        concept.CollectedIds.Value
        { ConceptId = og_s; ExampleId = example_b; RevisionId = updateExample_i; CardIds = [card_1] }
    do! testGetCollected og_s updateExample_i

    // collecting old revision doesn't change LatestId
    Assert.Equal(update_i, c.Db.Concept.Include(fun x -> x.DefaultExample).Single(fun x -> x.Id = og_s).Examples.Single().LatestId)
    Assert.Equal(updateExample_i, c.Db.Concept.Include(fun x -> x.Examples).Single(fun x -> x.Id = og_s).Examples.Single(fun x -> x.Id = example_b).LatestId)
    let! _ = ConceptRepository.CollectCard c.Db userId example_i [ card_1 ]
    Assert.Equal(update_i, c.Db.Concept.Include(fun x -> x.DefaultExample).Single(fun x -> x.Id = og_s).Examples.Single().LatestId)
    Assert.Equal(updateExample_i, c.Db.Concept.Include(fun x -> x.Examples).Single(fun x -> x.Id = og_s).Examples.Single(fun x -> x.Id = example_b).LatestId)

    // tests OtherExampleCollected
    let! concept = ExploreConceptRepository.get c.Db userId og_s
    Assert.equal
        concept.CollectedIds.Value
        { ConceptId = og_s; ExampleId = example_b; RevisionId = example_i; CardIds = [card_1] }
    do! testGetCollected og_s example_i

    // try to create a new example again, but fail
    let! (command: ViewEditConceptCommand) = SanitizeConceptRepository.getUpsert c.Db user_2 (VNewExample_SourceConceptId og_s) ids_1
    let! (error: Result<_,_>) = SanitizeConceptRepository.Update c.Db userId [] command
    Assert.equal (sprintf "Concept #%A already has a Example named 'New Example'." og_s) error.error

    // create new example again
    let example_i2 = revision_ 5
    let example_b2 = example_3
    let! (command: ViewEditConceptCommand) = SanitizeConceptRepository.getUpsert c.Db user_2 (VNewExample_SourceConceptId og_s) ((og_s, example_b2, example_i2, []) |> UpsertIds.fromTuple)
    let command = { command with Title = Guid.NewGuid().ToString() }
    let! actualExampleId = SanitizeConceptRepository.Update c.Db userId [] (setCardIds command [card_1])
    Assert.Equal(example_b2, actualExampleId)

    // tests LatestExampleCollected
    let! concept = ExploreConceptRepository.get c.Db userId og_s
    Assert.equal
        concept.CollectedIds.Value
        { ConceptId = og_s; ExampleId = example_b2; RevisionId = example_i2; CardIds = [card_1] }
    do! testGetCollected og_s example_i2

    // tests LatestExampleCollected with og_s
    let! concept = ExploreConceptRepository.get c.Db userId og_s
    Assert.equal
        concept.CollectedIds.Value
        { ConceptId = og_s; ExampleId = example_b2; RevisionId = example_i2; CardIds = [card_1] }
    do! testGetCollected og_s example_i2

    // collecting old revision doesn't change LatestId; can also collect old example
    Assert.Equal(update_i, c.Db.Concept.Include(fun x -> x.DefaultExample).Single(fun x -> x.Id = og_s).Examples.Single().LatestId)
    Assert.Equal(updateExample_i, c.Db.Concept.Include(fun x -> x.Examples).Single(fun x -> x.Id = og_s).Examples.Single(fun x -> x.Id = example_b).LatestId)
    let! _ = ConceptRepository.CollectCard c.Db userId example_i [ card_1 ]
    Assert.Equal(update_i, c.Db.Concept.Include(fun x -> x.DefaultExample).Single(fun x -> x.Id = og_s).Examples.Single().LatestId)
    Assert.Equal(updateExample_i, c.Db.Concept.Include(fun x -> x.Examples).Single(fun x -> x.Id = og_s).Examples.Single(fun x -> x.Id = example_b).LatestId)

    // can't collect missing id
    let missingId = Ulid.create
    let! (error: Result<_,_>) = ConceptRepository.CollectCard c.Db userId missingId [ card_1 ]
    Assert.Equal(sprintf "Example Revision #%A not found" missingId, error.error)

    // tests NotCollected
    let otherUser = user_1
    let! concept = ExploreConceptRepository.get c.Db otherUser og_s
    Assert.Equal(None, concept.CollectedIds)
    } |> TaskResult.getOk)

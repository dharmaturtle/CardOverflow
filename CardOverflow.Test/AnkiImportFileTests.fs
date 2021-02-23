module AnkiImportFileTests

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
open AnkiImportTestData
open SimpleInjector
open SimpleInjector.Lifestyles
open CardOverflow.Sanitation
open System.Threading.Tasks
open FSharp.Control.Tasks
open System.Security.Cryptography
open System.Collections.Generic
open FsToolkit.ErrorHandling

[<Theory>]
[<ClassData(typeof<AllDefaultGromplatesAndImageAndMp3>)>]
let ``AnkiImporter.save saves three files`` ankiFileName ankiDb: Task<unit> = (taskResult {
    let userId = user_3
    use c = new TestContainer(false, ankiFileName)
    
    do!
        SanitizeAnki.ankiExportsDir +/ ankiFileName
        |> AnkiImporter.loadFiles (fun _ -> None)
        |> Task.FromResult
        |> TaskResult.bind(AnkiImporter.save c.Db ankiDb userId)

    Assert.Equal(3, c.Db.File_Leaf.Count())
    Assert.Equal(3, c.Db.File.Count())
    Assert.NotEmpty(c.Db.Card.Where(fun x -> x.Index = 1s))
    Assert.Equal(7, c.Db.Grompleaf.Count())
    Assert.Equal(5, c.Db.LatestGrompleaf.Count())
    } |> TaskResult.getOk)

[<Theory>]
[<ClassData(typeof<AllDefaultGromplatesAndImageAndMp3>)>]
let ``Running AnkiImporter.save 3x only imports 3 files`` ankiFileName ankiDb: Task<unit> = (taskResult {
    let userId = user_3
    use c = new TestContainer(false, ankiFileName)

    for _ in [1..3] do
        do!
            SanitizeAnki.ankiExportsDir +/ ankiFileName
            |> AnkiImporter.loadFiles (fun sha256 -> c.Db.File.FirstOrDefault(fun f -> f.Sha256 = sha256) |> Option.ofObj)
            |> Task.FromResult
            |> TaskResult.bind(AnkiImporter.save c.Db ankiDb userId)

    Assert.Equal(3, c.Db.File_Leaf.Count())
    Assert.Equal(3, c.Db.File.Count())
    Assert.NotEmpty(c.Db.Card.Where(fun x -> x.Index = 1s))
    } |> TaskResult.getOk)

[<Fact>]
let ``Anki.replaceAnkiFilenames transforms anki filenames into our filenames`` (): unit =
    let expected = [
        "Basic FrontBasic Back"
        "Basic (and reversed card) frontBasic (and reversed card) back"
        "Basic (optional reversed card) frontBasic (optional reversed card) backBasic (optional reversed card) reverse"
        "Basic (type in the answer) frontBasic (type in the answer) back"
        "Cloze text.&nbsp;Canberra was founded in {{c1::1913}}.Cloze extra"
        """Basic with image&nbsp;<img src="/image/AAEECRAZJDFAUWR5kKnE4QAhRGmQueQRQHGk2RBJhME">Basic back, no image"""
        """Basic front with mp3
<audio controls autoplay>
    <source src="AAIGDBQeKjhIWm6EnLbS8BAyVnykzvooWIq+9CxmouA=" type="audio/mpeg">
    Your browser does not support the audio element.
</audio>
Basic back, no mp3"""
        """<img src="/image/AAIEBggKDA4QEhQWGBocHiAiJCYoKiwuMDI0Njg6PD4"><img src="/image/AAIEBggKDA4QEhQWGBocHiAiJCYoKiwuMDI0Njg6PD4">"""]
    let fields = AnkiImportTestData.allDefaultGromplatesAndImageAndMp3_colpkg.Notes |> List.map(fun x -> x.Flds)
    let map =
        [ ("png1.png", FileEntity(
            Sha256 = Array.init 32 (fun index -> index + index |> byte)
          ))
          ("png2.png", FileEntity(
            Sha256 = Array.init 32 (fun index -> index + index |> byte)
          ))
          ("favicon.ico", FileEntity(
            Sha256 = Array.init 32 (fun index -> index * index |> byte)
          ))
          ("bloop.wav", FileEntity(
            Sha256 = Array.init 32 (fun index -> index * index + index |> byte)
          ))
        ] |> Map.ofList
    
    let actual = fields |> List.map(fun x -> Anki.replaceAnkiFilenames x map |> snd)

    Assert.Equal (expected.Length, actual.Length)
    Seq.zip expected actual
    |> Seq.iter Assert.Equal
    Assert.Equal<string list> (expected, actual)

[<Fact(Skip=PgSkip.reason)>]
let ``AnkiImporter import cards that have the same collectHash as distinct cards`` (): Task<unit> = (taskResult { // lowTODO, perhaps they should be the same card
    let userId = user_3
    use c = new TestContainer()
    
    do! AnkiImporter.save c.Db duplicatesFromLightyear userId Map.empty
    
    Assert.Equal<string seq>(
        ["Bab::Endocrinology::Thyroid::Thyroidcancer"; "Bab::Gastroenterology::Clinical::Livertumors"; "Differentcaserepeatedtag"; "Pathoma::Neoplasia::Tumor_Progression"; "Repeatedtag"],
        c.Db.Card.ToList().SelectMany(fun x -> x.Tags :> IEnumerable<_>).OrderBy(fun x -> x))
    Assert.SingleI(c.Db.Deck.Where(fun x -> x.Name = "duplicate cards"))
    Assert.Equal(3, c.Db.Stack.Count())
    Assert.Equal(3, c.Db.Leaf.Count())
    Assert.Equal(8, c.Db.Grompleaf.Count())
    Assert.Equal(6, c.Db.LatestGrompleaf.Count())
    } |> TaskResult.getOk)

let testCommields (c: TestContainer) userId stackId expected = task {
    let! collected = StackRepository.GetCollected c.Db userId stackId
    let collected = collected.Value.Single()
    Assert.Equal<string seq>(
        expected |> List.map MappingTools.stripHtmlTags |> List.sort,
        collected.LeafMeta.Commields.Select(fun x -> x.Value |> MappingTools.stripHtmlTags) |> Seq.sort)}

[<Fact>]
let ``Multiple cloze indexes works and missing image => <img src="missingImage.jpg">`` (): Task<unit> = task {
    let userId = user_3
    use c = new TestContainer()
    let testCommields = testCommields c userId
    let! x = AnkiImporter.save c.Db multipleClozeAndSingleClozeAndNoClozeWithMissingImage userId Map.empty
    Assert.Equal(7, c.Db.Card.Count())
    Assert.Equal(3, c.Db.Leaf.Count())
    Assert.Equal(3, c.Db.Branch.Count())
    Assert.Equal(3, c.Db.Stack.Count())
    Assert.Equal(4s, c.Db.Leaf.Single(fun x -> x.FieldValues.Contains "Drugs").MaxIndexInclusive)
    Assert.Null x.Value
    let allLeafViews =
        c.Db.Leaf
            .Include(fun x -> x.Grompleaf)
            .Include(fun x -> x.Commeaf_Leafs :> IEnumerable<_>)
                .ThenInclude(fun (x: Commeaf_LeafEntity) -> x.Commeaf)
            .ToList()
            .Select(LeafView.load)
    let assertCount expected (clozeText: string) =
        allLeafViews
            .Count(fun x -> x.FieldValues.Any(fun x -> x.Value.Contains clozeText))
            |> fun x -> Assert.Equal(expected, x)
    assertCount 1 "may be remembered with the mnemonic"
    let longThing = """Drugs that act on microtubules may be remembered with the mnemonic "Microtubules Get Constructed Very Poorly":M: {{c1::Mebendazole (antihelminthic)}}G: {{c2::Griseofulvin (antifungal)}} C: {{c3::Colchicine (antigout)}} V: {{c4::Vincristine/Vinblastine (anticancer)}}P: {{c5::Palcitaxel (anticancer)}}"""
    let longThingUs = longThing + " "
    Assert.Equal<string seq>(
        [   """↑ {{c1::Cl−}} concentration (> 60 mEq/L) in sweat is diagnostic for Cystic FibrosisImage here"""
            """↑↑ BUN/CR ratio indicates which type of acute renal failure?Prerenal azotemia"""
            longThingUs],
        c.Db.Leaf.ToList().Select(fun x -> x.FieldValues |> MappingTools.stripHtmlTags).OrderBy(fun x -> x))
    assertCount 1 "Fibrosis"
    Assert.areEquivalent
        [   "<b><br /></b>"
            "<br /><div><br /></div><div>Image here</div>" ]
        (allLeafViews.SelectMany(fun x -> x.FieldValues.Where(fun x -> x.Field.Name = "Extra").Select(fun x -> x.Value)))
    Assert.areEquivalent
        [   longThing
            "↑ {{c1::Cl−}} concentration (> 60 mEq/L) in sweat is diagnostic for Cystic Fibrosis" ]
        (allLeafViews.SelectMany(fun x -> x.FieldValues.Where(fun x -> x.Field.Name = "Text").Select(fun x -> MappingTools.stripHtmlTags x.Value)))
    Assert.SingleI
        <| c.Db.Leaf
            .Where(fun x -> x.FieldValues.Contains("acute"))
    Assert.True(c.Db.Leaf.Select(fun x -> x.FieldValues).Single(fun x -> x.Contains "Prerenal").Contains """<img src="/missingImage.jpg">""")
    let leaf = Assert.Single <| c.Db.Leaf.Where(fun x -> x.FieldValues.Contains("microtubules"))
    let! stack = ExploreStackRepository.get c.Db userId leaf.StackId
    let stack = stack.Value
    Assert.Equal(
        """Drugs that act on microtubules may be remembered with the mnemonic "Microtubules Get Constructed Very Poorly":M: [ ... ] G: Griseofulvin (antifungal) C: Colchicine (antigout) V: Vincristine/Vinblastine (anticancer)P: Palcitaxel (anticancer)""",
        stack.Default.Leaf.StrippedFront)
    let! stack = ExploreStackRepository.get c.Db userId leaf.StackId
    Assert.Empty stack.Value.Relationships
    Assert.Empty c.Db.Relationship

    let! clozes = c.Db.Leaf.Where(fun x -> x.Commeaf_Leafs.Any(fun x -> x.Commeaf.Value.Contains "mnemonic")).ToListAsync()
    for leaf in clozes do
        do! testCommields leaf.StackId [longThing; ""]
    }

[<Fact>]
let ``LeafView.load works on cloze`` (): Task<unit> = task {
    let userId = user_3
    use c = new TestContainer()
    let clozeText = "{{c1::Portland::city}} was founded in {{c2::1845}}."
    let! _ = FacetRepositoryTests.addCloze clozeText c.Db userId [] (stack_1, branch_1, leaf_1, [card_1; card_2])

    Assert.Equal(2, c.Db.Card.Count(fun x -> x.UserId = userId))
    let! view = StackViewRepository.leaf c.Db leaf_1
    Assert.Equal(2, view.Value.FrontBackFrontSynthBackSynth.Count)
    Assert.Equal(1s, view.Value.MaxIndexInclusive)
    Assert.Equal<string seq>(
        [clozeText; "extra"],
        view.Value.FieldValues.Select(fun x -> x.Value))
    }

[<Fact>]
let ``Create card works with EditCardCommand`` (): Task<unit> = (taskResult {
    let userId = user_3
    use c = new TestContainer()
    let getCard branchId =
        c.Db.Branch.SingleAsync(fun x -> x.Id = branchId)
        |> Task.map (fun x -> x.StackId)
        |> Task.bind (fun stackId -> StackRepository.GetCollected c.Db userId stackId |> TaskResult.map Seq.exactlyOne)
    let branchId = branch_1
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    Assert.equal branchId actualBranchId
    let ccId = card_1
    
    // insert new stack with invalid settingsId
    let invalidCardId = Ulid.create
    let! (error: Result<_,_>) =
        {   EditCardCommand.init with
                CardSettingId = invalidCardId }
        |>  SanitizeCardRepository.update c.Db userId ccId
    Assert.equal "You provided an invalid or unauthorized card setting id." error.error
    
    // insert new stack with someone else's settingId
    let someoneElse'sSettingId = setting_2
    let! (error: Result<_,_>) =
        {   EditCardCommand.init with
                CardSettingId = someoneElse'sSettingId }
        |>  SanitizeCardRepository.update c.Db userId ccId
    Assert.equal "You provided an invalid or unauthorized card setting id." error.error
    
    // insert new stack with invalid deckId
    let invalidDeckId = Ulid.create
    let! (error: Result<_,_>) =
        {   EditCardCommand.init with
                DeckId = invalidDeckId }
        |>  SanitizeCardRepository.update c.Db userId ccId
    Assert.equal "You provided an invalid or unauthorized deck id." error.error
    
    // insert new stack with someone else's deckId
    let someoneElse'sDeckId = deck_2
    let! (error: Result<_,_>) =
        {   EditCardCommand.init with
                DeckId = someoneElse'sDeckId }
        |>  SanitizeCardRepository.update c.Db userId ccId
    Assert.equal "You provided an invalid or unauthorized deck id." error.error

    // insert new setting
    let! (options: ViewCardSetting ResizeArray) = SanitizeCardSettingRepository.getAll c.Db userId
    let options =
        options.Append
            { ViewCardSetting.load CardSettingsRepository.defaultCardSettings with
                IsDefault = false }
        |> toResizeArray
    let! (ids: _ list) = SanitizeCardSettingRepository.upsertMany c.Db userId options
    let defaultSettingId = ids.First()
    let latestSettingId = ids.Last()
    
    // insert new stack with latest settingsId
    do! {   EditCardCommand.init with
                CardSettingId = latestSettingId }
        |> SanitizeCardRepository.update c.Db userId ccId
    let! (card: Card) = getCard branchId
    Assert.Equal(latestSettingId, card.CardSettingId)

    // insert new stack with default settingsId
    do! EditCardCommand.init
        |> SanitizeCardRepository.update c.Db userId ccId
    let! (card: Card) = getCard branchId
    Assert.Equal(defaultSettingId, card.CardSettingId)

    let latestDeckId = Ulid.create
    do! SanitizeDeckRepository.create c.Db userId (Guid.NewGuid().ToString()) latestDeckId
    // insert new stack with latest deckId
    do! {   EditCardCommand.init with
                DeckId = latestDeckId }
        |> SanitizeCardRepository.update c.Db userId ccId
    let! (card: Card) = getCard branchId
    Assert.Equal(latestDeckId, card.DeckId)

    // insert new stack with default deckId
    do! EditCardCommand.init
        |> SanitizeCardRepository.update c.Db userId ccId
    let! (card: Card) = getCard branchId
    Assert.Equal(deck_3, card.DeckId)
    } |> TaskResult.getOk)

[<Fact>]
let ``Create cloze card works`` (): Task<unit> = (taskResult {
    let userId = user_3
    use c = new TestContainer()
    let testCommields = testCommields c userId

    let getLeafs clozeText = c.Db.Leaf.Where(fun x -> x.Commeaf_Leafs.Any(fun x -> x.Commeaf.Value = clozeText))
    let test clozeMaxIndex clozeText otherTest = task {
        let! r = FacetRepositoryTests.addCloze clozeText c.Db userId [] (Ulid.create, Ulid.create, Ulid.create, [1 .. clozeMaxIndex] |> List.map (fun _ -> Ulid.create))
        Assert.NotNull r.Value
        for i in [1 .. clozeMaxIndex] |> List.map int16 do
            Assert.SingleI <| c.Db.LatestLeaf.Where(fun x -> x.FieldValues.Contains clozeText)
            Assert.Equal(0, c.Db.LatestLeaf.Count(fun x -> x.Commeaf_Leafs.Any(fun x -> x.Commeaf.Value = clozeText)))
            let! commeafIds =
                (getLeafs clozeText)
                    .Select(fun x -> x.Commeaf_Leafs.Single().Commeaf.Id)
                    .ToListAsync()
            for id in commeafIds do
                Assert.True(c.Db.LatestCommeaf.Any(fun x -> x.Id = id))
            let! stackIds = (getLeafs clozeText).Select(fun x -> x.StackId).ToListAsync()
            for id in stackIds do
                do! testCommields id [clozeText]
        otherTest clozeText }
    let assertUserHasNormalCardCount expected =
        c.Db.Card.CountAsync(fun x -> x.UserId = userId && x.CardState = CardState.toDb Normal)
        |> Task.map (fun actual -> Assert.Equal(expected, actual))
    do! assertUserHasNormalCardCount 0
    let assertCount expected (clozeText: string) =
        c.Db.Leaf
            .Include(fun x -> x.Grompleaf)
            .Include(fun x -> x.Commeaf_Leafs :> IEnumerable<_>)
                .ThenInclude(fun (x: Commeaf_LeafEntity) -> x.Commeaf)
            .ToList()
            .Select(LeafView.load)
            .Count(fun x -> x.FieldValues.Any(fun x -> x.Value.Contains clozeText))
            |> fun x -> Assert.Equal(expected, x)
    do! test 1 "Canberra was founded in {{c1::1913}}."
        <| assertCount 1
    do! assertUserHasNormalCardCount 1
    do! test 1 "{{c1::Canberra::city}} was founded in {{c1::1913}}."
        <| assertCount 1
    do! assertUserHasNormalCardCount 2
    do! test 2 "{{c1::Portland::city}} was founded in {{c2::1845}}."
        <| fun clozeText ->
            assertCount 1 clozeText
            assertCount 0 "Portland was founded in {{c2::1845}}."
            assertCount 0 "{{c1::Portland::city}} was founded in 1845."
    do! assertUserHasNormalCardCount 4

    let! (e: PagedList<Result<Card, string>>) = StackRepository.GetCollectedPages c.Db userId 1 ""
    let expected =
        [   "Canberra was founded in [ ... ] .", "Canberra was founded in [ 1913 ] . extra"
            "[ city ] was founded in [ ... ] .", "[ Canberra ] was founded in [ 1913 ] . extra"
            "[ city ] was founded in 1845.", "[ Portland ] was founded in 1845. extra"
            "Portland was founded in [ ... ] .", "Portland was founded in [ 1845 ] . extra" ]
    Assert.areEquivalent expected <| e.Results.Select(fun x -> x.Value.LeafMeta.StrippedFront, x.Value.LeafMeta.StrippedBack)
    do! assertUserHasNormalCardCount 4
    } |> TaskResult.getOk)

[<Fact>]
let ``Create cloze card works with changing card number`` (): Task<unit> = (taskResult {
    let userId = user_3
    use c = new TestContainer()
    let assertUserHasNormalCardCount expected =
        c.Db.Card.CountAsync(fun x -> x.UserId = userId && x.CardState = CardState.toDb Normal)
        |> Task.map (fun actual -> Assert.Equal(expected, actual))
    let! _ = FacetRepositoryTests.addCloze "Canberra was founded in {{c1::1913}}." c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    do! assertUserHasNormalCardCount 1

    // updating card fails for mismatching cardIds
    let mismatching = Ulid.create
    let! command = SanitizeStackRepository.getUpsert c.Db userId (VUpdate_BranchId branch_1) ((stack_1, branch_1, leaf_2, []) |> UpsertIds.fromTuple)
    let! (r: Result<_, _>) = SanitizeStackRepository.Update c.Db userId [] (FacetRepositoryTests.setCardIds command [mismatching])
    Assert.equal (sprintf "Card ids don't match. Was given %A and expected %A" mismatching card_1) r.error
    do! assertUserHasNormalCardCount 1

    // go from 1 cloze to 2 clozes
    let! command = SanitizeStackRepository.getUpsert c.Db userId (VUpdate_BranchId branch_1) ((stack_1, branch_1, leaf_2, []) |> UpsertIds.fromTuple)
    let command =
        { command with
            ViewEditStackCommand.FieldValues =
                [   {   command.FieldValues.[0] with
                            Value = "{{c2::Canberra}} was founded in {{c1::1913}}."
                    }
                    command.FieldValues.[1]
                ].ToList()
        }
    let! actualBranchId = SanitizeStackRepository.Update c.Db userId [] (FacetRepositoryTests.setCardIds command [card_1; card_2])
    Assert.Equal(branch_1, actualBranchId)
    do! assertUserHasNormalCardCount 2
    
    // go from 2 clozes to 1 cloze
    let! command = SanitizeStackRepository.getUpsert c.Db userId (VUpdate_BranchId branch_1) ((stack_1, branch_1, leaf_3, []) |> UpsertIds.fromTuple)
    let command =
        { command with
            ViewEditStackCommand.FieldValues =
                [   {   command.FieldValues.[0] with
                            Value = "Canberra was founded in {{c1::1913}}."
                    }
                    command.FieldValues.[1]
                ].ToList()
        }
    let! actualBranchId = SanitizeStackRepository.Update c.Db userId [] (FacetRepositoryTests.setCardIds command [card_1])
    Assert.Equal(branch_1, actualBranchId)
    do! assertUserHasNormalCardCount 1
    
    // multiple c1's works
    let! command = SanitizeStackRepository.getUpsert c.Db userId (VUpdate_BranchId branch_1) ((stack_1, branch_1, leaf_ 4, [card_1]) |> UpsertIds.fromTuple)
    let command =
        { command with
            ViewEditStackCommand.FieldValues =
                [   {   command.FieldValues.[0] with
                            Value = "{{c1::Canberra}} was founded in {{c1::1913}}."
                    }
                    command.FieldValues.[1]
                ].ToList()
        }
    let! actualBranchId = SanitizeStackRepository.Update c.Db userId [] command
    Assert.Equal(branch_1, actualBranchId)
    do! assertUserHasNormalCardCount 1
    } |> TaskResult.getOk)

[<Fact>]
let ``SanitizeStackRepository.Update checks ids`` (): Task<unit> = (taskResult { // lowTODO add more tests for other ids
    let userId = user_3
    use c = new TestContainer()

    let! (x: Result<_, _>) = FacetRepositoryTests.update c userId (VNewBranch_SourceStackId stack_1) id ids_1 branch_1

    Assert.equal "Stack #00000000-0000-0000-0000-57ac00000001 not found." x.error

    let ex =
        Assert.Throws<Microsoft.EntityFrameworkCore.DbUpdateException>(fun () -> 
            (FacetRepositoryTests.addBasicStack c.Db userId [] (Guid.Empty, branch_1, leaf_1, [card_1]))
                .GetAwaiter().GetResult() |> ignore
        )
    Assert.equal
        "23514: new row for relation \"stack\" violates check constraint \"stack. id. is valid\""
        ex.InnerException.Message

    // https://codereview.stackexchange.com/questions/188948/correct-using-of-try-catch-clause-on-database-execution
    // https://github.com/StackExchange/Dapper/issues/710
    // this code is very temp
    //let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    //let! (x: Result<_, _>) = FacetRepositoryTests.update c userId (VNewBranch_SourceStackId stack_1) id ids_1 branch_1
    
    //Assert.equal "Stack #00000000-0000-0000-0000-57ac00000001 not found." x.error
    } |> TaskResult.getOk)

[<Fact>]
let ``UpdateRepository.stack on addReversedBasicStack works`` (): Task<unit> = (taskResult {
    let userId = user_3
    use c = new TestContainer()

    // inserting with just 1 cardId fails
    let! (x: Result<_, _>) = FacetRepositoryTests.addReversedBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1])
    Assert.equal "Leaf#00000000-0000-0000-0000-1eaf00000001 requires 2 card id(s). You provided 1." x.error

    // setup
    let! _ = FacetRepositoryTests.addReversedBasicStack c.Db userId [] (stack_1, branch_1, leaf_1, [card_1; card_2])
    let stackId = stack_1
    let branchId_og = branch_1
    Assert.equal 2 <| c.Db.Card.Count(fun x -> x.UserId = userId && x.BranchId = branchId_og)
    let! revisions = StackRepository.Revisions c.Db userId branchId_og
    Assert.equal 1 revisions.SortedMeta.Length

    // branching a stack collects it
    let branchId_alt = branch_2
    do! FacetRepositoryTests.update c userId
            (VNewBranch_SourceStackId stackId) id { ids_2 with StackId = stack_1; CardIds = [card_1; card_2] } branchId_alt

    Assert.equal 0 <| c.Db.Card.Count(fun x -> x.UserId = userId && x.BranchId = branchId_og)
    Assert.equal 2 <| c.Db.Card.Count(fun x -> x.UserId = userId && x.BranchId = branchId_alt)

    // updating an uncollected branch doesn't change the Cards
    do! FacetRepositoryTests.update c userId
            (VUpdate_BranchId branchId_og) (fun command -> (FacetRepositoryTests.setCardIds command [card_1; card_2])) { StackId = stack_1; BranchId = branchId_og; LeafId = leaf_3; CardIds = [] } branchId_og

    Assert.equal 0 <| c.Db.Card.Count(fun x -> x.UserId = userId && x.BranchId = branchId_og)
    Assert.equal 2 <| c.Db.Card.Count(fun x -> x.UserId = userId && x.BranchId = branchId_alt)

    // switching to testing StackRepository.Revisions
    let! revisions = StackRepository.Revisions c.Db userId branchId_og

    Assert.equal 2 revisions.SortedMeta.Length

    // invalid branchId
    let invalidBranchId = Ulid.create
    let! (revisions: Result<_, _>) = StackRepository.Revisions c.Db userId invalidBranchId

    Assert.equal (sprintf "BranchId #%A not found" invalidBranchId) revisions.error
    } |> TaskResult.getOk)

//[<Fact>] // medTODO uncomment when you bring back communals
let ``Creating card with shared "Back" field works twice`` (): Task<unit> = task {
    let userId = user_3
    use c = new TestContainer()
    let! gromplate =
        TestGromplateRepo.Search c.Db "Basic"
        |> Task.map (fun x -> x.Single(fun x -> x.Name = "Basic"))
    let editSummary = Guid.NewGuid().ToString()
    let communalValue = Guid.NewGuid().ToString()
    let stackId = stack_1
    let leafId = leaf_1

    let test customTest = task {
        let! _ =
            SanitizeStackRepository.Update
                c.Db
                userId
                []
                {   EditSummary = editSummary
                    FieldValues =
                        gromplate
                            .Fields
                            .Select(fun f ->
                                let value =
                                    if f.Name = "Front" then
                                        "Front"
                                    else
                                        communalValue
                                {   EditField = ViewField.copyTo f
                                    Value = value
                                })
                            .ToList()
                    Grompleaf = gromplate
                    Kind = NewOriginal_TagIds Set.empty
                    Title = null
                    Ids = ids_1
                }
            |> Task.map Result.getOk
        let! field = c.Db.Commield.SingleAsync()
        Assert.Equal(stackId, field.Id)
        Assert.Equal(user_3, field.AuthorId)
        let! leaf = c.Db.Commeaf.Include(fun x -> x.Commeaf_Leafs).SingleAsync(fun x -> x.Value = communalValue)
        Assert.Equal(leafId, leaf.Id)
        Assert.Equal(commield_1, leaf.CommieldId)
        Assert.Equal("Back", leaf.FieldName)
        Assert.Equal(communalValue, leaf.Value)
        Assert.Null leaf.Modified
        Assert.Equal(editSummary, leaf.EditSummary)
        customTest leaf }
    do! test <| fun i ->
            Assert.Equal(leafId, i.Commeaf_Leafs.Single().LeafId)
            Assert.Equal(commeaf_1, i.Commeaf_Leafs.Single().CommeafId)
            Assert.True(c.Db.LatestCommeaf.Any(fun x -> x.Id = i.Id))
    do! test <| fun i ->
            Assert.Equal([leaf_1   ; leaf_2   ], i.Commeaf_Leafs.Select(fun x -> x.LeafId))
            Assert.Equal([commeaf_1; commeaf_1], i.Commeaf_Leafs.Select(fun x -> x.CommeafId))
            Assert.True(c.Db.LatestCommeaf.Any(fun x -> x.Id = i.Id))
    Assert.SingleI c.Db.Commield
    Assert.SingleI c.Db.Commeaf }

[<Fact>]
let ``AnkiDefaults.gromplateIdByHash is same as initial database`` (): unit =
    let c = new TestContainer()
    use hasher = SHA512.Create()
    let dbGromplates =
        c.Db.Grompleaf
            .OrderBy(fun x -> x.Id)
            .ToList()
    
    // test that the calculated hash is the same as the one stored in the db
    for gromplate in dbGromplates do
        let calculated = GrompleafEntity.hashBase64 hasher gromplate
        let dbValue = LeafEntity.bitArrayToByteArray gromplate.Hash |> Convert.ToBase64String
        //for x in GrompleafEntity.hash hasher gromplate do
        //    Console.Write(if x then "1" else "0")
        //Console.WriteLine()
        Assert.Equal(calculated, dbValue)

    // test that AnkiDefaults.gromplateIdByHash is up to date
    for dbGromplate in dbGromplates do
        let calculated = GrompleafEntity.hashBase64 hasher dbGromplate
        //calculated.D(string dbGromplate.Id)
        Assert.Equal(AnkiDefaults.grompleafIdByHash.[calculated], dbGromplate.Id)

//[<Fact>]
let ``Manual Anki import`` (): Task<unit> = (taskResult {
    let userId = user_3
    let pathToCollection = @""
    
    use c = new TestContainer()
    let db = c.Db
    
    //use c = new Container()
    //c.RegisterStuffTestOnly
    //c.RegisterStandardConnectionString
    //use __ = AsyncScopedLifestyle.BeginScope c
    //let db = c.GetLeaf<CardOverflowDb>()

    let ankiDb =
        AnkiImporter.getSimpleAnkiDb
        |> using(SanitizeAnki.ankiDb pathToCollection)
    do! pathToCollection
        |> AnkiImporter.loadFiles (fun sha256 -> db.File |> Seq.tryFind(fun f -> f.Sha256 = sha256))
        |> Task.FromResult
        |> TaskResult.bind(AnkiImporter.save db ankiDb userId)
    } |> TaskResult.getOk)

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
[<ClassData(typeof<AllDefaultCollatesAndImageAndMp3>)>]
let ``AnkiImporter.save saves three files`` ankiFileName ankiDb: Task<unit> = (taskResult {
    let userId = 3
    use c = new TestContainer(false, ankiFileName)
    
    do!
        SanitizeAnki.ankiExportsDir +/ ankiFileName
        |> AnkiImporter.loadFiles (fun _ -> None)
        |> Task.FromResult
        |> TaskResult.bind(AnkiImporter.save c.Db ankiDb userId)

    Assert.Equal(3, c.Db.File_BranchInstance.Count())
    Assert.Equal(3, c.Db.File.Count())
    Assert.NotEmpty(c.Db.CollectedCard.Where(fun x -> x.Index = 1s))
    Assert.Equal(7, c.Db.CollateInstance.Count())
    Assert.Equal(5, c.Db.LatestCollateInstance.Count())
    } |> TaskResult.getOk)

[<Theory>]
[<ClassData(typeof<AllDefaultCollatesAndImageAndMp3>)>]
let ``Running AnkiImporter.save 3x only imports 3 files`` ankiFileName ankiDb: Task<unit> = (taskResult {
    let userId = 3
    use c = new TestContainer(false, ankiFileName)

    for _ in [1..3] do
        do!
            SanitizeAnki.ankiExportsDir +/ ankiFileName
            |> AnkiImporter.loadFiles (fun sha256 -> c.Db.File.FirstOrDefault(fun f -> f.Sha256 = sha256) |> Option.ofObj)
            |> Task.FromResult
            |> TaskResult.bind(AnkiImporter.save c.Db ankiDb userId)

    Assert.Equal(3, c.Db.File_BranchInstance.Count())
    Assert.Equal(3, c.Db.File.Count())
    Assert.NotEmpty(c.Db.CollectedCard.Where(fun x -> x.Index = 1s))
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
    let fields = AnkiImportTestData.allDefaultCollatesAndImageAndMp3_colpkg.Notes |> List.map(fun x -> x.Flds)
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

[<Fact>]
let ``AnkiImporter import cards that have the same acquireHash as distinct cards`` (): Task<unit> = (taskResult { // lowTODO, perhaps they should be the same card
    let userId = 3
    use c = new TestContainer()
    
    do! AnkiImporter.save c.Db duplicatesFromLightyear userId Map.empty
    
    Assert.Equal<string seq>(
        ["Bab::Endocrinology::Thyroid::Thyroidcancer"; "Bab::Gastroenterology::Clinical::Livertumors"; "Differentcaserepeatedtag"; "Pathoma::Neoplasia::Tumor_Progression"; "Repeatedtag"],
        c.Db.Tag.Select(fun x -> x.Name).OrderBy(fun x -> x))
    Assert.SingleI(c.Db.Deck.Where(fun x -> x.Name = "duplicate cards"))
    Assert.Equal(3, c.Db.Stack.Count())
    Assert.Equal(3, c.Db.BranchInstance.Count())
    Assert.Equal(8, c.Db.CollateInstance.Count())
    Assert.Equal(6, c.Db.LatestCollateInstance.Count())
    } |> TaskResult.getOk)

let testCommunalFields (c: TestContainer) userId stackId expected = task {
    let! acquired = StackRepository.GetAcquired c.Db userId stackId
    let acquired = acquired.Value.Single()
    Assert.Equal<string seq>(
        expected |> List.map MappingTools.stripHtmlTags |> List.sort,
        acquired.BranchInstanceMeta.CommunalFields.Select(fun x -> x.Value |> MappingTools.stripHtmlTags) |> Seq.sort)}

[<Fact>]
let ``Multiple cloze indexes works and missing image => <img src="missingImage.jpg">`` (): Task<unit> = task {
    let userId = 3
    use c = new TestContainer()
    let testCommunalFields = testCommunalFields c userId
    let! x = AnkiImporter.save c.Db multipleClozeAndSingleClozeAndNoClozeWithMissingImage userId Map.empty
    Assert.Equal(7, c.Db.CollectedCard.Count())
    Assert.Equal(3, c.Db.BranchInstance.Count())
    Assert.Equal(3, c.Db.Branch.Count())
    Assert.Equal(3, c.Db.Stack.Count())
    Assert.Equal(4s, c.Db.BranchInstance.Single(fun x -> x.FieldValues.Contains "Drugs").MaxIndexInclusive)
    Assert.Null x.Value
    let allBranchInstanceViews =
        c.Db.BranchInstance
            .Include(fun x -> x.CollateInstance)
            .Include(fun x -> x.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance)
            .ToList()
            .Select(BranchInstanceView.load)
    let assertCount expected (clozeText: string) =
        allBranchInstanceViews
            .Count(fun x -> x.FieldValues.Any(fun x -> x.Value.Contains clozeText))
            |> fun x -> Assert.Equal(expected, x)
    assertCount 1 "may be remembered with the mnemonic"
    let longThing = """Drugs that act on microtubules may be remembered with the mnemonic "Microtubules Get Constructed Very Poorly":M: {{c1::Mebendazole (antihelminthic)}}G: {{c2::Griseofulvin (antifungal)}} C: {{c3::Colchicine (antigout)}} V: {{c4::Vincristine/Vinblastine (anticancer)}}P: {{c5::Palcitaxel (anticancer)}}"""
    let longThingUs = longThing + " "
    Assert.Equal<string seq>(
        [   """↑ {{c1::Cl−}} concentration (> 60 mEq/L) in sweat is diagnostic for Cystic FibrosisImage here"""
            """↑↑ BUN/CR ratio indicates which type of acute renal failure?Prerenal azotemia"""
            longThingUs],
        c.Db.BranchInstance.ToList().Select(fun x -> x.FieldValues |> MappingTools.stripHtmlTags).OrderBy(fun x -> x))
    assertCount 1 "Fibrosis"
    Assert.Equal<string seq>(
        [   "<b><br /></b>"
            "<br /><div><br /></div><div>Image here</div>" ],
        allBranchInstanceViews
            .SelectMany(fun x -> x.FieldValues.Where(fun x -> x.Field.Name = "Extra").Select(fun x -> x.Value))
    )
    Assert.Equal<string seq>(
        [   longThing
            "↑ {{c1::Cl−}} concentration (> 60 mEq/L) in sweat is diagnostic for Cystic Fibrosis" ],
        allBranchInstanceViews
            .SelectMany(fun x -> x.FieldValues.Where(fun x -> x.Field.Name = "Text").Select(fun x -> MappingTools.stripHtmlTags x.Value))
    )
    Assert.SingleI
        <| c.Db.BranchInstance
            .Where(fun x -> x.FieldValues.Contains("acute"))
    Assert.True(c.Db.BranchInstance.Select(fun x -> x.FieldValues).Single(fun x -> x.Contains "Prerenal").Contains """<img src="/missingImage.jpg">""")
    let! stack = ExploreStackRepository.get c.Db userId 1
    let stack = stack.Value
    Assert.Equal(
        """Drugs that act on microtubules may be remembered with the mnemonic "Microtubules Get Constructed Very Poorly":M: [ ... ] G: Griseofulvin (antifungal) C: Colchicine (antigout) V: Vincristine/Vinblastine (anticancer)P: Palcitaxel (anticancer)""",
        stack.Default.Instance.StrippedFront)
    let! stack = ExploreStackRepository.get c.Db userId 1
    Assert.Empty stack.Value.Relationships
    Assert.Empty c.Db.Relationship

    let! clozes = c.Db.BranchInstance.Where(fun x -> x.CommunalFieldInstance_BranchInstances.Any(fun x -> x.CommunalFieldInstance.Value.Contains "mnemonic")).ToListAsync()
    for instance in clozes do
        do! testCommunalFields instance.StackId [longThing; ""]
    }

[<Fact>]
let ``BranchInstanceView.load works on cloze`` (): Task<unit> = task {
    let userId = 3
    use c = new TestContainer()
    let clozeText = "{{c1::Portland::city}} was founded in {{c2::1845}}."
    let! _ = FacetRepositoryTests.addCloze clozeText c.Db userId []

    Assert.Equal(2, c.Db.CollectedCard.Count(fun x -> x.UserId = userId))
    let! view = StackViewRepository.instance c.Db 1001
    Assert.Equal(2, view.Value.FrontBackFrontSynthBackSynth.Count)
    Assert.Equal(1s, view.Value.MaxIndexInclusive)
    Assert.Equal<string seq>(
        [clozeText; "extra"],
        view.Value.FieldValues.Select(fun x -> x.Value))
    }

[<Fact>]
let ``Create card works with EditAcquiredCardCommand`` (): Task<unit> = (taskResult {
    let userId = 3
    use c = new TestContainer()
    let getAcquiredCard branchId =
        c.Db.Branch.SingleAsync(fun x -> x.Id = branchId)
        |> Task.map (fun x -> x.StackId)
        |> Task.bind (fun stackId -> StackRepository.GetAcquired c.Db userId stackId |> TaskResult.map Seq.exactlyOne)
    let branchId = 1
    let! actualBranchId = FacetRepositoryTests.addBasicStack c.Db userId []
    Assert.equal branchId actualBranchId
    let acId = 1
    
    // insert new stack with invalid settingsId
    let invalidCardId = 1337
    let! (error: Result<_,_>) =
        {   EditAcquiredCardCommand.init with
                CardSettingId = invalidCardId }
        |>  SanitizeAcquiredCardRepository.update c.Db userId acId
    Assert.equal "You provided an invalid or unauthorized card setting id." error.error
    
    // insert new stack with someone else's settingId
    let someoneElse'sSettingId = userId - 1
    let! (error: Result<_,_>) =
        {   EditAcquiredCardCommand.init with
                CardSettingId = someoneElse'sSettingId }
        |>  SanitizeAcquiredCardRepository.update c.Db userId acId
    Assert.equal "You provided an invalid or unauthorized card setting id." error.error
    
    // insert new stack with invalid deckId
    let invalidDeckId = 1337
    let! (error: Result<_,_>) =
        {   EditAcquiredCardCommand.init with
                DeckId = invalidDeckId }
        |>  SanitizeAcquiredCardRepository.update c.Db userId acId
    Assert.equal "You provided an invalid or unauthorized deck id." error.error
    
    // insert new stack with someone else's deckId
    let someoneElse'sDeckId = userId - 1
    let! (error: Result<_,_>) =
        {   EditAcquiredCardCommand.init with
                DeckId = someoneElse'sDeckId }
        |>  SanitizeAcquiredCardRepository.update c.Db userId acId
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
    do! {   EditAcquiredCardCommand.init with
                CardSettingId = latestSettingId }
        |> SanitizeAcquiredCardRepository.update c.Db userId acId
    let! (card: CollectedCard) = getAcquiredCard branchId
    Assert.Equal(latestSettingId, card.CardSettingId)

    // insert new stack with default settingsId
    do! EditAcquiredCardCommand.init
        |> SanitizeAcquiredCardRepository.update c.Db userId acId
    let! (card: CollectedCard) = getAcquiredCard branchId
    Assert.Equal(defaultSettingId, card.CardSettingId)

    let! latestDeckId = SanitizeDeckRepository.create c.Db userId <| Guid.NewGuid().ToString()
    // insert new stack with latest deckId
    do! {   EditAcquiredCardCommand.init with
                DeckId = latestDeckId }
        |> SanitizeAcquiredCardRepository.update c.Db userId acId
    let! (card: CollectedCard) = getAcquiredCard branchId
    Assert.Equal(latestDeckId, card.DeckId)

    // insert new stack with default deckId
    let defaultDeckId = userId
    do! EditAcquiredCardCommand.init
        |> SanitizeAcquiredCardRepository.update c.Db userId acId
    let! (card: CollectedCard) = getAcquiredCard branchId
    Assert.Equal(defaultDeckId, card.DeckId)
    } |> TaskResult.getOk)

[<Fact>]
let ``Create cloze card works`` (): Task<unit> = (taskResult {
    let userId = 3
    use c = new TestContainer()
    let testCommunalFields = testCommunalFields c userId

    let getBranchInstances clozeText = c.Db.BranchInstance.Where(fun x -> x.CommunalFieldInstance_BranchInstances.Any(fun x -> x.CommunalFieldInstance.Value = clozeText))
    let test clozeMaxIndex clozeText otherTest = task {
        let! _ = FacetRepositoryTests.addCloze clozeText c.Db userId []
        for i in [1 .. clozeMaxIndex] |> List.map int16 do
            Assert.SingleI <| c.Db.LatestBranchInstance.Where(fun x -> x.FieldValues.Contains clozeText)
            Assert.Equal(0, c.Db.LatestBranchInstance.Count(fun x -> x.CommunalFieldInstance_BranchInstances.Any(fun x -> x.CommunalFieldInstance.Value = clozeText)))
            let! communalFieldInstanceIds =
                (getBranchInstances clozeText)
                    .Select(fun x -> x.CommunalFieldInstance_BranchInstances.Single().CommunalFieldInstance.Id)
                    .ToListAsync()
            for id in communalFieldInstanceIds do
                Assert.True(c.Db.LatestCommunalFieldInstance.Any(fun x -> x.Id = id))
            let! stackIds = (getBranchInstances clozeText).Select(fun x -> x.StackId).ToListAsync()
            for id in stackIds do
                do! testCommunalFields id [clozeText]
        otherTest clozeText }
    let assertUserHasNormalCardCount expected =
        c.Db.CollectedCard.CountAsync(fun x -> x.UserId = userId && x.CardState = CardState.toDb Normal)
        |> Task.map (fun actual -> Assert.Equal(expected, actual))
    do! assertUserHasNormalCardCount 0
    let assertCount expected (clozeText: string) =
        c.Db.BranchInstance
            .Include(fun x -> x.CollateInstance)
            .Include(fun x -> x.CommunalFieldInstance_BranchInstances :> IEnumerable<_>)
                .ThenInclude(fun (x: CommunalFieldInstance_BranchInstanceEntity) -> x.CommunalFieldInstance)
            .ToList()
            .Select(BranchInstanceView.load)
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

    let! (e: PagedList<Result<CollectedCard, string>>) = StackRepository.GetAcquiredPages c.Db userId 1 ""
    let expected =
        [   "Canberra was founded in [ ... ] .", "Canberra was founded in [ 1913 ] . extra"
            "[ city ] was founded in [ ... ] .", "[ Canberra ] was founded in [ 1913 ] . extra"
            "[ city ] was founded in 1845.", "[ Portland ] was founded in 1845. extra"
            "Portland was founded in [ ... ] .", "Portland was founded in [ 1845 ] . extra" ]
    Assert.Equal(expected, e.Results.Select(fun x -> x.Value.BranchInstanceMeta.StrippedFront, x.Value.BranchInstanceMeta.StrippedBack))
    do! assertUserHasNormalCardCount 4

    // go from 1 cloze to 2 clozes
    let branchId = 1
    let! command = SanitizeStackRepository.getUpsert c.Db <| VUpdateBranchId branchId
    let command =
        { command with
            ViewEditStackCommand.FieldValues =
                [   {   command.FieldValues.[0] with
                            Value = "{{c2::Canberra}} was founded in {{c1::1913}}."
                    }
                    command.FieldValues.[1]
                ].ToList()
        }
    let! actualBranchId = SanitizeStackRepository.Update c.Db userId [] command
    Assert.Equal(branchId, actualBranchId)
    do! assertUserHasNormalCardCount 5
    
    // go from 2 clozes to 1 cloze
    let! command = SanitizeStackRepository.getUpsert c.Db <| VUpdateBranchId branchId
    let command =
        { command with
            ViewEditStackCommand.FieldValues =
                [   {   command.FieldValues.[0] with
                            Value = "Canberra was founded in {{c1::1913}}."
                    }
                    command.FieldValues.[1]
                ].ToList()
        }
    let! actualBranchId = SanitizeStackRepository.Update c.Db userId [] command
    Assert.Equal(branchId, actualBranchId)
    do! assertUserHasNormalCardCount 4
    
    // multiple c1's works
    let! command = SanitizeStackRepository.getUpsert c.Db <| VUpdateBranchId branchId
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
    Assert.Equal(branchId, actualBranchId)
    do! assertUserHasNormalCardCount 4
    } |> TaskResult.getOk)

[<Fact>]
let ``UpdateRepository.stack on addReversedBasicStack works`` (): Task<unit> = (taskResult {
    let userId = 3
    use c = new TestContainer()
    let! _ = FacetRepositoryTests.addReversedBasicStack c.Db userId []
    let stackId = 1
    let branchId_og = 1
    Assert.equal 2 <| c.Db.CollectedCard.Count(fun x -> x.UserId = userId && x.BranchId = branchId_og)
    let! revisions = StackRepository.Revisions c.Db userId branchId_og
    Assert.equal 1 revisions.SortedMeta.Length

    // branching a stack acquires it
    let branchId_alt = 2
    do! FacetRepositoryTests.update c userId
            (VNewBranchSourceStackId stackId) id branchId_alt

    Assert.equal 0 <| c.Db.CollectedCard.Count(fun x -> x.UserId = userId && x.BranchId = branchId_og)
    Assert.equal 2 <| c.Db.CollectedCard.Count(fun x -> x.UserId = userId && x.BranchId = branchId_alt)

    // updating an unacquired branch doesn't change the AcquiredCards
    do! FacetRepositoryTests.update c userId
            (VUpdateBranchId branchId_og) id branchId_og

    Assert.equal 0 <| c.Db.CollectedCard.Count(fun x -> x.UserId = userId && x.BranchId = branchId_og)
    Assert.equal 2 <| c.Db.CollectedCard.Count(fun x -> x.UserId = userId && x.BranchId = branchId_alt)

    // switching to testing StackRepository.Revisions
    let! revisions = StackRepository.Revisions c.Db userId branchId_og

    Assert.equal 2 revisions.SortedMeta.Length

    // invalid branchId
    let invalidBranchId = 1337
    let! (revisions: Result<_, _>) = StackRepository.Revisions c.Db userId invalidBranchId

    Assert.equal (sprintf "BranchId #%i not found" invalidBranchId) revisions.error
    } |> TaskResult.getOk)

//[<Fact>] // medTODO uncomment when you bring back communals
let ``Creating card with shared "Back" field works twice`` (): Task<unit> = task {
    let userId = 3
    use c = new TestContainer()
    let! collate =
        TestCollateRepo.Search c.Db "Basic"
        |> Task.map (fun x -> x.Single(fun x -> x.Name = "Basic"))
    let editSummary = Guid.NewGuid().ToString()
    let communalValue = Guid.NewGuid().ToString()
    let stackId = 1
    let branchInstanceId = 1001

    let test instanceId customTest = task {
        let! _ =
            SanitizeStackRepository.Update
                c.Db
                userId
                []
                {   EditSummary = editSummary
                    FieldValues =
                        collate
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
                    CollateInstance = collate
                    Kind = NewOriginal_TagIds []
                    Title = null
                }
            |> Task.map Result.getOk
        let! field = c.Db.CommunalField.SingleAsync()
        Assert.Equal(stackId, field.Id)
        Assert.Equal(3, field.AuthorId)
        let! instance = c.Db.CommunalFieldInstance.Include(fun x -> x.CommunalFieldInstance_BranchInstances).SingleAsync(fun x -> x.Value = communalValue)
        Assert.Equal(branchInstanceId, instance.Id)
        Assert.Equal(1, instance.CommunalFieldId)
        Assert.Equal("Back", instance.FieldName)
        Assert.Equal(communalValue, instance.Value)
        Assert.Null instance.Modified
        Assert.Equal(editSummary, instance.EditSummary)
        customTest instance }
    do! test <| None <| fun i ->
            Assert.Equal(branchInstanceId, i.CommunalFieldInstance_BranchInstances.Single().BranchInstanceId)
            Assert.Equal(1001, i.CommunalFieldInstance_BranchInstances.Single().CommunalFieldInstanceId)
            Assert.True(c.Db.LatestCommunalFieldInstance.Any(fun x -> x.Id = i.Id))
    do! test <| Some branchInstanceId <| fun i ->
            Assert.Equal([1001; 1002], i.CommunalFieldInstance_BranchInstances.Select(fun x -> x.BranchInstanceId))
            Assert.Equal([1001; 1001], i.CommunalFieldInstance_BranchInstances.Select(fun x -> x.CommunalFieldInstanceId))
            Assert.True(c.Db.LatestCommunalFieldInstance.Any(fun x -> x.Id = i.Id))
    Assert.SingleI c.Db.CommunalField
    Assert.SingleI c.Db.CommunalFieldInstance }

[<Fact>]
let ``AnkiDefaults.collateIdByHash is same as initial database`` (): unit =
    let c = new TestContainer()
    use hasher = SHA512.Create()
    let dbCollates =
        c.Db.CollateInstance
            .OrderBy(fun x -> x.Id)
            .ToList()
    
    // test that the calculated hash is the same as the one stored in the db
    for collate in dbCollates do
        let calculated = CollateInstanceEntity.hashBase64 hasher collate
        let dbValue = BranchInstanceEntity.bitArrayToByteArray collate.Hash |> Convert.ToBase64String
        //for x in CollateInstanceEntity.hash hasher collate do
        //    Console.Write(if x then "1" else "0")
        //Console.WriteLine()
        Assert.Equal(calculated, dbValue)

    // test that AnkiDefaults.collateIdByHash is up to date
    for dbCollate in dbCollates do
        let calculated = CollateInstanceEntity.hashBase64 hasher dbCollate
        //calculated.D(string dbCollate.Id)
        Assert.Equal(AnkiDefaults.collateInstanceIdByHash.[calculated], dbCollate.Id)

//[<Fact>]
let ``Manual Anki import`` (): Task<unit> = (taskResult {
    let userId = 3
    let pathToCollection = @""
    
    use c = new TestContainer()
    let db = c.Db
    
    //use c = new Container()
    //c.RegisterStuffTestOnly
    //c.RegisterStandardConnectionString
    //use __ = AsyncScopedLifestyle.BeginScope c
    //let db = c.GetInstance<CardOverflowDb>()

    let ankiDb =
        AnkiImporter.getSimpleAnkiDb
        |> using(SanitizeAnki.ankiDb pathToCollection)
    do! pathToCollection
        |> AnkiImporter.loadFiles (fun sha256 -> db.File |> Seq.tryFind(fun f -> f.Sha256 = sha256))
        |> Task.FromResult
        |> TaskResult.bind(AnkiImporter.save db ankiDb userId)
    } |> TaskResult.getOk)

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

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``AnkiImporter.save saves three files`` ankiFileName ankiDb: Task<unit> = task {
    let userId = 3
    use c = new TestContainer(ankiFileName)
    
    do!
        SanitizeAnki.ankiExportsDir +/ ankiFileName
        |> AnkiImporter.loadFiles (fun _ -> None)
        |> Result.bind (AnkiImporter.save c.Db ankiDb userId)
        |> Result.getOk

    Assert.Equal(3, c.Db.File_CardInstance.Count())
    Assert.Equal(3, c.Db.File.Count())
    Assert.NotEmpty(c.Db.CardInstance.Where(fun x -> x.AnkiNoteOrd = Nullable 1uy))
    }

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``Running AnkiImporter.save 3x only imports 3 files`` ankiFileName ankiDb: Task<unit> = task {
    let userId = 3
    use c = new TestContainer(ankiFileName)

    for _ in [1..3] do
        do!
            SanitizeAnki.ankiExportsDir +/ ankiFileName
            |> AnkiImporter.loadFiles (fun sha256 -> c.Db.File.FirstOrDefault(fun f -> f.Sha256 = sha256) |> Option.ofObj)
            |> Result.bind (AnkiImporter.save c.Db ankiDb userId)
            |> Result.getOk

    Assert.Equal(3, c.Db.File_CardInstance.Count())
    Assert.Equal(3, c.Db.File.Count())
    Assert.NotEmpty(c.Db.CardInstance.Where(fun x -> x.AnkiNoteOrd = Nullable 1uy))
    }

[<Fact>]
let ``Anki.replaceAnkiFilenames transforms anki filenames into our filenames`` () =
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
    let fields = AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_colpkg.Notes |> List.map(fun x -> x.Flds)
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
let ``AnkiImporter import cards that have the same acquireHash as distinct cards`` (): Task<unit> = task { // lowTODO, perhaps they should be the same card
    let userId = 3
    use c = new TestContainer()
    do!
        AnkiImporter.save c.Db duplicatesFromLightyear userId Map.empty
        |> Result.getOk
    Assert.Equal<string seq>(
        ["bab::endocrinology::thyroid::thyroidcancer"; "bab::gastroenterology::clinical::livertumors"; "Deck:duplicate cards"; "DifferentCaseRepeatedTag"; "Pathoma::Neoplasia::Tumor_Progression"; "repeatedTag"],
        c.Db.Tag.Select(fun x -> x.Name).OrderBy(fun x -> x))
    Assert.Equal(3, c.Db.Card.Count())
    Assert.Equal(3, c.Db.CardInstance.Count())
    }

[<Fact>]
let ``Multiple cloze indexes works and missing image => <img src="missingImage.jpg">`` (): Task<unit> = task {
    let userId = 3
    use c = new TestContainer()
    do!
        AnkiImporter.save c.Db multipleClozeAndSingleClozeAndNoClozeWithMissingImage userId Map.empty
        |> Result.getOk
    Assert.Equal(
        5,
        c.Db.CardInstance
            .Count(fun x -> x.FieldValues.Contains("may be remembered with the mnemonic"))
        )
    Assert.SingleI
        <| c.Db.CardInstance
            .Where(fun x -> x.FieldValues.Contains("Fibrosis"))
    Assert.SingleI
        <| c.Db.CardInstance
            .Where(fun x -> x.FieldValues.Contains("acute"))
    Assert.True(c.Db.CardInstance.Select(fun x -> x.FieldValues).Single(fun x -> x.Contains "Prerenal").Contains """<img src="/missingImage.jpg">""")
    let longThing = """Drugs that act on microtubules may be remembered with the mnemonic "Microtubules Get Constructed Very Poorly":M: {{c1::Mebendazole (antihelminthic)}}G: {{c2::Griseofulvin (antifungal)}} C: {{c3::Colchicine (antigout)}} V: {{c4::Vincristine/Vinblastine (anticancer)}}P: {{c5::Palcitaxel (anticancer)}}"""
    Assert.Equal<string seq>(
        [   longThing
            "↑ {{c1::Cl−}} concentration (> 60 mEq/L) in sweat is diagnostic for Cystic Fibrosis"],
        c.Db.CommunalFieldInstance.Select(fun x -> MappingTools.stripHtmlTags x.Value))
    let! card = CardRepository.Get c.Db userId 1
    Assert.Equal(
        longThing,
        card.LatestMeta.CommunalFields.Single().Value |> MappingTools.stripHtmlTags)
    Assert.Equal(
        """Drugs that act on microtubules may be remembered with the mnemonic "Microtubules Get Constructed Very Poorly":M: [ ... ] G: Griseofulvin (antifungal) C: Colchicine (antigout) V: Vincristine/Vinblastine (anticancer)P: Palcitaxel (anticancer)""",
        card.LatestMeta.StrippedFront)
    let! card = CardRepository.Get c.Db userId 1
    Assert.Empty card.Relationships
    Assert.Empty c.Db.Relationship }

[<Fact>]
let ``AnkiDefaults.cardTemplateIdByHash is same as initial db`` () =
    let c = new TestContainer()
    let userId = 1
    let toEntity (cardTemplate: AnkiCardTemplateInstance) =
        cardTemplate.CopyToNew userId null
    use hasher = SHA256.Create()
    let dbidByHash =
        Anki.parseModels
            userId
            InitializeDatabase.ankiModels
        |> Result.getOk
        |> List.collect (snd >> List.map toEntity)
        |> List.mapi (fun i entity ->
            CardTemplateInstanceEntity.acquireHash hasher entity, i + 1
        ) |> Map.ofList
    let actualDbIdByHash =
        c.Db.CardTemplate
            .Include(fun x -> x.CardTemplateInstances)
            .AsEnumerable()
            .Select(fun x -> x.CardTemplateInstances.Single())
            .Select(fun x -> CardTemplateInstanceEntity.acquireHash hasher x, x.Id)
            |> Map.ofSeq

    dbidByHash |> Map.iter(fun hash expectedId ->
        Assert.Equal(
            expectedId,
            actualDbIdByHash.[hash]))
    Assert.Equal<Map<string, int>>(
        dbidByHash,
        AnkiDefaults.cardTemplateIdByHash)

//[<Fact>]
let ``Manual Anki import`` (): Task<unit> = task {
    let userId = 3
    let pathToCollection = @""
    
    use c = new TestContainer()
    let db = c.Db
    
    //use c = new Container()
    //c.RegisterStuff
    //c.RegisterStandardConnectionString
    //use __ = AsyncScopedLifestyle.BeginScope c
    //let db = c.GetInstance<CardOverflowDb>()

    do!    
        let ankiDb =
            AnkiImporter.getSimpleAnkiDb
            |> using(SanitizeAnki.ankiDb pathToCollection)
        pathToCollection
        |> AnkiImporter.loadFiles (fun sha256 -> db.File |> Seq.tryFind(fun f -> f.Sha256 = sha256))
        |> Result.bind (AnkiImporter.save db ankiDb userId)
        |> function
        | Ok x -> x
        | Error x -> failwith x
    }

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

[<Fact(Skip=PgSkip.reason)>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``AnkiImporter.save saves three files`` ankiFileName ankiDb: Task<unit> = (taskResult {
    let userId = user_3
    use c = new TestContainer(false, ankiFileName)
    
    do!
        SanitizeAnki.ankiExportsDir +/ ankiFileName
        |> AnkiImporter.loadFiles (fun _ -> None)
        |> Task.FromResult
        |> TaskResult.bind(AnkiImporter.save c.Db ankiDb userId)

    Assert.Equal(3, c.Db.File_Revision.Count())
    Assert.Equal(3, c.Db.File.Count())
    Assert.NotEmpty(c.Db.Card.Where(fun x -> x.Index = 1s))
    Assert.Equal(7, c.Db.TemplateRevision.Count())
    Assert.Equal(5, c.Db.LatestTemplateRevision.Count())
    } |> TaskResult.getOk)

[<Fact(Skip=PgSkip.reason)>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``Running AnkiImporter.save 3x only imports 3 files`` ankiFileName ankiDb: Task<unit> = (taskResult {
    let userId = user_3
    use c = new TestContainer(false, ankiFileName)

    for _ in [1..3] do
        do!
            SanitizeAnki.ankiExportsDir +/ ankiFileName
            |> AnkiImporter.loadFiles (fun sha256 -> c.Db.File.FirstOrDefault(fun f -> f.Sha256 = sha256) |> Option.ofObj)
            |> Task.FromResult
            |> TaskResult.bind(AnkiImporter.save c.Db ankiDb userId)

    Assert.Equal(3, c.Db.File_Revision.Count())
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

[<Fact(Skip=PgSkip.reason)>]
let ``AnkiImporter import cards that have the same collectHash as distinct cards`` (): Task<unit> = (taskResult { // lowTODO, perhaps they should be the same card
    let userId = user_3
    use c = new TestContainer()
    
    do! AnkiImporter.save c.Db duplicatesFromLightyear userId Map.empty
    
    Assert.Equal<string seq>(
        ["Bab::Endocrinology::Thyroid::Thyroidcancer"; "Bab::Gastroenterology::Clinical::Livertumors"; "Differentcaserepeatedtag"; "Pathoma::Neoplasia::Tumor_Progression"; "Repeatedtag"],
        c.Db.Card.ToList().SelectMany(fun x -> x.Tags :> IEnumerable<_>).OrderBy(fun x -> x))
    Assert.SingleI(c.Db.Deck.Where(fun x -> x.Name = "duplicate cards"))
    Assert.Equal(3, c.Db.Concept.Count())
    Assert.Equal(3, c.Db.Revision.Count())
    Assert.Equal(8, c.Db.TemplateRevision.Count())
    Assert.Equal(6, c.Db.LatestTemplateRevision.Count())
    } |> TaskResult.getOk)

[<Fact(Skip=PgSkip.reason)>]
let ``Multiple cloze indexes works and missing image => <img src="missingImage.jpg">`` (): Task<unit> = task {
    let userId = user_3
    use c = new TestContainer()
    let! x = AnkiImporter.save c.Db multipleClozeAndSingleClozeAndNoClozeWithMissingImage userId Map.empty
    Assert.Equal(7, c.Db.Card.Count())
    Assert.Equal(3, c.Db.Revision.Count())
    Assert.Equal(3, c.Db.Example.Count())
    Assert.Equal(3, c.Db.Concept.Count())
    Assert.Equal(4s, c.Db.Revision.Single(fun x -> x.FieldValues.Contains "Drugs").MaxIndexInclusive)
    Assert.Null x.Value
    let allRevisionViews =
        c.Db.Revision
            .Include(fun x -> x.TemplateRevision)
            .ToList()
            .Select(RevisionView.load)
    let assertCount expected (clozeText: string) =
        allRevisionViews
            .Count(fun x -> x.FieldValues.Any(fun x -> x.Value.Contains clozeText))
            |> fun x -> Assert.Equal(expected, x)
    assertCount 1 "may be remembered with the mnemonic"
    let longThing = """Drugs that act on microtubules may be remembered with the mnemonic "Microtubules Get Constructed Very Poorly":M: {{c1::Mebendazole (antihelminthic)}}G: {{c2::Griseofulvin (antifungal)}} C: {{c3::Colchicine (antigout)}} V: {{c4::Vincristine/Vinblastine (anticancer)}}P: {{c5::Palcitaxel (anticancer)}}"""
    let longThingUs = longThing + " "
    Assert.Equal<string seq>(
        [   """↑ {{c1::Cl−}} concentration (> 60 mEq/L) in sweat is diagnostic for Cystic FibrosisImage here"""
            """↑↑ BUN/CR ratio indicates which type of acute renal failure?Prerenal azotemia"""
            longThingUs],
        c.Db.Revision.ToList().Select(fun x -> x.FieldValues |> MappingTools.stripHtmlTags).OrderBy(fun x -> x))
    assertCount 1 "Fibrosis"
    Assert.areEquivalent
        [   "<b><br /></b>"
            "<br /><div><br /></div><div>Image here</div>" ]
        (allRevisionViews.SelectMany(fun x -> x.FieldValues.Where(fun x -> x.Field.Name = "Extra").Select(fun x -> x.Value)))
    Assert.areEquivalent
        [   longThing
            "↑ {{c1::Cl−}} concentration (> 60 mEq/L) in sweat is diagnostic for Cystic Fibrosis" ]
        (allRevisionViews.SelectMany(fun x -> x.FieldValues.Where(fun x -> x.Field.Name = "Text").Select(fun x -> MappingTools.stripHtmlTags x.Value)))
    Assert.SingleI
        <| c.Db.Revision
            .Where(fun x -> x.FieldValues.Contains("acute"))
    Assert.True(c.Db.Revision.Select(fun x -> x.FieldValues).Single(fun x -> x.Contains "Prerenal").Contains """<img src="/missingImage.jpg">""")
    }

[<Fact(Skip=PgSkip.reason)>]
let ``AnkiDefaults.templateIdByHash is same as initial database`` (): unit =
    let c = new TestContainer()
    use hasher = SHA512.Create()
    let dbTemplates =
        c.Db.TemplateRevision
            .OrderBy(fun x -> x.Id)
            .ToList()
    
    // test that the calculated hash is the same as the one stored in the db
    for template in dbTemplates do
        let calculated = TemplateRevisionEntity.hashBase64 hasher template
        let dbValue = RevisionEntity.bitArrayToByteArray template.Hash |> Convert.ToBase64String
        //for x in TemplateRevisionEntity.hash hasher template do
        //    Console.Write(if x then "1" else "0")
        //Console.WriteLine()
        Assert.Equal(calculated, dbValue)

    // test that AnkiDefaults.templateIdByHash is up to date
    for dbTemplate in dbTemplates do
        let calculated = TemplateRevisionEntity.hashBase64 hasher dbTemplate
        //calculated.D(string dbTemplate.Id)
        Assert.Equal(AnkiDefaults.templateRevisionIdByHash.[calculated], dbTemplate.Id)

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
    //let db = c.GetRevision<CardOverflowDb>()

    let ankiDb =
        AnkiImporter.getSimpleAnkiDb
        |> using(SanitizeAnki.ankiDb pathToCollection)
    do! pathToCollection
        |> AnkiImporter.loadFiles (fun sha256 -> db.File |> Seq.tryFind(fun f -> f.Sha256 = sha256))
        |> Task.FromResult
        |> TaskResult.bind(AnkiImporter.save db ankiDb userId)
    } |> TaskResult.getOk)

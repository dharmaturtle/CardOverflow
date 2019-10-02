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

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``AnkiImporter.save saves three files`` ankiFileName ankiDb =
    let userId = 3
    use c = new TestContainer(ankiFileName)
    
    SanitizeAnki.ankiExportsDir +/ ankiFileName
    |> AnkiImporter.loadFiles (fun _ -> None)
    |> Result.bind (AnkiImporter.save c.Db ankiDb userId)
    |> Result.getOk

    Assert.Equal(3, c.Db.File_CardInstance.Count())
    Assert.Equal(3, c.Db.File.Count())

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``Running AnkiImporter.save 3x only imports 3 files`` ankiFileName ankiDb =
    let userId = 3
    use c = new TestContainer(ankiFileName)

    for _ in [1..3] do
        SanitizeAnki.ankiExportsDir +/ ankiFileName
        |> AnkiImporter.loadFiles (fun sha256 -> c.Db.File.FirstOrDefault(fun f -> f.Sha256 = sha256) |> Option.ofObj)
        |> Result.bind (AnkiImporter.save c.Db ankiDb userId)
        |> Result.isOk
        |> Assert.True

    Assert.Equal(3, c.Db.File_CardInstance.Count())
    Assert.Equal(3, c.Db.File.Count())

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
let ``AnkiImporter.save can import cards that have the same acquireHash`` () =
    let userId = 3
    use c = new TestContainer()
    AnkiImporter.save c.Db duplicatesFromLightyear userId Map.empty
    |> function
    | Ok () -> ()
    | Error x -> failwith x
    Assert.Equal<string seq>(
        ["bab::endocrinology::thyroid::thyroidcancer"; "bab::gastroenterology::clinical::livertumors"; "Deck:duplicate cards"; "DifferentCaseRepeatedTag"; "Pathoma::Neoplasia::Tumor_Progression"; "repeatedTag"],
        c.Db.Tag.Select(fun x -> x.Name).OrderBy(fun x -> x))
    Assert.Equal("3/8/2018 23:48:00", c.Db.Card.Include(fun x -> x.CardInstances).Single().CardInstances.Single().Created.ToString("M/d/yyyy HH:mm:ss"))
    Assert.Equal("4/26/2018 02:54:00", c.Db.Card.Include(fun x -> x.CardInstances).Single().CardInstances.Single().Modified.Value.ToString("M/d/yyyy HH:mm:ss"))

[<Fact>]
let ``Multiple cloze indexes works and missing image => <img src="missingImage.jpg">`` (): Task<unit> = task {
    let userId = 3
    use c = new TestContainer()
    AnkiImporter.save c.Db multipleClozeAndSingleClozeAndNoClozeWithMissingImage userId Map.empty
    |> function
    | Ok () -> ()
    | Error x -> failwith x
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
    Assert.Equal<(int * int) seq>(
        (Core.combination 2 [1.. 5])
            .Select(fun x -> x.[0], x.[1])
            .OrderBy(fun (a, _) -> a)
            .ThenBy(fun (_, b) -> b),
        c.Db.Relationship
            .Where(fun x -> x.Name = "Cloze").AsEnumerable()
            .Select(fun x -> x.SourceId, x.TargetId)
            .OrderBy(fun (a, _) -> a)
                .ThenBy(fun (_, b) -> b)
    )
    let card = (CardRepository.Get c.Db userId 1).GetAwaiter().GetResult()
    let f, _, _, _ = card.LatestInstance.FrontBackFrontSynthBackSynth
    Assert.True(f.Contains """<div>&nbsp;<u>Drugs</u>&nbsp;that act on&nbsp;<b>microtubules</b>&nbsp;may be remembered with the mnemonic "<i><b>M</b>icrotubules&nbsp;<b>G</b>et&nbsp;<b>C</b>onstructed&nbsp;<b>V</b>ery&nbsp;<b>P</b>oorly</i>":</div><div><br /></div><div><b>M</b>:&nbsp;Mebendazole (antihelminthic)<div><b>G</b>:&nbsp;Griseofulvin (antifungal)&nbsp;</div><div><b>C</b>:&nbsp;
        <span class="cloze-brackets-front">[</span>
        <span class="cloze-filler-front">...</span>
        <span class="cloze-brackets-front">]</span>
        &nbsp;</div><div><b>V</b>:&nbsp;Vincristine/Vinblastine (anticancer)</div><div><b>P</b>:&nbsp;Palcitaxel (anticancer)&nbsp;</div><br /></div>""")
    Assert.Equal(4, card.Relationships.Count())
    Assert.Equal<int seq>(
        [1; 2; 4; 5],
        card.Relationships.Select(fun x -> x.CardId).OrderBy(fun x -> x)
    )
    Assert.Equal(10, c.Db.Relationship.Count(fun x -> x.Name = "Cloze"))
    let! card = CardRepository.Get c.Db 1 userId
    Assert.Equal<int seq>(
        [1; 1; 1; 1],
        card.Relationships.Select(fun x -> x.Users)
    )}

//[<Fact>]
let ``Manual Anki import`` () =
    let userId = 3
    let pathToCollection = @""
    
    use c = new TestContainer()
    let db = c.Db
    
    //use c = new Container()
    //c.RegisterStuff
    //c.RegisterStandardConnectionString
    //use __ = AsyncScopedLifestyle.BeginScope c
    //let db = c.GetInstance<CardOverflowDb>()
    
    let ankiDb =
        AnkiImporter.getSimpleAnkiDb
        |> using(SanitizeAnki.ankiDb pathToCollection)
    pathToCollection
    |> AnkiImporter.loadFiles (fun sha256 -> db.File |> Seq.tryFind(fun f -> f.Sha256 = sha256))
    |> Result.bind (AnkiImporter.save db ankiDb userId)
    |> function
    | Ok () -> ()
    | Error x -> failwith x

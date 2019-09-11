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

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``AnkiImporter.save saves three files`` ankiFileName ankiDb =
    let userId = 3
    use c = new TestContainer(ankiFileName)
    
    ankiExportsDir +/ ankiFileName
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
        ankiExportsDir +/ ankiFileName
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
        """Basic with image&nbsp;<img src="image/AAEECRAZJDFAUWR5kKnE4QAhRGmQueQRQHGk2RBJhME">Basic back, no image"""
        """Basic front with mp3
<audio controls autoplay>
    <source src="AAIGDBQeKjhIWm6EnLbS8BAyVnykzvooWIq+9CxmouA=" type="audio/mpeg">
    Your browser does not support the audio element.
</audio>
Basic back, no mp3"""
        """<img src="image/AAIEBggKDA4QEhQWGBocHiAiJCYoKiwuMDI0Njg6PD4"><img src="image/AAIEBggKDA4QEhQWGBocHiAiJCYoKiwuMDI0Njg6PD4">"""]
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
    Assert.Equal("3/8/2018 23:48:00", c.Db.Card.Include(fun x -> x.CardInstances).Single().CardInstances.Single().Created.ToString())
    Assert.Equal("4/26/2018 02:54:00", c.Db.Card.Include(fun x -> x.CardInstances).Single().CardInstances.Single().Modified.ToString())

[<Fact>]
let ``Multiple cloze indexes works and missing image => <img src="missingImage.jpg">`` () =
    let userId = 3
    use c = new TestContainer()
    AnkiImporter.save c.Db multipleClozeAndSingleClozeAndNoClozeWithMissingImage userId Map.empty
    |> function
    | Ok () -> ()
    | Error x -> failwith x
    //highTODO uncomment
    //Assert.Equal<byte Nullable seq>(
    //    [ 0uy; 1uy; 2uy; 3uy; 4uy ] |> Seq.map Nullable,
    //    c.Db.CardInstance
    //        .Include(fun x -> x.Card)
    //        .Single(fun x -> x.FieldValues.Any(fun x -> x.Value.Contains "c5"))
    //        .Cards.Select(fun x -> x.ClozeIndex).OrderBy(fun x -> x))
    //Assert.Equal(
    //    Nullable 0uy,
    //    c.Db.CardInstance
    //        .Include(fun x -> x.Cards)
    //        .Single(fun x -> x.FieldValues.Any(fun x -> x.Value.Contains "Fibrosis"))
    //        .Cards.Single().ClozeIndex)
    //Assert.Equal(
    //    Nullable(),
    //    c.Db.CardInstance
    //        .Include(fun x -> x.Cards)
    //        .Single(fun x -> x.FieldValues.Any(fun x -> x.Value.Contains "acute"))
    //        .Cards.Single().ClozeIndex)
    //Assert.True(c.Db.FieldValue.Single(fun x -> x.Value.Contains "Prerenal").Value.Contains """<img src="missingImage.jpg">""")
    //Assert.SingleI <| c.Db.Concept.Where(fun x -> x.Name = "↑↑ BUN/CR ratio indicates which type of acute renal failure?")
    //Assert.SingleI <| c.Db.Concept.Where(fun x -> x.Name = "Drugs that act on microtubules may be remembered with the mnemonic \"Microtubules Get Constructed Ve…")

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
        |> using(ankiDb pathToCollection "Manual Anki import")
    pathToCollection
    |> AnkiImporter.loadFiles (fun sha256 -> db.File |> Seq.tryFind(fun f -> f.Sha256 = sha256))
    |> Result.bind (AnkiImporter.save db ankiDb userId)
    |> function
    | Ok () -> ()
    | Error x -> failwith x

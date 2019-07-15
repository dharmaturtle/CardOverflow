module AnkiImportFileTests

open CardOverflow.Api
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

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``AnkiImporter.save saves three files`` ankiFileName ankiDb =
    let userId = 3
    use c = new TestContainer(ankiFileName)
    
    ankiExportsDir +/ ankiFileName
    |> AnkiImporter.loadFiles (fun _ -> None)
    |> Result.bind (AnkiImporter.save c.Db ankiDb userId)
    |> Result.getOk

    Assert.Equal(3, c.Db.FileConcepts.Count())
    Assert.Equal(3, c.Db.Files.Count())

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``Running AnkiImporter.save 3x only imports 3 files`` ankiFileName ankiDb =
    let userId = 3
    use c = new TestContainer(ankiFileName)

    for _ in [1..3] do
        ankiExportsDir +/ ankiFileName
        |> AnkiImporter.loadFiles (fun sha256 -> c.Db.Files |> Seq.tryFind(fun f -> f.Sha256 = sha256))
        |> Result.bind (AnkiImporter.save c.Db ankiDb userId)
        |> Result.isOk
        |> Assert.True

    Assert.Equal(3, c.Db.FileConcepts.Count())
    Assert.Equal(3, c.Db.Files.Count())

[<Fact>]
let ``Anki.replaceAnkiFilenames transforms anki filenames into our filenames`` () =
    let expected = [
        "Basic FrontBasic Back"
        "Basic (and reversed card) frontBasic (and reversed card) back"
        "Basic (optional reversed card) frontBasic (optional reversed card) backBasic (optional reversed card) reverse"
        "Basic (type in the answer) frontBasic (type in the answer) back"
        "Cloze text.&nbsp;Canberra was founded in {{c1::1913}}.Cloze extra"
        """Basic with image&nbsp;<img src="AAEECRAZJDFAUWR5kKnE4QAhRGmQueQRQHGk2RBJhME=">Basic back, no image"""
        """Basic front with mp3
<audio controls autoplay>
    <source src="AAIGDBQeKjhIWm6EnLbS8BAyVnykzvooWIq+9CxmouA=" type="audio/mpeg">
    Your browser does not support the audio element.
</audio>
Basic back, no mp3"""
        """<img src="AAIEBggKDA4QEhQWGBocHiAiJCYoKiwuMDI0Njg6PD4="><img src="AAIEBggKDA4QEhQWGBocHiAiJCYoKiwuMDI0Njg6PD4=">"""]
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
    
    let actual =
        fields |> List.map(fun x ->
            let _, fields, errors = Anki.replaceAnkiFilenames x map
            Assert.Empty errors
            fields
        ) |> List.ofSeq

    Assert.Equal (expected.Length, actual.Length)
    Seq.zip expected actual
    |> Seq.iter Assert.Equal
    Assert.Equal<string list> (expected, actual)

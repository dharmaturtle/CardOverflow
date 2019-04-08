module AnkiImportTests

open CardOverflow.Api
open System.Linq
open Xunit
open System.IO
open System.IO.Compression

let unzipAndGetAnkiDb ankiDb =
    let baseDir = @"..\netcoreapp3.0\AnkiExports\"
    let tempDir = baseDir  + @"Temp\"
    let apkgPath = baseDir + ankiDb + ".apkg"
    if Directory.Exists(tempDir) 
    then Directory.Delete(tempDir, true)
    ZipFile.Open(apkgPath, ZipArchiveMode.Read).ExtractToDirectory(tempDir)
    tempDir + "collection.anki2"

[<Fact>]
let ``AnkiDbService can read from AllDefaultTemplatesAndImageAndMp3``() =
    let service = "AllDefaultTemplatesAndImageAndMp3" |> unzipAndGetAnkiDb |> AnkiDbFactory |> AnkiDbService
    service.Query(fun x -> x.Cards.ToList()) |> Assert.NotEmpty
    service.Query(fun x -> x.Notes.ToList()) |> Assert.NotEmpty

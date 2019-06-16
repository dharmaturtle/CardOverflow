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

let emptyDb = {
        Cards = []
        Cols = allDefaultTemplatesAndImageAndMp3_colpkg.Cols
        Notes = []
        Revlogs = []
    }

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``AnkiImporter.save saves two files`` ankiFileName _ =
    let userId = 3
    use c = new TestContainer(ankiFileName)
    
    ankiExportsDir +/ ankiFileName
    |> AnkiImporter.loadFiles (fun sha256 -> c.Db.Files.Any(fun f -> f.Sha256 = sha256))
    |> Result.bind (AnkiImporter.save c.Db emptyDb userId)
    |> Result.getOk

    Assert.Equal(3, c.Db.Files.Count())

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``Running AnkiImporter.save 3x only imports 3 files`` ankiFileName _ =
    let userId = 3
    use c = new TestContainer(ankiFileName)

    for _ in [1..3] do
        ankiExportsDir +/ ankiFileName
        |> AnkiImporter.loadFiles (fun sha256 -> c.Db.Files.Any(fun f -> f.Sha256 = sha256))
        |> Result.bind (AnkiImporter.save c.Db emptyDb userId)
        |> Result.isOk
        |> Assert.True

    Assert.Equal(3, c.Db.Files.Count())

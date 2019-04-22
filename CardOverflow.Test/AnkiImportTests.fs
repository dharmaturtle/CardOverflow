module AnkiImportTests

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Test
open System.Linq
open System.IO
open System.IO.Compression
open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Quotations
open Xunit

let nameof (q:Expr<_>) = // https://stackoverflow.com/a/48311816
  match q with 
  | Patterns.Let(_, _, DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _))) -> mi.Name
  | Patterns.PropertyGet(_, mi, _) -> mi.Name
  | DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _)) -> mi.Name
  | _ -> failwith "Unexpected format"
let any<'R> : 'R = failwith "!"

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

[<Fact>]
let ``AnkiImporter can import AllDefaultTemplatesAndImageAndMp3``() =
    use temp = new TempDbService()
    let service = "AllDefaultTemplatesAndImageAndMp3" |> unzipAndGetAnkiDb |> AnkiDbFactory |> AnkiDbService
    
    AnkiImporter(service, temp.DbService, 3).run()
    |> function 
    | Ok _ -> true
    | Error _ -> false
    |> Assert.True
    Assert.Equal(7, temp.DbService.Query(fun x -> x.Concepts.Count()))
    Assert.Equal<string>(
        ["Basic"; "OtherTag"; "Tag"],
        temp.DbService.Query(fun x -> x.PrivateTags.ToList()).Select(fun x -> x.Name).OrderBy(fun x -> x))
    Assert.Equal<string>(
        ["OtherTag"],
        temp.DbService.Query(fun db ->
            db.Concepts
                .Include(nameof <@ any<ConceptEntity>.PrivateTagConcepts @> + "." + nameof <@ any<PrivateTagConceptEntity>.PrivateTag @>)
                .Single(fun c -> c.Fields.Contains("mp3"))
                .PrivateTagConcepts.Select(fun t -> t.PrivateTag.Name)))

module AnkiImportTests

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Test
open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Quotations
open System.Linq
open Xunit

let nameof (q: Expr<_>) = // https://stackoverflow.com/a/48311816
    match q with
    | Patterns.Let(_, _, DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _))) -> mi.Name
    | Patterns.PropertyGet(_, mi, _) -> mi.Name
    | DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _)) -> mi.Name
    | _ -> failwith "Unexpected format"
let any<'R> : 'R = failwith "!"

let assertHasBasicInfo ankiDb dbService =
    let userId = 3
    AnkiImporter.save dbService ankiDb userId
    |> Result.isOk
    |> Assert.True
    Assert.Equal(7, dbService.Query(fun db -> db.Concepts.Count()))
    Assert.Equal(9, dbService.Query(fun db -> db.Cards.Count()))
    Assert.Single(dbService.Query(fun db -> db.CardOptions.Where(fun db -> db.UserId = userId).ToList())) |> ignore
    Assert.Equal<string>(
        [ "Basic"; "OtherTag"; "Tag" ],
        dbService.Query(fun db -> db.PrivateTags.ToList()).Select(fun x -> x.Name) |> Seq.sortBy id)
    Assert.Equal<string>(
        [ "OtherTag" ],
        dbService.Query(fun db ->
            db.Concepts
                .Include(nameof <@ any<ConceptEntity>.PrivateTagConcepts @> + "." + nameof <@ any<PrivateTagConceptEntity>.PrivateTag @>)
                .Single(fun c -> c.Fields.Contains("mp3"))
                .PrivateTagConcepts.Select(fun t -> t.PrivateTag.Name)))
    dbService.Query(fun db -> db.ConceptTemplates.ToList())
    |> Seq.collect (fun x -> x.CardTemplates |> CardTemplate.LoadMany |> Seq.map (fun x -> x.DefaultCardOptionId))
    |> Seq.filter (fun id -> id <= 0)
    |> Assert.Empty // invalidDefaultCardOptionIds is empty

[<Fact>]
let ``AnkiImporter can import AllDefaultTemplatesAndImageAndMp3.apkg``() =
    use p = new SqlTempDbProvider()
    AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_apkg |> assertHasBasicInfo <| p.DbService

[<Fact>]
let ``AnkiImporter can import AllDefaultTemplatesAndImageAndMp3.colpkg``() =
    use p = new SqlTempDbProvider()
    AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_colpkg |> assertHasBasicInfo <| p.DbService

let assertHasHistory ankiDb dbService =
    let userId = 3
    AnkiImporter.save dbService ankiDb userId
    |> Result.isOk
    |> Assert.True
    Assert.NotNull(dbService.Query(fun x -> x.Histories.FirstOrDefault()))

[<Fact>]
let ``AnkiImporter can import RandomReviews.colpkg``() =
    use p = new SqlTempDbProvider()
    AnkiImportTestData.getAnki2 "RandomReviews.colpkg"
    |> AnkiImporter.getSimpleAnkiDb
    |> assertHasHistory <| p.DbService

[<Fact>]
let ``AnkiImporter can import RandomReviews.apkg``() =
    use p = new SqlTempDbProvider()
    AnkiImportTestData.getAnki2 "RandomReviews.apkg"
    |> AnkiImporter.getSimpleAnkiDb
    |> assertHasHistory <| p.DbService

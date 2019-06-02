module AnkiImportTests

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Pure
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

let assertHasBasicInfo ankiDb db =
    let userId = 3
    AnkiImporter.save db ankiDb userId []
    |> Result.isOk
    |> Assert.True
    Assert.Equal(7, db.Concepts.Count())
    Assert.Equal(9, db.Cards.Count())
    Assert.Single(db.CardOptions.Where(fun db -> db.UserId = userId).ToList()) |> ignore
    Assert.Equal<string>(
        [ "Basic"; "OtherTag"; "Tag" ],
        (db.PrivateTags.ToList()).Select(fun x -> x.Name) |> Seq.sortBy id)
    Assert.Equal<string>(
        [ "OtherTag" ],
        db.Concepts
            .Include(nameof <@ any<ConceptEntity>.PrivateTagConcepts @> + "." + nameof <@ any<PrivateTagConceptEntity>.PrivateTag @>)
            .Single(fun c -> c.Fields.Contains("mp3"))
            .PrivateTagConcepts.Select(fun t -> t.PrivateTag.Name))

[<Fact>]
let ``AnkiImporter can import AllDefaultTemplatesAndImageAndMp3.apkg``() =
    use c = new TestContainer()
    AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_apkg |> assertHasBasicInfo <| c.Db

[<Fact>]
let ``AnkiImporter can import AllDefaultTemplatesAndImageAndMp3.colpkg``() =
    use c = new TestContainer()
    AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_colpkg |> assertHasBasicInfo <| c.Db

let assertHasHistory ankiDb db =
    let userId = 3
    AnkiImporter.save db ankiDb userId []
    |> Result.isOk
    |> Assert.True
    Assert.NotNull(db.Histories.FirstOrDefault())

[<Fact>]
let ``AnkiImporter can import RandomReviews.colpkg``() =
    use c = new TestContainer()
    AnkiImportTestData.getAnki2 "RandomReviews.colpkg"
    |> AnkiImporter.getSimpleAnkiDb
    |> assertHasHistory <| c.Db

[<Fact>]
let ``AnkiImporter can import RandomReviews.apkg``() =
    use c = new TestContainer()
    AnkiImportTestData.getAnki2 "RandomReviews.apkg"
    |> AnkiImporter.getSimpleAnkiDb
    |> assertHasHistory <| c.Db

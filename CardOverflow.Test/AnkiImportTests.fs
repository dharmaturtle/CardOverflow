module AnkiImportTests

open CardOverflow.Api
open LoadersAndCopiers
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Test
open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Quotations
open System.Linq
open Xunit
open System

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
    Assert.Equal(8, db.Concepts.Count())
    Assert.Equal(10, db.Cards.Count())
    Assert.Equal(10, db.Users.First(fun x -> x.Id = userId).AcquiredCards.Count)
    Assert.Equal(8, db.Users.First(fun x -> x.Id = userId).AcquiredCards.Select(fun x -> x.Card.ConceptId).Distinct().Count())
    Assert.Equal(2, db.CardOptions.Count(fun db -> db.UserId = userId))
    Assert.Equal(5, db.ConceptTemplateConceptTemplateDefaultUsers.Count(fun x -> x.UserId = userId))
    Assert.Equal<string>(
        [ "Basic"; "Deck:Default"; "OtherTag"; "Tag" ],
        (db.PrivateTags.ToList()).Select(fun x -> x.Name) |> Seq.sortBy id)
    Assert.Equal<string>(
        [ "Deck:Default"; "OtherTag" ],
        db.AcquiredCards
            .Single(fun c -> c.Card.Concept.Fields.Contains("mp3"))
            .PrivateTagAcquiredCards.Select(fun t -> t.PrivateTag.Name)
            |> Seq.sortBy id)
    db.Concepts.ToList().Select(fun x -> x.Fields).ToList().Dump()

[<Fact>]
let ``AnkiImporter can import AllDefaultTemplatesAndImageAndMp3.apkg``() =
    use c = new TestContainer()
    AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_apkg |> assertHasBasicInfo <| c.Db

[<Fact>]
let ``AnkiImporter can import AllDefaultTemplatesAndImageAndMp3.colpkg``() =
    use c = new TestContainer()
    AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_colpkg |> assertHasBasicInfo <| c.Db

[<Fact>]
let ``AnkiImporter can import AllDefaultTemplatesAndImageAndMp3-21.colpkg``() =
    use c = new TestContainer()
    AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_21_colpkg |> assertHasBasicInfo <| c.Db

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

[<Theory>]
[<ClassData(typeof<AnkiImportTestData.All>)>]
let ``Importing AnkiDb reuses previous CardOptions, PrivateTags, and ConceptTemplates`` simpleAnkiDb =
    use c = new TestContainer()
    let userId = 3
    for _ in [1..5] do
        AnkiImporter.save c.Db simpleAnkiDb userId []
        |> Result.isOk
        |> Assert.True

    Assert.Equal(2, c.Db.CardOptions.Count(fun x -> x.UserId = userId))
    Assert.Equal(4, c.Db.PrivateTags.Count(fun x -> x.UserId = userId))
    Assert.Equal(5, c.Db.ConceptTemplates.Count(fun x -> x.MaintainerId = userId))

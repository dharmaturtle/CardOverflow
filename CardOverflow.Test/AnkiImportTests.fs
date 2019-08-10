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
open AnkiImportTestData
open System.Collections.Generic

let nameof (q: Expr<_>) = // https://stackoverflow.com/a/48311816
    match q with
    | Patterns.Let(_, _, DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _))) -> mi.Name
    | Patterns.PropertyGet(_, mi, _) -> mi.Name
    | DerivedPatterns.Lambdas(_, Patterns.Call(_, mi, _)) -> mi.Name
    | _ -> failwith "Unexpected format"
let any<'R> : 'R = failwith "!"

let assertHasBasicInfo db ankiDb =
    let userId = 3
    AnkiImporter.save db ankiDb userId Map.empty
    |> Result.isOk
    |> Assert.True
    Assert.Equal<IEnumerable<string>>(
        [   "4/8/2019 02:14:00"
            "4/8/2019 02:14:00"
            "4/8/2019 02:14:00"
            "4/8/2019 02:14:00"
            "4/8/2019 02:14:00"
        ].ToList(),
        db.ConceptTemplateInstance.AsEnumerable().Select(fun x -> x.Created.ToString()).OrderBy(fun x -> x)
    )
    Assert.Equal<IEnumerable<string>>(
        [   "6/16/2019 00:51:00"
            "6/16/2019 00:52:00"
            "6/16/2019 00:52:00"
            "6/16/2019 00:52:00"
            "6/16/2019 00:54:00"
        ].ToList(),
        db.ConceptTemplateInstance.AsEnumerable().Select(fun x -> x.Modified.ToString()).OrderBy(fun x -> x)
    )
    Assert.Equal<IEnumerable<string>>(
        [   "4/8/2019 02:14:32" // lowTODO why do these have seconds when its a smalldatetime?
            "4/8/2019 02:14:57"
            "4/8/2019 02:15:50"
            "4/8/2019 02:16:27"
            "4/8/2019 02:16:42"
            "4/8/2019 02:18:20"
            "4/8/2019 02:21:11"
            "6/16/2019 00:53:20"
        ].ToList(),
        db.ConceptInstance.AsEnumerable().Select(fun x -> x.Created.ToString()).OrderBy(fun x -> x)
    )
    Assert.Equal<IEnumerable<string>>(
        [   "4/8/2019 02:14:53" // lowTODO why do these have seconds when its a smalldatetime?
            "4/8/2019 02:15:44"
            "4/8/2019 02:16:22"
            "4/8/2019 02:16:39"
            "4/8/2019 02:18:05"
            "4/8/2019 02:38:52"
            "4/8/2019 02:43:51"
            "6/16/2019 00:56:27"
        ].ToList(),
        db.ConceptInstance.AsEnumerable().Select(fun x -> x.Modified.ToString()).OrderBy(fun x -> x)
    )
    Assert.Equal(8, db.Concept.Count())
    Assert.Equal(10, db.Card.Count())
    Assert.Equal(10, db.AcquiredCard.Count(fun x -> x.UserId = userId))
    Assert.Equal(8, db.User.First(fun x -> x.Id = userId).AcquiredCards.Select(fun x -> x.Card.ConceptInstanceId).Distinct().Count())
    Assert.Equal(2, db.CardOption.Count(fun db -> db.UserId = userId))
    Assert.Equal(5, db.User_ConceptTemplateInstance.Count(fun x -> x.UserId = userId))
    Assert.Equal<string>(
        [ "Basic"; "Deck:Default"; "OtherTag"; "Tag" ],
        (db.PrivateTag.ToList()).Select(fun x -> x.Name) |> Seq.sort)
    Assert.Equal<string>(
        [ "Deck:Default"; "OtherTag" ],
        db.AcquiredCard
            .Single(fun c -> c.Card.ConceptInstance.FieldValues.Any(fun x -> x.Value.Contains("mp3")))
            .PrivateTag_AcquiredCards.Select(fun t -> t.PrivateTag.Name)
            |> Seq.sortBy id)

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``AnkiImporter can import AnkiImportTestData.All`` _ ankiDb =
    use c = new TestContainer()
    assertHasBasicInfo c.Db ankiDb

let assertHasHistory db ankiDb =
    let userId = 3
    AnkiImporter.save db ankiDb userId Map.empty
    |> Result.isOk
    |> Assert.True
    Assert.Equal(110, db.History.Count(fun x -> x.AcquiredCard.UserId = userId))

type AllRandomReviews () =
    inherit XunitClassDataBase
        ([  [|"RandomReviews.colpkg" |]
            [|"RandomReviews-21.colpkg" |]
            [|"RandomReviews.apkg" |] ])

[<Theory>]
[<ClassData(typeof<AllRandomReviews>)>]
let ``AnkiImporter imports RandomReviews`` randomReviews =
    use c = new AnkiTestContainer(randomReviews)
    c.AnkiDb()
    |> AnkiImporter.getSimpleAnkiDb
    |> assertHasHistory c.Db

[<Theory>]
[<ClassData(typeof<AllRandomReviews>)>]
let ``Importing AllRandomReviews reuses previous History`` randomReviews =
    use c = new AnkiTestContainer(randomReviews)
    for _ in [1..5] do
        c.AnkiDb()
        |> AnkiImporter.getSimpleAnkiDb
        |> assertHasHistory c.Db

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``Importing AnkiDb reuses previous CardOptions, PrivateTags, and ConceptTemplates`` _ simpleAnkiDb =
    use c = new TestContainer()
    let theCollectiveId = 2
    let userId = 3
    for _ in [1..5] do
        AnkiImporter.save c.Db simpleAnkiDb userId Map.empty
        |> Result.isOk
        |> Assert.True

    Assert.Equal(2, c.Db.CardOption.Count(fun x -> x.UserId = userId))
    Assert.Equal(4, c.Db.PrivateTag.Count(fun x -> x.UserId = userId))
    Assert.Equal(5, c.Db.ConceptTemplate.Count(fun x -> x.MaintainerId = theCollectiveId))
    Assert.Equal(5, c.Db.ConceptTemplateInstance.Count(fun x -> x.ConceptTemplate.MaintainerId = theCollectiveId))
    Assert.Equal(7, c.Db.CardTemplate.Count(fun x -> x.ConceptTemplateInstance.ConceptTemplate.MaintainerId = theCollectiveId))
    Assert.Equal(0, c.Db.ConceptTemplate.Count(fun x -> x.MaintainerId = userId))
    Assert.Equal(0, c.Db.ConceptTemplateInstance.Count(fun x -> x.ConceptTemplate.MaintainerId = userId))
    Assert.Equal(0, c.Db.CardTemplate.Count(fun x -> x.ConceptTemplateInstance.ConceptTemplate.MaintainerId = userId))
    Assert.Equal(8, c.Db.Concept.Count(fun x -> x.MaintainerId = userId))
    Assert.Equal(10, c.Db.Card.Count())
    Assert.Equal(1, c.Db.Card.Count(fun x -> x.ConceptInstance.FieldValues.Any(fun x -> x.Value = "Basic Front")))
    Assert.Equal(2, c.Db.Card.Count(fun x -> x.ConceptInstance.FieldValues.Any(fun x -> x.Value = "Basic (and reversed card) front")))
    Assert.Equal(2, c.Db.Card.Count(fun x -> x.ConceptInstance.FieldValues.Any(fun x -> x.Value = "Basic (optional reversed card) front")))
    Assert.Equal(10, c.Db.AcquiredCard.Count())
    Assert.Equal(1, c.Db.AcquiredCard.Count(fun x -> x.Card.ConceptInstance.FieldValues.Any(fun x -> x.Value = "Basic Front")))
    Assert.Equal(2, c.Db.AcquiredCard.Count(fun x -> x.Card.ConceptInstance.FieldValues.Any(fun x -> x.Value = "Basic (and reversed card) front")))
    Assert.Equal(2, c.Db.AcquiredCard.Count(fun x -> x.Card.ConceptInstance.FieldValues.Any(fun x -> x.Value = "Basic (optional reversed card) front")))

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``Importing AnkiDb, then again with different card lapses, updates db`` _ simpleAnkiDb =
    let lapseCountA = 13L
    let lapseCountB = 45L
    use c = new TestContainer()
    let userId = 3
    AnkiImporter.save c.Db simpleAnkiDb userId Map.empty
    |> Result.isOk
    |> Assert.True
    Assert.Equal(10, c.Db.AcquiredCard.Count(fun x -> x.LapseCount = 0uy))
    simpleAnkiDb.Cards |> List.iter (fun x -> x.Lapses <- lapseCountA)
    simpleAnkiDb.Cards.[0].Lapses <- lapseCountB

    AnkiImporter.save c.Db simpleAnkiDb userId Map.empty
    |> Result.isOk
    |> Assert.True

    Assert.Equal(9, c.Db.AcquiredCard.Count(fun x -> x.LapseCount = byte lapseCountA))
    Assert.Equal(1, c.Db.AcquiredCard.Count(fun x -> x.LapseCount = byte lapseCountB))

module AnkiImportTests

open FsToolkit.ErrorHandling
open CardOverflow.Sanitation
open System.Threading.Tasks
open FSharp.Control.Tasks
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
open FSharp.Text.RegexProvider
open ContainerExtensions
open SimpleInjector
open SimpleInjector.Lifestyles

[<Fact>]
let ``Import relationships has reduced Collates, also fieldvalue tests`` (): unit =
    let userId = 3
    let collates =
        AnkiImportTestData.relationships.Cols.Single().Models
        |> Anki.parseModels userId
        |> Result.getOk
        |> List.map snd
        |> List.groupBy (fun x -> x.AnkiId)
        |> List.map snd
    
    let myBasic = collates.[0].First()
    Assert.Equal(
        "Basic (optional reversed custom card) with source",
        myBasic.Name)
    Assert.Equal<string seq>(
        ["Front"; "Back"; "Front2"; "Back2"; "Source"],
        myBasic.Fields.Select(fun x -> x.Name))
    Assert.Equal(
        "{{Front}}\n\n<script>\nlet uls = document.getElementsByClassName(\"random\");\nlet ulsArray = Array.prototype.slice.call(uls);\n\nlet arrayLength = ulsArray.length;\nfor (let i = 0; i < arrayLength; i++) {\n  let lis = ulsArray[i].getElementsByTagName(\"li\");\n  let lisArray = Array.prototype.slice.call(lis);\n  shuffle(lisArray);\n\n  ulsArray[i].innerHTML = [].map.call(lisArray, function(node) {\n    return node.outerHTML;\n  }).join(\"\");\n}\n\n// http://stackoverflow.com/questions/6274339/how-can-i-shuffle-an-array-in-javascript\nfunction shuffle(a) {\n  let j, x, i;\n  for (i = a.length; i; i -= 1) {\n    j = Math.floor(Math.random() * i);\n    x = a[i - 1];\n    a[i - 1] = a[j];\n    a[j] = x;\n  }\n}\n\ndocument.onkeydown = function(evt) {\n  if (evt.keyCode == 90) {\n    // If you want to change the keyboard trigger, change the number http://keycode.info/ \n\n    let allDetails = document.getElementsByTagName('details');\n    for (i = 0; i < allDetails.length; i++) {\n      if (!allDetails[i].hasAttribute(\"open\")) {\n        allDetails[i].setAttribute('open', '');\n        break;\n      }\n    }\n  }\n};\n\n</script>",
        myBasic.Templates.[0].Front)
    Assert.Equal(
        "<div id=\"front\">\n{{FrontSide}}\n</div>\n\n<hr id=answer>\n\n{{Back}}\n\n<script>\nlet uls = document.getElementsByClassName(\"random\");\nlet ulsArray = Array.prototype.slice.call(uls);\n\nlet arrayLength = ulsArray.length;\nfor (let i = 0; i < arrayLength; i++) {\n  let lis = ulsArray[i].getElementsByTagName(\"li\");\n  let lisArray = Array.prototype.slice.call(lis);\n  shuffle(lisArray);\n\t\n  ulsArray[i].innerHTML = [].map.call(lisArray, function(node) {\n    return node.outerHTML;\n  }).join(\"\");\n}\n\n// http://stackoverflow.com/questions/6274339/how-can-i-shuffle-an-array-in-javascript\nfunction shuffle(a) {\n  let j, x, i;\n  for (i = a.length; i; i -= 1) {\n    j = Math.floor(Math.random() * i);\n    x = a[i - 1];\n    a[i - 1] = a[j];\n    a[j] = x;\n  }\n}\n\ndocument.onkeydown = function(evt) {\n  if (evt.keyCode == 90) {\n    // If you want to change the keyboard trigger, change the number http://keycode.info/ \n\n    let allDetails = document.getElementsByTagName('details');\n    for (i = 0; i < allDetails.length; i++) {\n      if (!allDetails[i].hasAttribute(\"open\")) {\n        allDetails[i].setAttribute('open', '');\n        break;\n      }\n    }\n  }\n};\n\nlet frontDetails = document.getElementById(\"front\").getElementsByTagName('details')\nfor (i = 0; i < frontDetails.length; i++) {\n  frontDetails[i].setAttribute('open', '');\n}\n\n</script>",
        myBasic.Templates.[0].Back)

    let sketchy = collates.[1]
    Assert.Equal<string seq>(
        ["SketchyPharm"],
        sketchy.Select(fun x -> x.Name) |> Seq.sort)
    Assert.Equal<string seq>(
        ["Entire Sketch"; "Extra"; "Extra 10"; "Extra 11"; "Extra 12"; "Extra 13";
         "Extra 14"; "Extra 15"; "Extra 16"; "Extra 17"; "Extra 18"; "Extra 19";
         "Extra 2"; "Extra 20"; "Extra 21"; "Extra 22"; "Extra 23"; "Extra 24";
         "Extra 25"; "Extra 26"; "Extra 27"; "Extra 28"; "Extra 29"; "Extra 3";
         "Extra 30"; "Extra 4"; "Extra 5"; "Extra 6"; "Extra 7"; "Extra 8"; "Extra 9";
         "Extra A"; "Extra A10"; "Extra A11"; "Extra A12"; "Extra A13"; "Extra A14";
         "Extra A15"; "Extra A16"; "Extra A17"; "Extra A18"; "Extra A19"; "Extra A2";
         "Extra A20"; "Extra A21"; "Extra A22"; "Extra A23"; "Extra A24"; "Extra A25";
         "Extra A26"; "Extra A27"; "Extra A28"; "Extra A29"; "Extra A3"; "Extra A30";
         "Extra A4"; "Extra A5"; "Extra A6"; "Extra A7"; "Extra A8"; "Extra A9";
         "Extra Q"; "Extra Q10"; "Extra Q11"; "Extra Q12"; "Extra Q13"; "Extra Q14";
         "Extra Q15"; "Extra Q16"; "Extra Q17"; "Extra Q18"; "Extra Q19"; "Extra Q2";
         "Extra Q20"; "Extra Q21"; "Extra Q22"; "Extra Q23"; "Extra Q24"; "Extra Q25";
         "Extra Q26"; "Extra Q27"; "Extra Q28"; "Extra Q29"; "Extra Q3"; "Extra Q30";
         "Extra Q4"; "Extra Q5"; "Extra Q6"; "Extra Q7"; "Extra Q8"; "Extra Q9";
         "More About This Topic"],
        sketchy |> List.map (fun x -> x.Fields |> List.map (fun x -> x.Name) |> List.sort)
        |> Seq.distinct |> Seq.exactlyOne)
    
    Assert.Equal(
        "<div class=textstyling>\n<font color=\"#DC143C\"></font><br><u>{{Extra Q}}</u>\n<br>\n<br>\n{{Extra A}}\n</div>\n\n\t\t\t\n<hr>\n<p style=\"font-size: 92%\"><i>\n{{Extra}}\n</i></hr>\n<hr color=\"white\">\n\n<div class=entiresketchstyle>\n{{Entire Sketch}}\n<br>\n{{hint:More About This Topic}}\n</div>\n\n\n\n\n\n\n\n\n",
        sketchy.Select(fun x -> x.Templates.[0].Back) |> Seq.distinct |> Seq.exactlyOne)
    Assert.Equal(
        "<div class=textstyling>\n{{#Extra Q}}<font color=\"#DC143C\"></font>\n<center>{{Extra Q}}</center>\n{{/Extra Q}}\n</div>\n\n",
        sketchy.Select(fun x -> x.Templates.[0].Front) |> Seq.distinct |> Seq.exactlyOne)

    let cloze = collates.[2].Single()
    Assert.Equal(
        "Cloze-Lightyear",
        cloze.Name)
    Assert.Equal<string seq>(
        ["Text"; "Extra"],
         cloze.Fields.Select(fun x -> x.Name))
    Assert.Equal(
        "{{cloze:Text}}",
        cloze.Templates.[0].Front)
    Assert.Equal(
        "{{cloze:Text}}<br>\n\n\n{{Extra}}\n",
        cloze.Templates.[0].Back)

    let cards, _ =
        let option = CardSettingsRepository.defaultCardSettings.CopyToNew userId
        AnkiImporter.load
            AnkiImportTestData.relationships
            userId
            Map.empty
            (fun _ -> [])
            (fun _ -> [])
            ([option].ToList())
            option
            (fun _ -> None)
            (fun _ -> None)
            (fun _ -> None)
            (fun _ -> None)
        |> Result.getOk
    let getFieldValues (collateName: string) =
        cards
            .Where(fun x -> x.BranchInstance.CollateInstance.Name.Contains collateName)
            .Select(fun x -> (BranchInstanceView.load x.BranchInstance).FieldValues.Select(fun x -> x.Value) |> List.ofSeq |> List.distinct) |> List.ofSeq |> List.distinct |> List.exactlyOne

    Assert.Equal<string list>(
        [   """What is the null hypothesis for the slope?"""
            """[$]H_0: \beta_1 = 0[/$]"""
            """What is the alternative hypothesis for the slope?"""
            """[$]H_0: \beta_1 \ne 0[/$]"""
            """https://classroom.udacity.com/courses/ud201/lessons/1309228537/concepts/1822139350923#"""],
        getFieldValues "Basic")
    Assert.Equal<string list>(
        ["{{c2::Toxic adenomas}} are thyroid nodules that usually contain a mutated {{c1::TSH receptor}}"; "<br /><div><br /></div><div><i>Multiple Toxic adenomas = Toxic multinodular goiter</i></div>"],
        getFieldValues "Cloze")
    let assertEqual (expecteds: string list) (actuals: string list) =
        for (expected, actual) in Seq.zip expecteds actuals do
            let stripUnicodeAndQuestionMark x = Regex.Replace(x, @"(?:[^\u0000-\u007F]|\?)+", "") // NCrunch can't deal with unicode here... seems to replace them with question marks
            Assert.Equal(
                stripUnicodeAndQuestionMark expected,
                stripUnicodeAndQuestionMark actual)
    ([ """How does a cytomegalovirus infection usually present in an HIV patient?"""
       """Retinitis ⇒ Hemorrhages and infiltrates"""
       """<img src="/missingImage.jpg" />"""
       """<b style="font-weight: bold; ">CD4</b>:&nbsp;CMV"""
       """CD4 &lt; 50"""
       """Why does Ganciclovir preferentially target CMV infected cells?&nbsp;"""
       """Activated by Viral Kinase UL97"""
       """What enzyme activates Ganciclovir?"""
       """Viral Kinase UL97<div><i><sup><br></sup></i></div><div><i><sup>(For first phosphorylation, then cellular kinase for second and third)</sup></i></div>"""
       """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
       """<b>MOA</b>: Ganciclovir"""
       """Guanosine nucleo<font color="#ff0000">s</font>ide analog<div><i><sup><br></sup></i></div><div><i><sup>(⇒ Inhibition of Viral DNA polymerase)</sup></i></div>"""
       """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" />&nbsp;"""
       """What is an orally administered version of Ganciclovir?"""
       """Valganciclovir<div><br></div><div><i><sup>(Val- prefix = prodrug)</sup></i></div>"""
       """Who is Ganciclovir indicated for?"""
       """High-risk transplant patients"""
       """<b>Adverse Effect</b>: Ganciclovir"""
       """Myelosuppression"""
       """An HIV patient is on Zidovudine as an anti-retroviral and Ganciclovir as prophylaxis for CMV. What lab values must you constantly follow?"""
       """WBC/RBC counts"""
       """What viruses is Foscarnet active against?"""
       """(1) HSV<div>(2) VZV</div><div>(3) CMV</div>"""
       """What organ does Cidofovir commonly damage?"""
       """Kidney<div><br></div><div><i><sup>(Foscarnet also commonly damages the kidney)</sup></i></div>"""
       """What electrolyte abnormalities are common with Foscarnet?"""
       """(1) ↓ Ca<sup>2+</sup><div>(2) ↓ Mg<sup>2+</sup></div><div>(3) ↓ K<sup>+</sup></div>"""
       """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
       """What antiviral is always administered with Probenecid?"""
       """Cidofovir"""
       """How does Probenecid protect the kidney from Cidofovir?"""
       """Inhibits tubular secretion<div><i><sup><br /></sup></i></div><div><i><sup>(∴ Reduced intra-lumenal Cidofovir concentration ⇒ Reduced toxicity)</sup></i></div>"""
       ""
       """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""
     ]
    , getFieldValues "Sketchy")
    ||> assertEqual

[<Fact>]
let ``Import relationships has relationships`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! r = AnkiImporter.save c.Db AnkiImportTestData.relationships userId Map.empty
    Assert.Null r.Value
    
    Assert.Equal(3, c.Db.Stack.Count())
    Assert.Equal(3, c.Db.BranchInstance.Count())
    Assert.Equal(AnkiDefaults.collateInstanceIdByHash.Count + 1, c.Db.Collate.Count())
    Assert.Equal(10, c.Db.CollateInstance.Count())

    let getInstances (collateName: string) =
        c.Db.CollateInstance
            .Include(fun x -> x.BranchInstances :> IEnumerable<_>)
                .ThenInclude(fun (x: BranchInstanceEntity) -> x.CollateInstance)
            .Where(fun x -> x.Name.Contains collateName)
            .SelectMany(fun x -> x.BranchInstances :> IEnumerable<_>)
            .ToListAsync()
    
    let! basic = getInstances "Basic"
    for branchInstance in basic do
        let! stack = ExploreStackRepository.get c.Db userId branchInstance.StackId
        let stack = stack.Value
        Assert.Empty stack.Relationships
        Assert.Empty stack.Default.Instance.CommunalFields
    
    let! sketchy = getInstances "Sketchy"
    let expectedFieldAndValues =
        ["Entire Sketch", """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""
         "More About This Topic","""<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""]
    for card in sketchy do
        let! stack = ExploreStackRepository.get c.Db userId card.StackId
        let stack = stack.Value
        Assert.Empty stack.Relationships
        Assert.Empty stack.Default.Instance.CommunalFields
        let! view = StackViewRepository.get c.Db stack.Id
        Assert.Equal(
            expectedFieldAndValues,
            view.Value.FieldValues
                .Where(fun x -> expectedFieldAndValues.Select(fun (field, _) -> field).Contains(x.Field.Name))
                .Select(fun x -> x.Field.Name, x.Value).OrderBy(fun x -> x))

    let! cloze = getInstances "Cloze"
    for instance in cloze do
        let! view = StackViewRepository.get c.Db instance.StackId
        [   "Text", "{{c2::Toxic adenomas}} are thyroid nodules that usually contain a mutated {{c1::TSH receptor}}"
            "Extra", "<br /><div><br /></div><div><i>Multiple Toxic adenomas = Toxic multinodular goiter</i></div>" ]
        |> fun expected -> Assert.Equal(expected, view.Value.FieldValues.Select(fun x -> x.Field.Name, x.Value))
        let instances = BranchInstanceMeta.loadAll true true instance
        Assert.Equal("Toxic adenomas are thyroid nodules that usually contain a mutated [ ... ]", instances.[0].StrippedFront)
        Assert.Equal("[ ... ] are thyroid nodules that usually contain a mutated TSH receptor", instances.[1].StrippedFront)
    }

[<Fact>]
let ``Can import myHighPriority, but really testing duplicate card collates`` (): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    do! AnkiImporter.save c.Db AnkiImportTestData.myHighPriority userId Map.empty
    
    Assert.Equal(2, c.Db.Stack.Count())
    Assert.Equal(2, c.Db.BranchInstance.Count())
    Assert.Equal(6, c.Db.Collate.Count())
    Assert.Equal(8, c.Db.CollateInstance.Count())
    Assert.Equal(0, c.Db.Relationship.Count())
    } |> TaskResult.getOk)

[<Theory>]
[<ClassData(typeof<AllDefaultCollatesAndImageAndMp3>)>]
let ``AnkiImporter can import AnkiImportTestData.All`` ankiFileName ankiDb: Task<unit> = task {
    use c = new TestContainer(false, ankiFileName)
    let userId = 3
    let! x = AnkiImporter.save c.Db ankiDb userId Map.empty
    Assert.Null x.Value
    Assert.Equal<IEnumerable<string>>(
        [   "4/23/2020 19:40:46"
            "4/23/2020 19:40:46"
            "4/8/2019 02:14:29"
            "4/8/2019 02:14:29"
            "4/8/2019 02:14:29"
            "4/8/2019 02:14:29"
            "4/8/2019 02:14:29"
        ].ToList(),
        c.Db.CollateInstance.AsEnumerable().Select(fun x -> x.Created.ToString("M/d/yyyy HH:mm:ss")).OrderBy(fun x -> x)
    )
    Assert.Equal<IEnumerable<string>>(
        [   "4/23/2020 19:40:46"
            "4/23/2020 19:40:46"
            "6/16/2019 00:51:28"
            "6/16/2019 00:51:28"
            "6/16/2019 00:51:46"
            "6/16/2019 00:51:55"
            "6/16/2019 00:53:30"
        ].ToList(),
        c.Db.CollateInstance.AsEnumerable().Select(fun x -> x.Modified.Value.ToString("M/d/yyyy HH:mm:ss")).OrderBy(fun x -> x)
    )
    Assert.Equal<IEnumerable<string>>(
        [   "4/8/2019 02:14:32"
            "4/8/2019 02:14:57"
            "4/8/2019 02:15:50"
            "4/8/2019 02:16:27"
            "4/8/2019 02:16:42"
            "4/8/2019 02:18:20"
            "4/8/2019 02:21:11"
            "6/16/2019 00:53:20"
        ].ToList(),
        c.Db.BranchInstance.AsEnumerable().Select(fun x -> x.Created.ToString("M/d/yyyy HH:mm:ss")).OrderBy(fun x -> x)
    )
    Assert.Equal<IEnumerable<string>>(
        [   "4/8/2019 02:14:53"
            "4/8/2019 02:15:44"
            "4/8/2019 02:16:22"
            "4/8/2019 02:16:39"
            "4/8/2019 02:18:05"
            "4/8/2019 02:38:52"
            "4/8/2019 02:43:51"
            "6/16/2019 00:56:27"
        ].ToList(),
        c.Db.BranchInstance.AsEnumerable().Select(fun x -> x.Modified.Value.ToString("M/d/yyyy HH:mm:ss")).OrderBy(fun x -> x)
    )
    Assert.Equal(8, c.Db.Stack.Count())
    Assert.Equal(10, c.Db.CollectedCard.Count(fun x -> x.UserId = userId))
    Assert.Equal(8, c.Db.User.Include(fun x -> x.CollectedCards).Single(fun x -> x.Id = userId).CollectedCards.Select(fun x -> x.BranchInstanceId).Distinct().Count())
    Assert.Equal(2, c.Db.CardSetting.Count(fun db -> db.UserId = userId))
    Assert.Equal<string seq>(
        [ "Default"; "Default Deck" ] |> Seq.sort,
        c.Db.Deck.Where(fun x -> x.UserId = userId).Select(fun x -> x.Name) |> Seq.sort
    )
    Assert.Equal<string>(
        [ "Basic"; "Othertag"; "Tag" ],
        (c.Db.Tag.ToList()).Select(fun x -> x.Name) |> Seq.sort)
    Assert.Equal<string>(
        [ "Othertag" ],
        c.Db.CollectedCard
            .Include(fun x -> x.BranchInstance)
            .Include(fun x -> x.Tag_CollectedCards :> IEnumerable<_>)
                .ThenInclude(fun (x: Tag_CollectedCardEntity) -> x.Tag)
            .Single(fun c -> c.BranchInstance.FieldValues.Contains("mp3"))
            .Tag_CollectedCards.Select(fun t -> t.Tag.Name)
            |> Seq.sort)
    Assert.Equal<string>(
        "Default",
        c.Db.CollectedCard
            .Include(fun x -> x.Deck)
            .Single(fun c -> c.BranchInstance.FieldValues.Contains("mp3"))
            .Deck.Name)

    let getInstances (collateName: string) =
        c.Db.CollateInstance
            .Include(fun x -> x.BranchInstances)
            .Where(fun x -> x.Name.Contains collateName)
            .SelectMany(fun x -> x.BranchInstances :> IEnumerable<_>)
            .ToListAsync()

    let! instances = getInstances "optional"
    for instance in instances do
        let! stack = ExploreStackRepository.get c.Db userId instance.StackId
        let stack = stack.Value
        Assert.Empty stack.Relationships
        Assert.Empty stack.Default.Instance.CommunalFields

    let! instances = getInstances "and reversed card)"
    for instance in instances do
        let! stack = ExploreStackRepository.get c.Db userId instance.StackId
        let stack = stack.Value
        Assert.Empty stack.Relationships
        Assert.Empty stack.Default.Instance.CommunalFields

    Assert.NotEmpty(c.Db.CollectedCard.Where(fun x -> x.Index = 1s))
    Assert.Equal(AnkiDefaults.collateInstanceIdByHash.Count - 1, c.Db.User_CollateInstance.Count(fun x -> x.UserId = userId))
    Assert.Equal(AnkiDefaults.collateInstanceIdByHash.Count, c.Db.CollateInstance.Count())
    Assert.Equal(AnkiDefaults.collateInstanceIdByHash.Count - 2, c.Db.LatestCollateInstance.Count())
    }

let assertHasHistory db ankiDb: Task<unit> = (taskResult {
    let userId = 3
    do! AnkiImporter.save db ankiDb userId Map.empty
    Assert.Equal(110, db.History.Count(fun x -> x.UserId = userId))
    } |> TaskResult.getOk)

type AllRandomReviews () =
    inherit XunitClassDataBase
        ([  [|"RandomReviews.colpkg" |]
            [|"RandomReviews-21.colpkg" |]
            [|"RandomReviews.apkg" |] ])

[<Theory>]
[<ClassData(typeof<AllRandomReviews>)>]
let ``AnkiImporter imports RandomReviews`` randomReviews: Task<unit> = task {
    use c = new AnkiTestContainer(randomReviews)
    do!
        c.AnkiDb()
        |> AnkiImporter.getSimpleAnkiDb
        |> assertHasHistory c.Db
    }

[<Theory>]
[<ClassData(typeof<AllRandomReviews>)>]
let ``Importing AllRandomReviews reuses previous History`` randomReviews: Task<unit> = task {
    use c = new AnkiTestContainer(randomReviews)
    for _ in [1..5] do
        do!
            c.AnkiDb()
            |> AnkiImporter.getSimpleAnkiDb
            |> assertHasHistory c.Db
    }

[<Fact>]
let ``110reviewsWithNoMatchingCards can be imported``() : Task<unit> = task {
    use c = new TestContainer()
    for _ in [1..5] do
        do!
            _110reviewsWithNoMatchingCards
            |> assertHasHistory c.Db
    }

[<Theory>]
[<ClassData(typeof<AllDefaultCollatesAndImageAndMp3>)>]
let ``Importing AnkiDb reuses old tags`` ankiFileName simpleAnkiDb: Task<unit> = (taskResult {
    use c = new TestContainer(false, ankiFileName)
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId [ "Tag"; "Deck:Default" ]
    Assert.Equal(2, c.Db.Tag.Count())

    do! AnkiImporter.save c.Db simpleAnkiDb userId Map.empty

    Assert.Equal(["Basic"; "Deck:Default"; "Othertag"; "Tag"], c.Db.Tag.Select(fun x -> x.Name).OrderBy(fun x -> x))
    } |> TaskResult.getOk)

[<Theory>]
[<ClassData(typeof<AllDefaultCollatesAndImageAndMp3>)>]
let ``Importing AnkiDb reuses previous CardSettings, Tags, and Collates`` ankiFileName simpleAnkiDb: Task<unit> = (taskResult {
    use c = new TestContainer(false, ankiFileName)
    let theCollectiveId = 2
    let userId = 3
    for _ in [1..5] do
        do! AnkiImporter.save c.Db simpleAnkiDb userId Map.empty
        Assert.Equal(2, c.Db.CardSetting.Count(fun x -> x.UserId = userId))
        Assert.Equal(3, c.Db.Tag.Count())
        Assert.Equal(5, c.Db.Collate.Count(fun x -> x.AuthorId = theCollectiveId))
        Assert.Equal(7, c.Db.CollateInstance.Count(fun x -> x.Collate.AuthorId = theCollectiveId))
        Assert.Equal(0, c.Db.Collate.Count(fun x -> x.AuthorId = userId))
        Assert.Equal(0, c.Db.CollateInstance.Count(fun x -> x.Collate.AuthorId = userId))
        Assert.Equal(0, c.Db.Collate.Count(fun x -> x.AuthorId = userId))
        Assert.Equal(8, c.Db.Stack.Count(fun x -> x.AuthorId = userId))
        Assert.Equal(8, c.Db.Stack.Count())
        Assert.Equal(2, c.Db.BranchInstance.Count(fun x -> EF.Functions.ILike(x.FieldValues, "%Basic Front%")))
        Assert.Equal(1, c.Db.BranchInstance.Count(fun x -> EF.Functions.ILike(x.FieldValues, "%Basic (and reversed card) front%")))
        Assert.Equal(1, c.Db.BranchInstance.Count(fun x -> EF.Functions.ILike(x.FieldValues, "%Basic (optional reversed card) front%")))
        Assert.Equal(10, c.Db.CollectedCard.Count())
        Assert.Equal(2, c.Db.CollectedCard.Count(fun x -> EF.Functions.ILike(x.BranchInstance.FieldValues, "%Basic Front%")))
        Assert.Equal(2, c.Db.CollectedCard.Count(fun x -> EF.Functions.ILike(x.BranchInstance.FieldValues, "%Basic (and reversed card) front%")))
        Assert.Equal(2, c.Db.CollectedCard.Count(fun x -> EF.Functions.ILike(x.BranchInstance.FieldValues, "%Basic (optional reversed card) front%")))
        Assert.NotEmpty(c.Db.CollectedCard.Where(fun x -> x.Index = 1s))
        Assert.Equal(0, c.Db.CommunalFieldInstance.Count())
        Assert.Equal(0, c.Db.CommunalField.Count())
        Assert.Equal(7, c.Db.CollateInstance.Count())
        Assert.Equal(5, c.Db.LatestCollateInstance.Count())
        Assert.Equal(4, c.Db.Deck.Count())
    } |> TaskResult.getOk)

[<Theory>]
[<ClassData(typeof<AllDefaultCollatesAndImageAndMp3>)>]
let ``Importing AnkiDb, then again with different card lapses, updates db`` ankiFileName simpleAnkiDb: Task<unit> = (taskResult {
    let easeFactorA = 13s
    let easeFactorB = 45s
    use c = new TestContainer(false, ankiFileName)
    let userId = 3
    do! AnkiImporter.save c.Db simpleAnkiDb userId Map.empty
    Assert.Equal(10, c.Db.CollectedCard.Count(fun x -> x.EaseFactorInPermille = 0s))
    simpleAnkiDb.Cards |> List.iter (fun x -> x.Factor <- int64 easeFactorA)
    simpleAnkiDb.Cards.[0].Factor <- int64 easeFactorB

    do! AnkiImporter.save c.Db simpleAnkiDb userId Map.empty

    Assert.Equal(9, c.Db.CollectedCard.Count(fun x -> x.EaseFactorInPermille = easeFactorA))
    Assert.Equal(1, c.Db.CollectedCard.Count(fun x -> x.EaseFactorInPermille = easeFactorB))
    Assert.NotEmpty(c.Db.CollectedCard.Where(fun x -> x.Index = 1s))
    } |> TaskResult.getOk)

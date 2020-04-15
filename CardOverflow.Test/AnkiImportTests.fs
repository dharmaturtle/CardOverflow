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
let ``Import relationships has reduced Templates, also fieldvalue tests`` (): unit =
    let userId = 3
    let templates =
        AnkiImportTestData.relationships.Cols.Single().Models
        |> Anki.parseModels userId
        |> Result.getOk
        |> List.collect snd
        |> List.groupBy (fun x -> x.AnkiId)
        |> List.map snd
    
    let myBasic = templates.[0].Single()
    Assert.Equal(
        "Basic (optional reversed custom card) with source - ReversibleForward",
        myBasic.Name)
    Assert.Equal<string seq>(
        ["Front"; "Back"; "Source"],
        myBasic.Fields.Select(fun x -> x.Name))
    Assert.Equal(
        "{{Front}}\n\n<script>\nlet uls = document.getElementsByClassName(\"random\");\nlet ulsArray = Array.prototype.slice.call(uls);\n\nlet arrayLength = ulsArray.length;\nfor (let i = 0; i < arrayLength; i++) {\n  let lis = ulsArray[i].getElementsByTagName(\"li\");\n  let lisArray = Array.prototype.slice.call(lis);\n  shuffle(lisArray);\n\n  ulsArray[i].innerHTML = [].map.call(lisArray, function(node) {\n    return node.outerHTML;\n  }).join(\"\");\n}\n\n// http://stackoverflow.com/questions/6274339/how-can-i-shuffle-an-array-in-javascript\nfunction shuffle(a) {\n  let j, x, i;\n  for (i = a.length; i; i -= 1) {\n    j = Math.floor(Math.random() * i);\n    x = a[i - 1];\n    a[i - 1] = a[j];\n    a[j] = x;\n  }\n}\n\ndocument.onkeydown = function(evt) {\n  if (evt.keyCode == 90) {\n    // If you want to change the keyboard trigger, change the number http://keycode.info/ \n\n    let allDetails = document.getElementsByTagName('details');\n    for (i = 0; i < allDetails.length; i++) {\n      if (!allDetails[i].hasAttribute(\"open\")) {\n        allDetails[i].setAttribute('open', '');\n        break;\n      }\n    }\n  }\n};\n\n</script>",
        myBasic.QuestionTemplate)
    Assert.Equal(
        "<div id=\"front\">\n{{FrontSide}}\n</div>\n\n<hr id=answer>\n\n{{Back}}\n\n<script>\nlet uls = document.getElementsByClassName(\"random\");\nlet ulsArray = Array.prototype.slice.call(uls);\n\nlet arrayLength = ulsArray.length;\nfor (let i = 0; i < arrayLength; i++) {\n  let lis = ulsArray[i].getElementsByTagName(\"li\");\n  let lisArray = Array.prototype.slice.call(lis);\n  shuffle(lisArray);\n\t\n  ulsArray[i].innerHTML = [].map.call(lisArray, function(node) {\n    return node.outerHTML;\n  }).join(\"\");\n}\n\n// http://stackoverflow.com/questions/6274339/how-can-i-shuffle-an-array-in-javascript\nfunction shuffle(a) {\n  let j, x, i;\n  for (i = a.length; i; i -= 1) {\n    j = Math.floor(Math.random() * i);\n    x = a[i - 1];\n    a[i - 1] = a[j];\n    a[j] = x;\n  }\n}\n\ndocument.onkeydown = function(evt) {\n  if (evt.keyCode == 90) {\n    // If you want to change the keyboard trigger, change the number http://keycode.info/ \n\n    let allDetails = document.getElementsByTagName('details');\n    for (i = 0; i < allDetails.length; i++) {\n      if (!allDetails[i].hasAttribute(\"open\")) {\n        allDetails[i].setAttribute('open', '');\n        break;\n      }\n    }\n  }\n};\n\nlet frontDetails = document.getElementById(\"front\").getElementsByTagName('details')\nfor (i = 0; i < frontDetails.length; i++) {\n  frontDetails[i].setAttribute('open', '');\n}\n\n</script>",
        myBasic.AnswerTemplate)

    let sketchy = templates.[1]
    Assert.Equal<string seq>(
        ["SketchyPharm - Card 36"; "SketchyPharm - Card 16"; "SketchyPharm - Card 30"],
        sketchy.Select(fun x -> x.Name))
    Assert.Equal<string seq seq>(
        [ seq["Extra Q3"; "Extra A3"; "Extra 3"; "More About This Topic"; "Entire Sketch"]
          seq["Extra Q"; "Extra A"; "Extra"; "More About This Topic"; "Entire Sketch"]
          seq["Extra Q2"; "Extra A2"; "Extra 2"; "More About This Topic"; "Entire Sketch"]],
        sketchy.Select(fun x -> x.Fields.Select(fun x -> x.Name)))
    Assert.Equal<string seq>(
        [   "<div class=textstyling>\n<font color=\"#DC143C\"></font><br><u>{{Extra Q3}}</u>\n<br>\n<br>\n{{Extra A3}}\n</div>\n\n<hr>\n<p style=\"font-size: 85%\"><i>\n{{Extra 3}}\n</i><br>\n<hr color=\"white\">\n<div class=entiresketchstyle>\n{{Entire Sketch}}\n<br>\n{{hint:More About This Topic}}\n</div>\n\n\n\n\n\n\n"
            "<div class=textstyling>\n<font color=\"#DC143C\"></font><br><u>{{Extra Q}}</u>\n<br>\n<br>\n{{Extra A}}\n</div>\n\n\t\t\t\n<hr>\n<p style=\"font-size: 92%\"><i>\n{{Extra}}\n</i></hr>\n<hr color=\"white\">\n\n<div class=entiresketchstyle>\n{{Entire Sketch}}\n<br>\n{{hint:More About This Topic}}\n</div>\n\n\n\n\n\n\n\n\n"
            "<div class=textstyling>\n<font color=\"#DC143C\"></font><br><u>{{Extra Q2}}</u>\n<br>\n<br>\n{{Extra A2}}\n</div>\n\n<hr>\n<p style=\"font-size: 85%\"><i>\n{{Extra 2}}\n</i><br>\n<hr color=\"white\">\n\n<div class=entiresketchstyle>\n{{Entire Sketch}}\n<br>\n{{hint:More About This Topic}}\n</div>\n\n\n\n\n\n\n\n" ],
        sketchy.Select(fun x -> x.AnswerTemplate))
    Assert.Equal<string seq>(
        [   "<div class=textstyling>\t\t\n{{#Extra Q3}}<font color=\"#DC143C\"></font><br>\n<center>{{Extra Q3}}</center>\n{{/Extra Q3}}\n</div>\n\n"
            "<div class=textstyling>\n{{#Extra Q}}<font color=\"#DC143C\"></font>\n<center>{{Extra Q}}</center>\n{{/Extra Q}}\n</div>\n\n"
            "<div class=textstyling>\n{{#Extra Q2}}<font color=\"#DC143C\"></font><br>\n<center>{{Extra Q2}} </center>\n{{/Extra Q2}}\n</div>\n\n" ],
        sketchy.Select(fun x -> x.QuestionTemplate))

    let cloze = templates.[2].Single()
    Assert.Equal(
        "Cloze-Lightyear",
        cloze.Name)
    Assert.Equal<string seq>(
        ["Text"; "Extra"],
         cloze.Fields.Select(fun x -> x.Name))
    Assert.Equal(
        "{{cloze:Text}}",
        cloze.QuestionTemplate)
    Assert.Equal(
        "{{cloze:Text}}<br>\n\n\n{{Extra}}\n",
        cloze.AnswerTemplate)

    let cards, _ =
        let option = CardSettingsRepository.defaultCardSettingsEntity userId
        AnkiImporter.load
            AnkiImportTestData.relationships
            userId
            Map.empty
            (fun _ -> [])
            ([option].ToList())
            option
            (fun _ -> None)
            (fun _ -> None)
            (fun _ -> None)
            (fun _ -> None)
        |> Result.getOk
    let getFieldValues (templateName: string) =
        cards
            .Where(fun x -> x.CardInstance.TemplateInstance.Name.Contains templateName)
            .Select(fun x -> (CardInstanceView.load x.CardInstance).FieldValues.Select(fun x -> x.Value))

    Assert.Equal<string seq>(
        [ seq["What is the null hypothesis for the slope?";        @"[$]H_0: \beta_1 = 0[/$]";   "https://classroom.udacity.com/courses/ud201/lessons/1309228537/concepts/1822139350923#"]
          seq["What is the alternative hypothesis for the slope?"; @"[$]H_0: \beta_1 \ne 0[/$]"; "https://classroom.udacity.com/courses/ud201/lessons/1309228537/concepts/1822139350923#"]],
        getFieldValues "Basic")
    Assert.Equal<string seq>(
        [ seq["Toxic adenomas are thyroid nodules that usually contain a mutated {{c1::TSH receptor}}"; "<br /><div><br /></div><div><i>Multiple Toxic adenomas = Toxic multinodular goiter</i></div>"]
          seq["{{c2::Toxic adenomas}} are thyroid nodules that usually contain a mutated TSH receptor"; "<br /><div><br /></div><div><i>Multiple Toxic adenomas = Toxic multinodular goiter</i></div>"]],
        getFieldValues "Cloze")
    let assertEqual (expectedss: string list list) (actualss: string seq seq) =
        for (expecteds, actuals) in Seq.zip expectedss actualss do
            for (expected, actual) in Seq.zip expecteds actuals do
                let stripUnicodeAndQuestionMark x = Regex.Replace(x, @"(?:[^\u0000-\u007F]|\?)+", "") // NCrunch can't deal with unicode here... seems to replace them with question marks
                Assert.Equal(
                    stripUnicodeAndQuestionMark expected,
                    stripUnicodeAndQuestionMark actual)
    ([  ["""How does a cytomegalovirus infection usually present in an HIV patient?"""
         """Retinitis ⇒ Hemorrhages and infiltrates"""
         """<img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""<b style="font-weight: bold; ">CD4</b>:&nbsp;CMV"""
         """CD4 &lt; 50"""
         """<img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""Why does Ganciclovir preferentially target CMV infected cells?&nbsp;"""
         """Activated by Viral Kinase UL97"""
         """<img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""What enzyme activates Ganciclovir?"""
         """Viral Kinase UL97<div><i><sup><br></sup></i></div><div><i><sup>(For first phosphorylation, then cellular kinase for second and third)</sup></i></div>"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""<b>MOA</b>: Ganciclovir"""
         """Guanosine nucleo<font color="#ff0000">s</font>ide analog<div><i><sup><br></sup></i></div><div><i><sup>(⇒ Inhibition of Viral DNA polymerase)</sup></i></div>"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" />&nbsp;"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""What is an orally administered version of Ganciclovir?"""
         """Valganciclovir<div><br></div><div><i><sup>(Val- prefix = prodrug)</sup></i></div>"""
         """<img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""Who is Ganciclovir indicated for?"""
         """High-risk transplant patients"""
         """<img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""<b>Adverse Effect</b>: Ganciclovir"""
         """Myelosuppression"""
         """<img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""An HIV patient is on Zidovudine as an anti-retroviral and Ganciclovir as prophylaxis for CMV. What lab values must you constantly follow?"""
         """WBC/RBC counts"""
         """<img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""What viruses is Foscarnet active against?"""
         """(1) HSV<div>(2) VZV</div><div>(3) CMV</div>"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""What organ does Cidofovir commonly damage?"""
         """Kidney<div><br></div><div><i><sup>(Foscarnet also commonly damages the kidney)</sup></i></div>"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""What electrolyte abnormalities are common with Foscarnet?"""
         """(1) ↓ Ca<sup>2+</sup><div>(2) ↓ Mg<sup>2+</sup></div><div>(3) ↓ K<sup>+</sup></div>"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""What antiviral is always administered with Probenecid?"""
         """Cidofovir"""
         """<img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
        ["""How does Probenecid protect the kidney from Cidofovir?"""
         """Inhibits tubular secretion<div><i><sup><br /></sup></i></div><div><i><sup>(∴ Reduced intra-lumenal Cidofovir concentration ⇒ Reduced toxicity)</sup></i></div>"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]]
    , getFieldValues "Sketchy")
    ||> assertEqual

[<Fact>]
let ``Import relationships has relationships`` (): Task<unit> = task {
    use c = new TestContainer()
    let userId = 3
    let! r = AnkiImporter.save c.Db AnkiImportTestData.relationships userId Map.empty
    Assert.Null r.Value
    
    Assert.Equal(18, c.Db.Card.Count())
    Assert.Equal(18, c.Db.CardInstance.Count())
    Assert.Equal(AnkiDefaults.templateIdByHash.Count + 5, c.Db.Template.Count())
    Assert.Equal(AnkiDefaults.templateIdByHash.Count + 5, c.Db.TemplateInstance.Count())

    let getInstances (templateName: string) =
        c.Db.TemplateInstance
            .Include(fun x -> x.CardInstances)
            .Where(fun x -> x.Name.Contains templateName)
            .SelectMany(fun x -> x.CardInstances :> IEnumerable<_>)
            .ToListAsync()
    
    let! basic = getInstances "Basic"
    for card in basic do
        let! card = ExploreCardRepository.get c.Db userId card.CardId
        let card = card.Value
        Assert.Empty card.Relationships
        let communalValue = "https://classroom.udacity.com/courses/ud201/lessons/1309228537/concepts/1822139350923#"
        Assert.Equal(
            { Id = 1001
              FieldName = "Source"
              Value = communalValue },
            card.Instance.CommunalFields.Single())
        let! command = SanitizeCardRepository.getEdit c.Db card.Instance.Id
        let command = Result.getOk command
        Assert.Equal<int seq>(
            basic.Select(fun x -> x.Id) |> Seq.sort,
            command.FieldValues |> Seq.collect (fun x -> x.CommunalCardInstanceIds) |> Seq.sort)
        Assert.Equal(
            communalValue,
            command.FieldValues.Single(fun x -> x.IsCommunal).Value)
    
    let! sketchy = getInstances "Sketchy"
    let expectedFieldAndValues =
        ["More About This Topic","""<img src="/missingImage.jpg" /><img src="/missingImage.jpg" /><img src="/missingImage.jpg" />"""
         "Entire Sketch", """8.2 - Ganciclovir, valganciclovir, foscarnet, cidofovir<img src="/missingImage.jpg" />"""]
    for card in sketchy do
        let! card = ExploreCardRepository.get c.Db userId card.CardId
        let card = card.Value
        Assert.Empty card.Relationships
        Assert.Equal(
            expectedFieldAndValues,
            card.Instance.CommunalFields.Select(fun x -> x.FieldName, x.Value))
        let! view = CardViewRepository.get c.Db card.Id
        Assert.Equal(
            expectedFieldAndValues,
            view.Value.FieldValues
                .Where(fun x -> expectedFieldAndValues.Select(fun (field, _) -> field).Contains(x.Field.Name))
                .Select(fun x -> x.Field.Name, x.Value))
        let! command = SanitizeCardRepository.getEdit c.Db card.Instance.Id
        Assert.Equal<int seq>(
            sketchy.Select(fun x -> x.Id).OrderBy(fun x -> x),
            command.Value.FieldValues |> Seq.collect (fun x -> x.CommunalCardInstanceIds) |> Seq.distinct |> Seq.sort)
        Assert.Equal<string seq>(
            expectedFieldAndValues |> List.map snd,
            command.Value.CommunalFieldValues.Select(fun x -> x.Value))

    let! cloze = getInstances "Cloze"
    for instance in cloze do
        let! view = CardViewRepository.get c.Db instance.CardId
        if instance.Id = 1002 then
            [   "Text", "Toxic adenomas are thyroid nodules that usually contain a mutated {{c1::TSH receptor}}"
                "Extra", "<br /><div><br /></div><div><i>Multiple Toxic adenomas = Toxic multinodular goiter</i></div>" ]
        else
            [   "Text", "{{c2::Toxic adenomas}} are thyroid nodules that usually contain a mutated TSH receptor"
                "Extra", "<br /><div><br /></div><div><i>Multiple Toxic adenomas = Toxic multinodular goiter</i></div>" ]
        |> fun expected -> Assert.Equal(expected, view.Value.FieldValues.OrderBy(fun x -> x.Field.Ordinal).Select(fun x -> x.Field.Name, x.Value))
        let! card = ExploreCardRepository.get c.Db userId instance.CardId
        let card = card.Value
        let communalValue = "{{c2::Toxic adenomas}} are thyroid nodules that usually contain a mutated {{c1::TSH receptor}}"
        Assert.Equal(
            {   Id = 1004
                FieldName = "Text"
                Value = communalValue },
            card.Instance.CommunalFields.Single(fun x -> x.FieldName = "Text"))
        if card.Id = 2 then
            Assert.Equal("Toxic adenomas are thyroid nodules that usually contain a mutated [ ... ]", card.Instance.StrippedFront)
        else
            Assert.Equal("[ ... ] are thyroid nodules that usually contain a mutated TSH receptor", card.Instance.StrippedFront)
        let! command = SanitizeCardRepository.getEdit c.Db card.Instance.Id
        let command = Result.getOk command
        Assert.Equal<int seq>(
            cloze.Select(fun x -> x.Id) |> Seq.sort,
            command.FieldValues |> Seq.collect (fun x -> x.CommunalCardInstanceIds) |> Seq.distinct |> Seq.sort)
        Assert.Equal<string seq>(
            [communalValue; "<br /><div><br /></div><div><i>Multiple Toxic adenomas = Toxic multinodular goiter</i></div>"],
            command.CommunalFieldValues.Select(fun x -> x.Value))
    }

[<Fact>]
let ``Can import myHighPriority, but really testing duplicate card templates`` (): Task<unit> = (taskResult {
    use c = new TestContainer()
    let userId = 3
    do! AnkiImporter.save c.Db AnkiImportTestData.myHighPriority userId Map.empty
    
    Assert.Equal(2, c.Db.Card.Count())
    Assert.Equal(2, c.Db.CardInstance.Count())
    Assert.Equal(6, c.Db.Template.Count())
    Assert.Equal(6, c.Db.TemplateInstance.Count())
    Assert.Equal(0, c.Db.Relationship.Count())
    } |> TaskResult.assertOk)

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``AnkiImporter can import AnkiImportTestData.All`` ankiFileName ankiDb: Task<unit> = task {
    use c = new TestContainer(false, ankiFileName)
    let userId = 3
    let! x = AnkiImporter.save c.Db ankiDb userId Map.empty
    Assert.Null x.Value
    Assert.Equal<IEnumerable<string>>(
        [   "4/8/2019 02:14:29"
            "4/8/2019 02:14:29"
            "4/8/2019 02:14:29"
            "4/8/2019 02:14:29"
            "4/8/2019 02:14:29"
        ].ToList(),
        c.Db.TemplateInstance.AsEnumerable().Select(fun x -> x.Created.ToString("M/d/yyyy HH:mm:ss")).OrderBy(fun x -> x)
    )
    Assert.Equal<IEnumerable<string>>(
        [   "6/16/2019 00:51:28"
            "6/16/2019 00:51:32"
            "6/16/2019 00:51:46"
            "6/16/2019 00:51:55"
            "6/16/2019 00:53:30"
        ].ToList(),
        c.Db.TemplateInstance.AsEnumerable().Select(fun x -> x.Modified.Value.ToString("M/d/yyyy HH:mm:ss")).OrderBy(fun x -> x)
    )
    Assert.Equal<IEnumerable<string>>(
        [   "4/8/2019 02:14:32"
            "4/8/2019 02:14:57"
            "4/8/2019 02:14:57"
            "4/8/2019 02:15:50"
            "4/8/2019 02:15:50"
            "4/8/2019 02:16:27"
            "4/8/2019 02:16:42"
            "4/8/2019 02:18:20"
            "4/8/2019 02:21:11"
            "6/16/2019 00:53:20"
        ].ToList(),
        c.Db.CardInstance.AsEnumerable().Select(fun x -> x.Created.ToString("M/d/yyyy HH:mm:ss")).OrderBy(fun x -> x)
    )
    Assert.Equal<IEnumerable<string>>(
        [   "4/8/2019 02:14:53"
            "4/8/2019 02:15:44"
            "4/8/2019 02:15:44"
            "4/8/2019 02:16:22"
            "4/8/2019 02:16:22"
            "4/8/2019 02:16:39"
            "4/8/2019 02:18:05"
            "4/8/2019 02:38:52"
            "4/8/2019 02:43:51"
            "6/16/2019 00:56:27"
        ].ToList(),
        c.Db.CardInstance.AsEnumerable().Select(fun x -> x.Modified.Value.ToString("M/d/yyyy HH:mm:ss")).OrderBy(fun x -> x)
    )
    Assert.Equal(10, c.Db.Card.Count())
    Assert.Equal(10, c.Db.AcquiredCard.Count(fun x -> x.UserId = userId))
    Assert.Equal(10, c.Db.User.Include(fun x -> x.AcquiredCards).Single(fun x -> x.Id = userId).AcquiredCards.Select(fun x -> x.CardInstanceId).Distinct().Count())
    Assert.Equal(2, c.Db.CardSetting.Count(fun db -> db.UserId = userId))
    Assert.Equal(5, c.Db.User_TemplateInstance.Count(fun x -> x.UserId = userId))
    Assert.Equal<string>(
        [ "Basic"; "Deck:Default"; "OtherTag"; "Tag" ],
        (c.Db.Tag.ToList()).Select(fun x -> x.Name) |> Seq.sort)
    Assert.Equal<string>(
        [ "Deck:Default"; "OtherTag" ],
        c.Db.AcquiredCard
            .Include(fun x -> x.CardInstance)
            .Include(fun x -> x.Tag_AcquiredCards :> IEnumerable<_>)
                .ThenInclude(fun (x: Tag_AcquiredCardEntity) -> x.Tag)
            .Single(fun c -> c.CardInstance.FieldValues.Contains("mp3"))
            .Tag_AcquiredCards.Select(fun t -> t.Tag.Name)
            |> Seq.sort)

    let getInstances (templateName: string) =
        c.Db.TemplateInstance
            .Include(fun x -> x.CardInstances)
            .Where(fun x -> x.Name.Contains templateName)
            .SelectMany(fun x -> x.CardInstances :> IEnumerable<_>)
            .ToListAsync()

    let! instances = getInstances "optional"
    for instance in instances do
        let! card = ExploreCardRepository.get c.Db userId instance.CardId
        let card = card.Value
        Assert.Empty <| card.Relationships
        Assert.Equal(
            [ { Id = 1001
                FieldName = "Back"
                Value = "Basic (optional reversed card) back" }
              { Id = 1006
                FieldName = "Front"
                Value = "Basic (optional reversed card) front" }
              { Id = 1007
                FieldName = "Add Reverse"
                Value = "Basic (optional reversed card) reverse" }],
            card.Instance.CommunalFields)

    let! instances = getInstances "and reversed card)"
    for instance in instances do
        let! card = ExploreCardRepository.get c.Db userId instance.CardId
        let card = card.Value
        Assert.Empty card.Relationships
        Assert.Equal(
            [ { Id = 1002
                FieldName = "Back"
                Value = "Basic (and reversed card) back" }
              { Id = 1003
                FieldName = "Front"
                Value = "Basic (and reversed card) front" }],
            card.Instance.CommunalFields)

    Assert.NotEmpty(c.Db.CardInstance.Where(fun x -> x.AnkiNoteOrd = Nullable 1s))
    Assert.Equal(c.Db.LatestTemplateInstance.Count(), c.Db.TemplateInstance.Count())
    }

let assertHasHistory db ankiDb: Task<unit> = (taskResult {
    let userId = 3
    do! AnkiImporter.save db ankiDb userId Map.empty
    Assert.Equal(110, db.History.Count(fun x -> x.AcquiredCard.UserId = userId))
    } |> TaskResult.assertOk)

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
let ``Tag collation is case insensitive and .Contains works as expected``() : Task<unit> = task { // if we ever change the collation, make sure all .Contains everywhere also work (like the one in AnkiImporter) medTODO remove after ElasticSearch
    use c = new TestContainer()
    let userId = 3
    let upperGuid = Guid.NewGuid().ToString().ToUpper()
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId [ upperGuid ]

    TagRepository.Search c.Db <| upperGuid.ToLower()
    
    |> Assert.SingleI }

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``Importing AnkiDb reuses old tags`` ankiFileName simpleAnkiDb: Task<unit> = (taskResult {
    use c = new TestContainer(false, ankiFileName)
    let userId = 3
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId [ "Tag"; "Deck:Default" ]
    Assert.Equal(2, c.Db.Tag.Count())

    do! AnkiImporter.save c.Db simpleAnkiDb userId Map.empty

    Assert.Equal(["Basic"; "Deck:Default"; "OtherTag"; "Tag"], c.Db.Tag.Select(fun x -> x.Name).OrderBy(fun x -> x))
    } |> TaskResult.assertOk)

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``Importing AnkiDb reuses previous CardSettings, Tags, and Templates`` ankiFileName simpleAnkiDb: Task<unit> = (taskResult {
    use c = new TestContainer(false, ankiFileName)
    let theCollectiveId = 2
    let userId = 3
    for _ in [1..5] do
        do! AnkiImporter.save c.Db simpleAnkiDb userId Map.empty
        Assert.Equal(2, c.Db.CardSetting.Count(fun x -> x.UserId = userId))
        Assert.Equal(4, c.Db.Tag.Count())
        Assert.Equal(5, c.Db.Template.Count(fun x -> x.AuthorId = theCollectiveId))
        Assert.Equal(5, c.Db.TemplateInstance.Count(fun x -> x.Template.AuthorId = theCollectiveId))
        Assert.Equal(0, c.Db.Template.Count(fun x -> x.AuthorId = userId))
        Assert.Equal(0, c.Db.TemplateInstance.Count(fun x -> x.Template.AuthorId = userId))
        Assert.Equal(0, c.Db.Template.Count(fun x -> x.AuthorId = userId))
        Assert.Equal(10, c.Db.Card.Count(fun x -> x.AuthorId = userId))
        Assert.Equal(10, c.Db.Card.Count())
        Assert.Equal(2, c.Db.CardInstance.Count(fun x -> EF.Functions.ILike(x.FieldValues, "%Basic Front%")))
        Assert.Equal(2, c.Db.CardInstance.Count(fun x -> EF.Functions.ILike(x.FieldValues, "%Basic (and reversed card) front%")))
        Assert.Equal(2, c.Db.CardInstance.Count(fun x -> EF.Functions.ILike(x.FieldValues, "%Basic (optional reversed card) front%")))
        Assert.Equal(10, c.Db.AcquiredCard.Count())
        Assert.Equal(2, c.Db.AcquiredCard.Count(fun x -> EF.Functions.ILike(x.CardInstance.FieldValues, "%Basic Front%")))
        Assert.Equal(2, c.Db.AcquiredCard.Count(fun x -> EF.Functions.ILike(x.CardInstance.FieldValues, "%Basic (and reversed card) front%")))
        Assert.Equal(2, c.Db.AcquiredCard.Count(fun x -> EF.Functions.ILike(x.CardInstance.FieldValues, "%Basic (optional reversed card) front%")))
        Assert.NotEmpty(c.Db.CardInstance.Where(fun x -> x.AnkiNoteOrd = Nullable 1s))
        Assert.Equal(7, c.Db.CommunalFieldInstance.Count())
        Assert.Equal(7, c.Db.CommunalField.Count())
        Assert.Equal(c.Db.LatestTemplateInstance.Count(), c.Db.TemplateInstance.Count())
    } |> TaskResult.assertOk)

[<Theory>]
[<ClassData(typeof<AllDefaultTemplatesAndImageAndMp3>)>]
let ``Importing AnkiDb, then again with different card lapses, updates db`` ankiFileName simpleAnkiDb: Task<unit> = (taskResult {
    let easeFactorA = 13s
    let easeFactorB = 45s
    use c = new TestContainer(false, ankiFileName)
    let userId = 3
    do! AnkiImporter.save c.Db simpleAnkiDb userId Map.empty
    Assert.Equal(10, c.Db.AcquiredCard.Count(fun x -> x.EaseFactorInPermille = 0s))
    simpleAnkiDb.Cards |> List.iter (fun x -> x.Factor <- int64 easeFactorA)
    simpleAnkiDb.Cards.[0].Factor <- int64 easeFactorB

    do! AnkiImporter.save c.Db simpleAnkiDb userId Map.empty

    Assert.Equal(9, c.Db.AcquiredCard.Count(fun x -> x.EaseFactorInPermille = easeFactorA))
    Assert.Equal(1, c.Db.AcquiredCard.Count(fun x -> x.EaseFactorInPermille = easeFactorB))
    Assert.NotEmpty(c.Db.CardInstance.Where(fun x -> x.AnkiNoteOrd = Nullable 1s))
    } |> TaskResult.assertOk)

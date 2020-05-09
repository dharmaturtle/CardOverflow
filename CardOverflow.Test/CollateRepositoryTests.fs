module CollateRepositoryTests

open FsToolkit.ErrorHandling
open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Debug
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open CardOverflow.Test
open System
open System.Linq
open Xunit
open CardOverflow.Pure
open CardOverflow.Pure.Core
open System.Collections.Generic
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Sanitation

[<Fact>]
let ``CollateRepository.UpdateFieldsToNewInstance works``(): Task<unit> = task {
    let collateId = 1
    let userId = 3
    use c = new TestContainer()
    
    let! collate = SanitizeCollate.AllInstances c.Db collateId
    let latestInstance = collate.Value.Instances |> Seq.maxBy (fun x -> x.Modified |?? lazy x.Created)
    
    Assert.Equal(
        "Basic",
        collate.Value.Instances.Single().Name)
    Assert.Equal<string seq>(
        ["Front"; "Back"],
        latestInstance.Fields.OrderBy(fun x -> x.Ordinal).Select(fun x -> x.Name))
    Assert.Equal(
        "{{Front}}",
        latestInstance.QuestionXemplate)
    Assert.Equal(1, c.Db.CollateInstance.Count(fun x -> x.CollateId = collateId))

    // Testing UpdateFieldsToNewInstance
    let! _ = FacetRepositoryTests.addBasicCard c.Db userId []
    let newQuestionXemplate = "modified {{Front mutated}}"
    let newCollateName = "new name"
    let updated =
        { latestInstance with
            Name = newCollateName
            QuestionXemplate = newQuestionXemplate
            Fields = latestInstance.Fields |> Seq.map (fun x -> { x with Name = x.Name + " mutated" }) |> toResizeArray
        } |> ViewCollateInstance.copyTo
    
    do! CollateRepository.UpdateFieldsToNewInstance c.Db userId updated

    let! collate = SanitizeCollate.AllInstances c.Db collateId
    let latestInstance = collate.Value.Instances |> Seq.maxBy (fun x -> x.Created)
    Assert.Equal(
        newQuestionXemplate,
        latestInstance.QuestionXemplate)
    Assert.Equal(
        newCollateName,
        latestInstance.Name)
    Assert.Equal<string seq>(
        ["Front mutated"; "Back mutated"],
        latestInstance.Fields.OrderBy(fun x -> x.Ordinal).Select(fun x -> x.Name))
    Assert.Equal(userId, c.Db.AcquiredCard.Single().UserId)
    Assert.Equal(1002, c.Db.AcquiredCard.Single().BranchInstanceId)
    Assert.Equal(
        latestInstance.Id,
        c.Db.AcquiredCard.Include(fun x -> x.BranchInstance).Single().BranchInstance.CollateInstanceId)
    Assert.Equal(2, c.Db.CollateInstance.Count(fun x -> x.CollateId = collateId))
    Assert.Equal(2, c.Db.BranchInstance.Count())
    Assert.Equal(2, c.Db.BranchInstance.Count(fun x -> x.Branch.CardId = 1))
    let createds = c.Db.CollateInstance.Where(fun x -> x.CollateId = collateId).Select(fun x -> x.Created) |> Seq.toList
    Assert.NotEqual(createds.[0], createds.[1])
    let! x = CardViewRepository.get c.Db 1
    let front, _, _, _ = x.Value.FrontBackFrontSynthBackSynth
    Assert.Equal(
        """<!DOCTYPE html>
    <head>
        <style>
            .cloze-brackets-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-filler-front {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: dodgerblue;
            }
            .cloze-brackets-back {
                font-size: 150%;
                font-family: monospace;
                font-weight: bolder;
                color: red;
            }
        </style>
        <style>
            .card {
 font-family: arial;
 font-size: 20px;
 text-align: center;
 color: black;
 background-color: white;
}

        </style>
    </head>
    <body>
        modified Front
        <script type="text/javascript" src="/js/iframeResizer.contentWindow.min.js"></script> 
    </body>
</html>""",
        front,
        false, true
    )

    // test existing
    let testView getView id expectedFront expectedBack = task {
        let! (actual: Result<CollateInstance, string>) = getView c.Db id
        let front, back, _, _ = actual.Value.FrontBackFrontSynthBackSynth
        BusinessLogicTests.assertStripped expectedFront front
        BusinessLogicTests.assertStripped expectedBack back
    }
    
    do! testView CollateRepository.latest collateId newQuestionXemplate <| newQuestionXemplate + " {{Back}}"

    let priorInstance = collate.Value.Instances |> Seq.minBy (fun x -> x.Created)
    do! testView CollateRepository.instance priorInstance.Id "{{Front}}" "{{Front}} {{Back}}"

    // test missing
    let testViewError getView id expected =
        getView c.Db id
        |> Task.map(fun (x: Result<_, _>) -> Assert.Equal(expected, x.error))
    do! testViewError CollateRepository.latest 0 "Collate #0 not found"
    do! testViewError CollateRepository.instance 0 "Collate Instance #0 not found"
    }

module GromplateRepositoryTests

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
open System.Collections.Generic
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Sanitation

[<Fact>]
let ``GromplateRepository.UpdateFieldsToNewLeaf works``(): Task<unit> = task {
    let userId = 3
    use c = new TestContainer()
    
    let gromplateId = c.Db.Gromplate.Single(fun x -> x.Grompleafs.Any(fun x -> x.Name = "Basic")).Id
    let! gromplate = SanitizeGromplate.AllLeafs c.Db gromplateId
    let latestLeaf = gromplate.Value.Leafs |> Seq.maxBy (fun x -> x.Modified |?? lazy x.Created)
    
    Assert.Equal(
        "Basic",
        gromplate.Value.Leafs.Single().Name)
    Assert.Equal<string seq>(
        ["Front"; "Back"],
        latestLeaf.Fields.Select(fun x -> x.Name))
    Assert.Equal(
        "{{Front}}",
        latestLeaf.FirstTemplate.Front)
    Assert.Equal(1, c.Db.Grompleaf.Count(fun x -> x.GromplateId = gromplateId))

    // Testing UpdateFieldsToNewLeaf
    let! _ = FacetRepositoryTests.addBasicStack c.Db userId []
    let newQuestionXemplate = "modified {{Front mutated}}"
    let newGromplateName = "new name"
    let updated =
        { latestLeaf with
            Name = newGromplateName
            Templates =
                {   latestLeaf.FirstTemplate with
                        Front = newQuestionXemplate
                } |> List.singleton |> Standard
            Fields = latestLeaf.Fields |> Seq.map (fun x -> { x with Name = x.Name + " mutated" }) |> toResizeArray
        } |> ViewGrompleaf.copyTo
    
    do! GromplateRepository.UpdateFieldsToNewLeaf c.Db userId updated

    let! gromplate = SanitizeGromplate.AllLeafs c.Db gromplateId
    let latestLeaf = gromplate.Value.Leafs |> Seq.maxBy (fun x -> x.Created)
    Assert.Equal(
        newQuestionXemplate,
        latestLeaf.FirstTemplate.Front)
    Assert.Equal(
        newGromplateName,
        latestLeaf.Name)
    Assert.Equal<string seq>(
        ["Front mutated"; "Back mutated"],
        latestLeaf.Fields.Select(fun x -> x.Name))
    Assert.Equal(userId, c.Db.Card.Single().UserId)
    Assert.Equal(1002, c.Db.Card.Single().LeafId)
    Assert.Equal(
        latestLeaf.Id,
        c.Db.Card.Include(fun x -> x.Leaf).Single().Leaf.GrompleafId)
    Assert.Equal(2, c.Db.Grompleaf.Count(fun x -> x.GromplateId = gromplateId))
    Assert.Equal(2, c.Db.Leaf.Count())
    Assert.Equal(2, c.Db.Leaf.Count(fun x -> x.Branch.StackId = 1))
    Assert.Equal(2, c.Db.Leaf.Count(fun x -> x.StackId = 1))
    let createds = c.Db.Grompleaf.Where(fun x -> x.GromplateId = gromplateId).Select(fun x -> x.Created) |> Seq.toList
    Assert.NotEqual(createds.[0], createds.[1])
    let! x = StackViewRepository.get c.Db 1
    let front, _, _, _ = x.Value.FrontBackFrontSynthBackSynth.[0]
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
        let! (actual: Result<Grompleaf, string>) = getView c.Db id
        let front, back, _, _ = actual.Value.FrontBackFrontSynthBackSynth() |> Seq.exactlyOne
        BusinessLogicTests.assertStripped expectedFront front
        BusinessLogicTests.assertStripped expectedBack back
    }
    
    do! testView GromplateRepository.latest gromplateId newQuestionXemplate <| newQuestionXemplate + " {{Back}}"

    let priorLeaf = gromplate.Value.Leafs |> Seq.minBy (fun x -> x.Created)
    do! testView GromplateRepository.leaf priorLeaf.Id "{{Front}}" "{{Front}} {{Back}}"

    // test missing
    let testViewError getView id expected =
        getView c.Db id
        |> Task.map(fun (x: Result<_, _>) -> Assert.Equal(expected, x.error))
    do! testViewError GromplateRepository.latest 0 "Gromplate #0 not found"
    do! testViewError GromplateRepository.leaf 0 "Gromplate Leaf #0 not found"
    }

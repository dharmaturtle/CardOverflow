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
    let userId = user_2
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
    let! _ = FacetRepositoryTests.addBasicConcept c.Db userId [] (concept_1, branch_1, leaf_1, [card_1])
    let newQuestionXemplate = "modified {{Front mutated}}"
    let newGromplateName = "new name"
    let oldLeafId = c.Db.Card.Single().LeafId
    let newLeafId = Ulid.create
    let updated =
        { latestLeaf with
            Id = newLeafId
            GromplateId = gromplateId
            Name = newGromplateName
            Templates =
                {   latestLeaf.FirstTemplate with
                        Front = newQuestionXemplate
                } |> List.singleton |> Standard
            Fields = latestLeaf.Fields |> Seq.map (fun x -> { x with Name = x.Name + " mutated" }) |> toResizeArray
        }
    
    let! r = SanitizeGromplate.Update c.Db userId updated
    Assert.Null r.Value

    let! gromplate = SanitizeGromplate.AllLeafs c.Db gromplateId
    let latestLeaf = gromplate.Value.Leafs.Single(fun x -> x.Id = newLeafId)
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
    Assert.NotEqual(oldLeafId, c.Db.Card.Single().LeafId)
    Assert.Equal(
        latestLeaf.Id,
        c.Db.Card.Include(fun x -> x.Leaf).Single().Leaf.GrompleafId)
    Assert.Equal(2, c.Db.Grompleaf.Count(fun x -> x.GromplateId = gromplateId))
    Assert.Equal(2, c.Db.Leaf.Count())
    let concept_1 = c.Db.Concept.Single().Id
    Assert.Equal(2, c.Db.Leaf.Count(fun x -> x.Branch.ConceptId = concept_1))
    Assert.Equal(2, c.Db.Leaf.Count(fun x -> x.ConceptId = concept_1))
    let createds = c.Db.Grompleaf.Where(fun x -> x.GromplateId = gromplateId).Select(fun x -> x.Created) |> Seq.toList
    Assert.NotEqual(createds.[0], createds.[1])
    let! x = ConceptViewRepository.get c.Db concept_1
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
    let gromplateMissingId = Ulid.create
    do! testViewError GromplateRepository.latest gromplateMissingId <| sprintf "Gromplate #%A not found" gromplateMissingId // TODO base64
    let grompleafMissingId = Ulid.create
    do! testViewError GromplateRepository.leaf   grompleafMissingId <| sprintf "Gromplate Leaf #%A not found" grompleafMissingId // TODO base64
    }

module EditCardCommandTests

open CardOverflow.Api
open ContainerExtensions
open LoadersAndCopiers
open Helpers
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Test
open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Quotations
open System.Linq
open Xunit
open System
open SimpleInjector
open SimpleInjector.Lifestyles
open CardOverflow.Sanitation
open System.Threading.Tasks
open FSharp.Control.Tasks
open System.Security.Cryptography
open System.Collections.Generic
open FsToolkit.ErrorHandling

let clozeFields =
    function
    | Cloze t -> ClozeTemplateRegex().TypedMatches(t.Front).Select(fun x -> x.fieldName.Value) |> Seq.toList
    | _ -> failwith "impossible"

let test text expected collateInstance =
    let view =
        {   EditSummary = ""
            FieldValues =
                CollateInstance.initialize.Fields.Select(fun f -> {
                    EditField = f
                    Value =
                        if f.Name = "Front" then
                            text
                        else
                            f.Name
                }).ToList()
            CollateInstance = collateInstance
            Kind = NewOriginal_TagIds []
        }
    if collateInstance.FirstTemplate.Name = "Cloze" then
        Assert.Equal<string seq>(["Front"], clozeFields view.CollateInstance.Templates)
    view.Backs.Value
    |> Seq.map MappingTools.stripHtmlTags
    |> fun x -> Assert.Equal<string seq>(expected, x)

[<Fact>]
let ``EditCardCommand's back works with basic`` (): unit =
    let testOrdinary text expected =
        test text expected
            ({ CollateInstance.initialize with
                Templates =
                {   Name = "Basic"
                    Front = "{{Front}}"
                    Back = "{{Front}} {{Back}}"
                    ShortFront = ""
                    ShortBack = ""
                } |> List.singleton |>Standard
            } |> ViewCollateInstance.load)
    testOrdinary
        "The front"
        [ "The front Back" ]

[<Fact>]
let ``EditCardCommand's back works with cloze`` (): unit =
    let testCloze text expected =
        test text expected
            ({ CollateInstance.initialize with
                Templates =
                    {   Name = "Cloze"
                        Front = "{{cloze:Front}}"
                        Back = "{{cloze:Front}} {{Back}}"
                        ShortFront = ""
                        ShortBack = ""
                    } |> Cloze
            } |> ViewCollateInstance.load)
    testCloze
        "{{c1::Canberra::city}} was founded in {{c1::1913}}."
        [   "[ Canberra ] was founded in [ 1913 ] . Back" ]
    testCloze
        "{{c2::Canberra::city}} was founded in {{c1::1913}}."
        [   "Canberra was founded in [ 1913 ] . Back"
            "[ Canberra ] was founded in 1913. Back" ]

    let testMultiCloze front back expectedBack = // https://eshapard.github.io/anki/the-power-of-making-new-cards-on-the-fly-in-anki.html
        let view =
            {   EditSummary = ""
                FieldValues =
                    CollateInstance.initialize.Fields.Select(fun f -> {
                        EditField = f
                        Value =
                            match f.Name with
                            | "Front" -> front
                            | "Back" -> back
                            | _ -> "Source goes here"
                    }).ToList()
                CollateInstance =
                    {   CollateInstance.initialize with
                            Templates =
                                {   CollateInstance.initialize.FirstTemplate with
                                        Front = "{{cloze:Front}}{{cloze:Back}}"
                                        Back = "{{cloze:Front}}{{cloze:Back}}{{Source}}"
                                } |> Cloze
                    } |> ViewCollateInstance.load
                Kind = NewOriginal_TagIds []
            }
        Assert.Equal<string seq>(["Front"; "Back"], clozeFields view.CollateInstance.Templates)
        view.Backs.Value
        |> Seq.map MappingTools.stripHtmlTags
        |> fun x -> Assert.Equal<string seq>(expectedBack, x)
    testMultiCloze
        "Columbus first crossed the Atlantic in {{c1::1492}}"
        ""
        ["Columbus first crossed the Atlantic in [ 1492 ] Source goes here"]
    testMultiCloze
        "Columbus first crossed the Atlantic in {{c1::1492}}"
        "In {{c2::1492}}, Columbus sailed the ocean {{c3::blue}}."
        [   "Columbus first crossed the Atlantic in [ 1492 ] Source goes here"
            "In [ 1492 ] , Columbus sailed the ocean blue.Source goes here"
            "In 1492, Columbus sailed the ocean [ blue ] .Source goes here" ]

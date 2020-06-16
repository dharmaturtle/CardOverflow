namespace CardOverflow.Test

open LoadersAndCopiers
open CardOverflow.Api
open CardOverflow.Test
open CardOverflow.Debug
open CardOverflow.Pure
open Xunit
open System
open System.Linq
open System.ComponentModel.DataAnnotations
open DataAnnotationsValidator
open FsCheck
open FsCheck.Xunit

module Gen =
    let gen<'a> = // uses the reflection based default generator instead of the registered generator
        Arb.Default.Derive<'a>()
        |> Arb.toGen
    let genMap<'a> f: Gen<'a> =
        gen<'a>
        |> Gen.map f
    let sample1 =
        Gen.elements >> Gen.sample 0 1 >> List.head

module Generators =
    let alphanumericChar = ['a'..'z'] @ ['A'..'Z'] @ ['0'..'9'] |> Gen.elements
    let alphanumericString = alphanumericChar |> Gen.nonEmptyListOf |> Gen.map (List.toArray >> String)
    let standardTemplate fields =
        gen {
            let templateGen =
                gen {
                    let! name = Arb.generate<string>
                    let! front = Gen.elements fields
                    let! back = Gen.elements fields
                    return
                        {   Name = name
                            Front = "{{" + front + "}}"
                            Back = "{{FrontSide}}<hr id=answer>{{" + back + "}}"
                            ShortFront = ""
                            ShortBack = ""
                        }
                }
            let! templates = Gen.nonEmptyListOf templateGen
            return Standard templates
        }
    let clozeTemplate fields =
        gen {
            let! name = Arb.generate<string>
            let! text = Gen.elements fields
            let! extra = Gen.elements fields
            return
                {   Name = name
                    Front = "{{cloze:" + text + "}}"
                    Back = "{{cloze:" + text + "}}<br>{{" + extra + "}}"
                    ShortFront = ""
                    ShortBack = ""
                } |> Cloze
        }
    let collateType fields =
        Gen.oneof [
            standardTemplate fields
            clozeTemplate fields
        ]
    let fields =
        List.map (fun fieldName -> Gen.genMap<Field> (fun field -> { field with Name = fieldName }))
        >> Gen.sequence
    let collateInstance collateType fieldNames =
        gen {
            let! fields = fields fieldNames
            return!
                Gen.genMap<CollateInstance> (fun collateInstance -> {
                    collateInstance with
                        Fields = fields
                        Templates = collateType
                })
        }
    type ClozeText = ClozeText of string
    let clozeText =
        gen { // medTODO make more realistic
            let! a = alphanumericString
            let! b = alphanumericString
            let! c = alphanumericString
            return sprintf "%s{{c1::%s}}%s" a b c
        }
    let editStackCommand =
        gen {
            let! fieldNames = alphanumericString |> Gen.nonEmptyListOf
            let! collateType = collateType fieldNames
            let! collateInstance = collateInstance collateType fieldNames
            let values =
                match collateType with
                | Standard -> alphanumericString
                | Cloze -> clozeText
            let! fields = fields fieldNames
            let! fields =
                fields
                |> List.map (fun f -> values |> Gen.map (fun value -> { EditField = f; Value = value }))
                |> Gen.sequence
            return!
                Gen.genMap<EditStackCommand> (fun c ->
                    {   c with
                            FieldValues = fields |> toResizeArray
                            CollateInstance = collateInstance
                    })
        }

type Generators =
    static member editStackCommand =
        Generators.editStackCommand |> Arb.fromGen
    static member clozeText =
        Generators.clozeText
        |> Gen.map Generators.ClozeText
        |> Arb.fromGen

type GeneratorsAttribute() =
    inherit PropertyAttribute(Arbitrary = [| typeof<Generators> |])

type ValidationTests () =
    let validator = DataAnnotationsValidator()
    
    [<Property>]
    let ``EditFieldAndValue - empty Value is valid`` (editFieldAndValue: EditFieldAndValue): unit =
        let validationResults = ResizeArray.empty
        let editFieldAndValue =
            {   editFieldAndValue
                    with Value = ""
            }

        let isValid = validator.TryValidateObjectRecursive(editFieldAndValue, validationResults)

        Assert.True isValid
        Assert.Empty validationResults

    [<Property>]
    let ``EditFieldAndValue - super long Value is invalid`` (editFieldAndValue: EditFieldAndValue): unit =
        let validationResults = ResizeArray.empty
        let editFieldAndValue =
            {   editFieldAndValue
                    with Value = String('-', 10_001)
            }

        let isValid = validator.TryValidateObjectRecursive(editFieldAndValue, validationResults)

        Assert.False isValid
        Assert.equal
            <| "The field Value must be a string with a maximum length of 10000."
            <| validationResults.Single().ErrorMessage

    [<Generators>]
    let ``EditStackCommand - Generators only produces valid commands`` (editStackCommand: EditStackCommand): unit =
        let validationResults = ResizeArray.empty
        
        let isValid = validator.TryValidateObjectRecursive(editStackCommand, validationResults)

        Assert.True isValid
        Assert.Empty validationResults

    [<Generators>]
    let ``EditStackCommand - super long Value is invalid`` (editStackCommand: EditStackCommand) (Generators.ClozeText clozeText): unit =
        let validationResults = ResizeArray.empty
        let editStackCommand =
            {   editStackCommand with
                    FieldValues =
                        editStackCommand.FieldValues.Select(fun x -> { x with Value = clozeText + String('-', 10_001 ) }).ToList()
            }
        
        let isValid = validator.TryValidateObjectRecursive(editStackCommand, validationResults)

        Assert.False isValid
        Assert.equal
            <| "The field Value must be a string with a maximum length of 10000."
            <| validationResults.Select(fun x -> x.ErrorMessage).Distinct().Single()

    [<Generators>]
    let ``EditStackCommand - text with random separator is invalid`` (editStackCommand: EditStackCommand) (Generators.ClozeText clozeText): unit =
        let validationResults = ResizeArray.empty
        let randomSeparator = ['\x1c'; '\x1d'; '\x1e'; '\x1f'] |> Gen.sample1 |> string
        let editStackCommand =
            {   editStackCommand with
                    FieldValues =
                        editStackCommand.FieldValues.Select(fun x -> { x with Value = clozeText + randomSeparator }).ToList()
            }
        
        let isValid = validator.TryValidateObjectRecursive(editStackCommand, validationResults)

        Assert.False isValid
        Assert.equal
            <| "Unit, record, group, and file separators are not permitted."
            <| validationResults.Select(fun x -> x.ErrorMessage).Distinct().Single()

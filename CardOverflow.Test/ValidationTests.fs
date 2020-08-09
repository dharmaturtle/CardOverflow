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
open CardOverflow.Entity

module internal Gen =
    let gen<'a> = // uses the reflection based default generator instead of the registered generator
        Arb.Default.Derive<'a>()
        |> Arb.toGen
    let genMap<'a> f: Gen<'a> =
        gen<'a>
        |> Gen.map f
    let sample1 x =
        x |> Gen.elements |> Gen.sample 0 1 |> List.head
    let sample1Gen x =
        x |> Gen.sample 10 1 |> List.head

module Generators =
    let alphanumericChar = ['a'..'z'] @ ['A'..'Z'] @ ['0'..'9'] |> Gen.elements
    let alphanumericString = alphanumericChar |> Gen.nonEmptyListOf |> Gen.map (List.toArray >> String)
    let seqOfLength lengthInterval generator =
        gen {
            let! length = Gen.choose lengthInterval
            let! items = generator |> Gen.arrayOfLength length
            return items |> Seq.ofArray
        }
    let stringOfLength lengthInterval =
        Arb.generate<char>
        |> seqOfLength lengthInterval
        |> Gen.map String.Concat
    let differentPositives n =
        Arb.Default.PositiveInt().Generator
        |> Gen.map (fun x -> x.Get)
        |> Gen.listOfLength n
        |> Gen.filter (fun x -> x.Distinct().Count() = n)
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
    let gromplateType fields =
        Gen.oneof [
            standardTemplate fields
            clozeTemplate fields
        ]
    let fields =
        List.map (fun fieldName -> Gen.genMap<Field> (fun field -> { field with Name = fieldName }))
        >> Gen.sequence
    let grompleaf gromplateType fieldNames =
        gen {
            let! fields = fields fieldNames
            return!
                Gen.genMap<Grompleaf> (fun grompleaf -> {
                    grompleaf with
                        Fields = fields
                        Templates = gromplateType
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
            let! gromplateType = gromplateType fieldNames
            let! grompleaf = grompleaf gromplateType fieldNames
            let values =
                match gromplateType with
                | Standard _ -> alphanumericString
                | Cloze _ -> clozeText
            let! fields = fields fieldNames
            let! fields =
                fields
                |> List.map (fun f -> values |> Gen.map (fun value -> { EditField = f; Value = value }))
                |> Gen.sequence
            return!
                Gen.genMap<EditStackCommand> (fun c ->
                    {   c with
                            FieldValues = fields |> toResizeArray
                            Grompleaf = grompleaf
                    })
        }
    let notificationEntity = gen {
        let! id = Arb.generate<int>
        let! timestamp = Arb.generate<DateTime>
        let! message = Arb.generate<string>
        let! notificationType = Gen.gen<NotificationType>
        return
            NotificationEntity(
                Id = id,
                SenderId = Gen.sample1 [1; 2; 3],
                TimeStamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified),
                Type = notificationType,
                Message = message
            )}
    let genString15 = // unused; here as an example
        Gen.sized (fun s -> Gen.resize (min s 15) Arb.generate<NonNull<string>>) |> Gen.map (fun (NonNull str) -> str)
    let uniqueInts length = gen {
        let initialValue = Int32.MinValue
        let array = Array.create length initialValue
        while array.Contains initialValue do
            let! id = Arb.generate<int>
            if not <| array.Contains id then
                array.[Array.IndexOf(array, initialValue)] <- id
        return array }
    let StackLeafIds length = gen {
        let! stacks = uniqueInts length
        let! branches = uniqueInts length
        let! leafs = uniqueInts length
        return
            Seq.zip3 stacks branches leafs
            |> Seq.map StackLeafIds.fromTuple
            |> List.ofSeq
        }
    let StackLeafIds3 =
        StackLeafIds 3
    let StackLeafIndex length = gen {
        let! stacks = uniqueInts length
        let! branches = uniqueInts length
        let! leafs = uniqueInts length
        let! indexes = Arb.generate<int16> |> Gen.listOfLength length
        let! deckId = Arb.generate<int> |> Gen.listOfLength length
        return
            Seq.zip5 stacks branches leafs indexes deckId
            |> Seq.map StackLeafIndex.fromTuple
            |> List.ofSeq
        }
    let StackLeafIndex3 =
        StackLeafIndex 3

type Generators =
    static member editStackCommand =
        Generators.editStackCommand |> Arb.fromGen
    static member clozeText =
        Generators.clozeText
        |> Gen.map Generators.ClozeText
        |> Arb.fromGen
    static member notificationEntity =
        Generators.notificationEntity |> Arb.fromGen
    static member StackLeafIds3 =
        Generators.StackLeafIds3 |> Arb.fromGen
    static member StackLeafIndex3 =
        Generators.StackLeafIndex3 |> Arb.fromGen

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

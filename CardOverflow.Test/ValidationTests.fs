namespace CardOverflow.Test

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

type ValidationTests () =
    let validator = DataAnnotationsValidator()
    
    [<Property>]
    let ``EditFieldAndValue - empty Value is valid`` (editFieldAndValue: EditFieldAndValue): unit =
        let validationResults = new ResizeArray<ValidationResult>()
        let editFieldAndValue =
            {   editFieldAndValue
                    with Value = ""
            }

        let isValid = validator.TryValidateObjectRecursive(editFieldAndValue, validationResults)

        Assert.True isValid
        Assert.Empty validationResults

    [<Property>]
    let ``EditFieldAndValue - super long Value is invalid`` (editFieldAndValue: EditFieldAndValue): unit =
        let validationResults = new ResizeArray<ValidationResult>()
        let editFieldAndValue =
            {   editFieldAndValue
                    with Value = String('-', 10_001)
            }

        let isValid = validator.TryValidateObjectRecursive(editFieldAndValue, validationResults)

        Assert.False isValid
        Assert.equal
            <| "The field Value must be a string with a maximum length of 10000."
            <| validationResults.Single().ErrorMessage

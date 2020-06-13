namespace CardOverflow.Test

open CardOverflow.Api
open CardOverflow.Test
open CardOverflow.Pure
open Xunit
open System
open System.Linq
open System.ComponentModel.DataAnnotations
open DataAnnotationsValidator

type ValidationTests () =
    let validator = DataAnnotationsValidator()
    let validationResults = new ResizeArray<ValidationResult>()
    
    [<Fact>]
    let ``EditFieldAndValue - empty Value is valid``(): unit =
        let modelToValidate = {
            EditFieldAndValue.EditField =
                {   Name = ""
                    IsRightToLeft = false
                    IsSticky = false
                }
            Value = ""
        }

        let isValid = validator.TryValidateObjectRecursive(modelToValidate, validationResults)

        Assert.True isValid
        Assert.Empty validationResults

    [<Fact>]
    let ``EditFieldAndValue - super long Value is invalid``(): unit =
        let modelToValidate = {
            EditFieldAndValue.EditField =
                {   Name = ""
                    IsRightToLeft = false
                    IsSticky = false
                }
            Value = String('-', 10_001)
        }

        let isValid = validator.TryValidateObjectRecursive(modelToValidate, validationResults)

        Assert.False isValid
        Assert.equal
            <| "The field Value must be a string with a maximum length of 10000."
            <| validationResults.Single().ErrorMessage

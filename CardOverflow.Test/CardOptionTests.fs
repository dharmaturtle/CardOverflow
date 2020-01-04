module CardOptionTests

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
open CardOverflow.Pure
open CardOverflow.Pure.Core
open CardOverflow.Sanitation

[<Fact>]
let ``SanitizeCardOption.upsertMany can add/update new option``(): Task<unit> = task {
    let userId = 3
    use c = new TestContainer()
    let! options = SanitizeCardOptionRepository.getAll c.Db userId
    let oldId = 3
    Assert.Equal(oldId, options.Single().Id)
    let options =
        options.Append
            { ViewCardOption.load CardOptionsRepository.defaultCardOptions with
                IsDefault = false }
        |> toResizeArray
    let newId = oldId + 1

    let! id = SanitizeCardOptionRepository.upsertMany c.Db userId options
    
    Assert.Equal(newId, (Result.getOk id).Single(fun x -> x  <> oldId))
    let! option = c.Db.CardOption.SingleAsync(fun x -> x.Id = newId)
    Assert.False option.IsDefault

    // can update
    let! options = SanitizeCardOptionRepository.getAll c.Db userId
    let newName = Guid.NewGuid().ToString()
    
    let! id =
        SanitizeCardOptionRepository.upsertMany c.Db userId
            <| [options.Single(fun x -> x.Id = oldId)
                { options.Single(fun x -> x.Id = newId) with Name = newName }
               ].ToList()
    
    let id = (Result.getOk id).Single(fun x -> x  <> oldId)
    Assert.Equal(newId, id)
    Assert.Equal(newName, c.Db.CardOption.Single(fun x -> x.Id = id).Name)

    let canUpdateIsDefault oldIsDefault newIsDefault = task {
        let! options = SanitizeCardOptionRepository.getAll c.Db userId
    
        let! _ =
            SanitizeCardOptionRepository.upsertMany c.Db userId
                <| [{ options.Single(fun x -> x.Id = oldId) with IsDefault = oldIsDefault }
                    { options.Single(fun x -> x.Id = newId) with IsDefault = newIsDefault }
                   ].ToList()
    
        Assert.Equal(oldIsDefault, c.Db.CardOption.Single(fun x -> x.Id = oldId).IsDefault)
        Assert.Equal(newIsDefault,     c.Db.CardOption.Single(fun x -> x.Id = newId).IsDefault) }
    do! canUpdateIsDefault false true
    do! canUpdateIsDefault true false

    // Insert new card
    let! templates = SanitizeCardTemplate.Search c.Db "Basic"
    let template = templates.Single(fun x -> x.Name = "Basic")
    let! card = CardRepository.getNew c.Db userId
    let! r =
        SanitizeCardRepository.Update c.Db userId card
            {   EditSummary = "Initial creation"
                FieldValues =
                    template.Fields.Select(fun f -> {
                        EditField = ViewField.copyTo f
                        Value = Guid.NewGuid().ToString()
                        Communal = None
                    }).ToList()
                TemplateInstance = template
            }
    r |> Result.getOk

    // new card has default option
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.UserId = userId)
    let! options = SanitizeCardOptionRepository.getAll c.Db userId
    let defaultId = options.Single(fun x -> x.IsDefault).Id
    let otherId = options.Single(fun x -> not x.IsDefault).Id
    
    Assert.Equal(defaultId, ac.CardOptionId)

    // can set new option
    let! r = SanitizeCardOptionRepository.setCard c.Db userId ac.Id otherId
    r |> Result.getOk
    
    let! ac = c.Db.AcquiredCard.SingleAsync(fun x -> x.UserId = userId)
    Assert.Equal(otherId, ac.CardOptionId)
    }

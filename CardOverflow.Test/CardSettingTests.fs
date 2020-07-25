module CardSettingTests

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
open CardOverflow.Sanitation

[<Fact>]
let ``SanitizeCardSetting.upsertMany can add/update new option``(): Task<unit> = task {
    let userId = 3
    use c = new TestContainer()
    let! options = SanitizeCardSettingRepository.getAll c.Db userId
    let oldId = 3
    Assert.Equal(oldId, options.Single().Id)
    let options =
        options.Append
            { ViewCardSetting.load CardSettingsRepository.defaultCardSettings with
                IsDefault = false }
        |> toResizeArray
    let newId = oldId + 1

    let! ids = SanitizeCardSettingRepository.upsertMany c.Db userId options
    
    Assert.Equal(newId, ids.Value.Single(fun x -> x <> oldId))
    let! user = c.Db.User.SingleAsync(fun x -> x.Id = userId)
    Assert.Equal(oldId, user.DefaultCardSettingId)

    // can update
    let! options = SanitizeCardSettingRepository.getAll c.Db userId
    let newName = Guid.NewGuid().ToString()
    
    let! id =
        SanitizeCardSettingRepository.upsertMany c.Db userId
            <| [options.Single(fun x -> x.Id = oldId)
                { options.Single(fun x -> x.Id = newId) with Name = newName }
               ].ToList()
    
    let id = (Result.getOk id).Single(fun x -> x  <> oldId)
    Assert.Equal(newId, id)
    Assert.Equal(newName, c.Db.CardSetting.Single(fun x -> x.Id = id).Name)

    let canUpdateIsDefault expectedDefaultId = task {
        let! options = SanitizeCardSettingRepository.getAll c.Db userId
    
        let! _ =
            SanitizeCardSettingRepository.upsertMany c.Db userId
                <| [{ options.Single(fun x -> x.Id = oldId) with IsDefault = oldId = expectedDefaultId }
                    { options.Single(fun x -> x.Id = newId) with IsDefault = newId = expectedDefaultId }
                   ].ToList()
    
        let! user = c.Db.User.SingleAsync(fun x -> x.Id = userId)
        Assert.Equal(expectedDefaultId, user.DefaultCardSettingId) }
    do! canUpdateIsDefault oldId
    do! canUpdateIsDefault newId

    // Insert new card
    let branchId = 1
    let! collate = FacetRepositoryTests.basicCollate c.Db
    let! r =
        SanitizeStackRepository.Update c.Db userId []
            {   EditSummary = "Initial creation"
                FieldValues =
                    collate.Fields.Select(fun f -> {
                        EditField = ViewField.copyTo f
                        Value = Guid.NewGuid().ToString()
                    }).ToList()
                CollateInstance = collate
                Kind = NewOriginal_TagIds []
                Title = null
            }
    Assert.Equal(branchId, r.Value)

    // new card has default option
    let! cc = c.Db.CollectedCard.SingleAsync(fun x -> x.UserId = userId)
    let! options = SanitizeCardSettingRepository.getAll c.Db userId
    let defaultId = options.Single(fun x -> x.IsDefault).Id
    let otherId = options.Single(fun x -> not x.IsDefault).Id
    
    Assert.Equal(defaultId, cc.CardSettingId)

    // can set new option
    let! r = SanitizeCardSettingRepository.setCard c.Db userId cc.Id otherId
    r |> Result.getOk
    
    let! cc = c.Db.CollectedCard.SingleAsync(fun x -> x.UserId = userId)
    Assert.Equal(otherId, cc.CardSettingId)
    }

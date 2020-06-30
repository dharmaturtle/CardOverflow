module SanitizeStackRepositoryTests

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
open FsToolkit.ErrorHandling

[<Fact>]
let ``SanitizeStackRepository.Update with EditAcquiredCardCommands``(): Task<unit> = (taskResult {
    let userId = 3
    use c = new TestContainer()
    let! options =
        SanitizeCardSettingRepository.getAll c.Db userId
        |>% (fun options ->
             options.Append
                { ViewCardSetting.load CardSettingsRepository.defaultCardSettings with
                    IsDefault = false })
        |>% toResizeArray
    let! optionId =
        SanitizeCardSettingRepository.upsertMany c.Db userId options
        |>%% Gen.sample1
    let! deckId =
        Guid.NewGuid().ToString()
        |> SanitizeDeckRepository.create c.Db userId
        |>%% fun newDeckId -> [ userId; newDeckId ]
        |>%% Gen.sample1
    let front = Guid.NewGuid().ToString()
    let back = Guid.NewGuid().ToString()
    let state = Gen.gen<CardState> |> Gen.sample1Gen
    let! collate = FacetRepositoryTests.basicCollate c.Db
    let stackId = 1
    let branchId = 1

    do! SanitizeStackRepository.Update c.Db userId
            [   {   CardSettingId = optionId
                    DeckId = deckId
                    CardState = state
                    FrontPersonalField = front
                    BackPersonalField = back
            }   ]
            {   EditSummary = Guid.NewGuid().ToString()
                FieldValues = [].ToList()
                CollateInstance = collate
                Kind = NewOriginal_TagIds []
                Title = null
            }
        |>%% Assert.equal branchId

    let! ac =
        StackRepository.GetAcquired c.Db userId stackId
        |>%% Assert.Single
    Assert.equal
        {   AcquiredCardId = 1
            UserId = userId
            StackId = stackId
            BranchId = branchId
            BranchInstanceMeta = ac.BranchInstanceMeta // untested
            Index = 0s
            CardState = state
            IsLapsed = false
            EaseFactorInPermille = 0s
            IntervalOrStepsIndex = NewStepsIndex 0uy
            Due = ac.Due // untested
            CardSettingId = optionId
            Tags = []
            DeckId = deckId
        }
        ac
    } |> TaskResult.getOk)

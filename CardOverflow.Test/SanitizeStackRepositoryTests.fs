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
open FsCheck
open FsCheck.Xunit

[<Property(MaxTest = 1)>]
let ``SanitizeStackRepository.Update with EditAcquiredCardCommands``(stdGen: Random.StdGen): unit =
    (taskResult {
        let userId = 3
        use c = new TestContainer()
        let! options =
            SanitizeCardSettingRepository.getAll c.Db userId
            |>% (fun options ->
                 options.Append
                    { ViewCardSetting.load CardSettingsRepository.defaultCardSettings with
                        IsDefault = false })
            |>% toResizeArray
        let! optionIdGen =
            SanitizeCardSettingRepository.upsertMany c.Db userId options
            |>%% Gen.elements
        let! deckIdGen =
            Guid.NewGuid().ToString()
            |> SanitizeDeckRepository.create c.Db userId
            |>%% fun newDeckId -> [ userId; newDeckId ]
            |>%% Gen.elements
        let basicCommand, aRevCommand, bRevCommand =
            gen {
                let! cardSettingId = optionIdGen
                let! deckId = deckIdGen
                let! cardState = Gen.gen<CardState>
                let! front = Generators.alphanumericString
                let! back = Generators.alphanumericString
                return
                    {   CardSettingId = cardSettingId
                        DeckId = deckId
                        CardState = cardState
                        FrontPersonalField = front
                        BackPersonalField = back
                    }
            } |> Gen.listOfLength 3
            |> Gen.eval 100 stdGen
            |> fun x -> x.[0], x.[1], x.[2]

        let! collate = FacetRepositoryTests.basicCollate c.Db
        let stackId = 1
        let branchId = 1

        do! SanitizeStackRepository.Update c.Db userId
                [ basicCommand ]
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
                CardState = basicCommand.CardState
                IsLapsed = false
                EaseFactorInPermille = 0s
                IntervalOrStepsIndex = NewStepsIndex 0uy
                Due = ac.Due // untested
                CardSettingId = basicCommand.CardSettingId
                Tags = []
                DeckId = basicCommand.DeckId
            }
            ac
    
        // works on multiple acquired cards, e.g. reversedBasicCollate
        let! collate = FacetRepositoryTests.reversedBasicCollate c.Db
        let stackId = 2
        let branchId = 2

        do! SanitizeStackRepository.Update c.Db userId
                [ aRevCommand; bRevCommand ]
                {   EditSummary = Guid.NewGuid().ToString()
                    FieldValues = [].ToList()
                    CollateInstance = collate
                    Kind = NewOriginal_TagIds []
                    Title = null
                }
            |>%% Assert.equal branchId

        let! (acs: AcquiredCard ResizeArray) = StackRepository.GetAcquired c.Db userId stackId
        Assert.equal
            {   AcquiredCardId = 2
                UserId = userId
                StackId = stackId
                BranchId = branchId
                BranchInstanceMeta = acs.[0].BranchInstanceMeta // untested
                Index = 0s
                CardState = aRevCommand.CardState
                IsLapsed = false
                EaseFactorInPermille = 0s
                IntervalOrStepsIndex = NewStepsIndex 0uy
                Due = acs.[0].Due // untested
                CardSettingId = aRevCommand.CardSettingId
                Tags = []
                DeckId = aRevCommand.DeckId
            }
            acs.[0]
        Assert.equal
            {   AcquiredCardId = 3
                UserId = userId
                StackId = stackId
                BranchId = branchId
                BranchInstanceMeta = acs.[1].BranchInstanceMeta // untested
                Index = 1s
                CardState = bRevCommand.CardState
                IsLapsed = false
                EaseFactorInPermille = 0s
                IntervalOrStepsIndex = NewStepsIndex 0uy
                Due = acs.[1].Due // untested
                CardSettingId = bRevCommand.CardSettingId
                Tags = []
                DeckId = bRevCommand.DeckId
            }
            acs.[1]
    } |> TaskResult.getOk).GetAwaiter().GetResult()

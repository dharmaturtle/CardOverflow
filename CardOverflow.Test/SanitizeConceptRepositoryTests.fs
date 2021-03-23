module SanitizeConceptRepositoryTests

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
let ``SanitizeConceptRepository.Update with EditCardCommands``(stdGen: Random.StdGen): unit =
    (taskResult {
        let userId = user_3
        use c = new TestContainer()
        let! options =
            SanitizeCardSettingRepository.getAll c.Db userId
            |>% (fun options ->
                 options.Append
                    { (Guid.Empty |> CardSetting.newUserCardSettings |> ViewCardSetting.load) with
                        IsDefault = false })
            |>% toResizeArray
        let! optionIdGen =
            SanitizeCardSettingRepository.upsertMany c.Db userId options
            |>%% Gen.elements
        let! deckIdGen =
            let newDeckId = Ulid.create
            SanitizeDeckRepository.create c.Db userId (Guid.NewGuid().ToString()) newDeckId
            |>%% fun () -> [ deck_3; newDeckId ]
            |>%% Gen.elements
        let basicCommand, aRevCommand, bRevCommand, failDeckCommand, failCardSettingCommand =
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
            } |> Gen.listOfLength 5
            |> Gen.eval 100 stdGen
            |> fun x -> x.[0], x.[1], x.[2], x.[3], x.[4]
        let conceptCommand template ids =
            {   EditSummary = Guid.NewGuid().ToString()
                FieldValues = [].ToList()
                TemplateRevision = template
                Kind = NewOriginal_TagIds Set.empty
                Title = null
                Ids = ids
            }

        let! template = FacetRepositoryTests.basicTemplate c.Db
        let conceptId = concept_1
        let exampleId = example_1

        do! SanitizeConceptRepository.Update c.Db userId
                [ basicCommand ]
                (conceptCommand template ids_1)
            |>%% Assert.equal exampleId

        let! (cc: Card) =
            ConceptRepository.GetCollected c.Db userId conceptId
            |>%% Assert.Single
        Assert.equal
            {   CardId = cc.CardId
                UserId = userId
                ConceptId = conceptId
                ExampleId = exampleId
                RevisionMeta = cc.RevisionMeta // untested
                Index = 0s
                CardState = basicCommand.CardState
                IsLapsed = false
                EaseFactorInPermille = 0s
                IntervalOrStepsIndex = NewStepsIndex 0uy
                Due = cc.Due // untested
                CardSettingId = basicCommand.CardSettingId
                Tags = []
                DeckId = basicCommand.DeckId
            }
            cc
    
        // works on multiple collected cards, e.g. reversedBasicTemplate
        let! template = FacetRepositoryTests.reversedBasicTemplate c.Db
        let conceptId = concept_2
        let exampleId = example_2

        do! SanitizeConceptRepository.Update c.Db userId
                [ aRevCommand; bRevCommand ]
                (conceptCommand template { ids_2 with CardIds = [ card_2; card_3 ] })
            |>%% Assert.equal exampleId

        let! (ccs: Card ResizeArray) = ConceptRepository.GetCollected c.Db userId conceptId
        let a = ccs.First(fun x -> x.Index = 0s)
        let b = ccs.First(fun x -> x.Index = 1s)
        Assert.equal
            {   CardId = a.CardId
                UserId = userId
                ConceptId = conceptId
                ExampleId = exampleId
                RevisionMeta = a.RevisionMeta // untested
                Index = 0s
                CardState = aRevCommand.CardState
                IsLapsed = false
                EaseFactorInPermille = 0s
                IntervalOrStepsIndex = NewStepsIndex 0uy
                Due = a.Due // untested
                CardSettingId = aRevCommand.CardSettingId
                Tags = []
                DeckId = aRevCommand.DeckId
            }
            a
        Assert.equal
            {   CardId = b.CardId
                UserId = userId
                ConceptId = conceptId
                ExampleId = exampleId
                RevisionMeta = b.RevisionMeta // untested
                Index = 1s
                CardState = bRevCommand.CardState
                IsLapsed = false
                EaseFactorInPermille = 0s
                IntervalOrStepsIndex = NewStepsIndex 0uy
                Due = b.Due // untested
                CardSettingId = bRevCommand.CardSettingId
                Tags = []
                DeckId = bRevCommand.DeckId
            }
            b
    
        let ids_3 = { ids_3 with CardIds = [card_ 4; card_ 5] }
        
        // doesn't work with someone else's deckId
        let failDeckCommand = { failDeckCommand with DeckId = deck_1 }
        let! (error: Result<_, _>) =
            SanitizeConceptRepository.Update c.Db userId
                [ failDeckCommand ]
                (conceptCommand template ids_3)
        Assert.equal "You provided an invalid or unauthorized deck id." error.error
    
        // doesn't work with someone else's cardSettingId
        let failCardSettingCommand = { failCardSettingCommand with CardSettingId = setting_1 }
        let! (error: Result<_, _>) =
            SanitizeConceptRepository.Update c.Db userId
                [ failCardSettingCommand ]
                (conceptCommand template ids_3)
        Assert.equal "You provided an invalid or unauthorized card setting id." error.error
    
        // doesn't work with invalid deckId
        let failDeckCommand = { failDeckCommand with DeckId = Ulid.create }
        let! (error: Result<_, _>) =
            SanitizeConceptRepository.Update c.Db userId
                [ failDeckCommand ]
                (conceptCommand template ids_3)
        Assert.equal "You provided an invalid or unauthorized deck id." error.error
    
        // doesn't work with invalid cardSettingId
        let failCardSettingCommand = { failCardSettingCommand with CardSettingId = Ulid.create }
        let! (error: Result<_, _>) =
            SanitizeConceptRepository.Update c.Db userId
                [ failCardSettingCommand ]
                (conceptCommand template ids_3)
        Assert.equal "You provided an invalid or unauthorized card setting id." error.error
    } |> TaskResult.getOk).GetAwaiter().GetResult()

module AnkiPureTests

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Test
open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Quotations
open System.Linq
open System
open Xunit
open CardOverflow.Entity
open MappingTools
open NodaTime

[<Fact>]
let ``AnkiMap.parseDconf on allDefaultTemplatesAndImageAndMp3_apkg returns expected`` (): unit =
    let expected =
        {   Id = Guid.Empty
            Name = "Default"
            NewCardsSteps = [Duration.FromMinutes 1. ; Duration.FromMinutes 10.]
            NewCardsMaxPerDay = 20
            NewCardsGraduatingInterval = Duration.FromDays 1.
            NewCardsEasyInterval = Duration.FromDays 4.
            NewCardsStartingEaseFactor = 2.5
            NewCardsBuryRelated = false
            MatureCardsMaxPerDay = 200
            MatureCardsEaseFactorEasyBonusFactor = 1.3
            MatureCardsIntervalFactor = 1.
            MatureCardsMaximumInterval = 36500. |> Duration.FromDays
            MatureCardsHardIntervalFactor = 1.2
            MatureCardsBuryRelated = false
            LapsedCardsSteps = [ Duration.FromMinutes 10. ]
            LapsedCardsNewIntervalFactor = 0.
            LapsedCardsMinimumInterval = Duration.FromDays 1.
            LapsedCardsLeechThreshold = 8
            ShowAnswerTimer = false
            AutomaticallyPlayAudio = true
            ReplayQuestionAudioOnAnswer = true
        }
    let col = AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_apkg.Cols.Head

    let cardSettings = Anki.parseCardSettings col.Dconf
    
    cardSettings |> Result.isOk |> Assert.True
    let id, actual = cardSettings |> Result.getOk |> Assert.Single
    Assert.equal "1" id
    Assert.equal expected actual

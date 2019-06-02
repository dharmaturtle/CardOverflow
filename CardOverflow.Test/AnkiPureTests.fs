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

[<Fact>]
let ``AnkiMap.parseDconf on allDefaultTemplatesAndImageAndMp3_apkg returns expected``() =
    let expected =
        {   Id = 0
            Name = "Default"
            NewCardsSteps = [TimeSpan.FromMinutes 1. ; TimeSpan.FromMinutes 10.]
            NewCardsMaxPerDay = 20s
            NewCardsGraduatingInterval = TimeSpan.FromDays 1.
            NewCardsEasyInterval = TimeSpan.FromDays 4.
            NewCardsStartingEaseFactor = 2.5
            NewCardsBuryRelated = false
            MatureCardsMaxPerDay = 200s
            MatureCardsEaseFactorEasyBonusFactor = 1.3
            MatureCardsIntervalFactor = 1.
            MatureCardsMaximumInterval = TimeSpan.FromDays 36500.
            MatureCardsHardInterval = 1.2
            MatureCardsBuryRelated = false
            LapsedCardsSteps = [ TimeSpan.FromMinutes 10. ]
            LapsedCardsNewIntervalFactor = 0.
            LapsedCardsMinimumInterval = TimeSpan.FromDays 1.
            LapsedCardsLeechThreshold = 8uy
            ShowAnswerTimer = false
            AutomaticallyPlayAudio = true
            ReplayQuestionAudioOnAnswer = true
        }
    let col = AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_apkg.Cols.Head

    let dconf = Anki.parseDconf col.Dconf
    
    dconf |> Result.isOk |> Assert.True
    let id, actual = Result.getOk dconf |> Assert.Single
    Assert.Equal("1", id)
    Assert.Equal(expected, actual)

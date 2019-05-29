module AnkiPureTests

open CardOverflow.Api
open CardOverflow.Entity
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
            NewCardsSteps = [TimeSpan.FromMinutes 1.0 ; TimeSpan.FromMinutes 10.0]
            NewCardsMaxPerDay = 20s
            NewCardsGraduatingInterval = TimeSpan.FromDays 1.0
            NewCardsEasyInterval = TimeSpan.FromDays 4.0
            NewCardsStartingEaseFactor = 2.5
            NewCardsBuryRelated = false
            MatureCardsMaxPerDay = 200s
            MatureCardsEaseFactorEasyBonusFactor = 1.3
            MatureCardsIntervalFactor = 1.0
            MatureCardsMaximumInterval = TimeSpan.FromDays 36500.0
            MatureCardsHardInterval = 1.2
            MatureCardsBuryRelated = false
            LapsedCardsSteps = [ TimeSpan.FromMinutes 10.0 ]
            LapsedCardsNewIntervalFactor = 0.0
            LapsedCardsMinimumInterval = TimeSpan.FromDays 1.0
            LapsedCardsLeechThreshold = 8uy
            ShowAnswerTimer = false
            AutomaticallyPlayAudio = true
            ReplayQuestionAudioOnAnswer = true
            AnkiId = Some 1
        }
    let col = AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_apkg.Cols.Head

    let dconf = AnkiMap.parseDconf col.Dconf
    
    dconf |> Result.isOk |> Assert.True
    let id, actual = Result.getOk dconf |> Assert.Single
    Assert.Equal("1", id)
    Assert.Equal(expected, actual)

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

[<Fact>]
let ``AnkiMap.parseDconf on allDefaultGromplatesAndImageAndMp3_apkg returns expected`` (): unit =
    let expected =
        {   Id = 0
            Name = "Default"
            IsDefault = false
            NewCardsSteps = [TimeSpan.FromMinutes 1. ; TimeSpan.FromMinutes 10.]
            NewCardsMaxPerDay = 20s
            NewCardsGraduatingInterval = TimeSpan.FromDays 1.
            NewCardsEasyInterval = TimeSpan.FromDays 4.
            NewCardsStartingEaseFactor = 2.5
            NewCardsBuryRelated = false
            MatureCardsMaxPerDay = 200s
            MatureCardsEaseFactorEasyBonusFactor = 1.3
            MatureCardsIntervalFactor = 1.
            MatureCardsMaximumInterval = 36500. |> TimeSpanInt16.fromDays
            MatureCardsHardIntervalFactor = 1.2
            MatureCardsBuryRelated = false
            LapsedCardsSteps = [ TimeSpan.FromMinutes 10. ]
            LapsedCardsNewIntervalFactor = 0.
            LapsedCardsMinimumInterval = TimeSpan.FromDays 1.
            LapsedCardsLeechThreshold = 8s
            ShowAnswerTimer = false
            AutomaticallyPlayAudio = true
            ReplayQuestionAudioOnAnswer = true
        }
    let col = AnkiImportTestData.allDefaultGromplatesAndImageAndMp3_apkg.Cols.Head

    let cardSettings = Anki.parseCardSettings col.Dconf
    
    cardSettings |> Result.isOk |> Assert.True
    let id, actual = Result.getOk cardSettings |> Assert.Single
    Assert.Equal("1", id)
    Assert.Equal(expected, actual)

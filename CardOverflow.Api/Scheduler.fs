namespace CardOverflow.Api

open CardOverflow.Pure
open System
open Serilog
open NodaTime
open Domain.Summary

module Scheduler =
    let max a b = if a > b then a else b
    let min a b = if a < b then a else b
    let equals a b threshold = abs(a-b) < threshold

    let rawInterval utcNow (card: Card) (settings: CardSetting) score =
        let calculateStepsInterval toStep stepTimespans graduatingInterval easyInterval stepIndex =
            match score with
            | Again -> stepTimespans |> List.head, toStep 0uy
            | Hard ->
                match stepTimespans |> List.tryItem (int stepIndex) with
                | Some span -> span, toStep stepIndex
                | None ->
                    sprintf "Hard was chosen for QuizCard %A and it had %i as its stepIndex - an illegal value." card stepIndex |> Log.Error
                    stepTimespans.Head, toStep 0uy
            | Good ->
                let stepIndex = stepIndex + 1uy
                match stepTimespans |> List.tryItem (int stepIndex) with
                | Some span -> span, toStep stepIndex
                | None -> graduatingInterval, IntervalXX graduatingInterval
            | Easy -> easyInterval, IntervalXX easyInterval
        let calculateInterval previousInterval =
            let interval (previousInterval: Duration) (rawInterval: Duration) =
                max (rawInterval * settings.MatureCardsIntervalFactor)
                    (Duration.FromDays 1. |> (+) previousInterval)
                |> min settings.MatureCardsMaximumInterval
            let delta = utcNow - card.Due |> max Duration.Zero
            let hard = interval previousInterval <| previousInterval * settings.MatureCardsHardIntervalFactor
            let good = interval hard (delta * 0.5 |> (+) previousInterval |> fun x -> x * card.EaseFactor)
            let easy = interval good (delta * 1.  |> (+) previousInterval |> fun x -> x * card.EaseFactor) |> fun x -> x * settings.MatureCardsEaseFactorEasyBonusFactor
            match score with
            | Again -> settings.NewCardsSteps.Head, 0.
            | Hard -> hard, card.EaseFactor - 0.15
            | Good -> good, card.EaseFactor
            | Easy -> easy, max (card.EaseFactor + 0.15) 1.3
        match card.IntervalOrStepsIndex with
        | NewStepsIndex i ->
            calculateStepsInterval
                NewStepsIndex
                settings.NewCardsSteps
                settings.NewCardsGraduatingInterval
                settings.NewCardsEasyInterval
                i,
            settings.NewCardsStartingEaseFactor
        | LapsedStepsIndex i ->
            calculateStepsInterval
                LapsedStepsIndex
                settings.LapsedCardsSteps
                settings.NewCardsGraduatingInterval // medTODO consider an option for this
                settings.NewCardsGraduatingInterval // medTODO actually the card settings are all screwed up, refactor the entire scheduler later when you figure out how the hell the Anki one works. Or not, apparently constant intervals are better after a point...? http://learninglab.psych.purdue.edu/downloads/2010_Karpicke_Roediger_MC.pdf http://learninglab.psych.purdue.edu/publications/
                i,
            settings.LapsedCardsNewIntervalFactor
        | IntervalXX i ->
            let interval, ease = calculateInterval i
            (interval, IntervalXX interval), ease

open Scheduler
type Scheduler(randomProvider: RandomProvider, time: TimeProvider) =
    let fuzz(interval: Duration) =
        let fuzzRangeInDaysInclusive =
            let days = interval.TotalDays
            let atLeastOneDay = max 1.
            let buildFuzzierInterval = atLeastOneDay >> fun x -> (days - x, days + x)
            if days < 2.              then (1., 1.)
            elif equals days 2. 0.001 then (2., 3.)
            elif days < 7.  then        (days * 0.25) |> buildFuzzierInterval
            elif days < 30. then max 2. (days * 0.15) |> buildFuzzierInterval
            else                 max 4. (days * 0.05) |> buildFuzzierInterval
        // lowTODO find an implementation that is max inclusive
        randomProvider.float fuzzRangeInDaysInclusive |> Duration.FromDays

    member _.Calculate (card: Card) (settings: CardSetting) score =
        rawInterval time.utcNow card settings score
        |> fun ((interval, intervalOrSteps), easeFactor) -> fuzz interval, intervalOrSteps, easeFactor

    member _.Intervals (card: Card) (settings: CardSetting) =
        rawInterval time.utcNow card settings Again |> fst |> fst |> ViewLogic.toString,
        rawInterval time.utcNow card settings Hard  |> fst |> fst |> ViewLogic.toString,
        rawInterval time.utcNow card settings Good  |> fst |> fst |> ViewLogic.toString,
        rawInterval time.utcNow card settings Easy  |> fst |> fst |> ViewLogic.toString

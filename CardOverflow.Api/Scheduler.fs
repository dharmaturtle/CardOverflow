namespace CardOverflow.Api

open CardOverflow.Api
open CardOverflow.Pure
open System
open Serilog

type Scheduler(randomProvider: RandomProvider, time: TimeProvider) =
    let max a b = if a > b then a else b
    let min a b = if a < b then a else b
    let equals a b threshold = abs(a-b) < threshold

    let rawInterval (card: QuizCard) score =
        let intervalOfNewLearningOrLapsed card stepIndex = 
            function
            | Again -> card.Options.LapsedCardsSteps.Head
            | Hard ->
                match card.Options.NewCardsSteps |> List.tryItem (int32 stepIndex) with
                | Some step -> step
                | None ->
                    sprintf "Hard was chosen for QuizCard %A and it had None as its NewCardsSteps - an illegal value." card |> Log.Error
                    card.Options.NewCardsSteps.Head
            | Good ->
                match card.Options.NewCardsSteps |> List.tryItem (int32 stepIndex + 1) with
                | Some step -> step
                | None -> card.Options.NewCardsGraduatingInterval
            | Easy -> card.Options.NewCardsEasyInterval
        let intervalOfMature card oldInterval =
            let interval(previousInterval: TimeSpan) (rawInterval: TimeSpan) =
                max (rawInterval * card.Options.MatureCardsIntervalFactor)
                    (TimeSpan.FromDays 1. |> previousInterval.Add)
                |> min (TimeSpanInt16.value card.Options.MatureCardsMaximumInterval)
            let delta = time.utcNow - card.Due |> max TimeSpan.Zero
            let hard = interval oldInterval (oldInterval * card.Options.MatureCardsHardInterval)
            let good = interval hard (delta * 0.5 |> (+) oldInterval |> (*) card.EaseFactor)
            let easy = interval good (delta * 1.  |> (+) oldInterval |> (*) card.EaseFactor |> (*) card.Options.MatureCardsEaseFactorEasyBonusFactor)
            function
            | Again -> card.Options.NewCardsSteps.Head
            | Hard -> hard
            | Good -> good
            | Easy -> easy
        match card.Interval with
        | StepsIndex stepIndex -> intervalOfNewLearningOrLapsed card stepIndex score
        | Interval interval -> intervalOfMature card interval score

    let fuzz(interval: TimeSpan) =
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
        randomProvider.float fuzzRangeInDaysInclusive |> TimeSpan.FromDays

    member __.interval (card: QuizCard) score =
        rawInterval card score |> fuzz

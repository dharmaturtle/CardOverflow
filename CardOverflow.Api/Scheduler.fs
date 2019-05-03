namespace CardOverflow.Api

open CardOverflow.Api
open System

type Scheduler(randomFloatProvider: RandomFloatProvider, time: TimeProvider) =
    let max a b = if a > b then a else b
    let min a b = if a < b then a else b
    let equals a b threshold = abs(a-b) < threshold

    let interval (card: QuizCard) score =
        let intervalOfNewOrLearning card = 
            function
            | Again -> card.Options.NewCardsSteps.Head
            | Hard ->
                match card.StepsIndex with
                | Some index -> 
                    match card.Options.NewCardsSteps |> List.tryItem (int32 index) with
                    | Some step -> step
                    | None -> card.Options.NewCardsSteps.Head // medTODO log this, this branch should never be reached
                | None -> card.Options.NewCardsSteps.Head // medTODO log this, this branch should never be reached
            | Good ->
                match card.StepsIndex with
                | Some index ->
                    match card.Options.NewCardsSteps |> List.tryItem (int32 index + 1) with
                    | Some step -> step
                    | None -> card.Options.NewCardsGraduatingInterval
                | None -> card.Options.NewCardsGraduatingInterval // medTODO log this, this branch should never be reached
            | Easy -> card.Options.NewCardsEasyInterval
        let intervalOfMature card =
            let interval(previousInterval: TimeSpan) (rawInterval: TimeSpan) =
                max (rawInterval * card.Options.MatureCardsIntervalFactor)
                    (TimeSpan.FromDays 1.0 |> previousInterval.Add)
                |> min card.Options.MatureCardsMaximumInterval
            let delta = time - card.Due |> max TimeSpan.Zero
            let hard = interval card.Interval (card.Interval * card.Options.MatureCardsHardInterval)
            let good = interval hard (delta * 0.5 |> (+) card.Interval |> (*) card.EaseFactor)
            let easy = interval good (delta * 1.0 |> (+) card.Interval |> (*) card.EaseFactor |> (*) card.Options.MatureCardsEaseFactorEasyBonusFactor)
            function
            | Again -> card.Options.NewCardsSteps.Head
            | Hard -> hard
            | Good -> good
            | Easy -> easy
        match card.MemorizationState with
        | New 
        | Learning -> intervalOfNewOrLearning card score
        | Mature -> intervalOfMature card score

    let fuzz(interval: TimeSpan) =
        let fuzzRangeInDaysInclusive =
            let days = interval.TotalDays
            let atLeastOneDay = max 1.0
            let buildFuzzierInterval = atLeastOneDay >> fun x -> (days - x, days + x)
            if days < 2.0              then (1.0, 1.0)
            elif equals days 2.0 0.001 then (2.0, 3.0)
            elif days < 7.00 then         (days * 0.25) |> buildFuzzierInterval
            elif days < 30.0 then max 2.0 (days * 0.15) |> buildFuzzierInterval
            else                  max 4.0 (days * 0.05) |> buildFuzzierInterval
        // lowTODO find an implementation that is max inclusive
        randomFloatProvider fuzzRangeInDaysInclusive |> TimeSpan.FromDays

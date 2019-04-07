module Scheduler

open CardOverflow.Api
open System

let interval card score =
    let intervalOfNewOrLearning card = 
        function
        | Again -> card.Options.NewCardsSteps.Head
        | Hard ->
            match card.StepsIndex with
            | Some index -> 
                match card.Options.NewCardsSteps |> List.tryItem (int32 index) with
                | Some step -> step
                | None -> card.Options.NewCardsSteps.Head // TODO log this, this branch should never be reached
            | None -> card.Options.NewCardsSteps.Head // TODO log this, this branch should never be reached
        | Good ->
            match card.StepsIndex with
            | Some index ->
                match card.Options.NewCardsSteps |> List.tryItem (int32 index + 1) with
                | Some step -> step
                | None -> card.Options.NewCardsGraduatingInterval
            | None -> card.Options.NewCardsGraduatingInterval // TODO log this, this branch should never be reached
        | Easy -> card.Options.NewCardsEasyInterval
    let intervalOfMature card =
        let max a b = if a > b then a else b
        let min a b = if a < b then a else b
        let interval(previousInterval: TimeSpan) (rawInterval: TimeSpan) =
            max (rawInterval * card.Options.MatureCardsIntervalFactor)
                (TimeSpan.FromDays 1.0 |> previousInterval.Add)
            |> min card.Options.MatureCardsMaximumInterval
        let delta = DateTime.UtcNow - card.Due |> max TimeSpan.Zero
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

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
        let interval latenessCoefficient easeFactor (previousInterval: TimeSpan) =
            let max(a: TimeSpan)(b: TimeSpan) =
                if a.Ticks > b.Ticks then a else b
            max (DateTime.UtcNow - card.Due |> (*) latenessCoefficient |> (+) card.Interval |> (*) easeFactor |> (*) card.Options.MatureCardsIntervalFactor)
                (TimeSpan.FromDays 1.0 |> previousInterval.Add)
        let hard = interval 0.25 1.2 card.Interval
        let good = interval 0.50 card.EaseFactor hard
        let easy = interval 1.00 (card.EaseFactor * card.Options.MatureCardsEaseFactorEasyBonusFactor) good
        function
        | Again -> card.Options.NewCardsSteps.Head
        | Hard -> hard
        | Good -> good
        | Easy -> easy
    match card.MemorizationState with
    | New 
    | Learning -> intervalOfNewOrLearning card score
    | Mature -> intervalOfMature card score

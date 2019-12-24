namespace CardOverflow.Api

open CardOverflow.Pure
open System
open Serilog

type Scheduler(randomProvider: RandomProvider, time: TimeProvider) =
    let max a b = if a > b then a else b
    let min a b = if a < b then a else b
    let equals a b threshold = abs(a-b) < threshold

    let rawInterval (card: QuizCard) score =
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
                | None -> graduatingInterval, Interval graduatingInterval
            | Easy -> easyInterval, Interval easyInterval
        let calculateInterval previousInterval =
            let interval(previousInterval: TimeSpan) (rawInterval: TimeSpan) =
                max (rawInterval * card.Options.MatureCardsIntervalFactor)
                    (TimeSpan.FromDays 1. |> previousInterval.Add)
                |> min (TimeSpanInt16.value card.Options.MatureCardsMaximumInterval)
            let delta = time.utcNow - card.Due |> max TimeSpan.Zero
            let hard = interval previousInterval <| previousInterval * card.Options.MatureCardsHardIntervalFactor
            let good = interval hard (delta * 0.5 |> (+) previousInterval |> (*) card.EaseFactor)
            let easy = interval good (delta * 1.  |> (+) previousInterval |> (*) card.EaseFactor |> (*) card.Options.MatureCardsEaseFactorEasyBonusFactor)
            match score with
            | Again -> card.Options.NewCardsSteps.Head, 0.
            | Hard -> hard, card.EaseFactor - 0.15
            | Good -> good, card.EaseFactor
            | Easy -> easy, max (card.EaseFactor + 0.15) 1.3
        match card.IntervalOrStepsIndex with
        | NewStepsIndex i ->
            calculateStepsInterval
                NewStepsIndex
                card.Options.NewCardsSteps
                card.Options.NewCardsGraduatingInterval
                card.Options.NewCardsEasyInterval
                i,
            card.Options.NewCardsStartingEaseFactor
        | LapsedStepsIndex i ->
            calculateStepsInterval
                LapsedStepsIndex
                card.Options.LapsedCardsSteps
                card.Options.NewCardsGraduatingInterval // medTODO consider an option for this
                card.Options.NewCardsGraduatingInterval // medTODO actually the card options are all screwed up, refactor the entire scheduler later when you figure out how the hell the Anki one works
                i,
            card.Options.LapsedCardsNewIntervalFactor
        | Interval i ->
            let interval, ease = calculateInterval i
            (interval, Interval interval), ease

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

    member __.Calculate (card: QuizCard) score =
        rawInterval card score
        |> fun ((interval, intervalOrSteps), easeFactor) -> fuzz interval, intervalOrSteps, easeFactor

    member __.Intervals (card: QuizCard) =
        rawInterval card Again |> fst |> fst |> ViewLogic.toString,
        rawInterval card Hard |> fst |> fst |> ViewLogic.toString,
        rawInterval card Good |> fst |> fst |> ViewLogic.toString,
        rawInterval card Easy |> fst |> fst |> ViewLogic.toString

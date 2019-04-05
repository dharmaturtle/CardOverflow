module Scheduler

open CardOverflow.Api

let schedule card score =
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
        function
        | Again -> card.Options.NewCardsSteps.Head
        | Hard -> failwith "not implemented"
        | Good -> failwith "not implemented"
        | Easy -> failwith "not implemented"
    match card.MemorizationState with
    | New 
    | Learning -> intervalOfNewOrLearning card score
    | Mature -> intervalOfMature card score

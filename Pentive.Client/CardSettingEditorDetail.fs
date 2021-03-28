module Pentive.Client.CardSettingEditorDetail

open Domain.User
open CardOverflow.Pure
open System
open Elmish
open Bolero
open Bolero.Html

type Msg =
    | Saved
    | SetAsDefault

    | NameUpdated of string
    | ShowAnswerTimerUpdated of bool
    | AutomaticallyPlayAudioUpdated of bool
    | ReplayQuestionAudioOnAnswerUpdated of bool
    | NewCardsStepsUpdated of string
    | NewCardsMaxPerDayUpdated of int
    | NewCardsGraduatingIntervalUpdated of string
    | NewCardsEasyIntervalUpdated of string
    | NewCardsStartingEaseFactorUpdated of double
    | NewCardsBuryRelatedUpdated of bool
    | MatureCardsMaxPerDayUpdated of int
    | MatureCardsEaseFactorEasyBonusFactorUpdated of double
    | MatureCardsIntervalFactorUpdated of double
    | MatureCardsMaximumIntervalUpdated of string
    | MatureCardsHardIntervalFactorUpdated of double
    | MatureCardsBuryRelatedUpdated of bool
    | LapsedCardsStepsUpdated of string
    | LapsedCardsNewIntervalFactorUpdated of float
    | LapsedCardsMinimumIntervalUpdated of string
    | LapsedCardsLeechThresholdUpdated of int

type Cmd =
    | Save
    | MakeDefault of Guid

type DetailTemplate = Template<"wwwroot/CardSettingEditorDetail.html">

let update message (model: CardSetting) =
    let parseDuration s =
        (NodaTime.Text.DurationPattern.Create("", Globalization.CultureInfo.CurrentCulture).Parse s) // highTODO fix, also add validation
            .Value
    match message with
    | Saved                                          -> model
    | SetAsDefault                                   -> model

    | NameUpdated                                  x -> { model with Name                                 = x }
    | ShowAnswerTimerUpdated                       x -> { model with ShowAnswerTimer                      = x }
    | AutomaticallyPlayAudioUpdated                x -> { model with AutomaticallyPlayAudio               = x }
    | ReplayQuestionAudioOnAnswerUpdated           x -> { model with ReplayQuestionAudioOnAnswer          = x }
    | NewCardsStepsUpdated                         x -> { model with NewCardsSteps                        = x |> String.split ',' |> Array.map parseDuration |> List.ofArray }
    | NewCardsMaxPerDayUpdated                     x -> { model with NewCardsMaxPerDay                    = x }
    | NewCardsGraduatingIntervalUpdated            x -> { model with NewCardsGraduatingInterval           = parseDuration x }
    | NewCardsEasyIntervalUpdated                  x -> { model with NewCardsEasyInterval                 = parseDuration x }
    | NewCardsStartingEaseFactorUpdated            x -> { model with NewCardsStartingEaseFactor           = x }
    | NewCardsBuryRelatedUpdated                   x -> { model with NewCardsBuryRelated                  = x }
    | MatureCardsMaxPerDayUpdated                  x -> { model with MatureCardsMaxPerDay                 = x }
    | MatureCardsEaseFactorEasyBonusFactorUpdated  x -> { model with MatureCardsEaseFactorEasyBonusFactor = x }
    | MatureCardsIntervalFactorUpdated             x -> { model with MatureCardsIntervalFactor            = x }
    | MatureCardsMaximumIntervalUpdated            x -> { model with MatureCardsMaximumInterval           = parseDuration x }
    | MatureCardsHardIntervalFactorUpdated         x -> { model with MatureCardsHardIntervalFactor        = x }
    | MatureCardsBuryRelatedUpdated                x -> { model with MatureCardsBuryRelated               = x }
    | LapsedCardsStepsUpdated                      x -> { model with LapsedCardsSteps                     = x |> String.split ',' |> Array.map parseDuration |> List.ofArray }
    | LapsedCardsNewIntervalFactorUpdated          x -> { model with LapsedCardsNewIntervalFactor         = x }
    | LapsedCardsMinimumIntervalUpdated            x -> { model with LapsedCardsMinimumInterval           = parseDuration x }
    | LapsedCardsLeechThresholdUpdated             x -> { model with LapsedCardsLeechThreshold            = x }

let generate message (model: CardSetting) =
    match message with
        | Saved          -> Save                    |> Some
        | SetAsDefault   -> model.Id |> MakeDefault |> Some
        | _ -> None

let view dispatch (model: CardSetting) =
    let dt = DetailTemplate()
    let defaultButton =
        if model.IsDefault then
            DetailTemplate.IsDefaultButton().Elt()
        else
            DetailTemplate.SetAsDefaultButton()
                .SetAsDefault(fun _ -> SetAsDefault |> dispatch)
                .Elt()
    dt
        .Save(fun _ -> Saved |> dispatch)
        .DefaultButton(defaultButton)

        .Name(                                model.Name                                , fun x -> x |> NameUpdated                                 |> dispatch)
        .ShowAnswerTimer(                     model.ShowAnswerTimer                     , fun x -> x |> ShowAnswerTimerUpdated                      |> dispatch)
        .AutomaticallyPlayAudio(              model.AutomaticallyPlayAudio              , fun x -> x |> AutomaticallyPlayAudioUpdated               |> dispatch)
        .ReplayQuestionAudioOnAnswer(         model.ReplayQuestionAudioOnAnswer         , fun x -> x |> ReplayQuestionAudioOnAnswerUpdated          |> dispatch)
        .NewCardsSteps(                string model.NewCardsSteps                       , fun x -> x |> NewCardsStepsUpdated                        |> dispatch)
        .NewCardsMaxPerDay(               int model.NewCardsMaxPerDay                   , fun x -> x |> NewCardsMaxPerDayUpdated                    |> dispatch)
        .NewCardsGraduatingInterval(   string model.NewCardsGraduatingInterval          , fun x -> x |> NewCardsGraduatingIntervalUpdated           |> dispatch)
        .NewCardsEasyInterval(         string model.NewCardsEasyInterval                , fun x -> x |> NewCardsEasyIntervalUpdated                 |> dispatch)
        .NewCardsStartingEaseFactor(          model.NewCardsStartingEaseFactor          , fun x -> x |> NewCardsStartingEaseFactorUpdated           |> dispatch)
        .NewCardsBuryRelated(                 model.NewCardsBuryRelated                 , fun x -> x |> NewCardsBuryRelatedUpdated                  |> dispatch)
        .MatureCardsMaxPerDay(            int model.MatureCardsMaxPerDay                , fun x -> x |> MatureCardsMaxPerDayUpdated                 |> dispatch)
        .MatureCardsEaseFactorEasyBonusFactor(model.MatureCardsEaseFactorEasyBonusFactor, fun x -> x |> MatureCardsEaseFactorEasyBonusFactorUpdated |> dispatch)
        .MatureCardsIntervalFactor(           model.MatureCardsIntervalFactor           , fun x -> x |> MatureCardsIntervalFactorUpdated            |> dispatch)
        .MatureCardsMaximumInterval(   string model.MatureCardsMaximumInterval          , fun x -> x |> MatureCardsMaximumIntervalUpdated           |> dispatch)
        .MatureCardsHardIntervalFactor(       model.MatureCardsHardIntervalFactor       , fun x -> x |> MatureCardsHardIntervalFactorUpdated        |> dispatch)
        .MatureCardsBuryRelated(              model.MatureCardsBuryRelated              , fun x -> x |> MatureCardsBuryRelatedUpdated               |> dispatch)
        .LapsedCardsSteps(             string model.LapsedCardsSteps                    , fun x -> x |> LapsedCardsStepsUpdated                     |> dispatch)
        .LapsedCardsNewIntervalFactor(        model.LapsedCardsNewIntervalFactor        , fun x -> x |> LapsedCardsNewIntervalFactorUpdated         |> dispatch)
        .LapsedCardsMinimumInterval(   string model.LapsedCardsMinimumInterval          , fun x -> x |> LapsedCardsMinimumIntervalUpdated           |> dispatch)
        .LapsedCardsLeechThreshold(       int model.LapsedCardsLeechThreshold           , fun x -> x |> LapsedCardsLeechThresholdUpdated            |> dispatch)
        .Elt()

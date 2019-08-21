namespace CardOverflow.Sanitation

open Microsoft.FSharp.Core.Operators.Checked
open System.IO
open System.Linq
open System.IO.Compression
open Helpers
open System
open CardOverflow.Debug
open CardOverflow.Pure
open CardOverflow.Api
open CardOverflow.Entity

module SanitizeHistoryRepository =
    let AddAndSaveAsync (db: CardOverflowDb) userId cardId score timestamp interval easeFactor (timeFromSeeingQuestionToScore: TimeSpan) =
        HistoryEntity(
            UserId = userId,
            CardId = cardId,
            Score = Score.toDb score,
            Timestamp = timestamp,
            IntervalWithUnusedStepsIndex = (interval |> Interval |> AcquiredCard.intervalToDb),
            EaseFactorInPermille = (easeFactor * 1000. |> Math.Round |> int16),
            TimeFromSeeingQuestionToScoreInSecondsPlus32768 = (timeFromSeeingQuestionToScore.TotalSeconds + float Int16.MinValue |> int16)
        ) |> HistoryRepository.addAndSaveAsync db

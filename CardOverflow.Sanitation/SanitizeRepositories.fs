namespace CardOverflow.Sanitation

open Microsoft.FSharp.Core.Operators.Checked
open System.Linq
open Helpers
open System
open CardOverflow.Debug
open CardOverflow.Pure
open CardOverflow.Api
open CardOverflow.Entity
open System.ComponentModel.DataAnnotations

[<CLIMutable>]
type CommentText = {
    [<StringLength(500, ErrorMessage = "Comment must be less than 500 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Text: string
}

module SanitizeCommentRepository =
    let AddAndSaveAsync (db: CardOverflowDb) (time: TimeProvider) (comment: CommentText) facetId email = // lowTODO add idempotency key
        let userId = db.User.First(fun x -> x.Email = email).Id // lowTODO is there a way to get the userId directly from the UI?
        CommentFacetEntity(
            FacetId = facetId,
            UserId = userId,
            Text = comment.Text,
            Created = time.utcNow
        ) |> CommentRepository.addAndSaveAsync db

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

module SanitizeConceptRepository =
    let Update (db: CardOverflowDb) userId conceptId conceptName =
        let concept = db.Concept.First(fun x -> x.Id = conceptId)
        if concept.MaintainerId = userId
        then Ok <| ConceptRepository.Update db conceptId conceptName
        else Error "You aren't that concept's maintainer."

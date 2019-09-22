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
    let AddAndSaveAsync (db: CardOverflowDb) (time: TimeProvider) (comment: CommentText) cardId email = // lowTODO add idempotency key
        let userId = db.User.First(fun x -> x.Email = email).Id // lowTODO is there a way to get the userId directly from the UI?
        CommentCardEntity(
            CardId = cardId,
            UserId = userId,
            Text = comment.Text,
            Created = time.utcNow
        ) |> CommentRepository.addAndSaveAsync db

[<CLIMutable>]
type TagText = {
    [<StringLength(250, ErrorMessage = "Tag must be less than 250 characters.")>] // medTODO 250 needs to be tied to the DB max somehow
    Text: string
}

module SanitizeTagRepository =
    let AddTo (db: CardOverflowDb) (tag: TagText) userId cardId =
        db.AcquiredCard.FirstOrDefault(fun x -> x.UserId = userId && x.CardInstance.CardId = cardId)
        |> Option.ofObj
        |> function
        | Some x -> Ok <| TagRepository.AddTo db userId tag.Text x.Id
        | None -> Error "You haven't gotten that card."

module SanitizeHistoryRepository =
    let AddAndSaveAsync (db: CardOverflowDb) acquiredCardId score timestamp interval easeFactor (timeFromSeeingQuestionToScore: TimeSpan) =
        HistoryEntity(
            AcquiredCardId = acquiredCardId,
            Score = Score.toDb score,
            Timestamp = timestamp,
            IntervalWithUnusedStepsIndex = (interval |> Interval |> IntervalOrStepsIndex.intervalToDb),
            EaseFactorInPermille = (easeFactor * 1000. |> Math.Round |> int16),
            TimeFromSeeingQuestionToScoreInSecondsPlus32768 = (timeFromSeeingQuestionToScore.TotalSeconds + float Int16.MinValue |> int16)
        ) |> HistoryRepository.addAndSaveAsync db

module SanitizeCardRepository =
    let Update (db: CardOverflowDb) authorId (acquiredCard: AcquiredCard) = // medTODO how do we know that the card id hasn't been tampered with? It could be out of sync with card instance id
        let card = db.Card.First(fun x -> x.Id = acquiredCard.CardId)
        if card.AuthorId = authorId
        then Ok <| CardRepository.UpdateFieldsToNewInstance db acquiredCard
        else Error "You aren't that card's author."

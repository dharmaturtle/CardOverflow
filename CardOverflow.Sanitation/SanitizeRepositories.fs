namespace CardOverflow.Sanitation

open System.Collections.Generic
open Microsoft.EntityFrameworkCore
open FsToolkit.ErrorHandling
open FSharp.Text.RegexProvider
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
    let validate (db: CardOverflowDb) tag userId cardId action = // medTODO tag length needs validation
        db.AcquiredCard.FirstOrDefault(fun x -> x.UserId = userId && x.CardInstance.CardId = cardId)
        |> function
        | null -> Error "You haven't gotten that card."
        | card -> Ok <| action card.Id
    let AddTo db tag userId cardId =
        validate db tag.Text userId cardId
            <| TagRepository.AddTo db tag.Text
    let DeleteFrom db tag userId cardId =
        validate db tag userId cardId
            <| TagRepository.DeleteFrom db tag

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

[<CLIMutable>]
type AddRelationshipCommand = {
    [<Required>]
    [<StringLength(250, ErrorMessage = "Name must be less than 250 characters.")>]
    Name: string
    [<Required>]
    SourceId: int
    [<Required>]
    TargetLink: string
}
type CardIdRegex = Regex< """(?<cardId>\d+)$""" >
module SanitizeRelationshipRepository =
    let GetCardId input =
        let x = CardIdRegex().TypedMatch input
        if x.Success 
        then Ok <| int x.Value
        else Error "Couldn't find the card ID"
    let Add (db: CardOverflowDb) userId command = result {
        let! targetId = GetCardId command.TargetLink
        return!
            if  db.AcquiredCard.Where(fun x -> x.UserId = userId).Any(fun x -> x.CardInstance.CardId = targetId) && 
                db.AcquiredCard.Where(fun x -> x.UserId = userId).Any(fun x -> x.CardInstance.CardId = command.SourceId) then
                RelationshipEntity(
                    SourceId = command.SourceId,
                    TargetId = targetId,
                    Name = command.Name)
                |> RelationshipRepository.addAndSaveAsync db
                |> Ok
            else Error "You must have acquired both cards!"
        }

module SanitizeCardRepository =
    let Update (db: CardOverflowDb) authorId (acquiredCard: AcquiredCard) = // medTODO how do we know that the card id hasn't been tampered with? It could be out of sync with card instance id
        let card = db.Card.First(fun x -> x.Id = acquiredCard.CardId)
        if card.AuthorId = authorId
        then Ok <| CardRepository.UpdateFieldsToNewInstance db acquiredCard
        else Error "You aren't that card's author."

[<CLIMutable>]
type PotentialSignupCommand = {
    [<Required>]
    [<EmailAddress>]
    Email: string
    [<StringLength(1000, ErrorMessage = "Message must be less than 1000 characters.")>]
    Message: string
    OneIsAlpha2Beta3Ga: byte
}

module SanitizeLandingPage =
    let SignUp (db: CardOverflowDb) signUpForm =
        PotentialSignupsEntity(
            Email = signUpForm.Email,
            Message = signUpForm.Message,
            OneIsAlpha2Beta3Ga = signUpForm.OneIsAlpha2Beta3Ga,
            TimeStamp = DateTime.UtcNow
        ) |> db.PotentialSignups.AddI
        db.SaveChangesAsyncI()

namespace CardOverflow.Sanitation

open System.Threading.Tasks
open CardOverflow.Pure.Core
open LoadersAndCopiers
open FSharp.Control.Tasks
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
type ViewDeck = {
    Id: int
    UserId: int
    [<StringLength(128, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 128 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Name: string
    [<StringLength(256, ErrorMessage = "Query must be less than 256 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Query: string
} with
    member this.copyTo (e: DeckEntity) =
        e.UserId <- this.UserId
        e.Name <- this.Name
        e.Query <- this.Query
    member this.copyToNew =
        let e = DeckEntity()
        this.copyTo e
        e

module ViewDeck =
    let load (e: DeckEntity) = {
        Id = e.Id
        UserId = e.UserId
        Name = e.Name
        Query = e.Query
    }

type ViewDeckWithDue = {
    Id: int
    UserId: int
    [<StringLength(128, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 128 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Name: string
    [<StringLength(256, ErrorMessage = "Query must be less than 256 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Query: string
    Due: int
}

module ViewDeckWithDue =
    let load (db: CardOverflowDb) (e: DeckEntity) = {
        Id = e.Id
        UserId = e.UserId
        Name = e.Name
        Query = e.Query
        Due = CardRepository.GetDueCount db e.UserId e.Query
    }

module SanitizeDeckRepository =
    let UpsertAsync (db: CardOverflowDb) (deck: ViewDeck) = task {
        let! d = db.Deck.SingleOrDefaultAsync(fun x -> x.Id = deck.Id)
        let deck =
            match d with
            | null ->
                let d = deck.copyToNew
                db.Deck.AddI d
                d
            | d ->
                deck.copyTo d
                d
        do! db.SaveChangesAsyncI ()
        return deck.Id
        }
    let Delete (db: CardOverflowDb) userId (deck: ViewDeck) =
        let d = db.Deck.Single(fun x -> x.Id = deck.Id)
        if d.UserId = userId
        then Ok <| DeckRepository.Delete db d
        else Error <| "That isn't your deck"
    let Get (db: CardOverflowDb) userId = task {
        let! r = DeckRepository.Get db userId
        return r |> Seq.map ViewDeck.load |> toResizeArray
    }
    let GetWithDue (db: CardOverflowDb) userId = task {
        let! r = DeckRepository.Get db userId
        return r |> Seq.map (ViewDeckWithDue.load db) |> toResizeArray
    }
        
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
    let validate (db: CardOverflowDb) userId cardId action = // medTODO tag length needs validation
        db.AcquiredCard.FirstOrDefault(fun x -> x.UserId = userId && x.CardInstance.CardId = cardId)
        |> function
        | null -> Error "You haven't gotten that card."
        | card -> Ok <| action card.Id
    let AddTo db tag userId cardId =
        validate db userId cardId
            <| TagRepository.AddTo db tag.Text
    let DeleteFrom db tag userId cardId =
        validate db userId cardId
            <| TagRepository.DeleteFrom db tag

module SanitizeHistoryRepository =
    let AddAndSaveAsync (db: CardOverflowDb) acquiredCardId score timestamp interval easeFactor (timeFromSeeingQuestionToScore: TimeSpan) intervalOrSteps: Task<unit> = task {
        let! card = db.AcquiredCard.SingleAsync(fun x -> x.Id = acquiredCardId)
        card.Histories.Add
        <|  HistoryEntity(
                Score = Score.toDb score,
                Timestamp = timestamp,
                IntervalWithUnusedStepsIndex = (interval |> Interval |> IntervalOrStepsIndex.intervalToDb),
                EaseFactorInPermille = (easeFactor * 1000. |> Math.Round |> int16),
                TimeFromSeeingQuestionToScoreInSecondsPlus32768 = (timeFromSeeingQuestionToScore.TotalSeconds + float Int16.MinValue |> int16)
            )
        card.IntervalOrStepsIndex <- intervalOrSteps |> IntervalOrStepsIndex.intervalToDb
        card.Due <- DateTime.UtcNow + interval
        card.EaseFactorInPermille <- easeFactor * 1000. |> Math.Round |> int16
        card.IsLapsed <-
            match intervalOrSteps with
            | LapsedStepsIndex _ -> true
            | _ -> false
        do! db.SaveChangesAsyncI ()
        }

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
        let x = CardIdRegex().TypedMatch input // lowTODO make this a custom `ValidationAttribute` on TargetLink
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
                    Name = command.Name,
                    UserId = userId)
                |> RelationshipRepository.addAndSaveAsync db
                |> Ok
            else Error "You must have acquired both cards!"
        }
    let Remove db sourceId targetId userId name =
        RelationshipRepository.removeAndSaveAsync db sourceId targetId userId name // don't eta reduce - consumed by C#

[<CLIMutable>]
type SearchCommand = {
    [<StringLength(250, ErrorMessage = "Query must be less than 250 characters.")>]
    Query: string
}

[<CLIMutable>]
type EditCardCommand = {
    [<Required>]
    [<StringLength(200, ErrorMessage = "The summary must be less than 200 characters")>]
    EditSummary: string
    FieldValues: EditFieldAndValue ResizeArray
    TemplateInstance: ViewCardTemplateInstance
} with
    member this.Backs = result {
        let values = this.FieldValues.Select(fun x -> x.Value) |> List.ofSeq
        let! max = AnkiImportLogic.maxClozeIndex values "Something's wrong with your cloze indexes."
        return [1 .. max] |> List.map byte |> List.map (fun clozeIndex ->
            let zip =
                Seq.zip
                    <| this.FieldValues.Select(fun x -> x.Field.Name)
                    <| AnkiImportLogic.multipleClozeToSingleCloze clozeIndex values
                |> List.ofSeq
            CardHtml.generate
                zip
                this.TemplateInstance.QuestionTemplate
                this.TemplateInstance.AnswerTemplate
                this.TemplateInstance.Css
            |> fun (_, back, _, _) -> back
            ) |> toResizeArray }

module SanitizeCardRepository =
    let getEdit (db: CardOverflowDb) cardInstanceId = task {
        let! instance =
            db.CardInstance
                .Include(fun x -> x.CardTemplateInstance)
                .Include(fun x -> x.CommunalFieldInstance_CardInstances :> IEnumerable<_>)
                    .ThenInclude(fun (x: CommunalFieldInstance_CardInstanceEntity) -> x.CommunalFieldInstance.CommunalFieldInstance_CardInstances)
                .SingleOrDefaultAsync(fun x -> x.Id = cardInstanceId)
        return
            match instance with
            | null -> Error "Card instance not found"
            | instance ->
                let communalCardInstanceIdsAndValueByField =
                    instance.CommunalFieldInstance_CardInstances
                        .Select(fun x ->
                            x.CommunalFieldInstance.FieldName,
                            ( x.CommunalFieldInstance.Value,
                              x.CommunalFieldInstance.CommunalFieldInstance_CardInstances.Select(fun x -> x.CardInstanceId) |> List.ofSeq))
                    |> Map.ofSeq
                {   EditSummary = ""
                    FieldValues =
                        EditFieldAndValue.load
                            <| Fields.fromString instance.CardTemplateInstance.Fields
                            <| instance.FieldValues
                            <| communalCardInstanceIdsAndValueByField
                    TemplateInstance = instance.CardTemplateInstance |> CardTemplateInstance.load |> ViewCardTemplateInstance.load
                } |> Ok }
    let Update (db: CardOverflowDb) authorId (acquiredCard: AcquiredCard) (command: EditCardCommand) = task { // medTODO how do we know that the card id hasn't been tampered with? It could be out of sync with card instance id
        let update () =
            {   FieldValues = command.FieldValues.Select(fun x -> { Field = x.Field; Value = x.Value}).ToList()
                TemplateInstance = command.TemplateInstance |> ViewCardTemplateInstance.copyTo
            } |> CardRepository.UpdateFieldsToNewInstance db acquiredCard command.EditSummary
            |> Ok
        let! card = db.Card.SingleOrDefaultAsync(fun x -> x.Id = acquiredCard.CardId)
        return
            match card with
            | null ->
                update ()
            | card ->
                if card.AuthorId = authorId
                then update ()
                else Error "You aren't that card's author."
        }
    let SearchAsync (db: CardOverflowDb) userId pageNumber searchCommand =
        CardRepository.SearchAsync db userId pageNumber searchCommand.Query
    let GetAcquiredPages (db: CardOverflowDb) userId pageNumber searchCommand =
        CardRepository.GetAcquiredPages db userId pageNumber searchCommand.Query

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

[<CLIMutable>]
type Feedback = {
    [<Required>]
    [<StringLength(50, ErrorMessage = "Title must be less than 50 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Title: string
    [<Required>]
    [<StringLength(1000, ErrorMessage = "Description must be less than 1000 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Description: string
    [<Required>]
    Priority: string
}
module SanitizeFeedback =
    let addAndSaveAsync (db: CardOverflowDb) userId feedback =
        FeedbackRepository.addAndSaveAsync db userId feedback.Title feedback.Description (feedback.Priority |> byte |> Nullable)

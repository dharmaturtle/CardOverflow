namespace CardOverflow.Sanitation

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
open CardOverflow.Pure.Core
open CardOverflow.Api
open CardOverflow.Entity
open System.ComponentModel.DataAnnotations
open LoadersAndCopiers

[<CLIMutable>]
type ViewField = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be less than 100 characters.")>] // lowTODO the 100 is completely made up; what's the real max?
    Name: string
    Font: string
    FontSize: byte
    IsRightToLeft: bool
    Ordinal: int
    IsSticky: bool
}

module ViewField =
    let load (bznz: Field): ViewField = {
        Name = bznz.Name
        Font = bznz.Font
        FontSize = bznz.FontSize
        IsRightToLeft = bznz.IsRightToLeft
        Ordinal = bznz.Ordinal |> int
        IsSticky = bznz.IsSticky
    }
    let copyTo (view: ViewField): Field = {
        Name = view.Name
        Font = view.Font
        FontSize = view.FontSize
        IsRightToLeft = view.IsRightToLeft
        Ordinal = view.Ordinal |> byte
        IsSticky = view.IsSticky
    }

[<CLIMutable>]
type ViewCardTemplateInstance = {
    Id: int
    [<Required>]
    [<StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3-100 characters long.")>]
    Name: string
    CardTemplateId: int
    Css: string
    Fields: ViewField ResizeArray
    Created: DateTime
    Modified: DateTime option
    LatexPre: string
    LatexPost: string
    AcquireHash: byte[]
    QuestionTemplate: string
    AnswerTemplate: string
    ShortQuestionTemplate: string
    ShortAnswerTemplate: string
    [<StringLength(200, ErrorMessage = "The summary must be less than 200 characters")>]
    EditSummary: string
}

module ViewCardTemplateInstance =
    let load (bznz: CardTemplateInstance) = {
        Id = bznz.Id
        Name = bznz.Name
        CardTemplateId = bznz.CardTemplateId
        Css = bznz.Css
        Fields = bznz.Fields |> List.map ViewField.load |> toResizeArray
        Created = bznz.Created
        Modified = bznz.Modified
        LatexPre = bznz.LatexPre
        LatexPost = bznz.LatexPost
        AcquireHash = bznz.AcquireHash
        QuestionTemplate = bznz.QuestionTemplate
        AnswerTemplate = bznz.AnswerTemplate
        ShortQuestionTemplate = bznz.ShortQuestionTemplate
        ShortAnswerTemplate = bznz.ShortAnswerTemplate
        EditSummary = bznz.EditSummary
    }
    let copyTo (view: ViewCardTemplateInstance): CardTemplateInstance = {
        Id = view.Id
        Name = view.Name
        CardTemplateId = view.CardTemplateId
        Css = view.Css
        Fields = view.Fields |> Seq.map ViewField.copyTo |> Seq.toList
        Created = view.Created
        Modified = view.Modified
        LatexPre = view.LatexPre
        LatexPost = view.LatexPost
        AcquireHash = view.AcquireHash
        QuestionTemplate = view.QuestionTemplate
        AnswerTemplate = view.AnswerTemplate
        ShortQuestionTemplate = view.ShortQuestionTemplate
        ShortAnswerTemplate = view.ShortAnswerTemplate
        EditSummary = view.EditSummary
    }

[<CLIMutable>]
type ViewCardTemplateWithAllInstances = {
    Id: int
    AuthorId: int
    Instances: ViewCardTemplateInstance ResizeArray
    Editable: ViewCardTemplateInstance
} with
    static member load (entity: CardTemplateEntity) =
        let instances =
            entity.CardTemplateInstances
            |> Seq.sortByDescending (fun x -> x.Modified |?? lazy x.Created)
            |> Seq.map (CardTemplateInstance.load >> ViewCardTemplateInstance.load)
            |> toResizeArray
        {   Id = entity.Id
            AuthorId = entity.AuthorId
            Instances = instances
            Editable = {
                instances.First() with
                    Id = 0
                    EditSummary = "" }}

module SanitizeCardTemplate =
    let AllInstances (db: CardOverflowDb) templateId = task {
        let! template =
            db.CardTemplate
                .Include(fun x -> x.CardTemplateInstances)
                .SingleOrDefaultAsync(fun x -> templateId = x.Id)
        return
            match template with
            | null -> Error "That template doesn't exist"
            | x -> Ok <| ViewCardTemplateWithAllInstances.load x
        }
    let GetMine (db: CardOverflowDb) userId = task {
        let! x =
            db.CardTemplate
                .Include(fun x-> x.CardTemplateInstances)
                .Where(fun x -> x.CardTemplateInstances.Any(fun x -> x.CardInstances.Any(fun x -> x.AcquiredCards.Any(fun x -> x.UserId = userId))))
                .ToListAsync()
        return x |> Seq.map ViewCardTemplateWithAllInstances.load
        }
    let Update (db: CardOverflowDb) userId (instance: ViewCardTemplateInstance) =
        let cardTemplate = db.CardTemplate.Single(fun x -> x.Id = instance.CardTemplateId)
        if cardTemplate.AuthorId = userId
        then Ok <| CardTemplateRepository.UpdateFieldsToNewInstance db (ViewCardTemplateInstance.copyTo instance)
        else Error <| "You aren't that this template's author."
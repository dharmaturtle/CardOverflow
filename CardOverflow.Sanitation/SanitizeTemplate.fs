namespace CardOverflow.Sanitation

open System.Threading.Tasks
open X.PagedList
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
type ViewTemplateInstance = {
    Id: int
    [<Required>]
    [<StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3-100 characters long.")>]
    Name: string
    TemplateId: int
    Css: string
    Fields: ViewField ResizeArray
    Created: DateTime
    Modified: DateTime option
    LatexPre: string
    LatexPost: string
    QuestionTemplate: string
    AnswerTemplate: string
    ShortQuestionTemplate: string
    ShortAnswerTemplate: string
    [<StringLength(200, ErrorMessage = "The summary must be less than 200 characters")>]
    EditSummary: string
} with
    member this.IsCloze =
        Cloze.isCloze this.QuestionTemplate
    member this.ClozeFields =
        AnkiImportLogic.clozeFields this.QuestionTemplate

module ViewTemplateInstance =
    let load (bznz: TemplateInstance) = {
        Id = bznz.Id
        Name = bznz.Name
        TemplateId = bznz.TemplateId
        Css = bznz.Css
        Fields = bznz.Fields |> List.map ViewField.load |> toResizeArray
        Created = bznz.Created
        Modified = bznz.Modified
        LatexPre = bznz.LatexPre
        LatexPost = bznz.LatexPost
        QuestionTemplate = bznz.QuestionTemplate
        AnswerTemplate = bznz.AnswerTemplate
        ShortQuestionTemplate = bznz.ShortQuestionTemplate
        ShortAnswerTemplate = bznz.ShortAnswerTemplate
        EditSummary = bznz.EditSummary
    }
    let copyTo (view: ViewTemplateInstance): TemplateInstance = {
        Id = view.Id
        Name = view.Name
        TemplateId = view.TemplateId
        Css = view.Css
        Fields = view.Fields |> Seq.map ViewField.copyTo |> Seq.toList
        Created = view.Created
        Modified = view.Modified
        LatexPre = view.LatexPre
        LatexPost = view.LatexPost
        QuestionTemplate = view.QuestionTemplate
        AnswerTemplate = view.AnswerTemplate
        ShortQuestionTemplate = view.ShortQuestionTemplate
        ShortAnswerTemplate = view.ShortAnswerTemplate
        EditSummary = view.EditSummary
    }

type ViewSearchTemplateInstance = {
    Id: int
    Name: string
    TemplateId: int
    Css: string
    Fields: ViewField ResizeArray
    Created: DateTime
    Modified: DateTime option
    LatexPre: string
    LatexPost: string
    QuestionTemplate: string
    AnswerTemplate: string
    ShortQuestionTemplate: string
    ShortAnswerTemplate: string
    EditSummary: string
    TemplateUsers: int
    IsAcquired: bool
}

module ViewSearchTemplateInstance =
    let load templateUsers isAcquired (bznz: TemplateInstance) = {
        Id = bznz.Id
        Name = bznz.Name
        TemplateId = bznz.TemplateId
        Css = bznz.Css
        Fields = bznz.Fields |> List.map ViewField.load |> toResizeArray
        Created = bznz.Created
        Modified = bznz.Modified
        LatexPre = bznz.LatexPre
        LatexPost = bznz.LatexPost
        QuestionTemplate = bznz.QuestionTemplate
        AnswerTemplate = bznz.AnswerTemplate
        ShortQuestionTemplate = bznz.ShortQuestionTemplate
        ShortAnswerTemplate = bznz.ShortAnswerTemplate
        EditSummary = bznz.EditSummary
        TemplateUsers = templateUsers
        IsAcquired = isAcquired
    }

[<CLIMutable>]
type ViewTemplateWithAllInstances = {
    Id: int
    AuthorId: int
    Instances: ViewTemplateInstance ResizeArray
    Editable: ViewTemplateInstance
} with
    static member load (entity: TemplateEntity) =
        let instances =
            entity.TemplateInstances
            |> Seq.sortByDescending (fun x -> x.Modified |?? lazy x.Created)
            |> Seq.map (TemplateInstance.load >> ViewTemplateInstance.load)
            |> toResizeArray
        {   Id = entity.Id
            AuthorId = entity.AuthorId
            Instances = instances
            Editable = {
                instances.First() with
                    Id = 0
                    EditSummary = "" }}
    static member initialize userId =
        let instance = TemplateInstance.initialize |> ViewTemplateInstance.load
        {   Id = 0
            AuthorId = userId
            Instances = [instance].ToList()
            Editable = instance
        }

module SanitizeTemplate =
    let latest (db: CardOverflowDb) templateId =
        TemplateRepository.latest db templateId |> TaskResult.map ViewTemplateInstance.load
    let instance (db: CardOverflowDb) instanceId =
        TemplateRepository.instance db instanceId |> TaskResult.map ViewTemplateInstance.load
    let AllInstances (db: CardOverflowDb) templateId = task {
        let! template =
            db.Template
                .Include(fun x -> x.TemplateInstances)
                .SingleOrDefaultAsync(fun x -> templateId = x.Id)
        return
            match template with
            | null -> sprintf "Template #%i doesn't exist" templateId |> Error
            | x -> Ok <| ViewTemplateWithAllInstances.load x
        }
    let Search (db: CardOverflowDb) (userId: int) (pageNumber: int) (searchTerm: string) = task {
        let! r =
            db.LatestTemplateInstance
                .Where(fun x ->
                    String.IsNullOrWhiteSpace searchTerm
                    //EF.Functions.FreeText(x.TemplateInstance.AnswerTemplate, searchTerm) || // medTODO add ElasticSearch
                    //EF.Functions.FreeText(x.TemplateInstance.Css, searchTerm) ||
                    //EF.Functions.FreeText(x.TemplateInstance.Fields, searchTerm) ||
                    //EF.Functions.FreeText(x.TemplateInstance.LatexPost, searchTerm) ||
                    //EF.Functions.FreeText(x.TemplateInstance.LatexPre, searchTerm) ||
                    //EF.Functions.FreeText(x.TemplateInstance.Name, searchTerm) ||
                    //EF.Functions.FreeText(x.TemplateInstance.QuestionTemplate, searchTerm) ||
                    //EF.Functions.FreeText(x.TemplateInstance.ShortAnswerTemplate, searchTerm) ||
                    //EF.Functions.FreeText(x.TemplateInstance.ShortQuestionTemplate, searchTerm)
                ).Select(fun x ->
                    x.Template.TemplateInstances.Select(fun x -> x.User_TemplateInstances.Count).ToList(), // lowTODO sum here
                    x.User_TemplateInstances.Any(fun x -> x.UserId = userId),
                    x
                ).ToPagedListAsync(pageNumber, 15)
        return {
            Results = r |> Seq.map (fun (users, isAcquired, l) ->
                l |> TemplateInstance.loadLatest |> ViewSearchTemplateInstance.load (users.Sum()) isAcquired) |> toResizeArray
            Details = {
                CurrentPage = r.PageNumber
                PageCount = r.PageCount
            }
        }}
    let GetMine (db: CardOverflowDb) userId = task {
        let! x =
            db.User_TemplateInstance
                .Include(fun x -> x.TemplateInstance.Template)
                .Where(fun x ->  x.UserId = userId)
                .ToListAsync()
        return x |> Seq.map (fun x -> ViewTemplateWithAllInstances.load x.TemplateInstance.Template) |> toResizeArray
        }
    let Update (db: CardOverflowDb) userId (instance: ViewTemplateInstance) =
        let update () = task {
            let! r = ViewTemplateInstance.copyTo instance |> TemplateRepository.UpdateFieldsToNewInstance db userId
            return r |> Ok
        }
        if instance.Fields.Count = instance.Fields.Select(fun x -> x.Name.ToLower()).Distinct().Count() then
            db.Template.SingleOrDefault(fun x -> x.Id = instance.TemplateId)
            |> function
            | null -> update ()
            | template ->
                if template.AuthorId = userId then 
                    update ()
                else Error "You aren't that this template's author." |> Task.FromResult
        else
            Error "Field names must differ" |> Task.FromResult

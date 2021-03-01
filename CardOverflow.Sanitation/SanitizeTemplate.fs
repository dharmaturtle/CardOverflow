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
open CardOverflow.Api
open CardOverflow.Entity
open System.ComponentModel.DataAnnotations
open LoadersAndCopiers
open NodaTime

type UserClaims = {
    Id: Guid
    DisplayName: string
    Email: string
} with
    static member init = {
        Id = Guid.Empty
        DisplayName = ""
        Email = ""
    }
    member this.IsAuthenticated = this.Id <> UserClaims.init.Id

[<CLIMutable>]
type ViewField = {
    [<Required>]
    [<StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be less than 100 characters.")>] // lowTODO the 100 is completely made up; what's the real max?
    Name: string
    IsRightToLeft: bool
    IsSticky: bool
}

module ViewField =
    let load (bznz: Field): ViewField = {
        Name = bznz.Name
        IsRightToLeft = bznz.IsRightToLeft
        IsSticky = bznz.IsSticky
    }
    let copyTo (view: ViewField): Field = {
        Name = view.Name
        IsRightToLeft = view.IsRightToLeft
        IsSticky = view.IsSticky
    }

[<CLIMutable>]
type ViewTemplateRevision = {
    Id: Guid
    [<Required>]
    [<StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3-100 characters long.")>]
    Name: string
    TemplateId: Guid
    Css: string
    Fields: ViewField ResizeArray
    Created: Instant
    Modified: Instant option
    LatexPre: string
    LatexPost: string
    CardTemplates: TemplateType
    [<StringLength(200, ErrorMessage = "The summary must be less than 200 characters")>]
    EditSummary: string
} with
    member this.IsCloze =
        match this.CardTemplates with
        | Cloze _ -> true
        | _ -> false
    member this.FirstCardTemplate =
        match this.CardTemplates with
        | Cloze t -> t
        | Standard ts -> ts.[0]
    member this.JustCardTemplates =
        match this.CardTemplates with
        | Cloze t -> [t]
        | Standard ts -> ts

module ViewTemplateRevision =
    let load (bznz: TemplateRevision) = {
        Id = bznz.Id
        Name = bznz.Name
        TemplateId = bznz.TemplateId
        Css = bznz.Css
        Fields = bznz.Fields |> List.map ViewField.load |> toResizeArray
        Created = bznz.Created
        Modified = bznz.Modified
        LatexPre = bznz.LatexPre
        LatexPost = bznz.LatexPost
        CardTemplates = bznz.CardTemplates
        EditSummary = bznz.EditSummary
    }
    let copyTo (view: ViewTemplateRevision): TemplateRevision = {
        Id = view.Id
        Name = view.Name
        TemplateId = view.TemplateId
        Css = view.Css
        Fields = view.Fields |> Seq.map ViewField.copyTo |> Seq.toList
        Created = view.Created
        Modified = view.Modified
        LatexPre = view.LatexPre
        LatexPost = view.LatexPost
        CardTemplates = view.CardTemplates
        EditSummary = view.EditSummary
    }

type ViewSearchTemplateRevision = {
    Id: Guid
    Name: string
    TemplateId: Guid
    Css: string
    Fields: ViewField ResizeArray
    Created: Instant
    Modified: Instant option
    LatexPre: string
    LatexPost: string
    CardTemplates: TemplateType
    EditSummary: string
    TemplateUsers: int
    IsCollected: bool
}

module ViewSearchTemplateRevision =
    let load templateUsers isCollected (bznz: TemplateRevision) = {
        Id = bznz.Id
        Name = bznz.Name
        TemplateId = bznz.TemplateId
        Css = bznz.Css
        Fields = bznz.Fields |> List.map ViewField.load |> toResizeArray
        Created = bznz.Created
        Modified = bznz.Modified
        LatexPre = bznz.LatexPre
        LatexPost = bznz.LatexPost
        CardTemplates = bznz.CardTemplates
        EditSummary = bznz.EditSummary
        TemplateUsers = templateUsers
        IsCollected = isCollected
    }

[<CLIMutable>]
type ViewTemplateWithAllRevisions = {
    Id: Guid
    AuthorId: Guid
    Revisions: ViewTemplateRevision ResizeArray
    Editable: ViewTemplateRevision
} with
    static member load (entity: TemplateEntity) =
        let revisions =
            entity.TemplateRevisions
            |> Seq.sortByDescending (fun x -> x.Modified |?? lazy x.Created)
            |> Seq.map (TemplateRevision.load >> ViewTemplateRevision.load)
            |> toResizeArray
        {   Id = entity.Id
            AuthorId = entity.AuthorId
            Revisions = revisions
            Editable = {
                revisions.First() with
                    Id = Guid.Empty
                    EditSummary = "" }}
    static member initialize userId templateRevisionId templateId =
        let revision = TemplateRevision.initialize templateRevisionId templateId |> ViewTemplateRevision.load
        {   Id = templateId
            AuthorId = userId
            Revisions = [revision].ToList()
            Editable = revision
        }

module SanitizeTemplate =
    let latest (db: CardOverflowDb) templateId =
        TemplateRepository.latest db templateId |> TaskResult.map ViewTemplateRevision.load
    let revision (db: CardOverflowDb) revisionId =
        TemplateRepository.revision db revisionId |> TaskResult.map ViewTemplateRevision.load
    let AllRevisions (db: CardOverflowDb) templateId = task {
        let! template =
            db.Template
                .Include(fun x -> x.TemplateRevisions)
                .SingleOrDefaultAsync(fun x -> templateId = x.Id)
        return
            match template with
            | null -> sprintf "Template #%A doesn't exist" templateId |> Error
            | x -> Ok <| ViewTemplateWithAllRevisions.load x
        }
    let Search (db: CardOverflowDb) (userId: Guid) (pageNumber: int) (searchTerm: string) = task {
        let plain, wildcard = FullTextSearch.parse searchTerm
        let! r =
            db.LatestTemplateRevision
                .Where(fun x ->
                    String.IsNullOrWhiteSpace searchTerm ||
                    x.Tsv.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
                ).Select(fun x ->
                    x.Template.TemplateRevisions.Select(fun x -> x.User_TemplateRevisions.Count).ToList(), // lowTODO sum here
                    x.User_TemplateRevisions.Any(fun x -> x.UserId = userId),
                    x
                ).ToPagedListAsync(pageNumber, 15)
        return {
            Results = r |> Seq.map (fun (users, isCollected, l) ->
                l |> TemplateRevision.load |> ViewSearchTemplateRevision.load (users.Sum()) isCollected) |> toResizeArray
            Details = {
                CurrentPage = r.PageNumber
                PageCount = r.PageCount
            }
        }}
    let GetMine (db: CardOverflowDb) userId = task {
        let! x =
            db.User_TemplateRevision
                .Where(fun x ->  x.UserId = userId)
                .Select(fun x -> x.TemplateRevision.Template)
                .Distinct()
                .Include(fun x -> x.TemplateRevisions)
                .ToListAsync()
        return x |> Seq.map ViewTemplateWithAllRevisions.load |> toResizeArray
        }
    let GetMineWith (db: CardOverflowDb) userId templateId = task {
        let! x =
            db.User_TemplateRevision
                .Where(fun x ->  x.UserId = userId || x.TemplateRevision.TemplateId = templateId)
                .Select(fun x -> x.TemplateRevision.Template)
                .Distinct()
                .Include(fun x -> x.TemplateRevisions)
                .ToListAsync()
        return x |> Seq.map ViewTemplateWithAllRevisions.load |> toResizeArray
        }
    let Update (db: CardOverflowDb) userId (revision: ViewTemplateRevision) =
        let update template = task {
            let! r = ViewTemplateRevision.copyTo revision |> TemplateRepository.UpdateFieldsToNewRevision db userId template
            return r |> Ok
        }
        if revision.Fields.Count = revision.Fields.Select(fun x -> x.Name.ToLower()).Distinct().Count() then
            db.Template.SingleOrDefault(fun x -> x.Id = revision.TemplateId)
            |> function
            | null -> update (TemplateEntity(Id = revision.TemplateId, AuthorId = userId))
            | template ->
                if template.AuthorId = userId then
                    update template
                else Error "You aren't that this template's author." |> Task.FromResult
        else
            Error "Field names must differ" |> Task.FromResult

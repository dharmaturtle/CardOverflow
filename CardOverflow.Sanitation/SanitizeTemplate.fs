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
        Id = Guid.Empty //bznz.Id
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
        Id = 0 //view.Id
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
        Id = Guid.Empty //bznz.Id
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

module SanitizeTemplate =
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

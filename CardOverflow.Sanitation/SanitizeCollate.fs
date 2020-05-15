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
    IsRightToLeft: bool
    Ordinal: int
    IsSticky: bool
}

module ViewField =
    let load (bznz: Field): ViewField = {
        Name = bznz.Name
        IsRightToLeft = bznz.IsRightToLeft
        Ordinal = bznz.Ordinal |> int
        IsSticky = bznz.IsSticky
    }
    let copyTo (view: ViewField): Field = {
        Name = view.Name
        IsRightToLeft = view.IsRightToLeft
        Ordinal = view.Ordinal |> byte
        IsSticky = view.IsSticky
    }

[<CLIMutable>]
type ViewCollateInstance = {
    Id: int
    [<Required>]
    [<StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3-100 characters long.")>]
    Name: string
    CollateId: int
    Css: string
    Fields: ViewField ResizeArray
    Created: DateTime
    Modified: DateTime option
    LatexPre: string
    LatexPost: string
    Templates: CollateType
    [<StringLength(200, ErrorMessage = "The summary must be less than 200 characters")>]
    EditSummary: string
} with
    member this.IsCloze =
        match this.Templates with
        | Cloze -> true
        | _ -> false
    member this.FirstTemplate =
        match this.Templates with
        | Cloze t -> t
        | Standard ts -> ts.[0]
    member this.JustTemplates =
        match this.Templates with
        | Cloze t -> [t]
        | Standard ts -> ts

module ViewCollateInstance =
    let load (bznz: CollateInstance) = {
        Id = bznz.Id
        Name = bznz.Name
        CollateId = bznz.CollateId
        Css = bznz.Css
        Fields = bznz.Fields |> List.map ViewField.load |> toResizeArray
        Created = bznz.Created
        Modified = bznz.Modified
        LatexPre = bznz.LatexPre
        LatexPost = bznz.LatexPost
        Templates = bznz.Templates
        EditSummary = bznz.EditSummary
    }
    let copyTo (view: ViewCollateInstance): CollateInstance = {
        Id = view.Id
        Name = view.Name
        CollateId = view.CollateId
        Css = view.Css
        Fields = view.Fields |> Seq.map ViewField.copyTo |> Seq.toList
        Created = view.Created
        Modified = view.Modified
        LatexPre = view.LatexPre
        LatexPost = view.LatexPost
        Templates = view.Templates
        EditSummary = view.EditSummary
    }

type ViewSearchCollateInstance = {
    Id: int
    Name: string
    CollateId: int
    Css: string
    Fields: ViewField ResizeArray
    Created: DateTime
    Modified: DateTime option
    LatexPre: string
    LatexPost: string
    Templates: CollateType
    EditSummary: string
    CollateUsers: int
    IsAcquired: bool
}

module ViewSearchCollateInstance =
    let load collateUsers isAcquired (bznz: CollateInstance) = {
        Id = bznz.Id
        Name = bznz.Name
        CollateId = bznz.CollateId
        Css = bznz.Css
        Fields = bznz.Fields |> List.map ViewField.load |> toResizeArray
        Created = bznz.Created
        Modified = bznz.Modified
        LatexPre = bznz.LatexPre
        LatexPost = bznz.LatexPost
        Templates = bznz.Templates
        EditSummary = bznz.EditSummary
        CollateUsers = collateUsers
        IsAcquired = isAcquired
    }

[<CLIMutable>]
type ViewCollateWithAllInstances = {
    Id: int
    AuthorId: int
    Instances: ViewCollateInstance ResizeArray
    Editable: ViewCollateInstance
} with
    static member load (entity: CollateEntity) =
        let instances =
            entity.CollateInstances
            |> Seq.sortByDescending (fun x -> x.Modified |?? lazy x.Created)
            |> Seq.map (CollateInstance.load >> ViewCollateInstance.load)
            |> toResizeArray
        {   Id = entity.Id
            AuthorId = entity.AuthorId
            Instances = instances
            Editable = {
                instances.First() with
                    Id = 0
                    EditSummary = "" }}
    static member initialize userId =
        let instance = CollateInstance.initialize |> ViewCollateInstance.load
        {   Id = 0
            AuthorId = userId
            Instances = [instance].ToList()
            Editable = instance
        }

module SanitizeCollate =
    let latest (db: CardOverflowDb) collateId =
        CollateRepository.latest db collateId |> TaskResult.map ViewCollateInstance.load
    let instance (db: CardOverflowDb) instanceId =
        CollateRepository.instance db instanceId |> TaskResult.map ViewCollateInstance.load
    let AllInstances (db: CardOverflowDb) collateId = task {
        let! collate =
            db.Collate
                .Include(fun x -> x.CollateInstances)
                .SingleOrDefaultAsync(fun x -> collateId = x.Id)
        return
            match collate with
            | null -> sprintf "Collate #%i doesn't exist" collateId |> Error
            | x -> Ok <| ViewCollateWithAllInstances.load x
        }
    let Search (db: CardOverflowDb) (userId: int) (pageNumber: int) (searchTerm: string) = task {
        let plain, wildcard = FullTextSearch.parse searchTerm
        let! r =
            db.LatestCollateInstance
                .Where(fun x ->
                    String.IsNullOrWhiteSpace searchTerm ||
                    x.TsVector.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
                ).Select(fun x ->
                    x.Collate.CollateInstances.Select(fun x -> x.User_CollateInstances.Count).ToList(), // lowTODO sum here
                    x.User_CollateInstances.Any(fun x -> x.UserId = userId),
                    x
                ).ToPagedListAsync(pageNumber, 15)
        return {
            Results = r |> Seq.map (fun (users, isAcquired, l) ->
                l |> CollateInstance.load |> ViewSearchCollateInstance.load (users.Sum()) isAcquired) |> toResizeArray
            Details = {
                CurrentPage = r.PageNumber
                PageCount = r.PageCount
            }
        }}
    let GetMine (db: CardOverflowDb) userId = task {
        let! x =
            db.User_CollateInstance
                .Where(fun x ->  x.UserId = userId)
                .Select(fun x -> x.CollateInstance.Collate)
                .Distinct()
                .Include(fun x -> x.CollateInstances)
                .ToListAsync()
        return x |> Seq.map ViewCollateWithAllInstances.load |> toResizeArray
        }
    let Update (db: CardOverflowDb) userId (instance: ViewCollateInstance) =
        let update () = task {
            let! r = ViewCollateInstance.copyTo instance |> CollateRepository.UpdateFieldsToNewInstance db userId
            return r |> Ok
        }
        if instance.Fields.Count = instance.Fields.Select(fun x -> x.Name.ToLower()).Distinct().Count() then
            db.Collate.SingleOrDefault(fun x -> x.Id = instance.CollateId)
            |> function
            | null -> update ()
            | collate ->
                if collate.AuthorId = userId then 
                    update ()
                else Error "You aren't that this collate's author." |> Task.FromResult
        else
            Error "Field names must differ" |> Task.FromResult

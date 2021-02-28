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
type ViewGrompleaf = {
    Id: Guid
    [<Required>]
    [<StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3-100 characters long.")>]
    Name: string
    GromplateId: Guid
    Css: string
    Fields: ViewField ResizeArray
    Created: Instant
    Modified: Instant option
    LatexPre: string
    LatexPost: string
    CardTemplates: GromplateType
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

module ViewGrompleaf =
    let load (bznz: Grompleaf) = {
        Id = bznz.Id
        Name = bznz.Name
        GromplateId = bznz.GromplateId
        Css = bznz.Css
        Fields = bznz.Fields |> List.map ViewField.load |> toResizeArray
        Created = bznz.Created
        Modified = bznz.Modified
        LatexPre = bznz.LatexPre
        LatexPost = bznz.LatexPost
        CardTemplates = bznz.CardTemplates
        EditSummary = bznz.EditSummary
    }
    let copyTo (view: ViewGrompleaf): Grompleaf = {
        Id = view.Id
        Name = view.Name
        GromplateId = view.GromplateId
        Css = view.Css
        Fields = view.Fields |> Seq.map ViewField.copyTo |> Seq.toList
        Created = view.Created
        Modified = view.Modified
        LatexPre = view.LatexPre
        LatexPost = view.LatexPost
        CardTemplates = view.CardTemplates
        EditSummary = view.EditSummary
    }

type ViewSearchGrompleaf = {
    Id: Guid
    Name: string
    GromplateId: Guid
    Css: string
    Fields: ViewField ResizeArray
    Created: Instant
    Modified: Instant option
    LatexPre: string
    LatexPost: string
    CardTemplates: GromplateType
    EditSummary: string
    GromplateUsers: int
    IsCollected: bool
}

module ViewSearchGrompleaf =
    let load gromplateUsers isCollected (bznz: Grompleaf) = {
        Id = bznz.Id
        Name = bznz.Name
        GromplateId = bznz.GromplateId
        Css = bznz.Css
        Fields = bznz.Fields |> List.map ViewField.load |> toResizeArray
        Created = bznz.Created
        Modified = bznz.Modified
        LatexPre = bznz.LatexPre
        LatexPost = bznz.LatexPost
        CardTemplates = bznz.CardTemplates
        EditSummary = bznz.EditSummary
        GromplateUsers = gromplateUsers
        IsCollected = isCollected
    }

[<CLIMutable>]
type ViewGromplateWithAllLeafs = {
    Id: Guid
    AuthorId: Guid
    Leafs: ViewGrompleaf ResizeArray
    Editable: ViewGrompleaf
} with
    static member load (entity: GromplateEntity) =
        let leafs =
            entity.Grompleafs
            |> Seq.sortByDescending (fun x -> x.Modified |?? lazy x.Created)
            |> Seq.map (Grompleaf.load >> ViewGrompleaf.load)
            |> toResizeArray
        {   Id = entity.Id
            AuthorId = entity.AuthorId
            Leafs = leafs
            Editable = {
                leafs.First() with
                    Id = Guid.Empty
                    EditSummary = "" }}
    static member initialize userId grompleafId gromplateId =
        let leaf = Grompleaf.initialize grompleafId gromplateId |> ViewGrompleaf.load
        {   Id = gromplateId
            AuthorId = userId
            Leafs = [leaf].ToList()
            Editable = leaf
        }

module SanitizeGromplate =
    let latest (db: CardOverflowDb) gromplateId =
        GromplateRepository.latest db gromplateId |> TaskResult.map ViewGrompleaf.load
    let leaf (db: CardOverflowDb) leafId =
        GromplateRepository.leaf db leafId |> TaskResult.map ViewGrompleaf.load
    let AllLeafs (db: CardOverflowDb) gromplateId = task {
        let! gromplate =
            db.Gromplate
                .Include(fun x -> x.Grompleafs)
                .SingleOrDefaultAsync(fun x -> gromplateId = x.Id)
        return
            match gromplate with
            | null -> sprintf "Gromplate #%A doesn't exist" gromplateId |> Error
            | x -> Ok <| ViewGromplateWithAllLeafs.load x
        }
    let Search (db: CardOverflowDb) (userId: Guid) (pageNumber: int) (searchTerm: string) = task {
        let plain, wildcard = FullTextSearch.parse searchTerm
        let! r =
            db.LatestGrompleaf
                .Where(fun x ->
                    String.IsNullOrWhiteSpace searchTerm ||
                    x.Tsv.Matches(EF.Functions.PlainToTsQuery(plain).And(EF.Functions.ToTsQuery wildcard))
                ).Select(fun x ->
                    x.Gromplate.Grompleafs.Select(fun x -> x.User_Grompleafs.Count).ToList(), // lowTODO sum here
                    x.User_Grompleafs.Any(fun x -> x.UserId = userId),
                    x
                ).ToPagedListAsync(pageNumber, 15)
        return {
            Results = r |> Seq.map (fun (users, isCollected, l) ->
                l |> Grompleaf.load |> ViewSearchGrompleaf.load (users.Sum()) isCollected) |> toResizeArray
            Details = {
                CurrentPage = r.PageNumber
                PageCount = r.PageCount
            }
        }}
    let GetMine (db: CardOverflowDb) userId = task {
        let! x =
            db.User_Grompleaf
                .Where(fun x ->  x.UserId = userId)
                .Select(fun x -> x.Grompleaf.Gromplate)
                .Distinct()
                .Include(fun x -> x.Grompleafs)
                .ToListAsync()
        return x |> Seq.map ViewGromplateWithAllLeafs.load |> toResizeArray
        }
    let GetMineWith (db: CardOverflowDb) userId gromplateId = task {
        let! x =
            db.User_Grompleaf
                .Where(fun x ->  x.UserId = userId || x.Grompleaf.GromplateId = gromplateId)
                .Select(fun x -> x.Grompleaf.Gromplate)
                .Distinct()
                .Include(fun x -> x.Grompleafs)
                .ToListAsync()
        return x |> Seq.map ViewGromplateWithAllLeafs.load |> toResizeArray
        }
    let Update (db: CardOverflowDb) userId (leaf: ViewGrompleaf) =
        let update gromplate = task {
            let! r = ViewGrompleaf.copyTo leaf |> GromplateRepository.UpdateFieldsToNewLeaf db userId gromplate
            return r |> Ok
        }
        if leaf.Fields.Count = leaf.Fields.Select(fun x -> x.Name.ToLower()).Distinct().Count() then
            db.Gromplate.SingleOrDefault(fun x -> x.Id = leaf.GromplateId)
            |> function
            | null -> update (GromplateEntity(Id = leaf.GromplateId, AuthorId = userId))
            | gromplate ->
                if gromplate.AuthorId = userId then
                    update gromplate
                else Error "You aren't that this gromplate's author." |> Task.FromResult
        else
            Error "Field names must differ" |> Task.FromResult

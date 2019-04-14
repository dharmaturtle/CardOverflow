namespace CardOverflow.Test

open CardOverflow.Api
open System
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Xunit
open CardOverflow.Entity

type TestConnectionStringProvider(dbName: string) =
  interface IConnectionStringProvider with
    member __.Get = sprintf "Server=localhost;Database=CardOverflow_%s;Trusted_Connection=True;" dbName

type TempDbService( [<CallerMemberName>] ?memberName: string) =
  let dbFactory =
    match memberName with
    | Some testName -> Regex.Replace(testName, "[^A-Za-z0-9 _]", "").Replace(' ', '_') |> TestConnectionStringProvider |> DbFactory
    | _ -> failwith "Missing the caller's member name somehow."
  do 
    use db = dbFactory.Create()
    db.Database.EnsureDeleted() |> ignore
    db.Database.EnsureCreated() |> Assert.True

  interface IDisposable with
    member __.Dispose() =
      use db = dbFactory.Create()
      db.Database.EnsureDeleted() |> ignore

  member __.DbService =
    dbFactory |> DbService

  member this.WithUser =
    let user = UserEntity(Name = "Test User", Email = "test@user.com")
    this.DbService.Command(fun db -> db.Users.Add user)
    user

  member this.WithDefaultConceptOptions(user: UserEntity) =
    let option = ConceptOptionEntity()
    ConceptOption.Default.CopyTo option
    option.UserId <- user.Id
    this.DbService.Command(fun db -> db.ConceptOptions.Add option)
    option

  member this.WithConceptTemplate(conceptOption: ConceptOptionEntity) =
    let conceptTemplate = 
      ConceptTemplateEntity(
        Modified = DateTime.UtcNow,
        CardTemplates = "",
        Css = "",
        DefaultTags = "",
        Fields = "",
        Name = "",
        DefaultConceptOptionsId = conceptOption.Id
      )
    this.DbService.Command(fun db -> db.ConceptTemplates.Add conceptTemplate)
    conceptTemplate
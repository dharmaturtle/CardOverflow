namespace CardOverflow.Test

open CardOverflow.Api
open System
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Xunit
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Diagnostics

type TestDbFactory(connectionString: string) =
    member __.Create() =
        DbContextOptionsBuilder()
            .UseSqlServer(connectionString)
                .ConfigureWarnings(fun warnings -> warnings.Throw(RelationalEventId.QueryClientEvaluationWarning) |> ignore)
                .Options
            |> fun o -> new CardOverflowDb(o)

type SqlTempDbProvider( [<CallerMemberName>] ?memberName: string) =
    let createCardOverflowDb =
        match memberName with
        | Some testName ->
            Regex.Replace(testName, "[^A-Za-z0-9 _]", "").Replace(' ', '_')
            |> sprintf "Server=localhost;Database=CardOverflow_%s;Trusted_Connection=True;"
            |> TestDbFactory
            |> fun x -> x.Create
        | _ -> failwith "Missing the caller's member name somehow."
    do 
        createCardOverflowDb |> InitializeDatabase.deleteAndRecreateDatabase()

    interface IDisposable with
        member __.Dispose() =
            use db = createCardOverflowDb()
            db.Database.EnsureDeleted() |> ignore

    member __.DbService =
        createCardOverflowDb |> DbService

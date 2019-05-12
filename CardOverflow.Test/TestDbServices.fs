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
        DbContextOptionsBuilder<CardOverflowDb>()
            .UseSqlServer(connectionString)
            .ConfigureWarnings(fun warnings -> warnings.Throw(RelationalEventId.QueryClientEvaluationWarning) |> ignore)
            .Options
        |> fun o -> new CardOverflowDb(o)

type SqlTempDbProvider( [<CallerMemberName>] ?memberName: string) =
    let dbName =
        Regex.Replace(memberName.Value, "[^A-Za-z0-9 _]", "").Replace(' ', '_')
        |> sprintf "CardOverflow_%s"
    let createCardOverflowDb =
        dbName
        |> sprintf "Server=localhost;Database=%s;Trusted_Connection=True;"
        |> TestDbFactory
        |> fun x -> x.Create
    do 
        dbName |> InitializeDatabase.deleteAndRecreateDb

    interface IDisposable with
        member __.Dispose() =
            use db = createCardOverflowDb()
            db.Database.EnsureDeleted() |> ignore

    member __.DbService =
        createCardOverflowDb |> DbService :> IDbService

// Sqlite

type SqliteDbFactory() =
    let c =
        DbContextOptionsBuilder<CardOverflowDb>()
            .UseSqlite("DataSource=:memory:")
            .ConfigureWarnings(fun warnings -> warnings.Throw(RelationalEventId.QueryClientEvaluationWarning) |> ignore)
            .Options
        |> fun o -> new CardOverflowDb(o)
        |> fun c ->
            c.Database.OpenConnection()
            c.Database.EnsureCreated() |> Assert.True
            c
    member __.Create() = c

type SqliteDbService(createCardOverflowDb: CreateCardOverflowDb) =
    let db = createCardOverflowDb()
    interface IDbService with
        member __.Query q =
            q db
        member __.Command c =
            c db |> ignore
            db.SaveChanges() |> ignore

type SqliteTempDbProvider() =
    let dbFactory =
        SqliteDbFactory()
    do 
        dbFactory |> fun f -> f.Create |> SqliteDbService |> InitializeDatabase.deleteAndRecreateDatabase

    interface IDisposable with
        member __.Dispose() =
            use ___ = dbFactory.Create()
            ()

    member __.DbService =
        dbFactory |> fun x -> x.Create |> SqliteDbService :> IDbService

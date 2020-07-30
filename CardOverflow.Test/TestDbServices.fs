namespace CardOverflow.Test

open CardOverflow.Api
open CardOverflow.Debug
open System
open System.Linq
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Xunit
open CardOverflow.Entity
open SimpleInjector
open ContainerExtensions
open SimpleInjector.Lifestyles
open Microsoft.Extensions.Configuration
open FSharp.Control.Tasks
open LoadersAndCopiers
open Microsoft.EntityFrameworkCore
open CardOverflow.Sanitation
open CardOverflow.Pure
open Npgsql
open System.Threading.Tasks

type TestContainer(?newDb: bool, ?callerMembersArg: string, [<CallerMemberName>] ?memberName: string) =
    let container = new Container()
    let mutable scope = AsyncScopedLifestyle.BeginScope container
    do
        let dbName =
            let temp =
                if callerMembersArg.IsSome
                then memberName.Value + "_" + callerMembersArg.Value
                else memberName.Value
            Regex.Replace(temp, "[^A-Za-z0-9 _]", "").Replace(' ', '_')
            |> sprintf "Ω_%s"
        container.RegisterStuffTestOnly
        container.RegisterTestConnectionString dbName
        container.GetInstance<Task<NpgsqlConnection>>() |> ignore
        container.Verify()
        match newDb with
        | Some newDb ->
            if newDb then
                InitializeDatabase.fullReset
            else
                InitializeDatabase.reset
        | None ->
            InitializeDatabase.reset
        |> fun reset -> container.GetInstance<IConfiguration>().GetConnectionString "ServerConnection" |> ConnectionString |> reset dbName

    interface IDisposable with
        member this.Dispose() =
            //this.Db.Database.EnsureDeleted() |> ignore
            container.Dispose()
            scope.Dispose()

    member __.Db = // lowTODO this should take unit
        scope.Dispose()
        scope <- AsyncScopedLifestyle.BeginScope container
        container.GetInstance<CardOverflowDb>()

    member __.Conn () =
        scope.Dispose()
        scope <- AsyncScopedLifestyle.BeginScope container
        container.GetInstance<Task<NpgsqlConnection>>()

module TestCollateRepo =
    let Search (db: CardOverflowDb) (query: string) = task {
        let! x =
            db.LatestCollateInstance
                .Where(fun x -> x.Name.Contains query)
                .ToListAsync()
        return x |> Seq.map (CollateInstance.load >> ViewCollateInstance.load) |> toResizeArray
        }
    let SearchEarliest (db: CardOverflowDb) (query: string) = task {
        let! x =
            db.CollateInstance
                .Where(fun x -> x.Name = query)
                .OrderBy(fun x -> x.Created)
                .FirstAsync()
        return x |> CollateInstance.load |> ViewCollateInstance.load
        }

// Sqlite

//type SqliteDbFactory() =
//    let c =
//        DbContextOptionsBuilder<CardOverflowDb>()
//            .UseSqlite("DataSource=:memory:")
//            .ConfigureWarnings(fun warnings -> warnings.Throw(RelationalEventId.QueryClientEvaluationWarning) |> ignore)
//            .Options
//        |> fun o -> new CardOverflowDb(o)
//        |> fun c ->
//            c.Database.OpenConnection()
//            c.Database.EnsureCreated() |> Assert.True
//            c
//    member __.Create() = c

//type SqliteDbService(createCardOverflowDb: CreateCardOverflowDb) =
//    let db = createCardOverflowDb()
//    interface IDbService with
//        member __.Query q =
//            q db
//        member __.Command c =
//            c db |> ignore
//            db.SaveChanges() |> ignore

//type SqliteTempDbProvider() =
//    let dbFactory =
//        SqliteDbFactory()
//    do 
//        dbFactory |> fun f -> f.Create |> SqliteDbService |> InitializeDatabase.deleteAndRecreateDatabase

//    interface IDisposable with
//        member __.Dispose() =
//            use ___ = dbFactory.Create()
//            ()

//    member __.DbService =
//        dbFactory |> fun x -> x.Create |> SqliteDbService :> IDbService

namespace CardOverflow.Test

open CardOverflow.Api
open System
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Xunit
open CardOverflow.Entity
open SimpleInjector
open ContainerExtensions
open SimpleInjector.Lifestyles
open Microsoft.Extensions.Configuration

type TestContainer(?args: string, [<CallerMemberName>] ?memberName: string) =
    let container = new Container()
    let scope = AsyncScopedLifestyle.BeginScope container
    do
        let temp =
            if args.IsSome
            then memberName.Value + "_" + args.Value
            else memberName.Value
        let dbName =
            Regex.Replace(temp, "[^A-Za-z0-9 _]", "").Replace(' ', '_')
            |> sprintf "CardOverflow_%s"
        container.RegisterStuff
        container.RegisterTestConnectionString dbName
        container.GetInstance<IConfiguration>().GetConnectionString "BaseTestConnection" |> ConnectionString |> InitializeDatabase.deleteAndRecreateDb dbName

    interface IDisposable with
        member this.Dispose() =
            this.Db.Database.EnsureDeleted() |> ignore
            container.Dispose()
            scope.Dispose()

    member __.Db =
        container.GetInstance<CardOverflowDb>()

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

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
            |> sprintf "CardOverflow_%s"
        container.RegisterStuffTestOnly
        container.RegisterTestConnectionString dbName
        container.Verify()
        match newDb with
        | Some newDb ->
            if newDb then
                InitializeDatabase.fullReset
            else
                InitializeDatabase.fastReset
        | None ->
            InitializeDatabase.fastReset
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

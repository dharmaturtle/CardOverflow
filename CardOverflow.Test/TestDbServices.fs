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
open Nest
open Equinox.MemoryStore
open FsCodec
open Domain
open EventService

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
        member _.Dispose() =
            //this.Db.Database.EnsureDeleted() |> ignore
            container.Dispose()
            scope.Dispose()

    member _.Db = // lowTODO this should take unit
        scope.Dispose()
        scope <- AsyncScopedLifestyle.BeginScope container
        container.GetInstance<CardOverflowDb>()

    member _.Conn () =
        scope.Dispose()
        scope <- AsyncScopedLifestyle.BeginScope container
        container.GetInstance<Task<NpgsqlConnection>>()

module User =
    open User
    let memoryStore store =
        Resolver(store, Events.codec, Fold.fold, Fold.initial).Resolve
        |> create
module Stack =
    open Stack
    let memoryStore store =
        Resolver(store, Events.codec, Fold.fold, Fold.initial).Resolve
        |> create
module Branch =
    open Branch
    let memoryStore store =
        Resolver(store, Events.codec, Fold.fold, Fold.initial).Resolve
        |> create

type TestEsContainer(?callerMembersArg: string, [<CallerMemberName>] ?memberName: string) =
    let container = new Container()
    do
        let dbName =
            let temp =
                if callerMembersArg.IsSome
                then memberName.Value + "_" + callerMembersArg.Value
                else memberName.Value
            Regex.Replace(temp, "[^A-Za-z0-9 _]", "").Replace(' ', '_')
            |> sprintf "Ω_%s"
        container.RegisterStuff
        container.RegisterTestConnectionString dbName
        container.RegisterSingleton<VolatileStore<byte[]>>()
        container.RegisterSingleton<User.Service>(fun () ->
            container.GetInstance<VolatileStore<byte[]>>() |> User.memoryStore)
        container.RegisterSingleton<Stack.Service>(fun () ->
            container.GetInstance<VolatileStore<byte[]>>() |> Stack.memoryStore)
        container.RegisterSingleton<Branch.Service>(fun () ->
            container.GetInstance<VolatileStore<byte[]>>() |> Branch.memoryStore)
        container.Verify()

    member _.ElasticClient () =
        container.GetInstance<ElasticClient>()
    
    member _.ElseClient () =
        container.GetInstance<ElseClient>()
    
    member _.UserService () =
        container.GetInstance<User.Service>()
    
    member _.StackService () =
        container.GetInstance<Stack.Service>()
    
    member _.BranchService () =
        container.GetInstance<Branch.Service>()
    
    member _.StackBranchService () =
        container.GetInstance<StackBranch.Service>()
    
    member private _.events(streamName, codec: IEventCodec<_, _, _>) =
        streamName.ToString()
        |> (container.GetInstance<VolatileStore<byte[]>>().TryLoad >> Option.get)
        |> Array.map (codec.TryDecode >> Option.get)
    member this.StackEvents (id) = this.events(Stack .streamName id, Stack .Events.codec)
    member this.BranchEvents(id) = this.events(Branch.streamName id, Branch.Events.codec)
    member this.UserEvents  (id) = this.events(User  .streamName id, User  .Events.codec)

module TestGromplateRepo =
    let Search (db: CardOverflowDb) (query: string) = task {
        let! x =
            db.LatestGrompleaf
                .Where(fun x -> x.Name.Contains query)
                .ToListAsync()
        return x |> Seq.map (Grompleaf.load >> ViewGrompleaf.load) |> toResizeArray
        }
    let SearchEarliest (db: CardOverflowDb) (query: string) = task {
        let! x =
            db.Grompleaf
                .Where(fun x -> x.Name = query)
                .OrderBy(fun x -> x.Created)
                .FirstAsync()
        return x |> Grompleaf.load |> ViewGrompleaf.load
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

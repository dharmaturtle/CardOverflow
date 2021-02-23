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
open EventWriter

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
module UserSaga =
    open User
    let memoryStore store deckWriter =
        Resolver(store, Events.codec, Fold.fold, Fold.initial).Resolve
        |> UserSaga.create deckWriter
module Deck =
    open Deck
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
        let vStore () = container.GetInstance<VolatileStore<byte[]>>()
        container.RegisterSingleton<User.Writer>(fun () ->
            User.memoryStore
                <| vStore()
                <| container.GetInstance<TableClient>() )
        container.RegisterSingleton<Deck.Writer>(fun () ->
            Deck.memoryStore
                <| vStore()
                <| container.GetInstance<TableClient>() )
        container.RegisterSingleton<UserSaga.Writer>(fun () ->
            UserSaga.memoryStore
                <| vStore()
                <| container.GetInstance<Deck.Writer>() )
        container.RegisterInitializer<VolatileStore<byte[]>>(fun store ->
            let elseClient = container.GetInstance<ElseClient>()
            let tableClient = container.GetInstance<TableClient>()
            Handler(fun _ (streamName:StreamName, events:ITimelineEvent<byte[]> []) ->
                let category, id = streamName |> StreamName.splitCategoryAndId
                match category with
                | "Stack"  -> events |> Array.map (Stack .Events.codec.TryDecode >> Option.get >> tableClient.UpsertStack'  id)
                | "Branch" -> events |> Array.map (Branch.Events.codec.TryDecode >> Option.get >> tableClient.UpsertBranch' id)
                | "User"   -> events |> Array.map (User  .Events.codec.TryDecode >> Option.get >> tableClient.UpsertUser'   id)
                | "Deck"   -> events |> Array.map (Deck  .Events.codec.TryDecode >> Option.get >> tableClient.UpsertDeck'   id)
                | _ -> failwith $"Unsupported category: {category}"
                |> Async.Parallel
                |> Async.RunSynchronously
                |> ignore
            ) |> store.Committed.AddHandler
        )
        container.RegisterSingleton<Stack.Writer>(fun () ->
            Stack.memoryStore
                <| vStore()
                <| container.GetInstance<TableClient>() )
        container.RegisterSingleton<Branch.Writer>(fun () ->
            container.GetInstance<VolatileStore<byte[]>>() |> Branch.memoryStore)
        container.Verify()
        let tc = container.GetInstance<TableClient>()
        let table = tc.CloudTableClient.GetTableReference tc.TableName
        table.DeleteIfExists()    |> ignore
        table.CreateIfNotExists() |> ignore

    member _.TableClient () =
        container.GetInstance<TableClient>()

    member _.ElasticClient () =
        container.GetInstance<ElasticClient>()
    
    member _.ElseClient () =
        container.GetInstance<ElseClient>()
    
    member _.UserWriter () =
        container.GetInstance<User.Writer>()
    
    member _.UserSagaWriter () =
        container.GetInstance<UserSaga.Writer>()
    
    member _.DeckWriter () =
        container.GetInstance<Deck.Writer>()
    
    member _.StackWriter () =
        container.GetInstance<Stack.Writer>()
    
    member _.BranchWriter () =
        container.GetInstance<Branch.Writer>()
    
    member _.StackBranchWriter () =
        container.GetInstance<StackBranch.Writer>()
    
    member private _.events(streamName, codec: IEventCodec<_, _, _>) =
        streamName.ToString()
        |> (container.GetInstance<VolatileStore<byte[]>>().TryLoad >> Option.get)
        |> Array.map (codec.TryDecode >> Option.get)
    member this.StackEvents (id) = this.events(Stack .streamName id, Stack .Events.codec)
    member this.BranchEvents(id) = this.events(Branch.streamName id, Branch.Events.codec)
    member this.UserEvents  (id) = this.events(User  .streamName id, User  .Events.codec)
    member this.DeckEvents  (id) = this.events(Deck  .streamName id, Deck  .Events.codec)

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

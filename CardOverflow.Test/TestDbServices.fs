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
open EventAppender

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

module Resolve =
    let user     store = Resolver(store, User    .Events.codec, User    .Fold.fold,     User.Fold.initial).Resolve
    let deck     store = Resolver(store, Deck    .Events.codec, Deck    .Fold.fold,     Deck.Fold.initial).Resolve
    let template store = Resolver(store, Template.Events.codec, Template.Fold.fold, Template.Fold.initial).Resolve
    let example  store = Resolver(store, Example .Events.codec, Example .Fold.fold,  Example.Fold.initial).Resolve
    let stack    store = Resolver(store, Stack   .Events.codec, Stack   .Fold.fold,    Stack.Fold.initial).Resolve

module User =
    let memoryStore store =
        User.create
            (Resolve.user     store)
            (Resolve.deck     store)
            (Resolve.template store)
module UserSaga =
    let memoryStore store deckAppender =
        UserSaga.create
            deckAppender
            (Resolve.user store)
module Deck =
    let memoryStore store =
        Deck.create
            (Resolve.deck store)
module TemplateCombo =
    let memoryStore store =
        TemplateCombo.create
            (Resolve.template store)
module Stack =
    let memoryStore store =
        Stack.create
            (Resolve.stack    store)
            (Resolve.template store)
            (Resolve.example  store)
module Example =
    let memoryStore store =
        Example.create
            (Resolve.example  store)
            (Resolve.template store)

open Humanizer
type TestEsContainer(?withElasticSearch: bool, ?callerMembersArg: string, [<CallerMemberName>] ?memberName: string) =
    let isMemoryKeyValueStore = true
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
        match withElasticSearch with
        | Some withElasticSearch ->
            if withElasticSearch then
                container.RegisterTestConnectionString dbName
            else  container.RegisterSingleton<Elsea.IClient>(fun () -> Elsea.NoopClient() :> Elsea.IClient)
                  container.RegisterSingleton<IElasticClient>(fun () -> NoOpElasticClient() :> IElasticClient)
        | None -> container.RegisterSingleton<Elsea.IClient>(fun () -> Elsea.NoopClient() :> Elsea.IClient)
                  container.RegisterSingleton<IElasticClient>(fun () -> NoOpElasticClient() :> IElasticClient)
        container.RegisterSingleton<VolatileStore<byte[]>>()
        if isMemoryKeyValueStore then
            container.RegisterSingleton<IKeyValueStore, TableMemoryClient>()
        else
            container.RegisterSingleton<IKeyValueStore>(fun () ->
                let cs = container.GetInstance<IConfiguration>().GetConnectionString "AzureTableStorage"
                TableClient(cs, (dbName.Substring 2).Pascalize()) // chopping off the omega and first underscore, then pascal casing
                :> IKeyValueStore
            )
            container.RegisterInitializer<IKeyValueStore>(fun tc ->
                let tc = tc :?> TableClient
                let table = tc.CloudTableClient.GetTableReference tc.TableName
                table.DeleteIfExists()    |> ignore
                table.CreateIfNotExists() |> ignore
            )
        let vStore () = container.GetInstance<VolatileStore<byte[]>>()
        container.RegisterSingleton<User.Appender>(fun () ->
            User.memoryStore
                <| vStore() )
        container.RegisterSingleton<Deck.Appender>(fun () ->
            Deck.memoryStore
                <| vStore() )
        container.RegisterSingleton<TemplateCombo.Appender>(fun () ->
            TemplateCombo.memoryStore
                <| vStore() )
        container.RegisterSingleton<UserSaga.Appender>(fun () ->
            UserSaga.memoryStore
                <| vStore()
                <| container.GetInstance<Deck.Appender>() )
        container.RegisterInitializer<VolatileStore<byte[]>>(fun store ->
            let projector = container.GetInstance<Projector.ServerProjector>()
            Handler(fun _ (streamName:StreamName, events:ITimelineEvent<byte[]> []) ->
                projector.Project(streamName, events)
                |> Async.RunSynchronously
                |> ignore
            ) |> store.Committed.AddHandler
        )
        container.RegisterSingleton<Stack.Appender>(fun () ->
            Stack.memoryStore
                <| vStore() )
        container.RegisterSingleton<Example.Appender>(fun () ->
            Example.memoryStore
                <| vStore() )
        container.RegisterSingleton<NoCQS.User>(fun () ->
                NoCQS.User(container.GetInstance<UserSaga.Appender>(), container.GetInstance<KeyValueStore>())
            )
        container.Verify()

    member _.KeyValueStore () =
        container.GetInstance<KeyValueStore>()

    member _.ElasticClient () =
        container.GetInstance<IElasticClient>()
    
    member _.ElseaClient () =
        container.GetInstance<Elsea.IClient>()
    
    member _.UserAppender () =
        container.GetInstance<User.Appender>()
    
    member _.TemplateComboAppender () =
        container.GetInstance<TemplateCombo.Appender>()
    
    member _.UserSagaAppender () =
        container.GetInstance<UserSaga.Appender>()
    
    member _.DeckAppender () =
        container.GetInstance<Deck.Appender>()
    
    member _.StackAppender () =
        container.GetInstance<Stack.Appender>()
    
    member _.ExampleAppender () =
        container.GetInstance<Example.Appender>()
    
    member private _.events(streamName, codec: IEventCodec<_, _, _>) =
        streamName.ToString()
        |> (container.GetInstance<VolatileStore<byte[]>>().TryLoad >> Option.get)
        |> Array.map (codec.TryDecode >> Option.get)
    member this.StackEvents    id = this.events(Stack   .streamName id, Stack   .Events.codec)
    member this.ExampleEvents  id = this.events(Example .streamName id, Example .Events.codec)
    member this.UserEvents     id = this.events(User    .streamName id, User    .Events.codec)
    member this.DeckEvents     id = this.events(Deck    .streamName id, Deck    .Events.codec)
    member this.TemplateEvents id = this.events(Template.streamName id, Template.Events.codec)

module TestTemplateRepo =
    let Search (db: CardOverflowDb) (query: string) = task {
        let! x =
            db.LatestTemplateRevision
                .Where(fun x -> x.Name.Contains query)
                .ToListAsync()
        return x |> Seq.map (TemplateRevision.load >> ViewTemplateRevision.load) |> toResizeArray
        }
    let SearchEarliest (db: CardOverflowDb) (query: string) = task {
        let! x =
            db.TemplateRevision
                .Where(fun x -> x.Name = query)
                .OrderBy(fun x -> x.Created)
                .FirstAsync()
        return x |> TemplateRevision.load |> ViewTemplateRevision.load
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

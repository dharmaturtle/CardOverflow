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
    let user           store = MemoryStoreCategory(store, User          .Events.codec, User          .Fold.fold,           User.Fold.initial).Resolve
    let deck           store = MemoryStoreCategory(store, Deck          .Events.codec, Deck          .Fold.fold,           Deck.Fold.initial).Resolve
    let publicTemplate store = MemoryStoreCategory(store, PublicTemplate.Events.codec, PublicTemplate.Fold.fold, PublicTemplate.Fold.initial).Resolve
    let example        store = MemoryStoreCategory(store, Example       .Events.codec, Example       .Fold.fold,        Example.Fold.initial).Resolve
    let stack          store = MemoryStoreCategory(store, Stack         .Events.codec, Stack         .Fold.fold,          Stack.Fold.initial).Resolve

module User =
    let memoryStore store =
        User.create
            (Resolve.user           store)
            (Resolve.deck           store)
            (Resolve.publicTemplate store)
module UserSaga =
    let memoryStore store deckAppender =
        UserSaga.create
            deckAppender
            (Resolve.user           store)
module Deck =
    let memoryStore store =
        Deck.create
            (Resolve.deck           store)
module PublicTemplate =
    let memoryStore store =
        PublicTemplate.create
            (Resolve.publicTemplate store)
module Stack =
    let memoryStore store =
        Stack.create
            (Resolve.stack          store)
            (Resolve.publicTemplate store)
            (Resolve.example        store)
            (Resolve.deck           store)
            (Resolve.user           store)
module Example =
    let memoryStore store =
        Example.create
            (Resolve.example        store)
            (Resolve.publicTemplate store)

open Humanizer
type TestEsContainer(?withElasticSearch: bool, ?callerMembersArg: string, [<CallerMemberName>] ?memberName: string) =
    let isMemoryKeyValueStore = true
    let container = new Container()
    do
        IdempotentTest.init 1.0 123_456
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
        | None -> container.RegisterSingleton<Elsea.IClient>(fun () -> Elsea.NoopClient() :> Elsea.IClient)
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
        container.RegisterSingleton<PublicTemplate.Appender>(fun () ->
            PublicTemplate.memoryStore
                <| vStore() )
        container.RegisterSingleton<UserSaga.Appender>(fun () ->
            UserSaga.memoryStore
                <| vStore()
                <| container.GetInstance<Deck.Appender>() )
        container.RegisterInitializer<VolatileStore<byte[]>>(fun store ->
            let projector = container.GetInstance<Projector.ServerProjector>()
            Handler(fun _ (streamName:StreamName, events:ITimelineEvent<byte[]> []) ->
                for _ in [1; 2] do // tests idempotency
                    let mutable succeeded = false
                    while not succeeded do
                        try
                            projector.Project(streamName, events)
                            |> Async.RunSynchronously
                            succeeded <- true
                            IdempotentTest.failrate <- IdempotentTest.defaultFailrate
                        with
                            | TransientError ->
                                printfn "Transient Error with failrate %A" IdempotentTest.failrate
                                succeeded <- false
                                IdempotentTest.failrate <- IdempotentTest.failrate - 0.1
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
    
    member _.PublicTemplateAppender () =
        container.GetInstance<PublicTemplate.Appender>()
    
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
    member this.StackEvents    id = this.events(Stack         .streamName id, Stack         .Events.codec)
    member this.ExampleEvents  id = this.events(Example       .streamName id, Example       .Events.codec)
    member this.UserEvents     id = this.events(User          .streamName id, User          .Events.codec)
    member this.DeckEvents     id = this.events(Deck          .streamName id, Deck          .Events.codec)
    member this.TemplateEvents id = this.events(PublicTemplate.streamName id, PublicTemplate.Events.codec)

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

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
module Template =
    open Template
    let memoryStore store =
        Resolver(store, Events.codec, Fold.fold, Fold.initial).Resolve
        |> create
module Stack =
    open Stack
    let memoryStore store =
        Resolver(store, Events.codec, Fold.fold, Fold.initial).Resolve
        |> create
module Example =
    open Example
    let memoryStore store =
        Resolver(store, Events.codec, Fold.fold, Fold.initial).Resolve
        |> create

open FSharp.Azure.Storage.Table
open FsToolkit.ErrorHandling

type TableMemoryClient() =
    let dict = new System.Collections.Generic.Dictionary<(string * string), AzureTableStorageWrapper>()
    interface IKeyValueStore with
        member _.InsertOrReplace summary =
            let value = summary |> KeyValueStore.wrap
            let key = value.Partition, value.Partition
            dict.Remove key |> ignore
            dict.Add(key, value)
            {   HttpStatusCode = 0
                Etag = ""
            } |> Async.singleton
        member _.PointQuery (key: obj) =
            let key = string key, string key
            if dict.ContainsKey key then
                let meta =
                    { Etag = ""
                      Timestamp = DateTimeOffset.MinValue }
                (dict.Item key, meta)
                |> Seq.singleton
                |> Async.singleton
            else
                Seq.empty |> Async.singleton

open Humanizer
type TestEsContainer(?callerMembersArg: string, [<CallerMemberName>] ?memberName: string) =
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
        container.RegisterTestConnectionString dbName
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
        container.RegisterSingleton<User.Writer>(fun () ->
            User.memoryStore
                <| vStore()
                <| container.GetInstance<KeyValueStore>() )
        container.RegisterSingleton<Deck.Writer>(fun () ->
            Deck.memoryStore
                <| vStore()
                <| container.GetInstance<KeyValueStore>() )
        container.RegisterSingleton<Template.Writer>(fun () ->
            Template.memoryStore
                <| vStore()
                <| container.GetInstance<KeyValueStore>() )
        container.RegisterSingleton<UserSaga.Writer>(fun () ->
            UserSaga.memoryStore
                <| vStore()
                <| container.GetInstance<Deck.Writer>() )
        container.RegisterInitializer<VolatileStore<byte[]>>(fun store ->
            let elseClient = container.GetInstance<ElseClient>()
            let keyValueStore = container.GetInstance<KeyValueStore>()
            Handler(fun _ (streamName:StreamName, events:ITimelineEvent<byte[]> []) ->
                let category, id = streamName |> StreamName.splitCategoryAndId
                match category with
                | "Example"  -> events |> Array.map (Example .Events.codec.TryDecode >> Option.get >> keyValueStore.UpsertExample'  id)
                | "User"     -> events |> Array.map (User    .Events.codec.TryDecode >> Option.get >> keyValueStore.UpsertUser'     id)
                | "Deck"     -> events |> Array.map (Deck    .Events.codec.TryDecode >> Option.get >> keyValueStore.UpsertDeck'     id)
                | "Template" -> events |> Array.map (Template.Events.codec.TryDecode >> Option.get >> keyValueStore.UpsertTemplate' id)
                | "Stack"    -> events |> Array.map (Stack   .Events.codec.TryDecode >> Option.get >> keyValueStore.UpsertStack'    id)
                | _ -> failwith $"Unsupported category: {category}"
                |> Async.Parallel
                |> Async.RunSynchronously
                |> ignore
            ) |> store.Committed.AddHandler
        )
        container.RegisterSingleton<Stack.Writer>(fun () ->
            Stack.memoryStore
                <| vStore()
                <| container.GetInstance<KeyValueStore>() )
        container.RegisterSingleton<Example.Writer>(fun () ->
            container.GetInstance<VolatileStore<byte[]>>() |> Example.memoryStore)
        container.Verify()

    member _.KeyValueStore () =
        container.GetInstance<KeyValueStore>()

    member _.ElasticClient () =
        container.GetInstance<ElasticClient>()
    
    member _.ElseClient () =
        container.GetInstance<ElseClient>()
    
    member _.UserWriter () =
        container.GetInstance<User.Writer>()
    
    member _.TemplateWriter () =
        container.GetInstance<Template.Writer>()
    
    member _.UserSagaWriter () =
        container.GetInstance<UserSaga.Writer>()
    
    member _.DeckWriter () =
        container.GetInstance<Deck.Writer>()
    
    member _.StackWriter () =
        container.GetInstance<Stack.Writer>()
    
    member _.ExampleWriter () =
        container.GetInstance<Example.Writer>()
    
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

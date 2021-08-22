module ContainerExtensions

open Microsoft.Extensions.Configuration;
open Microsoft.Extensions.DependencyInjection;
open CardOverflow.Api;
open CardOverflow.Entity;
open CardOverflow.Debug;
open Microsoft.EntityFrameworkCore;
open Microsoft.Extensions.Configuration;
open Microsoft.Extensions.DependencyInjection;
open Serilog;
open Microsoft.Extensions.Logging;
open CardOverflow.Api
open SimpleInjector
open SimpleInjector.Lifestyles
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Configuration
open System.IO
open System
open Serilog
open Microsoft.EntityFrameworkCore.Diagnostics
open System.Security.Cryptography
open LoadersAndCopiers
open CardOverflow.Pure
open Npgsql
open FSharp.Control.Tasks
open System.Threading.Tasks
open Dapper.NodaTime
open Nest
open Elasticsearch.Net
open Domain

module Environment =
    let get =
        ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory, "config"))
            .AddJsonFile("environment.json", optional = false, reloadOnChange = false)
            .Build()
            .GetSection("environment")
            .Value

module Configuration =
    let get environment =
        ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory, "config"))
            .AddJsonFile("appsettings.json", optional = false, reloadOnChange = false)
            .AddJsonFile(sprintf "appsettings.%s.json" environment , optional = false, reloadOnChange = false)
            .AddJsonFile("appsettings.Local.json", optional = true, reloadOnChange = false)
            .Build() :> IConfiguration

module Logger =
    let get configuration =
        LoggerConfiguration()
            .ReadFrom
            .Configuration(configuration)
            .CreateLogger()

open Domain.Projection
type Container with
    member container.RegisterStuffTestOnly =
        container.Options.DefaultScopedLifestyle <- new AsyncScopedLifestyle() // https://simpleinjector.readthedocs.io/en/latest/lifetimes.html#web-request-lifestyle
        container.RegisterInstance<IConfiguration>(Environment.get |> Configuration.get)
        container.RegisterSingleton<ILogger>(fun () -> container.GetInstance<IConfiguration>() |> Logger.get :> ILogger)
        let npgsqlConnection = Func<Task<NpgsqlConnection>>(fun() -> task {
            let conn = new NpgsqlConnection(container.GetInstance<ConnectionString>() |> ConnectionString.value)
            do! conn.OpenAsync()
            return conn
        })
        container.Register<Task<NpgsqlConnection>>(npgsqlConnection, Lifestyle.Scoped)
        container.RegisterInitializer<ILogger>(fun logger -> Log.Logger <- logger)
        let loggerFactory = new LoggerFactory() // WARNING WARNING WARNING this is never disposed. Use only in tests. Remove TestOnly from the name when you fix this.
        DapperNodaTimeSetup.Register()
        ServiceCollection() // https://stackoverflow.com/a/60290696
            .AddEntityFrameworkNpgsql()
            .AddSingleton<ILoggerFactory>(loggerFactory)
            .AddDbContextPool<CardOverflowDb>(fun optionsBuilder ->
                //loggerFactory.AddSerilog(container.GetInstance<ILogger>()) |> ignore
                optionsBuilder
                    .UseNpgsql((container.GetInstance<ConnectionString>() |> ConnectionString.value), fun x -> x.UseNodaTime() |> ignore)
                    .UseSnakeCaseNamingConvention()
                    //.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) // lowTODO uncommenting this seems to require adding .Includes() in places, but shouldn't the above line do that?
                    //.EnableSensitiveDataLogging()
                    |> ignore)
            .AddSimpleInjector(container)
            .BuildServiceProvider(true)
            .UseSimpleInjector(container)
            |> ignore

    member container.RegisterStuff =
        container.RegisterInstance<IConfiguration>(Environment.get |> Configuration.get)
        container.RegisterSingleton<ILogger>(fun () -> container.GetInstance<IConfiguration>() |> Logger.get :> ILogger)
        container.RegisterInitializer<ILogger>(fun logger -> Log.Logger <- logger)
        container.RegisterSingleton<Projector.ServerProjector>(fun () ->
            let kvs = container.GetInstance<KeyValueStore>()
            let elsea = container.GetInstance<Elsea.IClient>()
            Projector.ServerProjector(kvs, elsea)
        )
        container.RegisterSingleton<KeyValueStore>(fun () ->
            let kvs = container.GetInstance<IKeyValueStore>()
            KeyValueStore(kvs)
        )
    
    member container.RegisterStandardConnectionString =
        container.RegisterSingleton<ConnectionString>(fun () -> container.GetInstance<IConfiguration>().GetConnectionString("DefaultConnection") |> ConnectionString)
        container.RegisterSingleton<IElasticClient>(fun () ->
            container.GetInstance<IConfiguration>().GetConnectionString("ElasticSearchUri")
            |> Uri
            |> fun x -> (new ConnectionSettings(x)).DefaultIndex("CardOverflow")
            |> ElasticClient
            :> IElasticClient
        )
    
    member container.RegisterServerConnectionString =
        container.RegisterSingleton<ConnectionString>(fun () -> container.GetInstance<IConfiguration>().GetConnectionString("ServerConnection") |> ConnectionString)

    member container.RegisterTestConnectionString dbName =
        container.RegisterSingleton<ConnectionString>(fun () -> container.GetInstance<IConfiguration>().GetConnectionString("TestConnection").Replace("CardOverflow_{TestName}", dbName) |> ConnectionString)
        
        container.RegisterSingleton<IElasticClient>(fun () ->
            container.GetInstance<IConfiguration>().GetConnectionString("ElasticSearchUri")
            |> Uri
            |> Elsea.Client.create dbName
        )
        container.RegisterSingleton<Elsea.IClient>(fun () ->
            container.GetInstance<IElasticClient>()
            |> Elsea.Client
            :> Elsea.IClient
        )
        container.RegisterInitializer<IElasticClient>(fun ec ->
            try
                (dbName.ToLower() + "*")
                |> Indices.Parse
                |> DeleteIndexRequest
                |> ec.Indices.Delete
                |> ignore
            with _ -> ()
        )

open Equinox.CosmosStore
module Cosmos =
    let cacheStrategy cache = CachingStrategy.SlidingWindow (cache, TimeSpan.FromMinutes 20.)

    module User =
        open User
        open User.Fold
        let resolve (context, cache) = CosmosStoreCategory(context, Events.codec, fold, initial, cacheStrategy cache, AccessStrategy.Snapshot (isOrigin, snapshot)).Resolve
    module Deck =
        open Deck
        open Deck.Fold
        let resolve (context, cache) = CosmosStoreCategory(context, Events.codec, fold, initial, cacheStrategy cache, AccessStrategy.Snapshot (isOrigin, snapshot)).Resolve
    module Example =
        open Example
        open Example.Fold
        let resolve (context, cache) = CosmosStoreCategory(context, Events.codec, fold, initial, cacheStrategy cache, AccessStrategy.Snapshot (isOrigin, snapshot)).Resolve
    module PublicTemplate =
        open PublicTemplate
        open PublicTemplate.Fold
        let resolve (context, cache) = CosmosStoreCategory(context, Events.codec, fold, initial, cacheStrategy cache, AccessStrategy.Snapshot (isOrigin, snapshot)).Resolve
    module Stack =
        open Stack
        open Stack.Fold
        let resolve (context, cache) = CosmosStoreCategory(context, Events.codec, fold, initial, cacheStrategy cache, AccessStrategy.Snapshot (isOrigin, snapshot)).Resolve

open Cosmos
open EventAppender
module User =
    let appender x =
        User.create
            (User          .resolve x)
            (Deck          .resolve x)
            (PublicTemplate.resolve x)
module UserSaga =
    let appender x deckAppender =
        UserSaga.create
            deckAppender
            (User          .resolve x)
module Deck =
    let appender x =
        Deck.create
            (Deck          .resolve x)
module PublicTemplate =
    let appender x =
        PublicTemplate.create
            (PublicTemplate.resolve x)
module Stack =
    let appender x =
        Stack.create
            (Stack         .resolve x)
            (PublicTemplate.resolve x)
            (Example       .resolve x)
            (Deck          .resolve x)
            (User          .resolve x)
module Example =
    let appender x =
        Example.create
            (Example       .resolve x)
            (PublicTemplate.resolve x)

let [<Literal>] appName = "CardOverflow"

let getEquinoxContextAndCache (configuration: IConfiguration) =
    let equinoxCosmosConnection = configuration.GetSection("Equinox:CosmosConnection").Value
    let equinoxCosmosDatabase   = configuration.GetSection("Equinox:CosmosDatabase"  ).Value
    let equinoxCosmosContainer  = configuration.GetSection("Equinox:CosmosContainer" ).Value
    // all the configuration values below are copy/pasted from Equinox examples. medTODO ASK
    let connector x =
        CosmosClientFactory(TimeSpan.FromSeconds 5., 2, TimeSpan.FromSeconds 5.)
            .CreateAndInitialize(Discovery.ConnectionString equinoxCosmosConnection, x)
    let storeClient = CosmosStoreClient.Connect(connector, equinoxCosmosDatabase, equinoxCosmosContainer) |> Async.RunSynchronously
    let context = CosmosStoreContext(storeClient, tipMaxEvents = 10)
    let cache = Equinox.Cache(appName, sizeMb = 50)
    context, cache

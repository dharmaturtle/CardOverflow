module ContainerExtensions

open CardOverflow.Sanitation
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

type EntityHasher () =
    interface IEntityHasher with
        member val LeafHasher =
            fun struct (leaf, grompleafHash, sha512) -> LeafEntity.hash grompleafHash sha512 leaf
        member val GrompleafHasher =
            fun struct (leaf, sha512) -> GrompleafEntity.hash sha512 leaf
        member _.GetMaxIndexInclusive =
            fun (e: LeafEntity) ->
                (e |> LeafView.load).MaxIndexInclusive
        member _.SanitizeTag = SanitizeTagRepository.sanitize

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
            .AddSingleton<IEntityHasher, EntityHasher>()
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
        container.RegisterSingleton<IEntityHasher, EntityHasher>()
    
    member container.RegisterStandardConnectionString =
        container.RegisterSingleton<ConnectionString>(fun () -> container.GetInstance<IConfiguration>().GetConnectionString("DefaultConnection") |> ConnectionString)
        container.RegisterSingleton<ElasticClient>(fun () ->
            container.GetInstance<IConfiguration>().GetConnectionString("ElasticSearchUri")
            |> Uri
            |> fun x -> (new ConnectionSettings(x)).DefaultIndex("CardOverflow")
            |> ElasticClient
        )
    
    member container.RegisterServerConnectionString =
        container.RegisterSingleton<ConnectionString>(fun () -> container.GetInstance<IConfiguration>().GetConnectionString("ServerConnection") |> ConnectionString)

    member container.RegisterTestConnectionString dbName =
        container.RegisterSingleton<ConnectionString>(fun () -> container.GetInstance<IConfiguration>().GetConnectionString("TestConnection").Replace("CardOverflow_{TestName}", dbName) |> ConnectionString)
        let elasticSearchIndexName t = $"{dbName}_{t}".ToLower()
        let stackIndex  = nameof Domain.Stack  |> elasticSearchIndexName
        let branchIndex = nameof Domain.Branch |> elasticSearchIndexName
        container.RegisterSingleton<ElasticClient>(fun () ->
            let sourceSerializerFactory =
                ConnectionSettings.SourceSerializerFactory
                    (fun x y -> Nest.JsonNetSerializer.JsonNetSerializer.Default (x, y))
            let uri = container.GetInstance<IConfiguration>().GetConnectionString("ElasticSearchUri") |> Uri
            let pool = new SingleNodeConnectionPool(uri)
            (new ConnectionSettings(pool, sourceSerializerFactory))
                .DefaultMappingFor<Domain.Stack.Events.Snapshotted>(fun x ->
                    x.IndexName stackIndex :> IClrTypeMapping<_>
                )
                .DefaultMappingFor<Domain.Branch.Events.Snapshotted>(fun x ->
                    x.IndexName branchIndex :> IClrTypeMapping<_>
                )
                .EnableDebugMode(fun x ->
                    if x.HttpStatusCode = Nullable 404 then // https://github.com/elastic/elasticsearch-net/issues/5227
                        failwith x.DebugInformation
                )
                .ThrowExceptions()
            |> ElasticClient
        )
        container.RegisterSingleton<ElseClient>(fun () ->
            container.GetInstance<ElasticClient>()
            |> ElseClient
        )
        container.RegisterInitializer<ElasticClient>(fun ec ->
            try
                (dbName.ToLower() + "*")
                |> Indices.Parse
                |> DeleteIndexRequest
                |> ec.Indices.Delete
                |> ignore
            with _ -> ()
        )

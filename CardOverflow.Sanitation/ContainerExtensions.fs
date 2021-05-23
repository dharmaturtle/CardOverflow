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

type EntityHasher () =
    interface IEntityHasher with
        member val RevisionHasher =
            fun struct (revision, templateRevisionHash, sha512) -> RevisionEntity.hash templateRevisionHash sha512 revision
        member val TemplateRevisionHasher =
            fun struct (revision, sha512) -> TemplateRevisionEntity.hash sha512 revision
        member _.GetMaxIndexInclusive =
            fun (e: RevisionEntity) ->
                (e |> RevisionView.load).MaxIndexInclusive
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
        container.RegisterSingleton<Projector.ServerProjector>(fun () ->
            let kvs = container.GetInstance<KeyValueStore>()
            let elsea = container.GetInstance<Elsea.IClient>()
            Projector.ServerProjector(kvs, elsea)
        )
        container.RegisterSingleton<KeyValueStore>(fun () ->
            let kvs = container.GetInstance<IKeyValueStore>()
            let elasticClient = container.GetInstance<IElasticClient>()
            KeyValueStore(kvs, elasticClient)
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
        
        let elasticSearchIndexName t = $"{dbName}_{t}".ToLower()
        let exampleSearchIndex  = nameof Projection.ExampleSearch  |> elasticSearchIndexName
        let stackSearchIndex    = nameof Projection.StackSearch    |> elasticSearchIndexName
        let templateSearchIndex = nameof Projection.TemplateSearch |> elasticSearchIndexName
        container.RegisterSingleton<IElasticClient>(fun () ->
            let uri = container.GetInstance<IConfiguration>().GetConnectionString("ElasticSearchUri") |> Uri
            let pool = new SingleNodeConnectionPool(uri)
            (new ConnectionSettings(pool, Elsea.sourceSerializerFactory))
                .DefaultMappingFor<Projection.ExampleSearch>(fun x ->
                    x.IndexName exampleSearchIndex :> IClrTypeMapping<_>
                )
                .DefaultMappingFor<Projection.StackSearch>(fun x ->
                    x.IndexName stackSearchIndex :> IClrTypeMapping<_>
                )
                .DefaultMappingFor<Projection.TemplateSearch>(fun x ->
                    x.IndexName templateSearchIndex :> IClrTypeMapping<_>
                )
                .EnableDebugMode(fun call ->
                    if call.HttpStatusCode = Nullable 404 then // https://github.com/elastic/elasticsearch-net/issues/5227
                        failwith call.DebugInformation
                    //if call.RequestBodyInBytes <> null then
                    //    call.RequestBodyInBytes
                    //    |> System.Text.Encoding.UTF8.GetString
                    //    |> printfn "ElasticSearch query: %s"
                )
                .ThrowExceptions()
            |> ElasticClient
            :> IElasticClient
        )
        container.RegisterSingleton<Elsea.IClient>(fun () ->
            Elsea.Client(
                container.GetInstance<IElasticClient>(),
                container.GetInstance<KeyValueStore>()
            ) :> Elsea.IClient
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

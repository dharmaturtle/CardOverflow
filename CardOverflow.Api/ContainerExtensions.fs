module ContainerExtensions

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

type Container with
    member container.RegisterStuff =
        container.Options.DefaultScopedLifestyle <- new AsyncScopedLifestyle()
        container.RegisterInstance<IConfiguration>(Environment.get |> Configuration.get)
        container.RegisterSingleton<ILogger>(fun () -> container.GetInstance<IConfiguration>() |> Logger.get :> ILogger)
        container.RegisterInitializer<ILogger>(fun logger -> Log.Logger <- logger)

        container.RegisterSingleton<DbContextOptions<CardOverflowDb>>(fun () ->
            DbContextOptionsBuilder<CardOverflowDb>()
                .UseSqlServer(container.GetInstance<ConnectionString>() |> ConnectionString.value )
                .ConfigureWarnings(fun warnings -> warnings.Throw(RelationalEventId.QueryClientEvaluationWarning) |> ignore)
                .Options)
        container.Register<CardOverflowDb> Lifestyle.Scoped
    
    member container.RegisterStandardConnectionString =
        container.RegisterSingleton<ConnectionString>(fun () -> container.GetInstance<IConfiguration>().GetConnectionString("DefaultConnection") |> ConnectionString)

    member container.RegisterTestConnectionString dbName =
        container.RegisterSingleton<ConnectionString>(fun () -> container.GetInstance<IConfiguration>().GetConnectionString("TestConnection").Replace("CardOverflow_{TestName}", dbName) |> ConnectionString)

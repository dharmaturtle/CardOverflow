module ContainerExtensionsTests

open CardOverflow.Api
open Xunit
open SimpleInjector
open ContainerExtensions
open Serilog
open Npgsql
open System.Threading.Tasks
open SimpleInjector.Lifestyles

[<Fact>]
let ``RegisterStuff verifies`` (): unit =
    use c = new Container()
    
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    AsyncScopedLifestyle.BeginScope c |> ignore
    c.GetInstance<Task<NpgsqlConnection>>() |> ignore
    
    c.Verify()

[<Fact>]
let ``Testing logging, needs manual checking`` (): unit =
    use c = new Container()
    
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    AsyncScopedLifestyle.BeginScope c |> ignore
    c.GetInstance<Task<NpgsqlConnection>>() |> ignore
    c.Verify()
    
    Log.Information("Logging test, success!")

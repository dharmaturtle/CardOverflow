module ContainerExtensionsTests

open CardOverflow.Api
open Xunit
open SimpleInjector
open ContainerExtensions
open Serilog

[<Fact>]
let ``RegisterStuff verifies``() =
    use c = new Container()
    
    c.RegisterStuff
    c.RegisterStandardConnectionString
    
    c.Verify()

[<Fact>]
let ``Testing logging, needs manual checking``() =
    use c = new Container()
    
    c.RegisterStuff
    c.RegisterStandardConnectionString
    c.Verify()
    
    Log.Information("Logging test, success!")

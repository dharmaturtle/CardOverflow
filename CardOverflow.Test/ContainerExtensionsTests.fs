module ContainerExtensionsTests

open CardOverflow.Api
open Xunit
open SimpleInjector
open ContainerExtensions
open Serilog

[<Fact>]
let ``RegisterStuff verifies`` (): unit =
    use c = new Container()
    
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    
    c.Verify()

[<Fact>]
let ``Testing logging, needs manual checking`` (): unit =
    use c = new Container()
    
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    c.Verify()
    
    Log.Information("Logging test, success!")

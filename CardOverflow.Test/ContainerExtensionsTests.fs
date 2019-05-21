module ContainerExtensionsTests

open CardOverflow.Api
open Xunit
open SimpleInjector
open ContainerExtensions

[<Fact>]
let ``RegisterStuff verifies``() =
    use c = new Container()
    
    c.RegisterStuff
    c.RegisterStandardConnectionString
    
    c.Verify()

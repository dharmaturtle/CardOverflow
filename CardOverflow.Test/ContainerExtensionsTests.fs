module ContainerExtensionsTests

open CardOverflow.Api
open Xunit
open SimpleInjector
open ContainerExtensions

[<Fact>]
let ``RegisterNonView verifies``() =
    use c = new Container()
    
    c.RegisterNonView
    
    c.Verify()

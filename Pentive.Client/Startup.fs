namespace Pentive.Client

open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Bolero.Remoting.Client
open System.Text.Json
open NodaTime
open NodaTime.Serialization.SystemTextJson
open System.Text.Json.Serialization

module Program =
    let serializerOptions (options: JsonSerializerOptions) =
        options
            .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)
            .Converters.Add(JsonFSharpConverter(JsonUnionEncoding.ThothLike))

    [<EntryPoint>]
    let Main args =
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        builder.RootComponents.Add<Main.MyApp>("#main")
        builder.Services.AddRemoting(builder.HostEnvironment, serializerOptions) |> ignore
        builder.Build().RunAsync() |> ignore
        0

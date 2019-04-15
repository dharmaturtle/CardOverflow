namespace CardOverflow.Api
open System
open System.Linq

module Temperature =
    [<Measure>] type degC
    [<Measure>] type degF

    let convFactor = 1.8<degF/degC>
    let convertDegCToF c = c * convFactor + 32.0<degF>
    let convertDegFToC f = (f - 32.0<degF>) * (1.0 / convFactor)

open Temperature

[<CLIMutable>]
type WeatherForecast = {
    Date: DateTime
    TemperatureC: float<degC>
    Summary: string
 } with
    member this.TemperatureF = convertDegCToF this.TemperatureC

type WeatherService() =

    let summaries = [| "Freezing"; "Bracing"; "Chilly"; "Cool"; "Mild"; "Warm"; "Balmy"; "Hot"; "Sweltering"; "Scorching" |]

    member __.GetForecasts() =
        let rng = Random()
        Enumerable.Range(1, 5).Select(fun index -> {
            Date = DateTime.Now.AddDays(float index)
            TemperatureC = rng.Next(-20, 55) |> float |> (*) 1.0<degC>
            Summary = summaries.[rng.Next summaries.Length]
        })

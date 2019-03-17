namespace TwitterCurator.Shared
open System

module Temperature =
  [<Measure>] type degC
  [<Measure>] type degF

  let convFactor = 1.8<degF/degC>
  let convertDegCToF c = c * convFactor + 32.0<degF>
  let convertDegFToC f = (f - 32.0<degF>) * (1.0 / convFactor)

open Temperature

[<CLIMutable>]
type WeatherForecast = {
  Date : DateTime
  TemperatureC : float<degC>
  Summary : string
} with
  member this.TemperatureF = convertDegCToF this.TemperatureC

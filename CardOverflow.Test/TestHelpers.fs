module Test

open CardOverflow.Api

let DbService =
  ConnectionStringProvider() |> DbFactory |> DbService

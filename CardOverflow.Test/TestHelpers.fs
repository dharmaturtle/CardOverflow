module Test

open CardOverflow.Api

let DbService =
  DbService(DbFactory(ConnectionStringProvider()))

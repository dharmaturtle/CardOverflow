namespace CardOverflow.Test

open CardOverflow.Api
open System
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Xunit
open CardOverflow.Entity

type TestConnectionStringProvider(dbName: string) =
    interface IConnectionStringProvider with
        member __.Get = sprintf "Server=localhost;Database=CardOverflow_%s;Trusted_Connection=True;" dbName

type TempDbService( [<CallerMemberName>] ?memberName: string) =
    let dbFactory =
        match memberName with
        | Some testName -> Regex.Replace(testName, "[^A-Za-z0-9 _]", "").Replace(' ', '_') |> TestConnectionStringProvider |> DbFactory
        | _ -> failwith "Missing the caller's member name somehow."
    do 
        dbFactory |> InitializeDatabase.deleteAndRecreateDatabase

    interface IDisposable with
        member __.Dispose() =
            use db = dbFactory.Create()
            db.Database.EnsureDeleted() |> ignore

    member __.DbService =
        dbFactory |> DbService

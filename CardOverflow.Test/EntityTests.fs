module EntityTests

open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Debug
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open CardOverflow.Test
open System
open System.Linq
open Xunit
open CardOverflow.Pure
open System.Collections.Generic
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Sanitation
open FsToolkit.ErrorHandling
open NodaTime
open Npgsql
open Dapper
open SimpleInjector
open ContainerExtensions
open SimpleInjector.Lifestyles
open NpgsqlTypes
open CardOverflow.Entity

[<Fact>] // Works in NodaTime 3.0 and PostgreSQL 12.2
let ``TimezoneName enums are both in NodaTime and Postgres``() : Task<unit> = (taskResult {
    use c = new Container()
    c.RegisterStuffTestOnly
    c.RegisterStandardConnectionString
    use _ = AsyncScopedLifestyle.BeginScope c
    let! (conn: NpgsqlConnection) = c.GetInstance<Task<NpgsqlConnection>>()
    let! (pgtzs: string seq) = conn.QueryAsync<string> """SELECT name FROM pg_timezone_names;"""
    let intersection = pgtzs.Intersect DateTimeZoneProviders.Tzdb.Ids |> fun x -> x.OrderBy(fun x -> x)

    let enums = TimezoneName.all |> fun x -> x.OrderBy(fun x -> x)
    
    Assert.equal
        <| enums.Count()
        <| intersection.Count()
    for (i, e) in Seq.zip intersection enums do
        Assert.equal i e
    Assert.areEquivalent
        intersection
        enums
    pgtzs.Except(DateTimeZoneProviders.Tzdb.Ids).D("Unique to Postgres") |> ignore
    DateTimeZoneProviders.Tzdb.Ids.Except(pgtzs).D("Unique to NodaTime") |> ignore
    } |> TaskResult.getOk)

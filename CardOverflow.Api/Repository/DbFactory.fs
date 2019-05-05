﻿namespace CardOverflow.Api

open CardOverflow.Entity
open CardOverflow.Entity.Anki
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Diagnostics

type ConnectionString = ConnectionString of string
module ConnectionString =
    let value (ConnectionString cs) = cs

type CreateCardOverflowDb = unit -> CardOverflowDb
module CreateCardOverflowDb =
    let create (connectionString: ConnectionString) () =
        DbContextOptionsBuilder()
            .UseSqlServer(connectionString |> ConnectionString.value)
            .ConfigureWarnings(fun warnings -> warnings.Throw(RelationalEventId.QueryClientEvaluationWarning) |> ignore)
            .Options
        |> fun o -> new CardOverflowDb(o)

type IDbService =
    abstract Query: (CardOverflowDb -> 'a) -> 'a
    abstract Command: (CardOverflowDb -> 'a) -> unit

type DbService(createCardOverflowDb: CreateCardOverflowDb) =
    interface IDbService with
        member __.Query q =
            use db = createCardOverflowDb()
            q db
        member __.Command c =
            use db = createCardOverflowDb()
            c db |> ignore
            db.SaveChanges() |> ignore

type AnkiDbFactory(dbPath: string) =
    member __.Create() =
        DbContextOptionsBuilder()
            .UseSqlite("DataSource=" + dbPath)
            .ConfigureWarnings(fun warnings -> warnings.Throw(RelationalEventId.QueryClientEvaluationWarning) |> ignore)
            .Options
        |> fun o -> new AnkiDb(o)

type AnkiDbService(dbFactory: AnkiDbFactory) =
    member __.Query(q) =
        use db = dbFactory.Create()
        q db
    member __.Command(q): unit =
        use db = dbFactory.Create()
        q db |> ignore
        db.SaveChanges() |> ignore

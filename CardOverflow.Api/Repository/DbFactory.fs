namespace CardOverflow.Api

open CardOverflow.Entity
open CardOverflow.Entity.Anki
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Diagnostics

type IConnectionStringProvider =
    abstract Get: string

type ConnectionStringProvider() =
    interface IConnectionStringProvider with
        member __.Get = "Server=localhost;Database=CardOverflow;Trusted_Connection=True;"

type IDbFactory =
    abstract Create: unit -> CardOverflowDb

type DbFactory(connectionStringProvider: IConnectionStringProvider) =
    interface IDbFactory with
        member __.Create() =
            DbContextOptionsBuilder()
                .UseSqlServer(connectionStringProvider.Get)
                .ConfigureWarnings(fun warnings -> warnings.Throw(RelationalEventId.QueryClientEvaluationWarning) |> ignore)
                .Options
            |> fun o -> new CardOverflowDb(o)

type DbService(dbFactory: IDbFactory) =
    member __.Query(q) =
        use db = dbFactory.Create()
        q db
    member __.Command(q): unit =
        use db = dbFactory.Create()
        q db |> ignore
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

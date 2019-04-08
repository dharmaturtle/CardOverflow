namespace CardOverflow.Api

open CardOverflow.Entity
open CardOverflow.Entity.Anki
open Microsoft.EntityFrameworkCore

type IConnectionStringProvider =
    abstract Get: string

type ConnectionStringProvider() =
    interface IConnectionStringProvider with
        member __.Get = "Server=localhost;Database=CardOverflow;Trusted_Connection=True;"

type DbFactory(connectionStringProvider: IConnectionStringProvider) =
    member __.Create() =
        DbContextOptionsBuilder().UseSqlServer(connectionStringProvider.Get).Options
        |> fun o -> new CardOverflowDb(o)

type DbService(dbFactory: DbFactory) =
    member __.Query(q) =
        use db = dbFactory.Create()
        q db
    member __.Command(q): unit =
        use db = dbFactory.Create()
        q db |> ignore
        db.SaveChanges() |> ignore

type AnkiDbFactory(dbPath: string) =
    member __.Create() =
        DbContextOptionsBuilder().UseSqlite("DataSource=" + dbPath).Options
        |> fun o -> new AnkiDb(o)

type AnkiDbService(dbFactory: AnkiDbFactory) =
    member __.Query(q) =
        use db = dbFactory.Create()
        q db
    member __.Command(q): unit =
        use db = dbFactory.Create()
        q db |> ignore
        db.SaveChanges() |> ignore

namespace CardOverflow.Api

open Microsoft.EntityFrameworkCore
open CardOverflow.Entity

type IConnectionStringProvider =
  abstract member Get : string

type ConnectionStringProvider() =
  interface IConnectionStringProvider with 
    member this.Get = "Server=localhost;Database=CardOverflow;Trusted_Connection=True;"

type DbFactory(connectionStringProvider:IConnectionStringProvider) =
  member this.Create() = 
    DbContextOptionsBuilder().UseSqlServer(connectionStringProvider.Get).Options
    |> fun o -> new CardOverflowDb(o)

type DbService(dbFactory: DbFactory) =
  member this.Query(q) =
    use db = dbFactory.Create()
    q db
  member this.Command(q) : unit =
    use db = dbFactory.Create()
    q db |> ignore
    db.SaveChanges() |> ignore

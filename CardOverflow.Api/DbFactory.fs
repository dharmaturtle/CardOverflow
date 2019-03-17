namespace TwitterCurator.Shared

open Microsoft.EntityFrameworkCore
open CardOverflow.Entity

module DbFactory =
  let options = DbContextOptionsBuilder().UseSqlServer("Server=localhost;Database=TwitterCurator;Trusted_Connection=True;").Options
  let create () = new CardOverflowDb(options)

module DbService =
  let query (q) =
    use db = DbFactory.create ()
    q db
  let command (q) : unit =
    use db = DbFactory.create () // is this actually disposed?
    q db |> ignore
    db.SaveChanges () |> ignore

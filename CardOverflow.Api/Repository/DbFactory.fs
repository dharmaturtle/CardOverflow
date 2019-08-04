namespace CardOverflow.Api

open CardOverflow.Entity
open CardOverflow.Entity.Anki
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Diagnostics

type ConnectionString = ConnectionString of string
module ConnectionString =
    let value (ConnectionString cs) = cs

module AnkiDbFactory =
    let Create (dbPath: string) =
        DbContextOptionsBuilder()
            .UseSqlite("DataSource=" + dbPath)
            .Options
        |> fun o -> new AnkiDb(o)

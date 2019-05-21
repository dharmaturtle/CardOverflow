module Helpers

open Microsoft.EntityFrameworkCore

type DbContext with
    member dbContext.SaveChangesI =
        dbContext.SaveChanges() |> ignore

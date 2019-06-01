module Helpers

open Microsoft.EntityFrameworkCore
open System.IO

// http://www.fssnip.net/1g/title/Working-with-paths
let (+/) path1 path2 = Path.Combine(path1, path2)

type DbContext with
    member dbContext.SaveChangesI () =
        dbContext.SaveChanges () |> ignore

type DbSet<'TEntity when 'TEntity : not struct> with
    member dbSet.AddI entity =
        dbSet.Add entity |> ignore
    member dbSet.RemoveI entity =
        dbSet.Remove entity |> ignore
    member dbSet.UpdateI entity =
        dbSet.Update entity |> ignore

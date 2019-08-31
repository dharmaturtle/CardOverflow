module Helpers

open Microsoft.EntityFrameworkCore
open System.IO
open System
open FSharp.Control.Tasks

type DbContext with
    [<ObsoleteAttribute>] // medTODO delete this function
    member dbContext.SaveChangesI () =
        dbContext.SaveChanges () |> ignore
    member dbSet.SaveChangesAsyncI () =
        task {
            let! _ = dbSet.SaveChangesAsync()
            return ()
        }

type DbSet<'TEntity when 'TEntity : not struct> with
    member dbSet.AddI entity =
        dbSet.Add entity |> ignore
    member dbSet.RemoveI entity =
        dbSet.Remove entity |> ignore
    member dbSet.UpdateI entity =
        dbSet.Update entity |> ignore

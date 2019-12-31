module CardOptionTests

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
open CardOverflow.Pure
open CardOverflow.Pure.Core
open CardOverflow.Sanitation

[<Fact>]
let ``SanitizeCardOption.upsertMany can add/update new option``(): Task<unit> = task {
    let userId = 3
    use c = new TestContainer()
    let! options = SanitizeCardOptionRepository.getAll c.Db userId
    let defaultId = 3
    Assert.Equal(defaultId, options.Single().Id)
    let options =
        options.Append
            { ViewCardOption.load CardOptionsRepository.defaultCardOptions with
                IsDefault = false }
        |> toResizeArray
    let newId = defaultId + 1

    let! id = SanitizeCardOptionRepository.upsertMany c.Db userId options
    
    Assert.Equal(newId, (Result.getOk id).Single(fun x -> x  <> defaultId))
    let! option = c.Db.CardOption.SingleAsync(fun x -> x.Id = newId)
    Assert.False option.IsDefault

    // can update
    let! options = SanitizeCardOptionRepository.getAll c.Db userId
    let newName = Guid.NewGuid().ToString()
    
    let! id =
        SanitizeCardOptionRepository.upsertMany c.Db userId
            <| [options.Single(fun x -> x.Id = defaultId)
                { options.Single(fun x -> x.Id = newId) with Name = newName }
               ].ToList()
    
    let id = (Result.getOk id).Single(fun x -> x  <> defaultId)
    Assert.Equal(newId, id)
    Assert.Equal(newName, c.Db.CardOption.Single(fun x -> x.Id = id).Name)
    }

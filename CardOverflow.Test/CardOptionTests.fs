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
open CardOverflow.Sanitation

[<Fact>]
let ``SanitizeCardOption.upsert can save new option``(): Task<unit> = task {
    let userId = 3
    use c = new TestContainer()
    let option =
        { ViewCardOption.load CardOptionsRepository.defaultCardOptions with
            IsDefault = false }
    let newId = userId + 1

    let! id = SanitizeCardOption.upsert c.Db userId option
    
    let id = Result.getOk id
    Assert.Equal(newId, id)
    let! option = c.Db.CardOption.SingleAsync(fun x -> x.Id = newId)
    Assert.False option.IsDefault

    // can update
    let! options = SanitizeCardOption.getAll c.Db userId
    let newName = Guid.NewGuid().ToString()
    
    let! id =
        SanitizeCardOption.upsert c.Db userId
            { options.Single(fun x -> x.Id = newId) with Name = newName }
    
    let id = Result.getOk id
    Assert.Equal(newId, id)
    Assert.Equal(newName, c.Db.CardOption.Single(fun x -> x.Id = id).Name)
    }

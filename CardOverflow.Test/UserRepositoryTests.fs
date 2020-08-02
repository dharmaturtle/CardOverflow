module UserRepositoryTests

open CardOverflow.Entity
open CardOverflow.Debug
open CardOverflow.Pure
open Microsoft.EntityFrameworkCore
open CardOverflow.Api
open CardOverflow.Test
open System
open System.Linq
open Xunit
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Control.Tasks
open CardOverflow.Sanitation
open FsToolkit.ErrorHandling

[<Fact>]
let ``UserRepository works``(): Task<unit> = (taskResult {
    use c = new TestContainer()
    let displayName = Guid.NewGuid().ToString().[0..31]
    Assert.equal 32 displayName.Length
    let userId = 4

    do! UserRepository.create c.Db userId displayName
    
    Assert.equal userId <| c.Db.User.Single(fun x -> x.DisplayName = displayName).Id
    let! (ucs: User_GromplateInstanceEntity ResizeArray) = c.Db.User_GromplateInstance.Where(fun x -> x.UserId = userId).ToListAsync()
    Assert.areEquivalent [1001; 1002; 1003; 1006; 1005] <| ucs.Select(fun x -> x.GromplateInstanceId)
    Assert.equal 4 <| ucs.Select(fun x -> x.DefaultCardSettingId).Distinct().Single()
    } |> TaskResult.getOk)

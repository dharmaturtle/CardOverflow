module DbServiceTests

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Test
open System
open System.Linq
open Xunit

[<Fact>]
let ``DbService can add and retreive a user``() =
    use p = new SqlTempDbProvider()
    let name = Guid.NewGuid().ToString().Take(32) |> String.Concat
    let email = Guid.NewGuid().ToString()

    p.DbService.Command(fun db -> db.Users.Add(UserEntity(Email = email, DisplayName = name)))

    p.DbService.Query(fun db -> db.Users.ToList())
    |> Seq.filter(fun x -> x.DisplayName = name && x.Email = email)
    |> Seq.length
    |> fun l -> Assert.Equal(1, l)

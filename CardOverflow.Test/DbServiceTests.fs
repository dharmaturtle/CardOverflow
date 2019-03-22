module DbServiceTests

open CardOverflow.Entity
open System
open System.Linq
open Xunit

[<Fact>]
let ``DbService can add and retreive a user``() =
  let name = Guid.NewGuid().ToString().Take(32) |> String.Concat
  let email = Guid.NewGuid().ToString()

  Test.DbService.Command(fun db -> db.Users.Add(User(
                                                  Email = email,
                                                  Name = name)
                                               ))

  Test.DbService.Query(fun db -> db.Users.ToList())
  |> Seq.filter (fun x -> x.Name = name && x.Email = email)
  |> Seq.length
  |> fun l -> Assert.Equal(1, l)

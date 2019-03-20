module DbServiceTests

open System
open System.Linq
open Xunit
open CardOverflow.Api
open CardOverflow.Entity

[<Fact>]
let ``DbService can add and retreive a user`` () =
  let name = Guid.NewGuid().ToString().Take(32) |> String.Concat
  let email = Guid.NewGuid().ToString()

  DbService.command (fun db -> db.Users.Add (User (
                                              Email = email,
                                              Name = name)
                                            ))

  DbService.query(fun db -> db.Users.ToList())
  |> Seq.filter (fun x -> x.Name = name && x.Email = email)
  |> Seq.length
  |> fun l -> Assert.Equal(1, l)

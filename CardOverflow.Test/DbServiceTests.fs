module DbServiceTests

open CardOverflow.Api
open CardOverflow.Entity
open CardOverflow.Test
open System
open System.Linq
open Xunit

[<Fact>]
let ``DbService can add and retreive a user``() =
  use tempDb = new TempDbService()
  let service = tempDb.DbService
  let name = Guid.NewGuid().ToString().Take(32) |> String.Concat
  let email = Guid.NewGuid().ToString()

  service.Command(fun db -> db.Users.Add(UserEntity(Email = email, Name = name)))

  service.Query(fun db -> db.Users.ToList())
  |> Seq.filter(fun x -> x.Name = name && x.Email = email)
  |> Seq.length
  |> fun l -> Assert.Equal(1, l)

//[<Fact>]
let ``Create a new database``() =
  ConnectionStringProvider() |> DbFactory |> DbService |> fun x -> x.Command(fun db -> db.Database.EnsureCreated())

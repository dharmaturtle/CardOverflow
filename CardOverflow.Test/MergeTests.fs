module MergeTests

open CardOverflow.Api.Extensions
open Xunit

type Person = { mutable id: int; mutable name: string }

let merge source (target: ResizeArray<Person>) =
  target.Merge
    (source)
    (fun (x, y) -> x.id = y.id)
    (id)
    (target.Remove >> ignore)
    (target.Add)
    (fun d s -> d.name <- s.name)

[<Fact>]
let ``Merge with different source and target updates``() =
  let newName0 = "new"
  let newName1 = "newer"
  let source = [ { id = 0; name = newName0 }; { id = 1; name = newName1 } ] |> ResizeArray<Person>
  let target = [ { id = 0; name = "old" }; { id = 1; name = "older" } ] |> ResizeArray<Person>

  merge source target

  Assert.Equal(newName0, target.[0].name)
  Assert.Equal(newName1, target.[1].name)

[<Fact>]
let ``Merge with more in source adds``() =
  let source = [ { id = 0; name = "x" }; { id = 1; name = "brand new" } ] |> ResizeArray<Person>
  let target = [ { id = 0; name = "y" }; ] |> ResizeArray<Person>

  merge source target

  Assert.Equal("x", target.[0].name)
  Assert.Equal("brand new", target.[1].name)

[<Fact>]
let ``Merge with more in target deletes``() =
  let source = [ { id = 0; name = "y" }; ] |> ResizeArray<Person>
  let target = [ { id = 0; name = "x" }; { id = 1; name = "I'm deleted" } ] |> ResizeArray<Person>

  merge source target

  Assert.Equal("y", target.[0].name)
  Assert.Single(target)

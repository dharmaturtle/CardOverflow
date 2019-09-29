module RelationshipTests

open CardOverflow.Api
open CardOverflow.Pure
open Xunit
open System

[<Fact>]
let ``Relationship.flipName with / flips``() =
    Assert.Equal(
        "child/clozeparent",
        Relationship.flipName "clozeparent/child"
    )
    
[<Fact>]
let ``Relationship.flipName without / does nothing``() =
    Assert.Equal(
        "clozeparentchild",
        Relationship.flipName "clozeparentchild"
    )

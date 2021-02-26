module ZtackTests

open Xunit
open CardOverflow.Pure
open Serilog
open System
open Domain
open Equinox.MemoryStore
open FSharp.UMX
open FsCheck.Xunit
open CardOverflow.Pure
open CardOverflow.Test
open EventWriter
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp
open Domain.Ztack

[<StandardProperty>]
let ``Create summary roundtrips`` (ztackSummary: Ztack.Events.Summary) (branchSummary: Branch.Events.Summary) = asyncResult {
    let c = TestEsContainer()
    let ztackWriter = c.ZtackWriter()
    do! c.BranchWriter().Create branchSummary
    let ztackSummary = { ztackSummary with ExpressionRevisionId = branchSummary.LeafIds.Head }

    do! ztackWriter.Create ztackSummary

    // event store roundtrips
    ztackSummary.Id
    |> c.ZtackEvents
    |> Seq.exactlyOne
    |> Assert.equal (Ztack.Events.Created ztackSummary)

    // azure table roundtrips
    let! actual, _ = c.TableClient().GetZtack ztackSummary.Id
    Assert.equal ztackSummary actual
    }

[<StandardProperty>]
let ``Edited roundtrips`` (ztackSummary: Ztack.Events.Summary) branchSummary tagsChanged = asyncResult {
    let c = TestEsContainer()
    let ztackWriter = c.ZtackWriter()
    do! c.BranchWriter().Create branchSummary
    let ztackSummary = { ztackSummary with ExpressionRevisionId = branchSummary.LeafIds.Head }
    do! ztackWriter.Create ztackSummary
    
    do! ztackWriter.ChangeTags tagsChanged ztackSummary.AuthorId ztackSummary.Id

    // event store roundtrips
    ztackSummary.Id
    |> c.ZtackEvents
    |> Seq.last
    |> Assert.equal (Ztack.Events.TagsChanged tagsChanged)

    // azure table roundtrips
    let! actual, _ = c.TableClient().GetZtack ztackSummary.Id
    Assert.equal (ztackSummary |> Fold.evolveTagsChanged tagsChanged) actual
    }

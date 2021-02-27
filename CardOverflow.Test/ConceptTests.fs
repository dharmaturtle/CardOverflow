module ConceptTests

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

[<StandardProperty>]
let ``ChangeDefaultBranch works`` (authorId, { NewOriginal = s; NewBranch = b; BranchTitle = _ }) = asyncResult {
    let c = TestEsContainer()
    let conceptBranchWriter = c.ConceptBranchWriter()
    let conceptWriter = c.ConceptWriter()
    do! conceptBranchWriter.Upsert(authorId, s)
    do! conceptBranchWriter.Upsert(authorId, b)

    do! conceptWriter.ChangeDefaultBranch (% s.Ids.ConceptId) (% b.Ids.BranchId) authorId

    % b.Ids.ConceptId
    |> c.ConceptEvents
    |> Seq.last
    |> Assert.equal (Concept.Events.DefaultBranchChanged { BranchId = % b.Ids.BranchId })
    }

[<StandardProperty>]
let ``ChangeDefaultBranch fails when branch is on a different concept`` (authorId, { NewOriginal = s1; NewBranch = _; BranchTitle = _ }, { NewOriginal = s2; NewBranch = b2; BranchTitle = _ }) = asyncResult {
    let c = TestEsContainer()
    let conceptBranchWriter = c.ConceptBranchWriter()
    let conceptWriter = c.ConceptWriter()
    do! conceptBranchWriter.Upsert(authorId, s1)
    do! conceptBranchWriter.Upsert(authorId, s2)
    do! conceptBranchWriter.Upsert(authorId, b2)

    do! conceptWriter.ChangeDefaultBranch (% s1.Ids.ConceptId) (% b2.Ids.BranchId) authorId
        
    |>% Result.getError
    |>% Assert.equal $"Branch {b2.Ids.BranchId} doesn't belong to Concept {s1.Ids.ConceptId}"
    }

[<StandardProperty>]
let ``ChangeDefaultBranch fails when branch author tries to be default`` (conceptAuthorId, branchAuthorId, { NewOriginal = s; NewBranch = b; BranchTitle = _ }) = asyncResult {
    let c = TestEsContainer()
    let conceptBranchWriter = c.ConceptBranchWriter()
    let conceptWriter = c.ConceptWriter()
    do! conceptBranchWriter.Upsert(conceptAuthorId,  s)
    do! conceptBranchWriter.Upsert(branchAuthorId, b)

    do! conceptWriter.ChangeDefaultBranch (% s.Ids.ConceptId) (% b.Ids.BranchId) branchAuthorId

    |>% Result.getError
    |>% Assert.equal $"Concept {s.Ids.ConceptId} doesn't belong to User {branchAuthorId}"
    }

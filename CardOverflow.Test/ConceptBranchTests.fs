module ConceptBranchTests

open Xunit
open Serilog
open System
open Domain
open Equinox.MemoryStore
open FSharp.UMX
open FsCheck.Xunit
open CardOverflow.Pure
open CardOverflow.Test
open FsCodec
open EventWriter
open Hedgehog
open CardOverflow.Api
open FsToolkit.ErrorHandling

[<StandardProperty>]
let ``ConceptBranchWriter.Upsert persists both summaries`` (authorId, command, tags) = asyncResult {
    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags }
    let c = TestEsContainer()
    let conceptBranchWriter = c.ConceptBranchWriter()
    let expectedConcept, expectedBranch = ConceptBranch.conceptBranch authorId command None "Default"
    
    do! conceptBranchWriter.Upsert(authorId, command)
    
    // memory store roundtrips
    % expectedConcept.Id
    |> c.ConceptEvents
    |> Assert.Single
    |> Assert.equal (Concept.Events.Created expectedConcept)
    % expectedBranch.Id
    |> c.BranchEvents
    |> Assert.Single
    |> Assert.equal (Branch.Events.Created expectedBranch)
    
    // azure table roundtrips
    let! actual, _ = c.TableClient().GetConcept (string command.Ids.ConceptId)
    Assert.equal expectedConcept actual
    let! actual, _ = c.TableClient().GetBranch (string command.Ids.BranchId)
    Assert.equal expectedBranch actual
    let! actual, _ = c.TableClient().GetExpressionRevision (string command.Ids.LeafId)
    Assert.equal (Branch.toLeafSummary expectedBranch) actual
    }
    
[<StandardProperty>]
let ``ConceptBranchWriter.Upsert persists edit`` (authorId, command1, command2, tags, title) = asyncResult {
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewLeaf_Title title; Ids = { command1.Ids with LeafId = command2.Ids.LeafId } }
    let c = TestEsContainer()
    let conceptBranchWriter = c.ConceptBranchWriter()
    let expectedConcept, expectedBanchSummary1 = ConceptBranch.conceptBranch authorId command1 None "Default"
    let expectedBranchEdit : Branch.Events.Edited =
        let _, b = ConceptBranch.conceptBranch authorId command2 None title
        { LeafId             = b.LeafIds.Head
          Title              = b.Title
          TemplateRevisionId = b.TemplateRevisionId
          FieldValues        = b.FieldValues
          EditSummary        = b.EditSummary }
    do! conceptBranchWriter.Upsert(authorId, command1)
        
    do! conceptBranchWriter.Upsert(authorId, command2)

    % command2.Ids.BranchId
    |> c.BranchEvents
    |> Seq.last
    |> Assert.equal (Branch.Events.Edited expectedBranchEdit)
    
    // azure table roundtrips
    let! actual, _ = c.TableClient().GetConcept (string command2.Ids.ConceptId)
    Assert.equal expectedConcept actual
    let! actual, _ = c.TableClient().GetBranch (string command2.Ids.BranchId)
    let evolvedSummary = Branch.Fold.evolveEdited expectedBranchEdit expectedBanchSummary1
    Assert.equal evolvedSummary actual
    let! actual, _ = c.TableClient().GetExpressionRevision (string command1.Ids.LeafId)
    Assert.equal (Branch.toLeafSummary expectedBanchSummary1) actual
    let! actual, _ = c.TableClient().GetExpressionRevision (string command2.Ids.LeafId)
    Assert.equal (Branch.toLeafSummary evolvedSummary) actual
    }
    
[<StandardProperty>]
let ``ConceptBranchWriter.Upsert persists new branch`` (authorId, { NewOriginal = newOriginal; NewBranch = newBranch; BranchTitle = title }) =
    let c = TestEsContainer()
    let conceptBranchWriter = c.ConceptBranchWriter()
    let expectedBranch : Branch.Events.Summary =
        ConceptBranch.conceptBranch authorId newBranch None title
        |> snd
    conceptBranchWriter.Upsert(authorId, newOriginal) |> RunSynchronously.OkEquals ()
        
    conceptBranchWriter.Upsert(authorId, newBranch) |> RunSynchronously.OkEquals ()

    % newBranch.Ids.BranchId
    |> c.BranchEvents
    |> Seq.exactlyOne
    |> Assert.equal (Branch.Events.Created expectedBranch)

[<StandardProperty>]
let ``ConceptBranchWriter.Upsert fails to persist edit with duplicate leafId`` (authorId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewLeaf_Title title; Ids = command1.Ids }
    let c = TestEsContainer()
    let conceptBranchWriter = c.ConceptBranchWriter()
    conceptBranchWriter.Upsert(authorId, command1) |> RunSynchronously.OkEquals ()
        
    conceptBranchWriter.Upsert(authorId, command2)

    |> RunSynchronously.ErrorEquals $"Duplicate leafId:{command1.Ids.LeafId}"

[<StandardProperty>]
let ``ConceptBranchWriter.Upsert fails to persist edit with another author`` (authorId, hackerId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewLeaf_Title title; Ids = command1.Ids }
    let c = TestEsContainer()
    let conceptBranchWriter = c.ConceptBranchWriter()
    conceptBranchWriter.Upsert(authorId, command1) |> RunSynchronously.OkEquals ()
        
    conceptBranchWriter.Upsert(hackerId, command2)

    |> RunSynchronously.ErrorEquals $"You ({hackerId}) aren't the author"

[<StandardProperty>]
let ``ConceptBranchWriter.Upsert fails to insert twice`` (authorId, command, tags) =
    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags }
    let c = TestEsContainer()
    let conceptBranchWriter = c.ConceptBranchWriter()
    conceptBranchWriter.Upsert(authorId, command) |> RunSynchronously.OkEquals ()
        
    conceptBranchWriter.Upsert(authorId, command)

    |> RunSynchronously.ErrorEquals $"Concept '{command.Ids.ConceptId}' already exists."

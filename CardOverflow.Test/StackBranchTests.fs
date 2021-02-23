module StackBranchTests

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

[<StandardProperty>]
let ``StackBranchWriter.Upsert persists both summaries`` (authorId, command, tags) =
    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags }
    let c = TestEsContainer()
    let stackBranchWriter = c.StackBranchWriter()
    let expectedStack, expectedBranch = StackBranch.stackBranch authorId command None tags "Default"
    
    stackBranchWriter.Upsert(authorId, command)
    
    |> RunSynchronously.OkEquals ()
    % expectedStack.Id
    |> c.StackEvents
    |> Assert.Single
    |> Assert.equal (Stack.Events.Created expectedStack)
    % expectedBranch.Id
    |> c.BranchEvents
    |> Assert.Single
    |> Assert.equal (Branch.Events.Created expectedBranch)
    
[<StandardProperty>]
let ``StackBranchWriter.Upsert persists edit`` (authorId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewLeaf_Title title; Ids = { command1.Ids with LeafId = command2.Ids.LeafId } }
    let c = TestEsContainer()
    let stackBranchWriter = c.StackBranchWriter()
    let expectedBranch : Branch.Events.Edited =
        let _, b = StackBranch.stackBranch authorId command2 None tags title
        { LeafId      = b.LeafId
          Title       = b.Title
          GrompleafId = b.GrompleafId
          FieldValues = b.FieldValues
          EditSummary = b.EditSummary }
    stackBranchWriter.Upsert(authorId, command1) |> RunSynchronously.OkEquals ()
        
    stackBranchWriter.Upsert(authorId, command2) |> RunSynchronously.OkEquals ()

    % command2.Ids.BranchId
    |> c.BranchEvents
    |> Seq.last
    |> Assert.equal (Branch.Events.Edited expectedBranch)
    
[<StandardProperty>]
let ``StackBranchWriter.Upsert persists new branch`` (authorId, { NewOriginal = newOriginal; NewBranch = newBranch; BranchTitle = title }) =
    let c = TestEsContainer()
    let stackBranchWriter = c.StackBranchWriter()
    let expectedBranch : Branch.Events.Summary =
        StackBranch.stackBranch authorId newBranch None Set.empty title
        |> snd
    stackBranchWriter.Upsert(authorId, newOriginal) |> RunSynchronously.OkEquals ()
        
    stackBranchWriter.Upsert(authorId, newBranch) |> RunSynchronously.OkEquals ()

    % newBranch.Ids.BranchId
    |> c.BranchEvents
    |> Seq.exactlyOne
    |> Assert.equal (Branch.Events.Created expectedBranch)

[<StandardProperty>]
let ``StackBranchWriter.Upsert fails to persist edit with duplicate leafId`` (authorId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewLeaf_Title title; Ids = command1.Ids }
    let c = TestEsContainer()
    let stackBranchWriter = c.StackBranchWriter()
    stackBranchWriter.Upsert(authorId, command1) |> RunSynchronously.OkEquals ()
        
    stackBranchWriter.Upsert(authorId, command2)

    |> RunSynchronously.ErrorEquals $"Duplicate leafId:{command1.Ids.LeafId}"

[<StandardProperty>]
let ``StackBranchWriter.Upsert fails to persist edit with another author`` (authorId, hackerId, command1, command2, tags, title) =
    let command1 = { command1 with Kind = UpsertKind.NewOriginal_TagIds tags }
    let command2 = { command2 with Kind = UpsertKind.NewLeaf_Title title; Ids = command1.Ids }
    let c = TestEsContainer()
    let stackBranchWriter = c.StackBranchWriter()
    stackBranchWriter.Upsert(authorId, command1) |> RunSynchronously.OkEquals ()
        
    stackBranchWriter.Upsert(hackerId, command2)

    |> RunSynchronously.ErrorEquals $"You aren't the author"

[<StandardProperty>]
let ``StackBranchWriter.Upsert fails to insert twice`` (authorId, command, tags) =
    let command = { command with Kind = UpsertKind.NewOriginal_TagIds tags }
    let c = TestEsContainer()
    let stackBranchWriter = c.StackBranchWriter()
    stackBranchWriter.Upsert(authorId, command) |> RunSynchronously.OkEquals ()
        
    stackBranchWriter.Upsert(authorId, command)

    |> RunSynchronously.ErrorEquals $"Stack '{command.Ids.StackId}' already exists."

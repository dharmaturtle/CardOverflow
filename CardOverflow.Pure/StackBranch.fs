module Domain.StackBranch

open Domain
open FSharp.UMX
open FsToolkit.ErrorHandling
open CardOverflow.Pure

let branch authorId command tags title : Branch.Events.Snapshotted =
    { BranchId = % command.Ids.BranchId
      LeafId = % command.Ids.LeafId
      LeafIds = [ % command.Ids.LeafId ]
      Title = title
      StackId = % command.Ids.StackId
      AuthorId = authorId
      GrompleafId = % command.Grompleaf.Id
      AnkiNoteId = None
      GotDMCAed = false
      FieldValues = command.FieldValues |> Seq.map (fun x -> x.EditField.Name, x.Value) |> Map.ofSeq
      EditSummary = command.EditSummary
      Tags = tags }
let stack authorId command sourceLeafId : Stack.Events.Snapshotted =
    { StackId = % command.Ids.StackId
      DefaultBranchId = % command.Ids.BranchId
      AuthorId = authorId
      CopySourceLeafId = sourceLeafId }
let stackBranch authorId command sourceLeafId tags title =
    (stack  authorId command sourceLeafId),
    (branch authorId command tags title)

type Service
    (   stacks : Stack.Service,
        branches : Branch.Service) =
    
    let create (stackSnapshot, branchSnapshot) = asyncResult {
        do!     stacks  .Create stackSnapshot
        return! branches.Create branchSnapshot
    }

    member _.Upsert (authorId, command) =
        match command.Kind with
        | NewOriginal_TagIds tags ->
            stackBranch authorId command None                     tags "Default" |> create
        | NewCopy_SourceLeafId_TagIds (sourceLeafId, tags) ->
            stackBranch authorId command (% sourceLeafId |> Some) tags "Default" |> create
        | NewBranch_Title title ->
            branches.Create(branch authorId command [] title)
        | NewLeaf_Title title ->
            let branch  =   branch authorId command [] title
            let edited : Branch.Events.Edited =
                { LeafId      = branch.LeafId
                  Title       = branch.Title
                  GrompleafId = branch.GrompleafId
                  FieldValues = branch.FieldValues
                  EditSummary = branch.EditSummary }
            branches.Edit(edited, branch.BranchId, authorId)

module DiffTests

open CardOverflow.Api
open CardOverflow.Pure
open Xunit
open System
open System.Linq
open CardOverflow.Test

[<Generators>]
let ``Diff Unchanged`` (stackBranchInstanceIds: StackBranchInstanceIds list): bool =
    let theirs = stackBranchInstanceIds
    let mine = stackBranchInstanceIds
    
    Diff.ids theirs mine

    |> (=) (stackBranchInstanceIds |> List.map Unchanged)

[<Generators>]
let ``Diff AddedStack`` (stackBranchInstanceIds: StackBranchInstanceIds list): bool =
    let theirs = stackBranchInstanceIds
    let mine = stackBranchInstanceIds.[0] |> List.singleton
    
    Diff.ids theirs mine
    
    |> (=)
        (stackBranchInstanceIds |> List.mapi (fun i x ->
            match i with
            | 0 -> Unchanged x
            | _ -> AddedStack x
        ))

[<Generators>]
let ``Diff RemovedStack`` (stackBranchInstanceIds: StackBranchInstanceIds list): bool =
    let mine = stackBranchInstanceIds
    let theirs = stackBranchInstanceIds.[0] |> List.singleton
    
    Diff.ids theirs mine
    
    |> (=)
        (stackBranchInstanceIds |> List.mapi (fun i x ->
            match i with
            | 0 -> Unchanged x
            | _ -> RemovedStack x
        ))

[<Generators>]
let ``Diff BranchInstanceChanged`` (stackBranchInstanceIds: StackBranchInstanceIds list): bool =
    let theirs = stackBranchInstanceIds
    let mine =
        {   stackBranchInstanceIds.[0] with
                BranchInstanceId = Int32.MinValue
        }   |> List.singleton
    
    Diff.ids theirs mine
    
    |> (=)
        (stackBranchInstanceIds |> List.mapi (fun i x ->
            match i with
            | 0 -> BranchInstanceChanged (x, mine.[0])
            | _ -> AddedStack x
        ))

[<Generators>]
let ``Diff BranchChanged`` (stackBranchInstanceIds: StackBranchInstanceIds list): bool =
    let theirs = stackBranchInstanceIds
    let mine =
        {   stackBranchInstanceIds.[0] with
                BranchId = Int32.MinValue
                BranchInstanceId = Int32.MinValue
        }   |> List.singleton
    
    Diff.ids theirs mine
    
    |> (=)
        (stackBranchInstanceIds |> List.mapi (fun i x ->
            match i with
            | 0 -> BranchChanged (x, mine.[0])
            | _ -> AddedStack x
        ))

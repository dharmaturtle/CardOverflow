module DiffTests

open CardOverflow.Api
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System
open System.Linq
open CardOverflow.Test

[<Generators>]
let ``Diff Unchanged`` (stackBranchInstanceIndexes: StackBranchInstanceIndex list) : unit =
    let theirs = stackBranchInstanceIndexes
    let mine = stackBranchInstanceIndexes
    
    Diff.ids theirs mine

    |> Assert.areEquivalent (stackBranchInstanceIndexes |> List.map Unchanged)

[<Generators>]
let ``Diff AddedStack`` (stackBranchInstanceIndexes: StackBranchInstanceIndex list) : unit =
    let theirs = stackBranchInstanceIndexes
    let mine = stackBranchInstanceIndexes.[0] |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (stackBranchInstanceIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> Unchanged x
            | _ -> AddedStack x
        ))

[<Generators>]
let ``Diff RemovedStack`` (stackBranchInstanceIndexes: StackBranchInstanceIndex list) : unit =
    let mine = stackBranchInstanceIndexes
    let theirs = stackBranchInstanceIndexes.[0] |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (stackBranchInstanceIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> Unchanged x
            | _ -> RemovedStack x
        ))

[<Generators>]
let ``Diff BranchInstanceChanged`` (stackBranchInstanceIndexes: StackBranchInstanceIndex list) : unit =
    let theirs = stackBranchInstanceIndexes
    let mine =
        {   stackBranchInstanceIndexes.[0] with
                BranchInstanceId = Int32.MinValue
        }   |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (stackBranchInstanceIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> BranchInstanceChanged (x, mine.[0])
            | _ -> AddedStack x
        ))

[<Generators>]
let ``Diff BranchChanged`` (stackBranchInstanceIndexes: StackBranchInstanceIndex list) : unit =
    let theirs = stackBranchInstanceIndexes
    let mine =
        {   stackBranchInstanceIndexes.[0] with
                BranchId = Int32.MinValue
                BranchInstanceId = Int32.MinValue
        }   |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (stackBranchInstanceIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> BranchChanged (x, mine.[0])
            | _ -> AddedStack x
        ))
        
[<Generators>]
let ``Diff of deck with itself is unchanged _ when it contains 2 of the same branch with differing indexes`` (stackBranchInstanceIndex: StackBranchInstanceIndex) : unit =
    let a =
        [ { stackBranchInstanceIndex with Index = 0s }
          { stackBranchInstanceIndex with Index = 1s } ]
    
    Diff.ids a a
    
    |> Assert.areEquivalent
        (a |> List.map Unchanged)

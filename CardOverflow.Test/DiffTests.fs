module DiffTests

open CardOverflow.Api
open LoadersAndCopiers
open CardOverflow.Pure
open CardOverflow.Debug
open Xunit
open System
open System.Linq
open CardOverflow.Test

[<Generators>]
let ``Diff Unchanged`` (stackLeafIndexes: StackLeafIndex list) : unit =
    let theirs = stackLeafIndexes
    let mine = stackLeafIndexes
    
    Diff.ids theirs mine

    |> Assert.areEquivalent (stackLeafIndexes |> List.map Unchanged)

[<Generators>]
let ``Diff AddedStack`` (stackLeafIndexes: StackLeafIndex list) : unit =
    let theirs = stackLeafIndexes
    let mine = stackLeafIndexes.[0] |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (stackLeafIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> Unchanged x
            | _ -> AddedStack x
        ))

[<Generators>]
let ``Diff RemovedStack`` (stackLeafIndexes: StackLeafIndex list) : unit =
    let mine = stackLeafIndexes
    let theirs = stackLeafIndexes.[0] |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (stackLeafIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> Unchanged x
            | _ -> RemovedStack x
        ))

[<Generators>]
let ``Diff LeafChanged`` (stackLeafIndexes: StackLeafIndex list) : unit =
    let theirs = stackLeafIndexes
    let mine =
        {   stackLeafIndexes.[0] with
                LeafId = Ulid.create
        }   |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (stackLeafIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> LeafChanged (x, mine.[0])
            | _ -> AddedStack x
        ))

[<Generators>]
let ``Diff BranchChanged`` (stackLeafIndexes: StackLeafIndex list) : unit =
    let theirs = stackLeafIndexes
    let mine =
        {   stackLeafIndexes.[0] with
                BranchId = Ulid.create
                LeafId = Ulid.create
        }   |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (stackLeafIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> BranchChanged (x, mine.[0])
            | _ -> AddedStack x
        ))
        
[<Generators>]
let ``Diff of deck with itself is unchanged _ when it contains 2 of the same branch with differing indexes`` (stackLeafIndex: StackLeafIndex) : unit =
    let a =
        [ { stackLeafIndex with Index = 0s }
          { stackLeafIndex with Index = 1s } ]
    
    Diff.ids a a
    
    |> Assert.areEquivalent
        (a |> List.map Unchanged)

[<Generators>]
let ``Diff of MoveToAnotherDeck works`` (ids: StackLeafIndex) : unit =
    let ids index = { ids with Index = index }
    let theirs = [ ids 0s ]
    let mine   = [ ids 0s; ids 1s ]
    
    Diff.ids theirs mine |> Diff.toSummary
    
    |> Assert.equal 
        { Unchanged             = [ ids 0s ]
          MoveToAnotherDeck     = [ ids 1s ]
          LeafChanged = []
          BranchChanged         = []
          AddedStack            = []
          RemovedStack          = [] }

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
let ``Diff Unchanged`` (conceptLeafIndexes: ConceptLeafIndex list) : unit =
    let theirs = conceptLeafIndexes
    let mine = conceptLeafIndexes
    
    Diff.ids theirs mine

    |> Assert.areEquivalent (conceptLeafIndexes |> List.map Unchanged)

[<Generators>]
let ``Diff AddedConcept`` (conceptLeafIndexes: ConceptLeafIndex list) : unit =
    let theirs = conceptLeafIndexes
    let mine = conceptLeafIndexes.[0] |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (conceptLeafIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> Unchanged x
            | _ -> AddedConcept x
        ))

[<Generators>]
let ``Diff RemovedConcept`` (conceptLeafIndexes: ConceptLeafIndex list) : unit =
    let mine = conceptLeafIndexes
    let theirs = conceptLeafIndexes.[0] |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (conceptLeafIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> Unchanged x
            | _ -> RemovedConcept x
        ))

[<Generators>]
let ``Diff LeafChanged`` (conceptLeafIndexes: ConceptLeafIndex list) : unit =
    let theirs = conceptLeafIndexes
    let mine =
        {   conceptLeafIndexes.[0] with
                LeafId = Ulid.create
        }   |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (conceptLeafIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> LeafChanged (x, mine.[0])
            | _ -> AddedConcept x
        ))

[<Generators>]
let ``Diff BranchChanged`` (conceptLeafIndexes: ConceptLeafIndex list) : unit =
    let theirs = conceptLeafIndexes
    let mine =
        {   conceptLeafIndexes.[0] with
                BranchId = Ulid.create
                LeafId = Ulid.create
        }   |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (conceptLeafIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> BranchChanged (x, mine.[0])
            | _ -> AddedConcept x
        ))
        
[<Generators>]
let ``Diff of deck with itself is unchanged _ when it contains 2 of the same branch with differing indexes`` (conceptLeafIndex: ConceptLeafIndex) : unit =
    let a =
        [ { conceptLeafIndex with Index = 0s }
          { conceptLeafIndex with Index = 1s } ]
    
    Diff.ids a a
    
    |> Assert.areEquivalent
        (a |> List.map Unchanged)

[<Generators>]
let ``Diff of MoveToAnotherDeck works`` (ids: ConceptLeafIndex) : unit =
    let ids index = { ids with Index = index }
    let theirs = [ ids 0s ]
    let mine   = [ ids 0s; ids 1s ]
    
    Diff.ids theirs mine |> Diff.toSummary
    
    |> Assert.equal 
        { Unchanged             = [ ids 0s ]
          MoveToAnotherDeck     = [ ids 1s ]
          LeafChanged = []
          BranchChanged         = []
          AddedConcept            = []
          RemovedConcept          = [] }

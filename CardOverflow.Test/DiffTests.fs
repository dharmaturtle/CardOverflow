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
let ``Diff Unchanged`` (conceptRevisionIndexes: ConceptRevisionIndex list) : unit =
    let theirs = conceptRevisionIndexes
    let mine = conceptRevisionIndexes
    
    Diff.ids theirs mine

    |> Assert.areEquivalent (conceptRevisionIndexes |> List.map Unchanged)

[<Generators>]
let ``Diff AddedConcept`` (conceptRevisionIndexes: ConceptRevisionIndex list) : unit =
    let theirs = conceptRevisionIndexes
    let mine = conceptRevisionIndexes.[0] |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (conceptRevisionIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> Unchanged x
            | _ -> AddedConcept x
        ))

[<Generators>]
let ``Diff RemovedConcept`` (conceptRevisionIndexes: ConceptRevisionIndex list) : unit =
    let mine = conceptRevisionIndexes
    let theirs = conceptRevisionIndexes.[0] |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (conceptRevisionIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> Unchanged x
            | _ -> RemovedConcept x
        ))

[<Generators>]
let ``Diff RevisionChanged`` (conceptRevisionIndexes: ConceptRevisionIndex list) : unit =
    let theirs = conceptRevisionIndexes
    let mine =
        {   conceptRevisionIndexes.[0] with
                RevisionId = Ulid.create
        }   |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (conceptRevisionIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> RevisionChanged (x, mine.[0])
            | _ -> AddedConcept x
        ))

[<Generators>]
let ``Diff ExampleChanged`` (conceptRevisionIndexes: ConceptRevisionIndex list) : unit =
    let theirs = conceptRevisionIndexes
    let mine =
        {   conceptRevisionIndexes.[0] with
                ExampleId = Ulid.create
                RevisionId = Ulid.create
        }   |> List.singleton
    
    Diff.ids theirs mine
    
    |> Assert.areEquivalent
        (conceptRevisionIndexes |> List.mapi (fun i x ->
            match i with
            | 0 -> ExampleChanged (x, mine.[0])
            | _ -> AddedConcept x
        ))
        
[<Generators>]
let ``Diff of deck with itself is unchanged _ when it contains 2 of the same example with differing indexes`` (conceptRevisionIndex: ConceptRevisionIndex) : unit =
    let a =
        [ { conceptRevisionIndex with Index = 0s }
          { conceptRevisionIndex with Index = 1s } ]
    
    Diff.ids a a
    
    |> Assert.areEquivalent
        (a |> List.map Unchanged)

[<Generators>]
let ``Diff of MoveToAnotherDeck works`` (ids: ConceptRevisionIndex) : unit =
    let ids index = { ids with Index = index }
    let theirs = [ ids 0s ]
    let mine   = [ ids 0s; ids 1s ]
    
    Diff.ids theirs mine |> Diff.toSummary
    
    |> Assert.equal 
        { Unchanged         = [ ids 0s ]
          MoveToAnotherDeck = [ ids 1s ]
          RevisionChanged       = []
          ExampleChanged    = []
          AddedConcept      = []
          RemovedConcept    = [] }

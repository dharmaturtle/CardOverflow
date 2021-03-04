module TableClientTests

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
let ``can insert, get, and update concept summary`` (concept: Concept.Events.Summary, revisionId: RevisionId) = async {
    let tc = TestEsContainer().KeyValueStore()

    // insert
    let! _ = tc.InsertOrReplace concept
    
    // get
    let! actual, _ = tc.Get<Concept.Events.Summary> concept.Id

    Assert.equal concept actual 

    // update
    do! tc.Update(fun (x: Concept.Events.Summary) -> { x with CopySourceRevisionId = Some revisionId }) concept.Id
    
    let! actual, _ = tc.Get<Concept.Events.Summary> concept.Id

    Assert.equal { concept with CopySourceRevisionId = Some revisionId } actual 
    }

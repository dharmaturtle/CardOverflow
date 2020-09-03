namespace CardOverflow.Api

open CardOverflow.Debug
open MappingTools
open CardOverflow.Entity
open CardOverflow.Pure
open CardOverflow.Pure.Core
open System
open System.Linq
open FsToolkit.ErrorHandling
open System.Security.Cryptography
open System.Text
open System.Collections.Generic
open System.Text.RegularExpressions
open System.Collections
open NUlid

type Ulid =
    static member create with get () = Ulid.NewUlid().ToGuid()

module UpsertIds =
    let create = {
        StackId = Ulid.create
        BranchId = Ulid.create
        LeafId = Ulid.create
        CardIds = [ Ulid.create ]
    }

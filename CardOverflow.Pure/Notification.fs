namespace CardOverflow.Pure

open FsToolkit.ErrorHandling
open System.Runtime.InteropServices
open CardOverflow.Pure.Extensions
open CardOverflow.Debug
open System
open System.Linq
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open Thoth.Json.Net

(*  If you change this file, make sure it is backwards compatible!

    You should probably write a test covering legacy types. *)

type NotificationType = // the indexes currently have no semantic meaning - they're here because F# requires them
    | DeckAddedBranchInstance = 0
    | DeckUpdatedBranchInstance = 1
    | DeckDeletedBranchInstance = 2

type DeckAddedBranchInstance = {
    DeckId: int
    NewStackId: int
    NewBranchId: int
    NewBranchInstanceId: int
}

type DeckUpdatedBranchInstance = {
    DeckId: int
    NewStackId: int
    NewBranchId: int
    NewBranchInstanceId: int
    AcquiredStackId: int
    AcquiredBranchId: int
    AcquiredBranchInstanceId: int
}

type DeckDeletedBranchInstance = {
    DeckId: int
    DeletedStackId: int
    DeletedBranchId: int
    DeletedBranchInstanceId: int
}

type Message =
    | DeckAddedBranchInstance of DeckAddedBranchInstance
    | DeckUpdatedBranchInstance of DeckUpdatedBranchInstance
    | DeckDeletedBranchInstance of DeckDeletedBranchInstance

type Notification = {
    Id: int
    SenderId: int
    SenderDisplayName: string
    TimeStamp: DateTime
    Message: Message
}

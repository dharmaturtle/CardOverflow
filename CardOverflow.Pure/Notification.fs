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
    | DeckAddedStack = 0
    | DeckUpdatedStack = 1
    | DeckDeletedStack = 2

type DeckAddedStack = {
    DeckId: int
    NewStackId: int
    NewBranchId: int
    NewBranchInstanceId: int
}

type DeckUpdatedStack = {
    DeckId: int
    NewStackId: int
    NewBranchId: int
    NewBranchInstanceId: int
    AcquiredStackId: int Option
    AcquiredBranchId: int Option
    AcquiredBranchInstanceId: int Option
}

type DeckDeletedStack = {
    DeckId: int
    DeletedStackId: int
    DeletedBranchId: int
    DeletedBranchInstanceId: int
}

type Message =
    | DeckAddedStack of DeckAddedStack
    | DeckUpdatedStack of DeckUpdatedStack
    | DeckDeletedStack of DeckDeletedStack

type Notification = {
    Id: int
    SenderId: int
    SenderDisplayName: string
    TimeStamp: DateTime
    Message: Message
}

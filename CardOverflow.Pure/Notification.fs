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

type IdName = {
    Id: int
    Name: string
}

type DeckAddedStack = {
    TheirDeck: IdName
    MyDeck: IdName Option
    New: StackBranchInstanceIds
    Collected: StackBranchInstanceIds Option
}

type DeckUpdatedStack = {
    TheirDeck: IdName
    MyDeck: IdName Option
    New: StackBranchInstanceIds
    Collected: StackBranchInstanceIds Option
}

type DeckDeletedStack = {
    TheirDeck: IdName
    MyDeck: IdName Option
    Deleted: StackBranchInstanceIds
    Collected: StackBranchInstanceIds Option
}

type Message =
    | DeckAddedStack of DeckAddedStack
    | DeckUpdatedStack of DeckUpdatedStack
    | DeckDeletedStack of DeckDeletedStack
with
    member this.TryDeckAddedStack([<Out>] out: _ byref) =
        match this with
        | DeckAddedStack x -> out <- x; true
        | _ -> false
    member this.TryDeckUpdatedStack([<Out>] out: _ byref) =
        match this with
        | DeckUpdatedStack x -> out <- x; true
        | _ -> false
    member this.TryDeckDeletedStack([<Out>] out: _ byref) =
        match this with
        | DeckDeletedStack x -> out <- x; true
        | _ -> false

type Notification = {
    Id: int
    SenderId: int
    SenderDisplayName: string
    TimeStamp: DateTime
    Message: Message
}

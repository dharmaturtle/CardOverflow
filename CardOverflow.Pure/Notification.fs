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
open NodaTime

(*  If you change this file, make sure it is backwards compatible!

    You should probably write a test covering legacy types. *)

type NotificationType = // the indexes currently have no semantic meaning - they're here because F# requires them to make the "DU" an Enum
    | DeckAddedConcept = 0
    | DeckUpdatedConcept = 1
    | DeckDeletedConcept = 2

type StudyOrder = // the indexes currently have no semantic meaning - they're here because F# requires them to make the "DU" an Enum
    | Mixed = 0
    | NewCardsFirst = 1
    | NewCardsLast = 2

[<CLIMutable>]
type IdName = {
    Id: Guid
    Name: string
}

type DeckAddedConcept = {
    TheirDeck: IdName
    MyDeck: IdName Option
    New: ConceptRevisionIds
    NewCardCount: int
    Collected: UpsertIds Option
}

type DeckUpdatedConcept = {
    TheirDeck: IdName
    MyDeck: IdName Option
    New: ConceptRevisionIds
    NewCardCount: int
    Collected: UpsertIds Option
}

type DeckDeletedConcept = {
    TheirDeck: IdName
    MyDeck: IdName Option
    Deleted: ConceptRevisionIds
    DeletedCardCount: int
    Collected: UpsertIds Option
}

type Message =
    | DeckAddedConcept of DeckAddedConcept
    | DeckUpdatedConcept of DeckUpdatedConcept
    | DeckDeletedConcept of DeckDeletedConcept
with
    member this.TryDeckAddedConcept([<Out>] out: _ byref) =
        match this with
        | DeckAddedConcept x -> out <- x; true
        | _ -> false
    member this.TryDeckUpdatedConcept([<Out>] out: _ byref) =
        match this with
        | DeckUpdatedConcept x -> out <- x; true
        | _ -> false
    member this.TryDeckDeletedConcept([<Out>] out: _ byref) =
        match this with
        | DeckDeletedConcept x -> out <- x; true
        | _ -> false

type Notification = {
    Id: Guid
    SenderId: Guid
    SenderDisplayName: string
    Created: Instant
    Message: Message
}

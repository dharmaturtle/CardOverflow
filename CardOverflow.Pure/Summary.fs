module Domain.Summary

open FSharp.UMX
open System
open CardOverflow.Pure

type Deck =
    { Id: DeckId
      AuthorId: UserId
      Name: string
      Description: string
      Visibility: Visibility }
let defaultDeck userId deckId =
    { Id = deckId
      AuthorId = userId
      Name = "Default Deck"
      Description = ""
      Visibility = Private }

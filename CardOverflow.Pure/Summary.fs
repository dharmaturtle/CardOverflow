module Domain.Summary

open FSharp.UMX
open System
open CardOverflow.Pure

type Deck =
    { Id: DeckId
      AuthorId: UserId
      Name: string
      Visibility: Visibility
      SourceId: DeckId option }
let defaultDeck userId deckId =
    { Id = deckId
      AuthorId = userId
      Name = "Default Deck"
      Visibility = Private
      SourceId = None }

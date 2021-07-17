module StackTests

open Xunit
open CardOverflow.Pure
open Serilog
open System
open Domain
open Equinox.MemoryStore
open FSharp.UMX
open FsCheck.Xunit
open CardOverflow.Pure
open CardOverflow.Test
open EventAppender
open Hedgehog
open D
open FsToolkit.ErrorHandling
open AsyncOp
open Domain.Stack

[<StandardProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_0000)>]
let ``Changing tags roundtrips`` signedUp tagsChanged { TemplateCreated = templateCreated; ExampleCreated = exampleCreated; StackCreated = stackCreated; } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    do! c.TemplateAppender().Create templateCreated
    do! c.ExampleAppender().Create exampleCreated
    let stackAppender = c.StackAppender()
    do! stackAppender.Create stackCreated
    
    do! stackAppender.ChangeTags tagsChanged stackCreated.Id

    // event store roundtrips
    stackCreated.Id
    |> c.StackEvents
    |> Seq.last
    |> Assert.equal (Stack.Events.TagsChanged tagsChanged)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetStack stackCreated.Id
    stackCreated |> Fold.evolveCreated |> Fold.evolveTagsChanged tagsChanged |> Assert.equal actual
    }

let extra =
    function
    | Deck.Fold.Active x -> x.Extra |> deserializeFromJson<Projection.Kvs.DeckExtra>
    | _ -> failwith "you goofed"
let exampleRevisionIds deck =
    (extra deck).ExampleRevisionIds

[<StandardProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_0000)>]
let ``DecksChanged works`` signedUp meta1 meta2 { TemplateCreated = templateCreated; ExampleCreated = exampleCreated; StackCreated = stackCreated } { DeckCreated = deckCreated } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    do! c.TemplateAppender().Create templateCreated
    do! c.ExampleAppender().Create exampleCreated
    let stackAppender = c.StackAppender()
    do! stackAppender.Create stackCreated
    let kvs = c.KeyValueStore()
    let! defaultDeckId =
        signedUp.Meta.UserId
        |> kvs.GetProfile
        |>% fun x -> x.Decks
        |>% Set.exactlyOne
        |>% fun x -> x.Id
    let deckChanged: Stack.Events.DecksChanged = { Meta    = meta1
                                                   DeckIds = defaultDeckId |> Set.singleton }
    
    
    
    (***   Change to Default Deck   ***)
    do! stackAppender.ChangeDecks deckChanged stackCreated.Id

    // event store roundtrips
    stackCreated.Id
    |> c.StackEvents
    |> Seq.last
    |> Assert.equal (Stack.Events.DecksChanged deckChanged)

    // azure table roundtrips
    let! actual = c.KeyValueStore().GetStack stackCreated.Id
    stackCreated |> Fold.evolveCreated |> Fold.evolveDecksChanged deckChanged |> Assert.equal actual

    // DefaultDeck has new Example
    let! deck = kvs.GetDeck defaultDeckId
    deck |> exampleRevisionIds |> Set.exactlyOne |> Assert.equal (exampleCreated.Id, Example.Fold.initialExampleRevisionOrdinal)
    
    // Profile's DefaultDeck's ExampleCount incremented
    let! profile = kvs.GetProfile signedUp.Meta.UserId
    profile.Decks |> Set.exactlyOne |> fun x -> x.ExampleCount |> Assert.equal 1

    
    
    (***   Change to New Deck   ***)
    let newDeckId = deckCreated.Id
    do! c.DeckAppender().Create deckCreated
    let deckChanged: Stack.Events.DecksChanged = { Meta    = meta2
                                                   DeckIds = newDeckId |> Set.singleton }
    do! stackAppender.ChangeDecks deckChanged stackCreated.Id
    
    // DefaultDeck is empty
    let! deck = kvs.GetDeck defaultDeckId
    deck |> exampleRevisionIds |> Assert.equal Set.empty
    
    // Profile's DefaultDeck has no examples
    let! profile = kvs.GetProfile signedUp.Meta.UserId
    profile.Decks |> Set.filter (fun x -> x.Id = defaultDeckId) |> Set.exactlyOne |> fun x -> x.ExampleCount |> Assert.equal 0

    // NewDeck has Example
    let! deck = kvs.GetDeck newDeckId
    deck |> exampleRevisionIds |> Set.exactlyOne |> Assert.equal (exampleCreated.Id, Example.Fold.initialExampleRevisionOrdinal)
    
    // NewDeck has no examples
    let! profile = kvs.GetProfile signedUp.Meta.UserId
    profile.Decks |> Set.filter (fun x -> x.Id = newDeckId) |> Set.exactlyOne |> fun x -> x.ExampleCount |> Assert.equal 1
    }

[<StandardProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_0000)>]
let ``StackCreated works with deck`` signedUp { TemplateCreated = templateCreated; ExampleCreated = exampleCreated; StackCreated = stackCreated } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    do! c.TemplateAppender().Create templateCreated
    do! c.ExampleAppender().Create exampleCreated
    let kvs = c.KeyValueStore()
    let! defaultDeckId =
        signedUp.Meta.UserId
        |> kvs.GetProfile
        |>% fun x -> x.Decks
        |>% Set.exactlyOne
        |>% fun x -> x.Id
    
    do! c.StackAppender().Create { stackCreated with DeckIds = Set.singleton defaultDeckId }

    // DefaultDeck has new Example
    let! deck = kvs.GetDeck defaultDeckId
    deck |> exampleRevisionIds |> Set.exactlyOne |> Assert.equal (exampleCreated.Id, Example.Fold.initialExampleRevisionOrdinal)
    
    // Profile's DefaultDeck's ExampleCount incremented
    let! profile = kvs.GetProfile signedUp.Meta.UserId
    profile.Decks |> Set.exactlyOne |> fun x -> x.ExampleCount |> Assert.equal 1
    }

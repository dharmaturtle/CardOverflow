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
let ``Changing tags roundtrips`` signedUp tagAdded meta { TemplateCreated = templateCreated; ExampleCreated = exampleCreated; StackCreated = stackCreated; } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    do! c.PublicTemplateAppender().Create templateCreated
    do! c.ExampleAppender().Create exampleCreated
    let stackAppender = c.StackAppender()
    do! stackAppender.Create stackCreated
    
    (***   Adding a tag to a Stack...   ***)
    do! stackAppender.AddTag tagAdded stackCreated.Id

    // ...updates KVS
    let! actual = c.KeyValueStore().GetStack_ stackCreated.Id
    let expected = stackCreated |> Fold.evolveCreated |> Fold.evolveTagAdded tagAdded
    Assert.equal expected actual
    
    (***   Removing that tag from the Stack...   ***)
    let tagRemoved : Stack.Events.TagRemoved = { Meta = meta; Tag = tagAdded.Tag }
    do! stackAppender.RemoveTag tagRemoved stackCreated.Id

    // ...updates KVS
    let! actual = c.KeyValueStore().GetStack_ stackCreated.Id
    expected |> Fold.evolveTagRemoved tagRemoved |> Assert.equal actual
    }

let extra =
    function
    | PrivateDeck.Fold.Active x -> x.Extra |> deserializeFromJson<Projection.Kvs.DeckExtra>
    | _ -> failwith "you goofed"
let exampleRevisionIds deck =
    (extra deck).ExampleRevisionIds

[<StandardProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_0000)>]
let ``DecksChanged works`` signedUp meta1 meta2 { TemplateCreated = templateCreated; ExampleCreated = exampleCreated; StackCreated = stackCreated } { DeckCreated = deckCreated } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    do! c.PublicTemplateAppender().Create templateCreated
    do! c.ExampleAppender().Create exampleCreated
    let stackAppender = c.StackAppender()
    do! stackAppender.Create stackCreated
    let kvs = c.KeyValueStore()
    let! defaultDeckId =
        signedUp.Meta.UserId
        |> kvs.GetProfile_
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
    let! actual = c.KeyValueStore().GetStack_ stackCreated.Id
    stackCreated |> Fold.evolveCreated |> Fold.evolveDecksChanged deckChanged |> Assert.equal actual

    // DefaultDeck has new Example
    let! deck = kvs.GetDeck_ defaultDeckId
    deck |> exampleRevisionIds |> Set.exactlyOne |> Assert.equal (exampleCreated.Id, Example.Fold.initialExampleRevisionOrdinal)
    
    // Profile's DefaultDeck's ExampleCount incremented
    let! profile = kvs.GetProfile_ signedUp.Meta.UserId
    profile.Decks |> Set.exactlyOne |> fun x -> x.ExampleCount |> Assert.equal 1

    
    
    (***   Change to New Deck   ***)
    let newDeckId = deckCreated.Id
    do! c.DeckAppender().Create deckCreated
    let deckChanged: Stack.Events.DecksChanged = { Meta    = meta2
                                                   DeckIds = newDeckId |> Set.singleton }
    do! stackAppender.ChangeDecks deckChanged stackCreated.Id
    
    // DefaultDeck is empty
    let! deck = kvs.GetDeck_ defaultDeckId
    deck |> exampleRevisionIds |> Assert.equal Set.empty
    
    // Profile's DefaultDeck has no examples
    let! profile = kvs.GetProfile_ signedUp.Meta.UserId
    profile.Decks |> Set.filter (fun x -> x.Id = defaultDeckId) |> Set.exactlyOne |> fun x -> x.ExampleCount |> Assert.equal 0

    // NewDeck has Example
    let! deck = kvs.GetDeck_ newDeckId
    deck |> exampleRevisionIds |> Set.exactlyOne |> Assert.equal (exampleCreated.Id, Example.Fold.initialExampleRevisionOrdinal)
    
    // NewDeck has no examples
    let! profile = kvs.GetProfile_ signedUp.Meta.UserId
    profile.Decks |> Set.filter (fun x -> x.Id = newDeckId) |> Set.exactlyOne |> fun x -> x.ExampleCount |> Assert.equal 1
    }

[<StandardProperty>]
[<NCrunch.Framework.TimeoutAttribute(600_0000)>]
let ``Stack Created/Discard works with deck`` signedUp (discarded: Stack.Events.Discarded) { TemplateCreated = templateCreated; ExampleCreated = exampleCreated; StackCreated = stackCreated } = asyncResult {
    let c = TestEsContainer()
    do! c.UserSagaAppender().Create signedUp
    do! c.PublicTemplateAppender().Create templateCreated
    do! c.ExampleAppender().Create exampleCreated
    let stackAppender = c.StackAppender()
    let kvs = c.KeyValueStore()
    let! defaultDeckId =
        signedUp.Meta.UserId
        |> kvs.GetProfile_
        |>% fun x -> x.Decks
        |>% Set.exactlyOne
        |>% fun x -> x.Id
    let stackCreated = { stackCreated with DeckIds = Set.singleton defaultDeckId }
    
    (***   Creating a stack with defaultDeckId...   ***)
    do! stackAppender.Create stackCreated

    // ...adds its example to the default deck
    let! deck = kvs.GetDeck_ defaultDeckId
    deck |> exampleRevisionIds |> Set.exactlyOne |> Assert.equal (exampleCreated.Id, Example.Fold.initialExampleRevisionOrdinal)
    
    // ...increments Profile's DefaultDeck's ExampleCount
    let! profile = kvs.GetProfile_ signedUp.Meta.UserId
    profile.Decks |> Set.exactlyOne |> fun x -> x.ExampleCount |> Assert.equal 1

    // ...adds it to the KVS
    let! actual = kvs.GetStack_ stackCreated.Id
    stackCreated |> Stack.Fold.evolveCreated |> Assert.equal actual

    // ...increments Concept's Collectors
    let expected =
        let templates = templateCreated |> PublicTemplate.Fold.evolveCreated |> Projection.toTemplateInstance PublicTemplate.Fold.initialTemplateRevisionOrdinal |> List.singleton
        let expected = exampleCreated |> Example.Fold.evolveCreated |> Projection.Kvs.toKvsExample signedUp.DisplayName Map.empty templates |> Projection.Concept.FromExample []
        { expected with
            Collectors = 1
            CommandIds = expected.CommandIds |> Set.add stackCreated.Meta.CommandId }
    
    let! actual = kvs.GetConcept_ exampleCreated.Id
    Assert.equal expected actual

    (***   Discarding a stack with defaultDeckId...   ***)
    do! stackAppender.Discard discarded stackCreated.Id

    // ...removes its example from the default deck
    let! deck = kvs.GetDeck_ defaultDeckId
    deck |> exampleRevisionIds |> Assert.equal Set.empty
    
    // ...decrements Profile's DefaultDeck's ExampleCount
    let! profile = kvs.GetProfile_ signedUp.Meta.UserId
    profile.Decks |> Set.exactlyOne |> fun x -> x.ExampleCount |> Assert.equal 0

    // ...removes it from the KVS
    let! actual = kvs.TryGet stackCreated.Id
    Assert.equal None actual

    // ...decrements Concept's Collectors
    let expected =
        { expected with
            Collectors = 0
            CommandIds = expected.CommandIds |> Set.add discarded.Meta.CommandId }
    
    let! actual = kvs.GetConcept_ exampleCreated.Id
    Assert.equal expected actual
    }

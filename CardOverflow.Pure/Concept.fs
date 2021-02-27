module Domain.Concept

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open FsToolkit.ErrorHandling

let streamName (id: ConceptId) = StreamName.create "Concept" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =
    
    type Summary =
        { Id: ConceptId
          DefaultExampleId: ExampleId
          AuthorId: UserId
          CopySourceLeafId: LeafId Option }
    type DefaultExampleChanged = { ExampleId: ExampleId }

    type Event =
        | DefaultExampleChanged of DefaultExampleChanged
        | Created              of Summary
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =
    
    type State =
        | Initial
        | Active of Events.Summary
    let initial = State.Initial

    let evolve state =
        function
        | Events.Created s -> State.Active s
        | Events.DefaultExampleChanged b ->
            match state with
            | State.Initial  -> invalidOp "Can't change the default example of an Initial Concept"
            | State.Active a -> { a with DefaultExampleId = b.ExampleId } |> State.Active
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let isOrigin = function Events.Created _ -> true | _ -> false

let decideCreate summary state =
    match state with
    | Fold.State.Active s -> Error $"Concept '{s.Id}' already exists."
    | Fold.State.Initial  -> Ok ()
    |> addEvent (Events.Created summary)

let decideDefaultExampleChanged (exampleId: ExampleId) (examplesConceptId: ConceptId) callerId state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit a example that doesn't exist"
    | Fold.State.Active s -> result {
        do! Result.requireEqual s.AuthorId callerId $"Concept {s.Id} doesn't belong to User {callerId}"
        do! Result.requireEqual s.Id examplesConceptId $"Example {exampleId} doesn't belong to Concept {s.Id}"
    } |> addEvent (Events.DefaultExampleChanged { ExampleId = exampleId })

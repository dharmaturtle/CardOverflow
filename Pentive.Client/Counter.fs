module Pentive.Client.Counter

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client

type Model =
    {
        Counter: int
    }

let initModel =
    {
        Counter = 0
    }

type Msg =
    | Incremented
    | Decremented
    | CounterUpdated of int

let update message model =
    match message with
    | Incremented          -> { model with Counter = model.Counter + 1 }
    | Decremented          -> { model with Counter = model.Counter - 1 }
    | CounterUpdated value -> { model with Counter = value }

type CounterTemplate = Template<"wwwroot/counter.html">

let view model dispatch =
    CounterTemplate()
        .Decrement(fun _ -> dispatch Decremented)
        .Increment(fun _ -> dispatch Incremented)
        .Value(model.Counter, dispatch << CounterUpdated)
        .Elt()

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

type Message =
    | Increment
    | Decrement
    | SetCounter of int

let update message model =
    match message with
    | Increment        -> { model with Counter = model.Counter + 1 }
    | Decrement        -> { model with Counter = model.Counter - 1 }
    | SetCounter value -> { model with Counter = value }

type CounterTemplate = Template<"wwwroot/counter.html">

let view model dispatch =
    CounterTemplate()
        .Decrement(fun _ -> dispatch Decrement)
        .Increment(fun _ -> dispatch Increment)
        .Value(model.Counter, dispatch << SetCounter)
        .Elt()

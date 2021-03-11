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
        counter: int
    }

let initModel =
    {
        counter = 0
    }

type Message =
    | Increment
    | Decrement
    | SetCounter of int

let update message model =
    match message with
    | Increment ->
        { model with counter = model.counter + 1 }, Cmd.none
    | Decrement ->
        { model with counter = model.counter - 1 }, Cmd.none
    | SetCounter value ->
        { model with counter = value }, Cmd.none

type Counter = Template<"wwwroot/counter.html">

let view model dispatch =
    Counter()
        .Decrement(fun _ -> dispatch Decrement)
        .Increment(fun _ -> dispatch Increment)
        .Value(model.counter, dispatch << SetCounter)
        .Elt()

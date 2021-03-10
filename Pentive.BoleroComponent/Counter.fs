namespace Pentive.BoleroComponent

open Bolero
open Bolero.Html
open Elmish

module Counter =

    type Model = { value: int }
    let initModel = { value = 0 }

    type Message = Increment | Decrement
    let update message model =
        match message with
        | Increment -> { model with value = model.value + 1 }
        | Decrement -> { model with value = model.value - 1 }

    let view model dispatch =
        div [] [
            button [on.click (fun _ -> dispatch Decrement)] [text "-"]
            text (string model.value)
            button [on.click (fun _ -> dispatch Increment)] [text "+"]
        ]

    let program =
        Program.mkSimple (fun _ -> initModel) update view

open Counter

type Counter() =
    inherit ProgramComponent<Model, Message>()

    override _.Program = program

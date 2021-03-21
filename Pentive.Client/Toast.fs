module Pentive.Client.Toast

open Microsoft.AspNetCore.Components.Web
open System
open Elmish
open Bolero
open Bolero.Html

let ofOption = function
    | Some x -> x
    | None -> empty

type Status =
    | Success
    | Warning
    | Error
    | Info

type Position =
    | BottomRight
    | BottomLeft
    | BottomCenter
    | TopRight
    | TopLeft
    | TopCenter

type Builder<'icon, 'msg> =
    { Inputs : (string * 'msg) list
      Message : string
      Title : string option
      Icon : 'icon option
      Position : Position
      Delay : TimeSpan option
      DismissOnClick : bool
      WithProgressBar : bool
      WithCloseButton : bool }

    static member Empty () =
        { Inputs = []
          Message = ""
          Title = None
          Icon = None
          Delay = Some (TimeSpan.FromSeconds 3.)
          Position = BottomLeft
          DismissOnClick = false
          WithProgressBar = false
          WithCloseButton = false }

type Toast<'icon, 'msg> =
    { Guid : Guid
      Inputs : (string * 'msg) list
      Message : string
      Title : string option
      Icon : 'icon option
      Position : Position
      Delay : TimeSpan option
      Status : Status
      DismissOnClick : bool
      WithProgressBar : bool
      WithCloseButton : bool }

let mapInputMsg f toast =
    { Guid            = toast.Guid
      Inputs          = toast.Inputs |> List.map (fun (txt, msg) -> txt, f msg)
      Message         = toast.Message
      Title           = toast.Title
      Icon            = toast.Icon
      Position        = toast.Position
      Delay           = toast.Delay
      Status          = toast.Status
      DismissOnClick  = toast.DismissOnClick
      WithProgressBar = toast.WithProgressBar
      WithCloseButton = toast.WithCloseButton }

/// Create a toast and set the message content
let message msg =
    { Builder<_, _>.Empty() with Message = msg }

/// Set the title content
let title title (builder : Builder<_, _>) =
    { builder with Title = Some title }

/// Set the position
let position pos (builder : Builder<_, _>) =
    { builder with Position = pos }

/// Add an input to the toast
let addInput txt msg (builder : Builder<_, _>) =
    { builder with Inputs = (txt, msg) :: builder.Inputs }

/// Set the icon
let icon icon (builder : Builder<_, _>) =
    { builder with Icon = Some icon }

/// Set the timeout in seconds
let timeout delay (builder : Builder<_, _>) =
    { builder with Delay = Some delay }

/// No timeout, make sure to add close button or dismiss on click
let noTimeout (builder : Builder<_, _>) =
    { builder with Delay = None }

/// Allow user to dismiss the toast by cliking on it
let dismissOnClick (builder : Builder<_, _>) =
    { builder with DismissOnClick = true }

/// Add an animated progress bar
// let withProgessBar (builder : Builder<_, _>) =
//     { builder with WithProgressBar = true }

/// Add a close button
let withCloseButton (builder : Builder<_, _>) =
    { builder with WithCloseButton = true }

let build status (builder : Builder<_, _>) =
    { Guid = Guid.NewGuid()
      Inputs = builder.Inputs
      Message = builder.Message
      Title = builder.Title
      Icon = builder.Icon
      Position = builder.Position
      Delay = builder.Delay
      Status = status
      DismissOnClick = builder.DismissOnClick
      WithProgressBar = builder.WithProgressBar
      WithCloseButton = builder.WithCloseButton }

///// Send the toast marked with Success status
//let success (builder : Builder<_, _>) : Cmd<'msg> =
//    [ fun dispatch ->
//        triggerEvent builder Success dispatch ]

///// Send the toast marked with Warning status
//let warning (builder : Builder<_, _>) : Cmd<'msg> =
//    [ fun dispatch ->
//        triggerEvent builder Warning dispatch ]

///// Send the toast marked with Error status
//let error (builder : Builder<_, _>) : Cmd<'msg> =
//    [ fun dispatch ->
//        triggerEvent builder Error dispatch ]

///// Send the toast marked with Info status
//let info (builder : Builder<_, _>) : Cmd<'msg> =
//    [ fun dispatch ->
//        triggerEvent builder Info dispatch ]

/// Interface used to customize the view
type IRenderer<'icon, 'msg> =

    /// **Description**
    /// Render the outer element of the toast
    /// **Parameters**
    /// * `content` - parameter of type `ReactElement list`
    ///     > This is the content of the toast.
    ///     > Ex:
    ///     >   - CloseButton
    ///     >   - Title
    ///     >   - Message
    /// * `color` - parameter of type `string`
    ///     > Class used to set toast color
    /// **Output Type**
    ///   * `ReactElement`
    abstract Toast : Node list -> string -> Node

    /// **Description**
    /// Render the close button of the toast
    /// **Parameters**
    /// * `onClick` - parameter of type `MouseEvent -> unit`
    ///     > OnClick event listener to attached
    /// **Output Type**
    ///   * `Node`
    abstract CloseButton : (MouseEventArgs -> unit) -> Node

    /// **Description**
    /// Render the outer element of the Input Area
    /// **Parameters**
    /// * `content` - parameter of type `Node list`
    ///     > This is the content of the input area.
    /// **Output Type**
    ///   * `Node`
    abstract InputArea : Node list -> Node

    /// **Description**
    /// Render one element of the Input Area
    /// **Parameters**
    /// * `text` - parameter of type `string`
    ///     > Text to display
    /// * `callback` - parameter of type `unit -> unit`
    ///     > Callback to execute when user click on the input
    /// **Output Type**
    ///   * `Node`
    abstract Input : string -> 'msg -> Node

    /// **Description**
    /// Render the title of the Toast
    /// **Parameters**
    /// * `text` - parameter of type `string`
    ///     > Text to display
    /// **Output Type**
    ///   * `Node`
    abstract Title : string -> Node

    /// **Description**
    /// Render the message of the Toast
    /// **Parameters**
    /// * `text` - parameter of type `string`
    ///     > Text to display
    /// **Output Type**
    ///   * `Node`
    abstract Message : string -> Node

    /// **Description**
    /// Render the icon part
    /// **Parameters**
    /// * `icon` - parameter of type `'icon`
    ///     > 'icon is generic so you can pass the Value as a String or Typed value like `Fa.I.FontAwesomeIcons` when using Fulma
    /// **Output Type**
    ///   * `Node`
    abstract Icon : 'icon -> Node

    /// **Description**
    /// Render the simple layout (when no icon has been provided to the Toast)
    /// **Parameters**
    /// * `title` - parameter of type `Node`
    /// * `message` - parameter of type `Node`
    /// **Output Type**
    ///   * `Node`
    abstract SingleLayout : Node -> Node -> Node -> Node


    /// **Description**
    /// Render the splitted layout (when toast has an Icon and Message)
    /// **Parameters**
    /// * `icon` - parameter of type `Node`
    ///     > Icon view
    /// * `title` - parameter of type `Node`
    /// * `message` - parameter of type `Node`
    /// **Output Type**
    ///   * `Node`
    abstract SplittedLayout : Node -> Node -> Node -> Node -> Node

    /// **Description**
    /// Obtain the class associated with the Status
    /// **Parameters**
    /// * `status` - parameter of type `Status`
    /// **Output Type**
    ///   * `string`
    abstract StatusToColor : Status -> string

type Msg<'icon, 'msg> =
    | Add of Toast<'icon, 'msg>
    | Remove of Toast<'icon, 'msg>
    | OnError of exn

type Model<'icon, 'msg> =
    { Toasts_BL : Toast<'icon, 'msg> list
      Toasts_BC : Toast<'icon, 'msg> list
      Toasts_BR : Toast<'icon, 'msg> list
      Toasts_TL : Toast<'icon, 'msg> list
      Toasts_TC : Toast<'icon, 'msg> list
      Toasts_TR : Toast<'icon, 'msg> list }

let inline private removeToast guid =
    List.filter (fun item -> item.Guid <> guid )

let private viewToastWrapper (classPosition : string) (render : IRenderer<_, _>) (toasts : Toast<_, _> list) dispatch =
    div [ attr.``class`` ("toast-wrapper " + classPosition) ]
        ( toasts
                |> List.map (fun n ->
                    let title =
                        Option.map
                            render.Title
                            n.Title

                    let withInputArea, inputArea =
                        if n.Inputs.Length = 0 then
                            "", None
                        else
                            let inputs =
                                render.InputArea
                                    (n.Inputs
                                        |> List.map (fun (txt, msg) ->
                                            render.Input txt msg
                                        ))

                            "with-inputs", Some inputs

                    let dismissOnClick =
                        if n.DismissOnClick then
                            "dismiss-on-click"
                        else
                            ""

                    let containerClass =
                        String.concat " " [ "toast-container"
                                            dismissOnClick
                                            withInputArea
                                            render.StatusToColor n.Status ]
                    let closeButton =
                        match n.WithCloseButton with
                        | true ->
                            render.CloseButton (fun _ -> dispatch (Remove n))
                            |> Some
                        | false -> None
                        |> ofOption

                    let layout =
                        match n.Icon with
                        | Some icon ->
                            render.SplittedLayout
                                (render.Icon icon)
                                (ofOption title)
                                (render.Message n.Message)
                                (ofOption inputArea)
                        | None ->
                            render.SingleLayout
                                (ofOption title)
                                (render.Message n.Message)
                                (ofOption inputArea)

                    let attrs =
                        if n.DismissOnClick then
                            [ on.click (fun _ -> dispatch (Remove n)) ]
                        else []
                    div (attr.``class`` containerClass :: attrs)
                        [ render.Toast
                            [ closeButton
                              layout
                            ]
                            (render.StatusToColor n.Status)
                        ]
                ) )

let view (render : IRenderer<_, _>) (model : Model<_, _>) dispatch =
    div [ attr.``class`` "elmish-toast" ]
        [ viewToastWrapper "toast-wrapper-bottom-left"   render model.Toasts_BL dispatch
          viewToastWrapper "toast-wrapper-bottom-center" render model.Toasts_BC dispatch
          viewToastWrapper "toast-wrapper-bottom-right"  render model.Toasts_BR dispatch
          viewToastWrapper "toast-wrapper-top-left"      render model.Toasts_TL dispatch
          viewToastWrapper "toast-wrapper-top-center"    render model.Toasts_TC dispatch
          viewToastWrapper "toast-wrapper-top-right"     render model.Toasts_TR dispatch ]


let private delayedCmd ((delay: TimeSpan), (notification : Toast<'icon, _>)) =
    async {
        do! Async.Sleep (int delay.TotalMilliseconds)
        return notification
    }

let update msg model =
    match msg with
    | Add newToast ->
        match newToast.Position with
        | BottomLeft   -> { model with Toasts_BL = newToast::model.Toasts_BL }
        | BottomCenter -> { model with Toasts_BC = newToast::model.Toasts_BC }
        | BottomRight  -> { model with Toasts_BR = newToast::model.Toasts_BR }
        | TopLeft      -> { model with Toasts_TL = newToast::model.Toasts_TL }
        | TopCenter    -> { model with Toasts_TC = newToast::model.Toasts_TC }
        | TopRight     -> { model with Toasts_TR = newToast::model.Toasts_TR }
    | Remove toast ->
        match toast.Position with
        | BottomLeft   -> { model with Toasts_BL = removeToast toast.Guid model.Toasts_BL }
        | BottomCenter -> { model with Toasts_BC = removeToast toast.Guid model.Toasts_BC }
        | BottomRight  -> { model with Toasts_BR = removeToast toast.Guid model.Toasts_BR }
        | TopLeft      -> { model with Toasts_TL = removeToast toast.Guid model.Toasts_TL }
        | TopCenter    -> { model with Toasts_TC = removeToast toast.Guid model.Toasts_TC }
        | TopRight     -> { model with Toasts_TR = removeToast toast.Guid model.Toasts_TR }
    | OnError _ -> model

let generate msg =
    match msg with
    | Add newToast ->
        match newToast.Delay with
        | Some delay -> Cmd.OfAsync.either delayedCmd (delay, newToast) Remove OnError
        | None -> Cmd.none
    | Remove _ -> Cmd.none
    | OnError error ->
        printfn "%s" error.Message
        Cmd.none

let initModel =
    { Toasts_BL = []
      Toasts_BC = []
      Toasts_BR = []
      Toasts_TL = []
      Toasts_TC = []
      Toasts_TR = [] }

/// **Description**
/// Default implementation for the Toast renderer,
/// you are encourage to write your own implementation
/// to match your application style
/// **Output Type**
///   * `IRenderer<string>`
let render dispatch =
    { new IRenderer<string, 'msg> with
        member _.Toast children _ =
            div [ attr.``class`` "toast" ]
                children
        member _.CloseButton onClick =
            span [ attr.``class`` "close-button"
                   on.click onClick ]
                [ ]
        member _.InputArea children =
            concat children
        member _.Input (txt : string) (msg : 'msg) =
            button
                [on.click (fun _ -> dispatch msg)]
                [text txt]
        member _.Title txt =
            span [ attr.``class`` "toast-title" ]
                [ text txt ]
        member _.Icon (icon : string) =
            div [ attr.``class`` "toast-layout-icon" ]
                [ i [ attr.``class`` ("fa fa-2x " + icon) ]
                    [  ] ]
        member _.SingleLayout title message inputArea =
            div [ attr.``class`` "toast-layout-content" ]
                [ title; message; inputArea ]
        member _.Message txt =
            span [ attr.``class`` "toast-message" ]
                [ text txt ]
        member _.SplittedLayout iconView title message inputArea =
            div [ attr.style "display: flex; width: 100%" ]
                [ iconView
                  div [ attr.``class`` "toast-layout-content" ]
                    [ title
                      message
                      inputArea ] ]
        member _.StatusToColor status =
            match status with
            | Status.Success -> "is-success"
            | Status.Warning -> "is-warning"
            | Status.Error -> "is-error"
            | Status.Info -> "is-info" }

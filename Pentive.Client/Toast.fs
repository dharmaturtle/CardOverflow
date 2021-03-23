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

/// Set the title content
let title title toast =
    { toast with Title = Some title }

/// Set the position
let position pos toast =
    { toast with Position = pos }

/// Add an input to the toast
let addInput txt msg toast =
    { toast with Inputs = (txt, msg) :: toast.Inputs }

/// Set the icon
let icon icon toast =
    { toast with Icon = Some icon }

/// Set the timeout in seconds
let timeout delay toast =
    { toast with Delay = Some delay }

/// No timeout, make sure to add close button or dismiss on click
let noTimeout toast =
    { toast with Delay = None }

/// Allow user to dismiss the toast by cliking on it
let dismissOnClick toast =
    { toast with DismissOnClick = true }

/// Add an animated progress bar
// let withProgessBar toast =
//     { toast with WithProgressBar = true }

/// Add a close button
let withCloseButton toast =
    { toast with WithCloseButton = true }

let build status toast =
    { toast with Status = status }

/// Send the toast marked with Success status
let success message =
    { Guid = Guid.NewGuid()
      Inputs = []
      Message = message
      Title = None
      Icon = Some "fas fa-check"
      Position = BottomLeft
      Delay = TimeSpan.FromSeconds 5. |> Some
      Status = Success
      DismissOnClick = true
      WithProgressBar = true
      WithCloseButton = false }

/// Send the toast marked with Warning status
let warning message =
    { Guid = Guid.NewGuid()
      Inputs = []
      Message = message
      Title = None
      Icon = Some "fas fa-exclamation"
      Position = BottomLeft
      Delay = TimeSpan.FromSeconds 10. |> Some
      Status = Warning
      DismissOnClick = true
      WithProgressBar = true
      WithCloseButton = false }

/// Send the toast marked with Error status
let error message =
    { Guid = Guid.NewGuid()
      Inputs = []
      Message = message
      Title = Some "Error"
      Icon = Some "fas fa-times-circle"
      Position = BottomLeft
      Delay = None
      Status = Error
      DismissOnClick = true
      WithProgressBar = false
      WithCloseButton = false }

/// Send the toast marked with Info status
let info message =
    { Guid = Guid.NewGuid()
      Inputs = []
      Message = message
      Title = None
      Icon = Some "fas fa-info"
      Position = BottomLeft
      Delay = TimeSpan.FromSeconds 5. |> Some
      Status = Info
      DismissOnClick = true
      WithProgressBar = true
      WithCloseButton = false }

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


let private delayedCmd ((delay: TimeSpan), (toast : Toast<'icon, _>)) =
    async {
        do! Async.Sleep (int delay.TotalMilliseconds)
        return toast
    }

let update msg model =
    match msg with
    | Add newToast ->
        match newToast.Position with
        | BottomLeft   -> { model with Toasts_BL = newToast :: model.Toasts_BL }
        | BottomCenter -> { model with Toasts_BC = newToast :: model.Toasts_BC }
        | BottomRight  -> { model with Toasts_BR = newToast :: model.Toasts_BR }
        | TopLeft      -> { model with Toasts_TL = newToast :: model.Toasts_TL }
        | TopCenter    -> { model with Toasts_TC = newToast :: model.Toasts_TC }
        | TopRight     -> { model with Toasts_TR = newToast :: model.Toasts_TR }
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
            | Status.Error   -> "is-error"
            | Status.Info    -> "is-info" }

module Pentive.Client.Login

open System
open Pentive.Client
open Elmish
open Bolero
open Bolero.Html

type Model =
    {
        username: string
        password: string
        signedInAs: option<string>
        signInFailed: bool
    }

let initModel =
    {
        username = ""
        password = ""
        signedInAs = None
        signInFailed = false
    }

type Message =
    | SetUsername of string
    | SetPassword of string
    | GetSignedInAs
    | RecvSignedInAs of option<string>
    | SendSignIn
    | RecvSignIn of option<string>
    | SendSignOut
    | RecvSignOut

type CmdMsg =
    | CM_SetPage of Page
    | CM_RecvSignIn of Model
    | CM_RecvSignedInAs
    | CM_RecvSignOut

let logout model =
    { model with signedInAs = None }

let update message model =
    match message with
    | SetUsername s ->
        { model with username = s }, []
    | SetPassword s ->
        { model with password = s }, []
    | GetSignedInAs ->
        model, [CM_RecvSignedInAs]
    | RecvSignedInAs username ->
        { model with signedInAs = username }, [CM_SetPage Page.Data]
    | SendSignIn ->
        model, [CM_RecvSignIn model]
    | RecvSignIn username ->
        { model with signedInAs = username; signInFailed = Option.isNone username }, [CM_SetPage Page.Data]
    | SendSignOut ->
        model, [CM_RecvSignOut]
    | RecvSignOut ->
        { model with signedInAs = None; signInFailed = false }, []

type Login = Template<"wwwroot/login.html">
type Main  = Template<"wwwroot/main.html">

let view model dispatch =
    Login()
        .Username(model.username, fun s -> dispatch (SetUsername s))
        .Password(model.password, fun s -> dispatch (SetPassword s))
        .SignIn(fun _ -> dispatch SendSignIn)
        .ErrorNotification(
            cond model.signInFailed <| function
            | false -> empty
            | true ->
                Main.ErrorNotification()
                    .HideClass("is-hidden")
                    .Text("Sign in failed. Use any username and the password \"password\".")
                    .Elt()
        )
        .Elt()

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
        loginFailed: bool
    }

let initModel =
    {
        username = ""
        password = ""
        loginFailed = false
    }

type Message =
    | SetUsername of string
    | SetPassword of string
    | SendSignIn
    | LoginFailed

let update message model =
    match message with
    | SetUsername s ->
        { model with username = s }, []
    | SetPassword s ->
        { model with password = s }, []
    | SendSignIn ->
        { model with password = ""; loginFailed = false }, [Auth.CmdMsg.CM_AttemptLogin (model.username, model.password)]
    | LoginFailed ->
        { model with loginFailed = true }, []

type LoginTemplate = Template<"wwwroot/login.html">

let view model dispatch =
    LoginTemplate()
        .Username(model.username, fun s -> dispatch (SetUsername s))
        .Password(model.password, fun s -> dispatch (SetPassword s))
        .SignIn(fun _ -> dispatch SendSignIn)
        .ErrorClass(if model.loginFailed then "" else "is-hidden")
        .ErrorText("Sign in failed. Use any username and the password \"password\".")
        .Elt()

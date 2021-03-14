module Pentive.Client.Login

open System
open Pentive.Client
open Elmish
open Bolero
open Bolero.Html

type Model =
    {
        Username: string
        Password: string
        LoginFailed: bool
    }

let initModel =
    {
        Username = ""
        Password = ""
        LoginFailed = false
    }

type Message =
    | SetUsername of string
    | SetPassword of string
    | SendSignIn
    | LoginFailed

let update message model =
    match message with
    | SetUsername x -> { model with Username = x }
    | SetPassword x -> { model with Password = x }
    | SendSignIn    -> { model with Password = ""; LoginFailed = false }
    | LoginFailed   -> { model with LoginFailed = true }

let generate message model =
    match message with
    | SetUsername _ -> []
    | SetPassword _ -> []
    | SendSignIn    -> [Auth.CM_AttemptLogin (model.Username, model.Password)]
    | LoginFailed   -> []

type LoginTemplate = Template<"wwwroot/login.html">

let view model dispatch =
    LoginTemplate()
        .Username(model.Username, fun s -> dispatch (SetUsername s))
        .Password(model.Password, fun s -> dispatch (SetPassword s))
        .SignIn(fun _ -> dispatch SendSignIn)
        .ErrorClass(if model.LoginFailed then "" else "is-hidden")
        .ErrorText("Sign in failed. Use any username and the password \"password\".")
        .Elt()

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
        Redirect: Page
    }

let initModelTo page =
    {
        Username = ""
        Password = ""
        LoginFailed = false
        Redirect = page
    }

type Msg =
    | UsernameUpdated of string
    | PasswordUpdated of string
    | LoginAttempted
    | LoginFailed

let update message model =
    match message with
    | UsernameUpdated x -> { model with Username = x }
    | PasswordUpdated x -> { model with Password = x }
    | LoginAttempted    -> { model with Password = ""; LoginFailed = false }
    | LoginFailed       -> { model with LoginFailed = true }

let generate message model =
    match message with
    | UsernameUpdated _ -> []
    | PasswordUpdated _ -> []
    | LoginAttempted    -> [Auth.AttemptLogin (model.Username, model.Password)]
    | LoginFailed       -> []

type LoginTemplate = Template<"wwwroot/login.html">

let view model dispatch =
    LoginTemplate()
        .Username(model.Username, fun s -> dispatch (UsernameUpdated s))
        .Password(model.Password, fun s -> dispatch (PasswordUpdated s))
        .SignIn(fun _ -> dispatch LoginAttempted)
        .ErrorClass(if model.LoginFailed then "" else "is-hidden")
        .ErrorText("Sign in failed. Use any username and the password \"password\".")
        .Elt()

module Pentive.Client.Auth

open System
open Pentive.Client
open Elmish
open Bolero
open Bolero.Html

type Model =
    {
        Username: string option
    }

let initModel =
    {
        Username = None
    }

type Redirect =
    | InitialLoad
    | Page of Page

type Message =
    | Logout
    | LoginAttempted of username: string option * Redirect

let initialLoginAttempted username =
    LoginAttempted (username, InitialLoad)

let loginAttemptedTo page username =
    LoginAttempted (username, Page page)

type CmdMsg =
    | CM_SetPage of Page
    | CM_Logout
    | CM_LoginFailed
    | CM_AttemptLogin of username: string * password: string
    | CM_Initialize

let logout model =
    { model with Username = None }

let update message model =
    match message with
    | Logout ->
        { model with Username = None }, [CM_SetPage Home; CM_Logout]
    | LoginAttempted (username, page) ->
        let cmd =
            match  page, username with
            | Page page  , Some _ -> [CM_SetPage page]
            | Page _     , None   -> [CM_LoginFailed]
            | InitialLoad, _      -> []
        { model with Username = username }, cmd
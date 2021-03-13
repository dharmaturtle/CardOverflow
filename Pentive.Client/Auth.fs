module Pentive.Client.Auth

open System
open Pentive.Client
open Elmish
open Bolero
open Bolero.Html

type Model =
    {
        username: string option
    }

let initModel =
    {
        username = None
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

let logout model =
    { model with username = None }

let update message model =
    match message with
    | Logout ->
        { model with username = None }, [CM_SetPage Home; CM_Logout]
    | LoginAttempted (username, page) ->
        let cmd =
            match  page, username with
            | Page page  , Some _ -> [CM_SetPage page]
            | Page _     , None   -> [CM_LoginFailed]
            | InitialLoad, _      -> []
        { model with username = username }, cmd
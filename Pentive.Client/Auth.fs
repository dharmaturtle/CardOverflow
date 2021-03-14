module Pentive.Client.Auth

open System
open Pentive.Client
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting

type Model =
    {
        Username: string option
    }

let initModel =
    {
        Username = None
    }

type AuthService =
    {
        /// Sign into the application.
        signIn : string * string -> Async<option<string>>

        /// Get the user's name
        getUsername : unit -> Async<string>

        /// Sign out from the application.
        signOut : unit -> Async<unit>
    }

    interface IRemoteService with
        member _.BasePath = "/auth"

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
    | Logout                       -> logout model
    | LoginAttempted (username, _) -> { model with Username = username }

let generate message =
    match message with
    | Logout                  -> [CM_Logout]
    | LoginAttempted (username, page) ->
        match  page, username with
        | Page page  , Some _ -> [CM_SetPage page]
        | Page _     , None   -> [CM_LoginFailed]
        | InitialLoad, _      -> []

module Pentive.Client.Profile

open System
open Pentive.Client
open Elmish
open Bolero
open Bolero.Html
open Auth

type ProfileTemplate = Template<"wwwroot/profile.html">

let view model dispatch =
    match model.username with
    | Some username ->
        ProfileTemplate()
            .Username(username)
            .SignOut(fun _ -> dispatch Logout)
            .Elt()
    | None ->
        text "You must login to see the Profile page."
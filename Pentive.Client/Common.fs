namespace Pentive.Client

open Bolero

type Page =
    | Home
    | Counter
    | Book
    | Login
    | Profile

module Page =
    let requireAuthenticated = function
        | Home
        | Counter
        | Login -> false
        
        | Book
        | Profile -> true

type RemoteData<'a> =
    | NotAsked
    | Loading
    | Failure of string
    | Success of 'a

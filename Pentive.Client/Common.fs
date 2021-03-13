namespace Pentive.Client

open Bolero

type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/counter">] Counter
    | [<EndPoint "/book">] Book
    | [<EndPoint "/login">] Login
    | [<EndPoint "/profile">] Profile

module Page =
    let requireAuthenticated = function
        | Home
        | Counter
        | Login -> false
        
        | Book
        | Profile -> true

type Loadable<'a> =
    | Initial
    | Loading
    | Error of string
    | Loaded of 'a

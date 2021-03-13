namespace Pentive.Client

open Bolero

type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/counter">] Counter
    | [<EndPoint "/data">] Data
    | [<EndPoint "/login">] Login
    | [<EndPoint "/profile">] Profile

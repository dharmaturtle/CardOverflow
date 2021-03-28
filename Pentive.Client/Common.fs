namespace Pentive.Client

type Redirect =
    | Home
    | Counter
    | Book
    | Login
    | Profile
    | CardSetting

type RemoteData<'a> =
    | NotAsked
    | Loading
    | Failure of string
    | Success of 'a

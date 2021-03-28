module Pentive.Client.Main

open System
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client

type Page =
    | Home
    | Counter
    | Book
    | Login of Login.Model
    | Profile
    with
        member this.is page =
            match this, page with
            | Home    , Home
            | Counter , Counter
            | Book    , Book
            | Login _ , Login _
            | Profile , Profile
                -> true
            | _ -> false
        
        member this.mapLogin       f          = match this with Login       m -> f m  | x -> x
        member this.mapCardSetting f          = match this with CardSetting m -> f m  | x -> x
        member this.ifLogin        f fallback = match this with Login       m -> f m  | _ -> fallback
        member this.ifCardSetting  f fallback = match this with CardSetting m -> f m  | _ -> fallback

module Page =
    let requireAuthenticated = function
        | Home
        | Counter
        | Login _ -> false
        
        | Book
        | Profile -> true

    let toRedirect = function
        | Home    -> Redirect.Home
        | Counter -> Redirect.Counter
        | Book    -> Redirect.Book
        | Login _ -> Redirect.Login
        | Profile -> Redirect.Profile
    
    let ofRedirect = function
        | Redirect.Home    -> Home
        | Redirect.Counter -> Counter
        | Redirect.Book    -> Book
        | Redirect.Login   -> Login Login.initModel
        | Redirect.Profile -> Profile

type Msg =
    | Navigated of Page
    | ErrorOccurred of exn
    | CounterMsg of Counter.Msg
    |   LoginMsg of Login  .Msg
    |    BookMsg of Book   .Msg
    |    AuthMsg of Auth   .Msg
    |   ToastMsg of Toast  .Msg<string, Msg>

type Model =
    {
        Page: Page
        Counter: Counter.Model
        Book   : Book   .Model
        Auth   : Auth   .Model
        Toast  : Toast  .Model<string, Msg>
    }

let initModel =
    {
        Page = Home
        Counter = Counter.initModel
        Book    = Book   .initModel
        Auth    = Auth   .initModel
        Toast   = Toast  .initModel
    }

type Cmd =
    | SetPage  of Page
    | AuthCmd  of Auth.Cmd
    | BookCmd  of Book.Cmd
    | ToastCmd of Elmish.Cmd<Toast.Msg<string, Msg>>

let isPermitted page (auth: Auth.Model) =
    if page |> Page.requireAuthenticated then
        match auth with
        | Auth.Authenticated _ -> true
        | _ -> false
    else true

let update message (model: Model) =
    match message with
    | Navigated page ->
        if isPermitted page model.Auth then
            { model with Page = page }
        else
            { model with
                Auth = model.Auth |> Auth.trySetRedirect (page |> Page.toRedirect)
                Page = Login.initModel |> Login }

    | CounterMsg msg -> { model with Counter = model.Counter      |> Counter.update msg }
    | BookMsg    msg -> { model with Book    = model.Book         |> Book   .update msg }
    | LoginMsg   msg -> { model with Page    = model.Page.mapLogin ( Login  .update msg >> Login) }
    | AuthMsg    msg -> { model with Auth    = model.Auth         |> Auth   .update msg }
    | ToastMsg   msg -> { model with Toast   = model.Toast        |> Toast  .update msg }

    | ErrorOccurred RemoteUnauthorizedException -> { model with Auth = Auth.logout }
    | ErrorOccurred _                           -> model

let toastErrorCmd =
    Toast.error >> Toast.Add >> Elmish.Cmd.ofMsg >> ToastCmd >> List.singleton

let generate message (model: Model) =
    match message with
    | Navigated page ->
        if isPermitted page model.Auth then
            match page with
            | Book -> [BookCmd Book.Initialize]
            | _ -> []
        else
            "You must login to view that page." |> toastErrorCmd

    | BookMsg  msg ->                     Book .generate msg     |> List.map BookCmd
    | LoginMsg msg -> model.Page.ifLogin (Login.generate msg) [] |> List.map AuthCmd
    | AuthMsg  msg ->                     Auth .generate msg     |> List.map AuthCmd
    | ToastMsg msg ->                     Toast.generate msg |> ToastCmd |> List.singleton

    | ErrorOccurred RemoteUnauthorizedException -> "You have been logged out." |> toastErrorCmd
    | ErrorOccurred ex ->                                           ex.Message |> toastErrorCmd

    | CounterMsg _ -> []

open Elmish.UrlParser
let parser =
    oneOf
        [ map Home                      (s "")
          map Home                      (s "home")
          map Counter                   (s "counter")
          map Book                      (s "book")
          map (Login Login.initModel)   (s "login")
          map Profile                   (s "profile") ]

let router = {
    getEndPoint = fun model -> model.Page
    setRoute = fun path ->
        path |> parsePath parser |> Option.map Navigated
    getRoute = function
        | Home    -> "/home"
        | Counter -> "/counter"
        | Book    -> "/book"
        | Login _ -> "/login"
        | Profile -> "/profile"
}

type Main = Template<"wwwroot/main.html">

let homePage =
    Main.Home().Elt()

let menuItem (model: Model) (page: Page) (text: string) =
    Main.MenuItem()
        .Active(if model.Page.is page then "is-active" else "")
        .Url(router.Link page)
        .Text(text)
        .Elt()

let view model dispatch =
    Main()
        .Menu(concat [
            menuItem model Home                    "Home"
            menuItem model (Login Login.initModel) "Login"
            menuItem model Profile                 "Profile"
            menuItem model Counter                 "Counter"
            menuItem model Book                    "Download Books"
        ])
        .Body(
            cond model.Page <| function
            | Home    -> homePage
            | Login m ->   LoginMsg >> dispatch |> Login  .view m
            | Counter -> CounterMsg >> dispatch |> Counter.view model.Counter
            | Book    ->    BookMsg >> dispatch |> Book   .view model.Auth model.Book
            | Profile ->    AuthMsg >> dispatch |> Profile.view model.Auth
        )
        .Toast(Toast.view (Toast.render dispatch) model.Toast (ToastMsg >> dispatch))
        .Elt()

open Elmish

let toCmd (model: Model) (authRemote: Auth.AuthService) (bookRemote: Book.BookService) = function
    | SetPage page -> page |> Navigated |> Cmd.ofMsg
    | BookCmd cmd ->
        match cmd with
        | Book.GetBooks
        | Book.Initialize ->
            Cmd.OfAsync.either bookRemote.getBooks ()
                (Book.BooksReceived      >> BookMsg)
                (Book.BooksReceivedError >> BookMsg)
        | Book.AddBook book ->
            Cmd.OfAsync.either bookRemote.addBook book
                (fun () -> Book.BooksRequested |> BookMsg)
                (Book.Errored >> BookMsg)
        | Book.Toast toast ->
            toast
            |> Toast.mapInputMsg BookMsg
            |> Toast.Add
            |> ToastMsg
            |> Cmd.ofMsg
        | Book.RemoveBook isbn ->
            Cmd.OfAsync.either bookRemote.removeBookByIsbn isbn
                (fun () -> Book.BooksRequested |> BookMsg)
                (Book.Errored >> BookMsg)
    | AuthCmd cmd ->
        match cmd with
        | Auth.AttemptLogin (username, password) ->
            Cmd.OfAsync.either authRemote.signIn (username, password)
                (Auth.manualLoginAttempted >> AuthMsg)
                ErrorOccurred
        | Auth.Logout ->
            Cmd.OfAsync.either authRemote.signOut ()
                (fun () -> Navigated Home)
                ErrorOccurred
        | Auth.LoginSuccessful ->
            match model.Auth with
            | Auth.Anonymous onLoginSuccessRedirect -> onLoginSuccessRedirect |> Page.ofRedirect |> Navigated |> Cmd.ofMsg
            | _ -> Cmd.none
        | Auth.FailLogin -> Login.Msg.LoginFailed |> LoginMsg |> Cmd.ofMsg
        | Auth.Initialize ->
            Cmd.OfAuthorized.either authRemote.getUsername ()
                (Auth.autoLoginAttempted >> AuthMsg)
                ErrorOccurred
    | ToastCmd cmd -> cmd |> Cmd.map ToastMsg

let toCmds model auth book =
    List.map (toCmd model auth book)

type MyApp() =
    inherit ProgramComponent<Model, Msg>()

    override this.Program =
        let bookRemote = this.Remote<Book.BookService>()
        let authRemote = this.Remote<Auth.AuthService>()
        let update msg model =
            let cmds = generate msg model |> toCmds model authRemote bookRemote |> Cmd.batch
            let model = update msg model
            model, cmds
        Program.mkProgram (fun _ -> initModel, Auth.Initialize |> AuthCmd |> toCmd initModel authRemote bookRemote) update view
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
        |> Program.withConsoleTrace
        |> Program.withTrace(fun msg model -> () ) // good place for a breakpoint with ?server=true
#endif

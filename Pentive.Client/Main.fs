module Pentive.Client.Main

open System
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client

type Model =
    {
        Page: Page
        Error: string option
        Counter: Counter.Model
        Book   : Book   .Model
        Login  : Login  .Model
        Auth   : Auth   .Model
    }

let initModel =
    {
        Page = Home
        Error = None
        Counter = Counter.initModel
        Book    = Book   .initModel
        Login   = Login  .initModel
        Auth    = Auth   .initModel
    }

type Msg =
    | Navigated of Page
    | ErrorOccured of exn
    | ErrorCleared
    | CounterMsg of Counter.Msg
    |   LoginMsg of Login  .Msg
    |    BookMsg of Book   .Msg
    |    AuthMsg of Auth   .Msg

type Cmd =
    | SetPage of Page
    | AuthCmd of Auth.Cmd
    | BookCmd of Book.Cmd

let isPermitted page (auth: Auth.Model) =
    if page |> Page.requireAuthenticated then
        auth.Username |> Option.isSome
    else true

let update message (model: Model) =
    match message with
    | Navigated page ->
        let model = // leaving Login clears it (unless they're already on the Login page)
            if model.Page = Login && page <> Login then
                { model with Login = Login.initModelTo (Some Profile) }
            else model
        if isPermitted page model.Auth then
            { model with Page = page }
        else
            { model with Error = Some $"You must login to view that page."; Login = Login.initModelTo (Some page) }

    | CounterMsg msg -> { model with Counter = model.Counter |> Counter.update msg }
    | BookMsg    msg -> { model with Book    = model.Book    |> Book   .update msg }
    | LoginMsg   msg -> { model with Login   = model.Login   |> Login  .update msg }
    | AuthMsg    msg -> { model with Auth    = model.Auth    |> Auth   .update msg }

    | ErrorOccured RemoteUnauthorizedException -> { model with Error = Some "You have been logged out."; Auth = Auth.logout model.Auth }
    | ErrorOccured exn                         -> { model with Error = Some exn.Message }
    | ErrorCleared                             -> { model with Error = None }

let generate message (model: Model) =
    match message with
    | Navigated page ->
        if isPermitted page model.Auth then
            match page with
            | Book -> [BookCmd Book.Initialize]
            | _ -> []
        else
            [SetPage Login]

    | BookMsg  msg ->                Book .generate msg |> List.map BookCmd
    | LoginMsg msg -> model.Login |> Login.generate msg |> List.map AuthCmd
    | AuthMsg  msg ->                Auth .generate msg |> List.map AuthCmd

    | CounterMsg _
    | ErrorOccured _
    | ErrorCleared -> []

open Elmish.UrlParser
let parser =
    oneOf
        [ map Home    (s "")
          map Counter (s "counter")
          map Book    (s "book")
          map Login   (s "login")
          map Profile (s "profile") ]

let router = {
    getEndPoint = fun model -> model.Page
    setRoute = fun path ->
        path |> parsePath parser |> Option.map Navigated
    getRoute = function
        | Home    -> "/"
        | Counter -> "/counter"
        | Book    -> "/book"
        | Login   -> "/login"
        | Profile -> "/profile"
}

type Main = Template<"wwwroot/main.html">

let homePage =
    Main.Home().Elt()

let menuItem (model: Model) (page: Page) (text: string) =
    Main.MenuItem()
        .Active(if model.Page = page then "is-active" else "")
        .Url(router.Link page)
        .Text(text)
        .Elt()

let view model dispatch =
    Main()
        .Menu(concat [
            menuItem model Home    "Home"
            menuItem model Login   "Login"
            menuItem model Profile "Profile"
            menuItem model Counter "Counter"
            menuItem model Book    "Download Books"
        ])
        .Body(
            cond model.Page <| function
            | Home    -> homePage
            | Login   ->   LoginMsg >> dispatch |> Login  .view model.Login
            | Counter -> CounterMsg >> dispatch |> Counter.view model.Counter
            | Book    ->    BookMsg >> dispatch |> Book   .view model.Auth.Username model.Book
            | Profile ->    AuthMsg >> dispatch |> Profile.view model.Auth
        )
        .Error(
            cond model.Error <| function
            | None -> empty
            | Some err ->
                Main.ErrorNotification()
                    .Text(err)
                    .Hide(fun _ -> dispatch ErrorCleared)
                    .Elt()
        )
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
        | Book.NotifyError exn -> exn |> ErrorOccured |> Cmd.ofMsg
        | Book.RemoveBook isbn ->
            Cmd.OfAsync.either bookRemote.removeBookByIsbn isbn
                (fun () -> Book.BooksRequested |> BookMsg)
                (Book.Errored >> BookMsg)
    | AuthCmd cmd ->
        match cmd with
        | Auth.AttemptLogin (username, password) ->
            Cmd.OfAsync.either authRemote.signIn (username, password)
                (Auth.manualLoginAttempted >> AuthMsg)
                ErrorOccured
        | Auth.Logout ->
            Cmd.OfAsync.either authRemote.signOut ()
                (fun () -> Navigated Home)
                ErrorOccured
        | Auth.LoginSuccessful ->
            match model.Login.Redirect with
            | Some  page -> page |> Navigated |> Cmd.ofMsg
            | None -> Cmd.none
        | Auth.FailLogin -> Login.Msg.LoginFailed |> LoginMsg |> Cmd.ofMsg
        | Auth.Initialize ->
            Cmd.OfAuthorized.either authRemote.getUsername ()
                (Auth.autoLoginAttempted >> AuthMsg)
                ErrorOccured

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

module Pentive.Client.Main

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client

/// The Elmish application's model.
type Model =
    {
        page: Page
        counter: Counter.Model
        book: Book.Model
        error: string option
        login: Login.Model
        auth: Auth.Model
    }

let initModel =
    {
        page = Home
        counter = Counter.initModel
        book = Book.initModel
        error = None
        login = Login.initModel
        auth = Auth.initModel
    }

/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | CounterMsg of Counter.Message
    | LoginMsg of Login.Message
    | BookMsg of Book.Message
    | AuthMsg of Auth.Message
    | Error of exn
    | ClearError

type CmdMsg =
    | CM_SetPage of Page
    | CM_Auth of Auth.CmdMsg
    | CM_Book of Book.CmdMsg

let update message (model: Model) =
    match message with
    | SetPage page ->
        let initializeCmds =
            match page with
            | Book -> [CmdMsg.CM_Book Book.CM_Initialize]
            | _ -> []
        match page with
        | Book
        | Profile ->
            match model.auth.username with
            | Some _ -> { model with page = page }, initializeCmds
            | None -> { model with error = Some "You must login to view that page." }, [CM_SetPage Login]
        | Home
        | Login
        | Counter -> { model with page = page }, initializeCmds

    | CounterMsg msg ->
        let counter = Counter.update msg model.counter
        { model with counter = counter }, []
    | BookMsg msg ->
        let book, cmds = Book.update msg model.book
        { model with book = book }, cmds |> List.map CmdMsg.CM_Book
    | LoginMsg msg ->
        let login, cmds = Login.update msg model.login
        { model with login = login }, cmds |> List.map CmdMsg.CM_Auth
    | AuthMsg msg ->
        let auth, cmds = Auth.update msg model.auth
        { model with auth = auth }, cmds |> List.map CmdMsg.CM_Auth

    | Error RemoteUnauthorizedException ->
        { model with error = Some "You have been logged out."; auth = Auth.logout model.auth }, []
    | Error exn ->
        { model with error = Some exn.Message }, []
    | ClearError ->
        { model with error = None }, []

/// Connects the routing system to the Elmish application.
let router = Router.infer SetPage (fun model -> model.page)

type Main = Template<"wwwroot/main.html">

let homePage =
    Main.Home().Elt()

let menuItem (model: Model) (page: Page) (text: string) =
    Main.MenuItem()
        .Active(if model.page = page then "is-active" else "")
        .Url(router.Link page)
        .Text(text)
        .Elt()

let view model dispatch =
    Main()
        .Menu(concat [
            menuItem model Home "Home"
            menuItem model Login "Login"
            menuItem model Profile "Profile"
            menuItem model Counter "Counter"
            menuItem model Book "Download Books"
        ])
        .Body(
            cond model.page <| function
            | Home -> homePage
            | Login -> Login.view model.login (LoginMsg >> dispatch)
            | Counter -> Counter.view model.counter (CounterMsg >> dispatch)
            | Book -> Book.view model.auth.username model.book (BookMsg >> dispatch)
            | Profile -> Profile.view model.auth (AuthMsg >> dispatch)
        )
        .Error(
            cond model.error <| function
            | None -> empty
            | Some err ->
                Main.ErrorNotification()
                    .Text(err)
                    .Hide(fun _ -> dispatch ClearError)
                    .Elt()
        )
        .Elt()

let toCmd (remote: Book.BookService) = function
    | CM_SetPage page -> SetPage page |> Cmd.ofMsg
    | CM_Book cmdMsg ->
        match cmdMsg with
        | Book.CM_GetBooks
        | Book.CM_Initialize -> Cmd.OfAsync.either remote.getBooks () (Book.GotBooks >> Message.BookMsg) Error
    | CM_Auth cmdMsg ->
        match cmdMsg with
        | Auth.CM_AttemptLogin (username, password) -> Cmd.OfAsync.either remote.signIn (username, password) (Auth.loginAttemptedTo Page.Profile >> Message.AuthMsg) Error
        | Auth.CM_Logout -> Cmd.OfAsync.attempt remote.signOut () Error
        | Auth.CM_SetPage page -> SetPage page |> Cmd.ofMsg
        | Auth.CM_LoginFailed -> Login.Message.LoginFailed |> Message.LoginMsg |> Cmd.ofMsg
        | Auth.CM_Initialize -> Cmd.OfAuthorized.either remote.getUsername () (Auth.initialLoginAttempted >> Message.AuthMsg) Error

let toCmds remote =
    List.map (toCmd remote)

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let bookService = this.Remote<Book.BookService>()
        let update msg model =
            let model, cmdMsgs = update msg model
            model, toCmds bookService cmdMsgs |> Cmd.batch
        Program.mkProgram (fun _ -> initModel, Auth.CM_Initialize |> CmdMsg.CM_Auth |> toCmd bookService) update view
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif

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
    }

let initModel =
    {
        page = Home
        counter = Counter.initModel
        book = Book.initModel
        error = None
        login = Login.initModel
    }

/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | CounterMsg of Counter.Message
    | LoginMsg of Login.Message
    | BookMsg of Book.Message
    | Error of exn
    | ClearError

type CmdMsg =
    | CM_SetPage of Page
    | CM_Login of Login.CmdMsg
    | CM_Book of Book.CmdMsg

let update message (model: Model) =
    match message with
    | SetPage page ->
        match page with
        | Data ->
            match model.login.signedInAs with
            | Some _ -> { model with page = page }, []
            | None -> { model with error = Some "You must login to view the Download Data page." }, [CM_SetPage Login]
        | _ -> { model with page = page }, []

    | CounterMsg msg ->
        let counter = Counter.update msg model.counter
        { model with counter = counter }, []

    | BookMsg msg ->
        let book, cmds = Book.update msg model.book
        { model with book = book }, cmds |> List.map CmdMsg.CM_Book

    | LoginMsg msg ->
        let login, cmds = Login.update msg model.login
        { model with login = login }, cmds |> List.map CmdMsg.CM_Login

    | Error RemoteUnauthorizedException ->
        { model with error = Some "You have been logged out."; login = Login.logout model.login }, []
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
            menuItem model Counter "Counter"
            menuItem model Data "Download data"
        ])
        .Body(
            cond model.page <| function
            | Home -> homePage
            | Login -> Login.view model.login (LoginMsg >> dispatch)
            | Counter -> Counter.view model.counter (CounterMsg >> dispatch)
            | Data -> Book.view model.login.signedInAs model.book (BookMsg >> dispatch)
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
        | Book.CM_GetBooks -> Cmd.OfAsync.either remote.getBooks () (Book.GotBooks >> Message.BookMsg) Error
    | CM_Login cmdMsg ->
        match cmdMsg with
        | Login.CM_RecvSignIn model -> Cmd.OfAsync.either remote.signIn (model.username, model.password) (Login.RecvSignIn >> Message.LoginMsg) Error
        | Login.CM_RecvSignedInAs -> Cmd.OfAuthorized.either remote.getUsername () (Login.RecvSignedInAs >> Message.LoginMsg) Error
        | Login.CM_RecvSignOut -> Cmd.OfAsync.either remote.signOut () (fun () -> Login.RecvSignOut |> Message.LoginMsg) Error
        | Login.CM_SetPage page -> SetPage page |> Cmd.ofMsg

let toCmds remote =
    List.map (toCmd remote)

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let bookService = this.Remote<Book.BookService>()
        let update msg model =
            let model, cmdMsgs = update msg model
            model, toCmds bookService cmdMsgs |> Cmd.batch
        Program.mkProgram (fun _ -> initModel, Login.GetSignedInAs |> Message.LoginMsg |> Cmd.ofMsg) update view
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif

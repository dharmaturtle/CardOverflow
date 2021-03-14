module Pentive.Client.Main

open System
open Elmish
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

type Message =
    | SetPage of Page
    | Error of exn
    | ClearError
    | CounterMsg of Counter.Message
    |   LoginMsg of Login  .Message
    |    BookMsg of Book   .Message
    |    AuthMsg of Auth   .Message

type CmdMsg =
    | CM_SetPage of Page
    | CM_Auth of Auth.CmdMsg
    | CM_Book of Book.CmdMsg

let isPermitted page (auth: Auth.Model) =
    if page |> Page.requireAuthenticated then
        auth.Username |> Option.isSome
    else true

let update message (model: Model) =
    match message with
    | SetPage page ->
        let model = // navigating from Login resets it
            match model.Page with
            | Login -> { model with Login = Login.initModel}
            | _ -> model
        if isPermitted page model.Auth then
            { model with Page = page }
        else
            { model with Error = Some "You must login to view that page." }

    | CounterMsg msg -> { model with Counter = model.Counter |> Counter.update msg }
    | BookMsg    msg -> { model with Book    = model.Book    |> Book   .update msg }
    | LoginMsg   msg -> { model with Login   = model.Login   |> Login  .update msg }
    | AuthMsg    msg -> { model with Auth    = model.Auth    |> Auth   .update msg }

    | Error RemoteUnauthorizedException -> { model with Error = Some "You have been logged out."; Auth = Auth.logout model.Auth }
    | Error exn                         -> { model with Error = Some exn.Message }
    | ClearError                        -> { model with Error = None }

let generate message (model: Model) =
    match message with
    | SetPage page ->
        if isPermitted page model.Auth then
            match page with
            | Book -> [CmdMsg.CM_Book Book.CM_Initialize]
            | _ -> []
        else
            [CM_SetPage Login]

    | BookMsg  msg ->                Book .generate msg |> List.map CmdMsg.CM_Book
    | LoginMsg msg -> model.Login |> Login.generate msg |> List.map CmdMsg.CM_Auth
    | AuthMsg  msg ->                Auth .generate msg |> List.map CmdMsg.CM_Auth

    | CounterMsg _
    | Error _
    | ClearError -> []

let router = Router.infer SetPage (fun model -> model.Page)

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
                    .Hide(fun _ -> dispatch ClearError)
                    .Elt()
        )
        .Elt()

let toCmd (authRemote: Auth.AuthService) (bookRemote: Book.BookService) = function
    | CM_SetPage page -> page |> SetPage |> Cmd.ofMsg
    | CM_Book cmdMsg ->
        match cmdMsg with
        | Book.CM_GetBooks
        | Book.CM_Initialize ->
            Cmd.OfAsync.either bookRemote.getBooks ()
                (Book.GotBooks      >> Message.BookMsg)
                (Book.GotBooksError >> Message.BookMsg)
    | CM_Auth cmdMsg ->
        match cmdMsg with
        | Auth.CM_AttemptLogin (username, password) ->
            Cmd.OfAsync.either authRemote.signIn (username, password)
                (Auth.loginAttempted Page.Profile >> Message.AuthMsg)
                Error
        | Auth.CM_Logout ->
            Cmd.OfAsync.either authRemote.signOut ()
                (fun () -> SetPage Home)
                Error
        | Auth.CM_SetPage page -> page |> SetPage |> Cmd.ofMsg
        | Auth.CM_LoginFailed -> Login.Message.LoginFailed |> Message.LoginMsg |> Cmd.ofMsg
        | Auth.CM_Initialize ->
            Cmd.OfAuthorized.either authRemote.getUsername ()
                (Auth.initialLoginAttempted >> Message.AuthMsg)
                Error

let toCmds auth book =
    List.map (toCmd auth book)

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let bookRemote = this.Remote<Book.BookService>()
        let authRemote = this.Remote<Auth.AuthService>()
        let update msg model =
            let cmds = generate msg model |> toCmds authRemote bookRemote |> Cmd.batch
            let model = update msg model
            model, cmds
        Program.mkProgram (fun _ -> initModel, Auth.CM_Initialize |> CmdMsg.CM_Auth |> toCmd authRemote bookRemote) update view
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif

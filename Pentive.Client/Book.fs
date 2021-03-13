module Pentive.Client.Book

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting

type Model =
    {
        Books: Book[] Loadable
    }

and Book =
    {
        Title: string
        Author: string
        PublishDate: DateTime
        Isbn: string
    }

let initModel =
    {
        Books = Initial
    }

type BookService =
    {
        /// Get the list of all books in the collection.
        getBooks: unit -> Async<Book[]>

        /// Add a book in the collection.
        addBook: Book -> Async<unit>

        /// Remove a book from the collection, identified by its ISBN.
        removeBookByIsbn: string -> Async<unit>

        /// Sign into the application.
        signIn : string * string -> Async<option<string>>

        /// Get the user's name, or None if they are not authenticated.
        getUsername : unit -> Async<string>

        /// Sign out from the application.
        signOut : unit -> Async<unit>
    }

    interface IRemoteService with
        member _.BasePath = "/books"

type Message =
    | GetBooks
    | GotBooks of Book[]

type CmdMsg =
    | CM_GetBooks
    | CM_Initialize

let update message model =
    match message with
    | GetBooks ->
        { model with Books = Loading }, [CM_GetBooks]
    | GotBooks books ->
        { model with Books = Loaded books }, []

type BookTemplate = Template<"wwwroot/book.html">

let view (username: string option) (model: Model) dispatch =
    match username with
    | Some username ->
        BookTemplate()
            .Reload(fun _ -> dispatch GetBooks)
            .Username(username)
            .Rows(cond model.Books <| function
                | Initial ->
                    BookTemplate.Initial().Elt()
                | Loading ->
                    BookTemplate.Loading().Elt()
                | Loaded books ->
                    forEach books <| fun book ->
                        tr [] [
                            td [] [text book.Title]
                            td [] [text book.Author]
                            td [] [text (book.PublishDate.ToString("yyyy-MM-dd"))]
                            td [] [text book.Isbn]
                        ])
            .Elt()
    | None -> text "You must login to view the Download Data page."

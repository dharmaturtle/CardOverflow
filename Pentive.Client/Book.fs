module Pentive.Client.Book

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting

type Model =
    {
        books: Book[] Loadable
    }

and Book =
    {
        title: string
        author: string
        publishDate: DateTime
        isbn: string
    }

let initModel =
    {
        books = Initial
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

let update message model =
    match message with
    | GetBooks ->
        { model with books = Loading }, [CM_GetBooks]
    | GotBooks books ->
        { model with books = Loaded books }, []

type BookTemplate = Template<"wwwroot/book.html">

let view (username: string option) (model: Model) dispatch =
    match username with
    | Some username ->
        BookTemplate()
            .Reload(fun _ -> dispatch GetBooks)
            .Username(username)
            .Rows(cond model.books <| function
                | Initial ->
                    BookTemplate.Initial().Elt()
                | Loading ->
                    BookTemplate.Loading().Elt()
                | Loaded books ->
                    forEach books <| fun book ->
                        tr [] [
                            td [] [text book.title]
                            td [] [text book.author]
                            td [] [text (book.publishDate.ToString("yyyy-MM-dd"))]
                            td [] [text book.isbn]
                        ])
            .Elt()
    | None -> text "You must login to view the Download Data page."

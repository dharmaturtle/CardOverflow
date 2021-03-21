module Pentive.Client.Book

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting

type Model =
    {
        Books: Book[] RemoteData
        NewBookTitle: string
        NewBookAuthor: string
        NewBookPublishDate: string
        NewBookIsbn: string
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
        Books = NotAsked
        NewBookTitle = ""
        NewBookAuthor = ""
        NewBookPublishDate = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)
        NewBookIsbn = ""
    }

type BookService =
    {
        /// Get the list of all books in the collection.
        getBooks: unit -> Async<Book[]>

        /// Add a book in the collection.
        addBook: Book -> Async<unit>

        /// Remove a book from the collection, identified by its ISBN.
        removeBookByIsbn: string -> Async<unit>
    }

    interface IRemoteService with
        member _.BasePath = "/books"

type Msg =
    | BooksRequested
    | BooksReceived of Book[]
    | BooksReceivedError of exn

    | NewBookTitleUpdated of string
    | NewBookAuthorUpdated of string
    | NewBookPublishDateUpdated of string
    | NewBookIsbnUpdated of string
    | NewBookSubmitted of Book

    | BookRemoved of isbn: string
    
    | Errored of exn

type Cmd =
    | GetBooks
    | Initialize
    | AddBook of Book
    | NotifyError of exn
    | Toast of Toast.Toast<string>
    | RemoveBook of isbn: string

let update message model =
    match message with
    | BooksRequested              -> { model with Books = Loading }
    | BooksReceived         books -> { model with Books = Success books }
    | BooksReceivedError       ex -> { model with Books = Failure ex.Message }
    | NewBookTitleUpdated       s -> { model with NewBookTitle = s }
    | NewBookAuthorUpdated      s -> { model with NewBookAuthor = s }
    | NewBookPublishDateUpdated s -> { model with NewBookPublishDate = s }
    | NewBookIsbnUpdated        s -> { model with NewBookIsbn = s }
    | NewBookSubmitted          _ -> model
    | BookRemoved               _ -> model
    | Errored                   _ -> model

let generate = function
    | BooksRequested              ->
        let toast =
            Toast.triggerEvent
                (Toast.message "Getting books..."
                    |> Toast.title "Some Title"
                    |> Toast.position Toast.BottomLeft
                    |> Toast.icon "fas fa-check"
                    |> Toast.timeout (TimeSpan.FromSeconds (5.))
                    |> Toast.dismissOnClick
                    |> Toast.withCloseButton
                )
                Toast.Info
                (fun () -> ())
        [GetBooks; Toast toast]
    | BooksReceived             _ -> []
    | BooksReceivedError        _ -> []
    | NewBookTitleUpdated       _ -> []
    | NewBookAuthorUpdated      _ -> []
    | NewBookPublishDateUpdated _ -> []
    | NewBookIsbnUpdated        _ -> []
    | NewBookSubmitted       book -> [AddBook book]
    | BookRemoved            book -> [RemoveBook book]
    | Errored                  ex -> [NotifyError ex]

type BookTemplate = Template<"wwwroot/book.html">

let newBook (model: Model) =
    {
        Title = model.NewBookTitle
        Author = model.NewBookAuthor
        PublishDate = model.NewBookPublishDate |> DateTime.Parse
        Isbn = model.NewBookIsbn
    }

let view (auth: Auth.Model) (model: Model) dispatch =
    match auth with
    | Auth.Authenticated username ->
        BookTemplate()
            .Reload(fun _ -> dispatch BooksRequested)
            .Username(username)
            .Rows(cond model.Books <| function
                | NotAsked ->
                    BookTemplate.Initial().Elt()
                | Loading ->
                    BookTemplate.Loading().Elt()
                | Failure e ->
                    BookTemplate.Error().ErrorText(e).Elt()
                | Success books ->
                    forEach books <| fun book ->
                        tr [] [
                            td [] [text    book.Title]
                            td [] [text    book.Author]
                            td [] [text <| book.PublishDate.ToString("yyyy-MM-dd")]
                            td [] [text    book.Isbn]
                            td [] [
                                button
                                    [on.click (fun _ -> book.Isbn |> BookRemoved |> dispatch)]
                                    [text "Delete"]
                            ]
                        ])
            .NewBookTitle(      model.NewBookTitle      , fun s -> s |> NewBookTitleUpdated       |> dispatch)
            .NewBookAuthor(     model.NewBookAuthor     , fun s -> s |> NewBookAuthorUpdated      |> dispatch)
            .NewBookPublishDate(model.NewBookPublishDate, fun s -> s |> NewBookPublishDateUpdated |> dispatch)
            .NewBookIsbn(       model.NewBookIsbn       , fun s -> s |> NewBookIsbnUpdated        |> dispatch)
            .NewBookSubmitted(fun _ ->  model |> newBook |> NewBookSubmitted |> dispatch)
            .Elt()
    | Auth.Authenticating _ -> text "Authenticating..."
    | Auth.Anonymous      _ -> text "You must login to view the Download Data page."

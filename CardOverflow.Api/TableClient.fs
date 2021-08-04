namespace CardOverflow.Api

open LoadersAndCopiers
open CardOverflow.Api
open CardOverflow.Pure
open Domain
open CardOverflow.Debug
open CardOverflow.Entity
open CardOverflow.Entity.Anki
open System
open System.Linq
open Thoth.Json.Net
open FsToolkit.ErrorHandling
open Helpers
open System.IO
open System.IO.Compression
open System.Security.Cryptography
open System.Collections.Generic
open NodaTime
open AsyncOp
open FSharp.Azure.Storage.Table
open Microsoft.Azure.Cosmos.Table
open Newtonsoft.Json
open Domain.Summary
open Domain.Projection
open FSharp.Control.Tasks
open Serilog

type AzureTableStorageWrapper =
    { [<PartitionKey>] Partition: string
      [<RowKey>] Row: string
      _0: string
      _1: string
      _2: string
      _3: string
      _4: string
      _5: string
      _6: string
      _7: string
      _8: string
      _9: string }

module AzureTableStorage =
    let encoding = System.Text.UnicodeEncoding() // this is UTF16 https://docs.microsoft.com/en-us/dotnet/api/system.text.unicodeencoding?view=net-5.0
    
    let getPartitionRow (summary: obj) =
        match summary with
        | :?         Concept  as x -> let id =     Concept.ProjectionId x.Id in id, id
        | :?     Kvs.Profile  as x -> let id = Kvs.Profile.ProjectionId x.Id in id, id
        | :?     Kvs.Example  as x -> let id =                     $"{x.Id}" in id, id
        | :?  Deck.Fold.State as x -> let id =        Kvs.deckProjectionId x in id, id
        | :?     Kvs.Template as x -> let id =                     $"{x.Id}" in id, id
        | :?         Stack    as x -> let id =                     $"{x.Id}" in id, id
        | :?         User     as x -> let id =                     $"{x.Id}" in id, id
        | _ -> failwith $"The type '{summary.GetType().FullName}' has not yet registered a PartitionKey or RowKey."

    let wrap payload =
        let partition, row = getPartitionRow payload
        let payload = payload |> serializeToJson |> encoding.GetBytes
        let azureTablesMaxPropertySize = 65_536 // 64KiB https://docs.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model
        let payload = Array.chunkBySize azureTablesMaxPropertySize payload
        let get index = payload |> Array.tryItem index |> Option.defaultValue [||] |> encoding.GetString
        { Partition = partition
          Row = row
          _0  = get 0
          _1  = get 1
          _2  = get 2
          _3  = get 3
          _4  = get 4
          _5  = get 5
          _6  = get 6
          _7  = get 7
          _8  = get 8
          _9  = get 9 } // according to math https://www.wolframalpha.com/input/?i=1+mib+%2F+64+kib this should keep going until _15 (0 indexed), but running tests on the Azure Table Emulator throws at about _9. medTODO find the real limit with the real Azure Table Storage

type IKeyValueStore =
    abstract Insert         :  'a           -> Async<unit>
    abstract Replace        :  'a -> string -> Async<unit>
    abstract Delete    : key: obj -> Async<unit>
    abstract PointQuery: key: obj -> Async<seq<AzureTableStorageWrapper * EntityMetadata>>

#if DEBUG

exception TransientError

module IdempotentTest =
    let mutable random = System.Random 1337_314159
    let mutable defaultFailrate = 0.
    let mutable        failrate = 0.
    let init fail_rate seed =
        failrate        <- fail_rate
        defaultFailrate <- fail_rate
        random <- System.Random seed
    let tryFail () =
        if failrate > random.NextDouble()
        then raise TransientError

// it could be argued that test stuff should only be in test assemblies, but I'm gonna put stuff that's tightly coupled together. Easier to make changes.
type TableMemoryClient() =
    let dict = System.Collections.Generic.Dictionary<(string * string), AzureTableStorageWrapper>()
    interface IKeyValueStore with
        member _.Insert  summary   = async { // Since async is "cold", `IdempotentTest.tryFail()` will only be called when it is awaited.
            IdempotentTest.tryFail()
            let value = summary |> AzureTableStorage.wrap
            let key = value.Partition, value.Partition
            match dict.TryGetValue key with
            | true, old -> if old <> value then failwith "Did you mean to use `Replace`?"
            | _         -> dict.Add (key, value)
            }
        member _.Replace summary _ = async { // Since async is "cold", `IdempotentTest.tryFail()` will only be called when it is awaited.
            IdempotentTest.tryFail()
            let value = summary |> AzureTableStorage.wrap
            let key = value.Partition, value.Partition
            if dict.ContainsKey key then
                dict.[key] <- value
            else
                failwith "Tried to `Replace` something that doesn't exist. Did you mean to use `Insert`?"
            }
        member _.Delete (key: obj) =
            IdempotentTest.tryFail()
            dict.Remove ((string key, string key)) |> ignore
            Async.singleton ()
        member _.PointQuery (key: obj) =
            let key = string key, string key
            if dict.ContainsKey key then
                let meta =
                    { Etag = ""
                      Timestamp = DateTimeOffset.MinValue }
                (dict.Item key, meta)
                |> Seq.singleton
                |> Async.singleton
            else
                Seq.empty |> Async.singleton
#endif

type TableClient(connectionString, tableName) =
    let account     = CloudStorageAccount.Parse connectionString
    let tableClient = account.CreateCloudTableClient()
    let inTable     = inTableAsync   tableClient tableName
    let fromTable   = fromTableAsync tableClient tableName
    
    member _.CloudTableClient = tableClient
    member _.TableName = tableName
    
    interface IKeyValueStore with
        member _.Insert summary = async {
            let! r = summary |> AzureTableStorage.wrap |> Insert |> inTable
            match r.HttpStatusCode with
            | 409    // EntityAlreadyExists. Ok because it should be idempotent https://docs.microsoft.com/en-us/rest/api/storageservices/table-service-error-codes
            | 200 -> ()
            | 400 -> Log.Error $"400 Bad Request error during Insert to Azure Table Storage. Payload as follows:{summary}"
            | _ ->
                let msg = $"Error {r.HttpStatusCode} during Insert to Azure Table Storage. Payload as follows:{summary}"
                Log.Error msg
                failwith  msg
            }
        member _.Replace summary etag = async {
            let! r = (summary |> AzureTableStorage.wrap, etag) |> Replace |> inTable
            match r.HttpStatusCode with
            | 200 -> ()
            | 400 -> Log.Error $"400 Bad Request error during Insert to Azure Table Storage. Payload as follows:{summary}"
            | _ ->
                let msg = $"Error {r.HttpStatusCode} during Insert to Azure Table Storage. Payload as follows:{summary}"
                Log.Error msg
                failwith  msg
            }
        member this.Delete key = async {
            match! key |> (this :> IKeyValueStore).PointQuery |> Async.map Seq.tryExactlyOne with
            | Some (x, _) -> let! _ = x |> ForceDelete |> inTable
                             ()
            | None        -> ()
            }
        member _.PointQuery (key: obj) = // point query https://docs.microsoft.com/en-us/azure/storage/tables/table-storage-design-for-query#how-your-choice-of-partitionkey-and-rowkey-impacts-query-performance:~:text=Point%20Query,-is
            Query.all<AzureTableStorageWrapper>
            |> Query.where <@ fun _ s -> s.PartitionKey = string key && s.RowKey = string key @>
            |> fromTable

type KeyValueStore(keyValueStore: IKeyValueStore) =
    member    _.Insert  (x:              Concept            ) = keyValueStore.Insert x
    member    _.Insert  (x:         Kvs. Profile            ) = keyValueStore.Insert x
    member    _.Insert  (x:         Kvs. Example            ) = keyValueStore.Insert x
    member    _.Insert  (x:         Kvs.Template            ) = keyValueStore.Insert x
    member    _.Insert  (x:      Deck.Fold.State            ) = keyValueStore.Insert x
    member    _.Insert  (x:                Stack            ) = keyValueStore.Insert x
    member    _.Insert  (x:                 User            ) = keyValueStore.Insert x
    member this.Insert  (x: Option<     Concept>            ) = match x with | None -> Async.singleton () | Some x -> this.Insert x
    member this.Insert  (x: Option<Kvs. Example>            ) = match x with | None -> Async.singleton () | Some x -> this.Insert x
    member this.Insert  (x: Option<Kvs.Template>            ) = match x with | None -> Async.singleton () | Some x -> this.Insert x
    member    _.Replace (x:              Concept, etag: Etag) = keyValueStore.Replace x (string etag)
    member    _.Replace (x:         Kvs. Profile, etag: Etag) = keyValueStore.Replace x (string etag)
    member    _.Replace (x:         Kvs. Example, etag: Etag) = keyValueStore.Replace x (string etag)
    member    _.Replace (x:         Kvs.Template, etag: Etag) = keyValueStore.Replace x (string etag)
    member    _.Replace (x:      Deck.Fold.State, etag: Etag) = keyValueStore.Replace x (string etag)
    member    _.Replace (x:                Stack, etag: Etag) = keyValueStore.Replace x (string etag)
    member    _.Replace (x:                 User, etag: Etag) = keyValueStore.Replace x (string etag)
    member this.Replace (x: Option<     Concept>, etag: Etag) = match x with | None -> Async.singleton () | Some y -> this.Replace (y, etag)
    member this.Replace (x: Option<Kvs. Example>, etag: Etag) = match x with | None -> Async.singleton () | Some y -> this.Replace (y, etag)
    member this.Replace (x: Option<Kvs.Template>, etag: Etag) = match x with | None -> Async.singleton () | Some y -> this.Replace (y, etag)
    member    _.Delete   x                                    = keyValueStore.Delete  x

    member _.TryGet<'a> (key: obj) =
        keyValueStore.PointQuery key
        |>% Seq.tryExactlyOne
        |>% Option.map (fun (x, m) ->
            String.concat "" [
              x._0
              x._1
              x._2
              x._3
              x._4
              x._5
              x._6
              x._7
              x._8
              x._9
            ] |> fun x -> deserializeFromJson<'a> x, m
        )

    member this.Get<'a> (key: obj) : Async<'a * Etag> = async {
        let! x = this.TryGet<'a> key
        return
            match x with
            | Some (a, m) -> a, FSharp.UMX.UMX.tag m.Etag
            | None -> failwith $"The {nameof KeyValueStore} couldn't find anything with the key '{key}'. The Type is '{typeof<'a>.FullName}'."
        }
    
    member this.Update update (rowKey: obj) = async {
        let! x, etag = rowKey |> this.Get
        return! keyValueStore.Replace (x |> update) (string etag)
        }
    member this.GetExample (exampleId: string) =
        this.Get<Kvs.Example> exampleId
    member this.GetExample (exampleId: ExampleId) =
        exampleId.ToString() |> this.GetExample
    member this.GetExample_ (exampleId: ExampleId) =
        exampleId |> this.GetExample |>% fst
    member this.GetConcept (exampleId: string) =
        exampleId |> Concept.ProjectionId |> this.Get<Concept>
    member this.GetConcept (exampleId: ExampleId) =
        exampleId.ToString() |> this.GetConcept
    member this.GetConcept_ (exampleId: ExampleId) =
        exampleId |> this.GetConcept |>% fst
    
    member this.GetUser (userId: string) =
        this.Get<User> userId
    member this.GetUser (userId: UserId) =
        userId.ToString() |> this.GetUser
    member this.GetUser_ (userId: UserId) =
        userId |> this.GetUser |>% fst
    member this.GetProfile (userId: string) = // medTODO needs security - also check on deck visibility
        userId |> Kvs.Profile.ProjectionId |> this.Get<Kvs.Profile>
    member this.GetProfile (profileId: UserId) =
        profileId.ToString() |> this.GetProfile
    member this.GetProfile_ (profileId: UserId) =
        profileId |> this.GetProfile |>% fst

    member this.GetDeck (deckId: string) =
        this.Get<Deck.Fold.State> deckId
    member this.GetDeck (deckId: DeckId) =
        deckId.ToString() |> this.GetDeck
    member this.GetDeck_ (deckId: DeckId) =
        deckId |> this.GetDeck |>% fst
    member this.GetDecks (deckIds: DeckId list) =
        deckIds
        |> List.map this.GetDeck
        |> Async.Parallel
        
    member this.GetDeckSummary_ (deckId: string) =
        deckId |> this.GetDeck |>% fst |>% Deck.getActive |>% Result.map Projection.Kvs.Deck.fromSummary
    member this.GetDeckSummary_ (deckId: DeckId) =
        deckId.ToString() |> this.GetDeckSummary_

    member this.GetTemplate (templateId: string) =
        this.Get<Kvs.Template> templateId
    member this.GetTemplate (templateId: TemplateId) =
        templateId.ToString() |> this.GetTemplate
    member this.GetTemplate_ (templateId: TemplateId) =
        templateId |> this.GetTemplate |>% fst
    member this.GetTemplateInstance (templateRevisionId: TemplateRevisionId) =
        this.GetTemplate (fst templateRevisionId)
        |>% mapFst (Kvs.toTemplateInstance templateRevisionId)
    member this.GetTemplateInstance_ (templateRevisionId: TemplateRevisionId) =
        templateRevisionId
        |> this.GetTemplateInstance
        |>% fst
    member this.GetTemplates (templateIds: TemplateId list) =
        templateIds
        |> List.map this.GetTemplate
        |> Async.Parallel

    member this.GetStack (stackId: string) =
        this.Get<Stack> stackId
    member this.GetStack (stackId: StackId) =
        stackId.ToString() |> this.GetStack
    member this.GetStack_ (stackId: StackId) =
        stackId |> this.GetStack |>% fst

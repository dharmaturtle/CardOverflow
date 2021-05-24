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
        | :?            Stack as x -> string x.Id                , string x.Id
        | :?          Example as x -> string x.Id                , string x.Id
        | :?  ExampleInstance as x -> ExampleRevisionId.ser x.Id , ExampleRevisionId.ser x.Id
        | :?             User as x -> string x.Id                , string x.Id
        | :?             Deck as x -> string x.Id                , string x.Id
        | :?     Kvs.Template as x -> string x.Id                , string x.Id
        | _ -> failwith $"The type '{summary.GetType().FullName}' has not yet registered a PartitionKey or RowKey."

    let wrap payload =
        let partition, row = getPartitionRow payload
        let payload = JsonConvert.SerializeObject(payload, Infrastructure.jsonSerializerSettings) |> encoding.GetBytes
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
    abstract InsertOrReplace: 'a -> Async<OperationResult>
    abstract Delete    : key: obj -> Async<unit>
    abstract PointQuery: key: obj -> Async<seq<AzureTableStorageWrapper * EntityMetadata>>

#if DEBUG // it could be argued that test stuff should only be in test assemblies, but I'm gonna put stuff that's tightly coupled together. Easier to make changes.
type TableMemoryClient() =
    let dict = new System.Collections.Generic.Dictionary<(string * string), AzureTableStorageWrapper>()
    interface IKeyValueStore with
        member _.InsertOrReplace summary =
            let value = summary |> AzureTableStorage.wrap
            let key = value.Partition, value.Partition
            dict.Remove key |> ignore
            dict.Add(key, value)
            {   HttpStatusCode = 0
                Etag = ""
            } |> Async.singleton
        member _.Delete (key: obj) =
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
        member _.InsertOrReplace summary =
            summary |> AzureTableStorage.wrap |> InsertOrReplace |> inTable
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
    member _.InsertOrReplace x = keyValueStore.InsertOrReplace x
    member _.Exists (key: obj) = // medTODO this needs to make sure it's in the Active state (could be just deleted or whatever). Actually... strongly consider deleting this entirely, and replacing it with more domain specific methods like TryGetDeck so you can check Visibility and Active state
        keyValueStore.PointQuery key
        |>% (Seq.isEmpty >> not)

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
            ] |> fun x -> JsonConvert.DeserializeObject<'a> (x, Infrastructure.jsonSerializerSettings), m
        )

    member this.Get<'a> (key: obj) = async {
        let! x = this.TryGet<'a> key
        return
            match x with
            | Some (a, _) -> a
            | None -> failwith $"The {nameof KeyValueStore} couldn't find anything with the key '{key}'. The Type is '{typeof<'a>.FullName}'."
        }
    
    member this.Update update (rowKey: obj) =
        rowKey
        |> this.Get
        |>% update
        |>! keyValueStore.InsertOrReplace
        |>% ignore
    member this.UpsertExample' (exampleId: string) e =
        match e with
        | Example.Events.Created created -> async {
            let! templateInstance = this.GetTemplateInstance created.TemplateRevisionId
            let example = Example.Fold.evolveCreated created
            return!
                [ keyValueStore.InsertOrReplace (Projection.toExampleInstance templateInstance example)
                  keyValueStore.InsertOrReplace example
                ] |> Async.Parallel |>% ignore
            }
        | Example.Events.Edited e -> async {
            let! summary = this.GetExample exampleId
            let summary = Example.Fold.evolveEdited e summary
            let! templateInstance = this.GetTemplateInstance summary.CurrentRevision.TemplateRevisionId
            return!
                [ keyValueStore.InsertOrReplace summary
                  keyValueStore.InsertOrReplace (Projection.toExampleInstance templateInstance summary)
                ] |> Async.Parallel |>% ignore
            }
    member this.UpsertExample (exampleId: ExampleId) =
        exampleId.ToString() |> this.UpsertExample'
    member this.GetExample (exampleId: string) =
        this.Get<Example> exampleId
    member this.GetExample (exampleId: ExampleId) =
        exampleId.ToString() |> this.GetExample
    member this.GetExampleInstance (exampleRevisionId: string) =
        this.Get<ExampleInstance> exampleRevisionId
    member this.GetExampleInstance (exampleRevisionId: ExampleRevisionId) =
        exampleRevisionId |> ExampleRevisionId.ser |> this.GetExampleInstance
    
    member this.UpsertUser' (userId: string) e =
        match e with
        | User.Events.SignedUp signedUp ->
            signedUp |> User.Fold.evolveSignedUp |> keyValueStore.InsertOrReplace |>% ignore
        | User.Events.OptionsEdited o ->
            this.Update (User.Fold.evolveOptionsEdited o) userId
        | User.Events.CollectedTemplatesEdited o ->
            this.Update (User.Fold.evolveCollectedTemplatesEdited o) userId
        | User.Events.CardSettingsEdited cs ->
            this.Update (User.Fold.evolveCardSettingsEdited cs) userId
        | User.Events.DeckFollowed d ->
            this.Update (User.Fold.evolveDeckFollowed d) userId
        | User.Events.DeckUnfollowed d ->
            this.Update (User.Fold.evolveDeckUnfollowed d) userId
    member this.UpsertUser (userId: UserId) =
        userId.ToString() |> this.UpsertUser'
    member this.GetUser (userId: string) =
        this.Get<User> userId
    member this.GetUser (userId: UserId) =
        userId.ToString() |> this.GetUser

    member this.UpsertDeck' (deckId: string) e =
        match e with
        | Deck.Events.Created summary ->
            summary |> Deck.Fold.evolveCreated |> keyValueStore.InsertOrReplace |>% ignore
        | Deck.Events.Edited e ->
            this.Update (Deck.Fold.evolveEdited e) deckId
    member this.UpsertDeck (deckId: DeckId) =
        deckId.ToString() |> this.UpsertDeck'
    member this.GetDeck (deckId: string) =
        this.Get<Deck> deckId
    member this.GetDeck (deckId: DeckId) =
        deckId.ToString() |> this.GetDeck
    member this.TryGetDeck (deckId: DeckId) =
        deckId |> string |> this.TryGet<Deck> |> AsyncOption.map fst

    member this.GetTemplate (templateId: string) =
        this.Get<Kvs.Template> templateId
    member this.GetTemplate (templateId: TemplateId) =
        templateId.ToString() |> this.GetTemplate
    member this.GetTemplateInstance (templateRevisionId: TemplateRevisionId) =
        this.GetTemplate (fst templateRevisionId)
        |>% Kvs.toTemplateInstance templateRevisionId
    member this.GetTemplateInstances (templateRevisionIds: TemplateRevisionId seq) =
        templateRevisionIds
        |> Seq.map this.GetTemplateInstance
        |> Async.Parallel

    member this.UpsertStack' (stackId: string) e =
        match e with
        | Stack.Events.Created created ->
            created |> Stack.Fold.evolveCreated |> keyValueStore.InsertOrReplace |>% ignore
        | Stack.Events.Discarded _ ->
            keyValueStore.Delete stackId
        | Stack.Events.TagsChanged e ->
            this.Update (Stack.Fold.evolveTagsChanged e) stackId
        | Stack.Events.CardStateChanged e ->
            this.Update (Stack.Fold.evolveCardStateChanged e) stackId
        | Stack.Events.RevisionChanged e ->
            this.Update (Stack.Fold.evolveRevisionChanged e) stackId
    member this.UpsertStack (stackId: StackId) =
        stackId.ToString() |> this.UpsertStack'
    member this.GetStack (stackId: string) =
        this.Get<Stack> stackId
    member this.GetStack (stackId: StackId) =
        stackId.ToString() |> this.GetStack

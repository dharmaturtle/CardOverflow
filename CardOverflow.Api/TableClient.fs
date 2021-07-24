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
    member _.Delete          x = keyValueStore.Delete          x
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
    member this.GetExample (exampleId: string) =
        this.Get<Kvs.Example> exampleId
    member this.GetExample (exampleId: ExampleId) =
        exampleId.ToString() |> this.GetExample
    member this.GetConcept (exampleId: string) =
        exampleId |> Concept.ProjectionId |> this.Get<Concept>
    member this.GetConcept (exampleId: ExampleId) =
        exampleId.ToString() |> this.GetConcept
    
    member this.GetUser (userId: string) =
        this.Get<User> userId
    member this.GetUser (userId: UserId) =
        userId.ToString() |> this.GetUser
    member this.GetProfile (userId: string) = // medTODO needs security - also check on deck visibility
        userId |> Kvs.Profile.ProjectionId |> this.Get<Kvs.Profile>
    member this.GetProfile (profileId: UserId) =
        profileId.ToString() |> this.GetProfile

    member this.GetDeck (deckId: string) =
        this.Get<Deck.Fold.State> deckId
    member this.GetDeck (deckId: DeckId) =
        deckId.ToString() |> this.GetDeck
    member this.GetDecks (deckIds: DeckId list) =
        deckIds
        |> List.map this.GetDeck
        |> Async.Parallel
        
    member this.GetDeckSummary (deckId: string) =
        deckId |> this.GetDeck |>% Deck.getActive |>% Result.map Projection.Kvs.Deck.fromSummary
    member this.GetDeckSummary (deckId: DeckId) =
        deckId.ToString() |> this.GetDeckSummary

    member this.GetTemplate (templateId: string) =
        this.Get<Kvs.Template> templateId
    member this.GetTemplate (templateId: TemplateId) =
        templateId.ToString() |> this.GetTemplate
    member this.GetTemplateInstance (templateRevisionId: TemplateRevisionId) =
        this.GetTemplate (fst templateRevisionId)
        |>% Kvs.toTemplateInstance templateRevisionId
    member this.GetTemplates (templateIds: TemplateId list) =
        templateIds
        |> List.map this.GetTemplate
        |> Async.Parallel

    member this.GetStack (stackId: string) =
        this.Get<Stack> stackId
    member this.GetStack (stackId: StackId) =
        stackId.ToString() |> this.GetStack

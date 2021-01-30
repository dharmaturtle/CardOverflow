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

type TableClient(connectionString, tableName) =
    let account = CloudStorageAccount.Parse connectionString
    let tableClient = account.CreateCloudTableClient()
    let inTable   = inTableAsync   tableClient tableName
    let fromTable = fromTableAsync tableClient tableName
    let encoding = System.Text.UnicodeEncoding() // this is UTF16 https://docs.microsoft.com/en-us/dotnet/api/system.text.unicodeencoding?view=net-5.0
    
    let getPartitionRow (snapshot: obj) =
        match snapshot with
        | :? Domain.Stack .Events.Snapshot as x -> string x.Id, string x.Id
        | :? Domain.Branch.Events.Snapshot as x -> string x.Id, string x.Id
        | :? Domain.User  .Events.Snapshot as x -> string x.Id, string x.Id
        | _ -> failwith $"The type '{snapshot.GetType().FullName}' has not yet registered a PartitionKey or RowKey."

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

    member _.CloudTableClient = tableClient
    member _.TableName = tableName
    
    member _.InsertOrReplace snapshot =
        snapshot |> wrap |> InsertOrReplace |> inTable

    member _.Get<'a> (key: obj) = // point query https://docs.microsoft.com/en-us/azure/storage/tables/table-storage-design-for-query#how-your-choice-of-partitionkey-and-rowkey-impacts-query-performance:~:text=Point%20Query,-is
        Query.all<AzureTableStorageWrapper>
        |> Query.where <@ fun _ s -> s.PartitionKey = string key && s.RowKey = string key @>
        |> fromTable
        |>% Seq.exactlyOne
        |>% fun (x, m) ->
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
    
    member this.Update update (rowKey: obj) =
        rowKey
        |> this.Get
        |>% fst
        |>% update
        |>! this.InsertOrReplace
        |>% ignore
    member this.UpsertStack' (stackId: string) e =
        match e with
        | Stack.Events.Snapshot snapshot ->
            this.InsertOrReplace snapshot |>% ignore
        | Stack.Events.DefaultBranchChanged b ->
            this.Update (fun (x:Stack.Events.Snapshot) ->
                { x with DefaultBranchId = b.BranchId }
            ) stackId |>% ignore
    member this.UpsertStack (stackId: StackId) =
        stackId.ToString() |> this.UpsertStack'
    member this.GetStack (stackId: string) =
        this.Get<Stack.Events.Snapshot> stackId
    member this.GetStack (stackId: StackId) =
        stackId.ToString() |> this.GetStack
    member this.UpsertBranch' (branchId: string) e =
        match e with
        | Branch.Events.Snapshot snapshot ->
            this.InsertOrReplace snapshot |>% ignore
        | Branch.Events.Edited
            { LeafId      = leafId
              Title       = title
              GrompleafId = grompleafId
              FieldValues = fieldValues
              EditSummary = editSummary } ->
            this.Update(
                fun (x: Branch.Events.Snapshot) ->
                    { x with
                        LeafId       = leafId
                        Title        = title
                        GrompleafId  = grompleafId
                        FieldValues  = fieldValues
                        EditSummary  = editSummary
                    }
            ) branchId
    member this.UpsertBranch (branchId: BranchId) =
        branchId.ToString() |> this.UpsertBranch'
    member this.GetBranch (branchId: string) =
        this.Get<Branch.Events.Snapshot> branchId
    member this.GetBranch (branchId: BranchId) =
        branchId.ToString() |> this.GetBranch
    
    member this.UpsertUser' (userId: string) e =
        match e with
        | User.Events.Snapshot snapshot ->
            this.InsertOrReplace snapshot |>% ignore
        | User.Events.OptionsEdited o ->
            this.Update (User.Fold.evolveOptionsEdited o) userId
        | User.Events.CardSettingsEdited cs ->
            this.Update (User.Fold.evolveCardSettingsEdited cs) userId
    member this.UpsertUser (userId: UserId) =
        userId.ToString() |> this.UpsertUser'
    member this.GetUser (userId: string) =
        this.Get<User.Events.Snapshot> userId
    member this.GetUser (userId: UserId) =
        userId.ToString() |> this.GetUser

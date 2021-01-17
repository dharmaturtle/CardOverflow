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
      Payload: string }

type TableClient(connectionString, tableName) =
    let account = CloudStorageAccount.Parse connectionString
    let tableClient = account.CreateCloudTableClient()
    let inTable   = inTableAsync   tableClient tableName
    let fromTable = fromTableAsync tableClient tableName
    
    let getPartitionRow (snapshot: obj) =
        match snapshot with
        | :? Domain.Stack .Events.Snapshotted as x -> string x.AuthorId, string x.Id
        | :? Domain.Branch.Events.Snapshotted as x -> string x.AuthorId, string x.Id
        | _ -> failwith $"The type '{snapshot.GetType().FullName}' has not yet registered a PartitionKey or RowKey."

    let wrap payload =
        let partition, row = getPartitionRow payload
        let payload = JsonConvert.SerializeObject payload
        if payload.Length >= 32_767 then failwith "Serialized payload is too large for Azure Table Storage."
        { Partition = partition
          Row = row
          Payload = payload }

    member _.CloudTableClient = tableClient
    member _.TableName = tableName
    
    member _.InsertOrReplace snapshot =
        snapshot |> wrap |> InsertOrReplace |> inTable

    member _.Get<'a> (rowKey: obj) =
        Query.all<AzureTableStorageWrapper>
        |> Query.where <@ fun _ s -> s.RowKey = string rowKey @>
        |> fromTable
        |>% Seq.exactlyOne
        |>% fun (x, m) -> JsonConvert.DeserializeObject<'a> x.Payload, m
    
    member this.Update update (rowKey: obj) =
        rowKey
        |> this.Get
        |>% fst
        |>% update
        |>! this.InsertOrReplace
        |>% ignore
    member this.UpsertStack' (stackId: string) e =
        match e with
        | Stack.Events.Snapshotted snapshot ->
            this.InsertOrReplace snapshot |>% ignore
        | Stack.Events.DefaultBranchChanged b ->
            this.Update (fun (x:Stack.Shot) ->
                { x with DefaultBranchId = b.BranchId }
            ) stackId |>% ignore
    member this.UpsertStack (stackId: StackId) =
        stackId.ToString() |> this.UpsertStack'
    member this.GetStack (stackId: string) =
        this.Get<Stack.Shot> stackId
    member this.GetStack (stackId: StackId) =
        stackId.ToString() |> this.GetStack
    member this.UpsertBranch' (branchId: string) e =
        match e with
        | Branch.Events.Snapshotted snapshot ->
            this.InsertOrReplace snapshot |>% ignore
        | Branch.Events.Edited
            { LeafId      = leafId
              Title       = title
              GrompleafId = grompleafId
              FieldValues = fieldValues
              EditSummary = editSummary } ->
            this.Update(
                fun (x: Branch.Shot) ->
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

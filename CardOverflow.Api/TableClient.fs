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
    
    let getPartitionRow (summary: obj) =
        match summary with
        | :? Domain.Concept  .Events.Summary as x -> string x.Id, string x.Id
        | :? Domain.Stack    .Events.Summary as x -> string x.Id, string x.Id
        | :? Domain.Example  .Events.Summary as x -> string x.Id, string x.Id
        | :? Domain.Example .RevisionSummary as x -> string x.Id, string x.Id
        | :? Domain.User     .Events.Summary as x -> string x.Id, string x.Id
        | :? Domain.Deck     .Events.Summary as x -> string x.Id, string x.Id
        | :? Domain.Template .Events.Summary as x -> string x.Id, string x.Id
        | :? Domain.Template.RevisionSummary as x -> string x.Id, string x.Id
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

    member _.CloudTableClient = tableClient
    member _.TableName = tableName
    
    member _.InsertOrReplace summary =
        summary |> wrap |> InsertOrReplace |> inTable

    member private _.PointQuery (key: obj) = // point query https://docs.microsoft.com/en-us/azure/storage/tables/table-storage-design-for-query#how-your-choice-of-partitionkey-and-rowkey-impacts-query-performance:~:text=Point%20Query,-is
        Query.all<AzureTableStorageWrapper>
        |> Query.where <@ fun _ s -> s.PartitionKey = string key && s.RowKey = string key @>
        |> fromTable

    member this.Exists (key: obj) = // medTODO this needs to make sure it's in the Active state (could be just deleted or whatever)
        this.PointQuery key
        |>% (Seq.isEmpty >> not)

    member this.TryGet<'a> (key: obj) =
        this.PointQuery key
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

    member this.Get<'a> (key: obj) =
        this.TryGet<'a> key |>% Option.get
    
    member this.Update update (rowKey: obj) =
        rowKey
        |> this.Get
        |>% fst
        |>% update
        |>! this.InsertOrReplace
        |>% ignore
    member this.UpsertConcept' (conceptId: string) e =
        match e with
        | Concept.Events.Created summary ->
            this.InsertOrReplace summary |>% ignore
        | Concept.Events.DefaultExampleChanged b ->
            this.Update (fun (x:Concept.Events.Summary) ->
                { x with DefaultExampleId = b.ExampleId }
            ) conceptId |>% ignore
    member this.UpsertConcept (conceptId: ConceptId) =
        conceptId.ToString() |> this.UpsertConcept'
    member this.GetConcept (conceptId: string) =
        this.Get<Concept.Events.Summary> conceptId
    member this.GetConcept (conceptId: ConceptId) =
        conceptId.ToString() |> this.GetConcept
    member this.UpsertExample' (exampleId: string) e =
        match e with
        | Example.Events.Created summary -> async {
            let! templateRevision, _ = this.GetTemplateRevision summary.TemplateRevisionId
            return!
                [ this.InsertOrReplace (Example.toRevisionSummary templateRevision summary)
                  this.InsertOrReplace summary
                ] |> Async.Parallel |>% ignore
            }
        | Example.Events.Edited e -> async {
            let! summary, _ = this.GetExample exampleId
            let summary = Example.Fold.evolveEdited e summary
            let! templateRevision, _ = this.GetTemplateRevision summary.TemplateRevisionId
            return!
                [ this.InsertOrReplace summary
                  this.InsertOrReplace (Example.toRevisionSummary templateRevision summary)
                ] |> Async.Parallel |>% ignore
            }
    member this.UpsertExample (exampleId: ExampleId) =
        exampleId.ToString() |> this.UpsertExample'
    member this.GetExample (exampleId: string) =
        this.Get<Example.Events.Summary> exampleId
    member this.GetExample (exampleId: ExampleId) =
        exampleId.ToString() |> this.GetExample
    member this.GetExampleRevision (exampleRevisionId: string) =
        this.Get<Example.RevisionSummary> exampleRevisionId
    member this.GetExampleRevision (exampleRevisionId: RevisionId) =
        exampleRevisionId.ToString() |> this.GetExampleRevision
    
    member this.UpsertUser' (userId: string) e =
        match e with
        | User.Events.Created summary ->
            this.InsertOrReplace summary |>% ignore
        | User.Events.OptionsEdited o ->
            this.Update (User.Fold.evolveOptionsEdited o) userId
        | User.Events.CardSettingsEdited cs ->
            this.Update (User.Fold.evolveCardSettingsEdited cs) userId
        | User.Events.DeckFollowed d ->
            this.Update (User.Fold.evolveDeckFollowed d) userId
        | User.Events.DeckUnfollowed d ->
            this.Update (User.Fold.evolveDeckUnfollowed d) userId
    member this.UpsertUser (userId: UserId) =
        userId.ToString() |> this.UpsertUser'
    member this.GetUser (userId: string) =
        this.Get<User.Events.Summary> userId
    member this.GetUser (userId: UserId) =
        userId.ToString() |> this.GetUser

    member this.UpsertDeck' (deckId: string) e =
        match e with
        | Deck.Events.Created summary ->
            this.InsertOrReplace summary |>% ignore
        | Deck.Events.Edited e ->
            this.Update (Deck.Fold.evolveEdited e) deckId
    member this.UpsertDeck (deckId: DeckId) =
        deckId.ToString() |> this.UpsertDeck'
    member this.GetDeck (deckId: string) =
        this.Get<Deck.Events.Summary> deckId
    member this.GetDeck (deckId: DeckId) =
        deckId.ToString() |> this.GetDeck

    member this.UpsertTemplate' (templateId: string) e =
        match e with
        | Template.Events.Created summary ->
            [ this.InsertOrReplace (Template.toRevisionSummary summary)
              this.InsertOrReplace summary
            ] |> Async.Parallel |>% ignore
        | Template.Events.Edited e -> async {
            let! summary, _ = this.GetTemplate templateId
            let summary = Template.Fold.evolveEdited e summary
            return!
                [ this.InsertOrReplace summary
                  this.InsertOrReplace (Template.toRevisionSummary summary)
                ] |> Async.Parallel |>% ignore
            }
    member this.UpsertTemplate (templateId: TemplateId) =
        templateId.ToString() |> this.UpsertTemplate'
    member this.GetTemplate (templateId: string) =
        this.Get<Template.Events.Summary> templateId
    member this.GetTemplate (templateId: TemplateId) =
        templateId.ToString() |> this.GetTemplate
    member this.GetTemplateRevision (templateRevisionId: string) =
        this.Get<Template.RevisionSummary> templateRevisionId
    member this.GetTemplateRevision (templateRevisionId: TemplateRevisionId) =
        templateRevisionId.ToString() |> this.GetTemplateRevision

    member this.UpsertStack' (stackId: string) e =
        match e with
        | Stack.Events.Created summary ->
            this.InsertOrReplace summary |>% ignore
        | Stack.Events.TagsChanged e ->
            this.Update (Stack.Fold.evolveTagsChanged e) stackId
    member this.UpsertStack (stackId: StackId) =
        stackId.ToString() |> this.UpsertStack'
    member this.GetStack (stackId: string) =
        this.Get<Stack.Events.Summary> stackId
    member this.GetStack (stackId: StackId) =
        stackId.ToString() |> this.GetStack

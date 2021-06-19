module FacetRepositoryTests

open LoadersAndCopiers
open Helpers
open CardOverflow.Api
open CardOverflow.Debug
open CardOverflow.Entity
open Microsoft.EntityFrameworkCore
open CardOverflow.Test
open System
open System.Linq
open Xunit
open CardOverflow.Pure
open System.Collections.Generic
open FSharp.Control.Tasks
open System.Threading.Tasks
open CardOverflow.Sanitation
open FsToolkit.ErrorHandling

let normalCommand fieldValues templateRevision tagIds ids =
    let fieldValues =
        match fieldValues with
        | [] -> ["Front"; "Back"]
        | _ -> fieldValues
    {   TemplateRevision = templateRevision
        FieldValues =
            templateRevision.Fields
            |> Seq.mapi (fun i field -> {
                EditField = field
                Value = fieldValues.[i]
            }) |> toResizeArray
        EditSummary = "Initial creation"
        Kind = NewOriginal_TagIds (tagIds |> Set.ofList)
        Title = null
        Ids = UpsertIds.fromTuple ids
    }

let add templateName createCommand (db: CardOverflowDb) userId tags (ids: Guid * Guid * Guid * Guid list) = taskResult {
    let! template = TestTemplateRepo.SearchEarliest db templateName
    return!
        createCommand template tags ids
        |> SanitizeConceptRepository.Update db userId []
    }

let reversedBasicTemplate db =
    TestTemplateRepo.SearchEarliest db "Basic (and reversed card)"

let basicTemplate db =
    TestTemplateRepo.SearchEarliest db "Basic"

let setCardIds (command: ViewEditConceptCommand) cardIds =
    { command with Ids = { command.Ids with CardIds = cardIds } }

[<Fact>]
let ``Revision with "" as FieldValues is parsed to empty`` (): unit =
    let view =
        RevisionEntity(
            FieldValues = "",
            TemplateRevision = TemplateRevisionEntity(
                Fields = "FrontArial20False0FalseBackArial20False1False"
            ))
        |> RevisionView.load

    Assert.Empty view.FieldValues

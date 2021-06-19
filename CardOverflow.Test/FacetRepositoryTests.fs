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

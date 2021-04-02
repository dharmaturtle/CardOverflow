module Domain.Projection

open FsCodec
open FsCodec.NewtonsoftJson
open NodaTime
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling

[<CLIMutable>]
type ExampleSearch =
    { Id: ExampleId
      ParentId: ExampleId option
      RevisionId: RevisionId
      Title: string
      AuthorId: UserId
      Author: string
      TemplateRevision: Template.RevisionSummary
      FieldValues: Map<string, string>
      EditSummary: string }

module ExampleSearch =
    let fromSummary (summary: Example.Events.Summary) displayName templateRevision =
        { Id = summary.Id
          ParentId = summary.ParentId
          RevisionId = summary.RevisionIds.Head
          Title = summary.Title
          AuthorId = summary.AuthorId
          Author = displayName
          TemplateRevision = templateRevision
          FieldValues = summary.FieldValues
          EditSummary = summary.EditSummary }
    let fromEdited (summary: ExampleSearch) (edited: Example.Events.Edited) templateRevision =
        { summary with
            RevisionId = edited.RevisionId
            Title = edited.Title
            TemplateRevision = templateRevision
            FieldValues = edited.FieldValues
            EditSummary = edited.EditSummary }

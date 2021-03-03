namespace CardOverflow.Pure

open CardOverflow.Debug
open System.Linq
open System
open Microsoft.FSharp.Core.Operators.Checked
open System.ComponentModel.DataAnnotations
open CardOverflow.Debug
open System.Text.RegularExpressions
open NodaTime
open Domain

type EditConceptCommand = {
    EditSummary: string
    FieldValues: EditFieldAndValue ResizeArray
    TemplateRevisionId: TemplateRevisionId
    Kind: UpsertKind
    Ids: UpsertIds
} with
    member this.CardView templateRevision = {   
        FieldValues =
            this.FieldValues.Select(fun x ->
                {   Field = x.EditField
                    Value =  x.Value
                }).ToList()
        TemplateRevision = templateRevision }
    member this.MaxIndexInclusive templateRevision =
        Helper.maxIndexInclusive
            (templateRevision.CardTemplates)
            (this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> Map.ofSeq) // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls

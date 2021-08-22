module Domain.Projection

open FsCodec
open FsCodec.NewtonsoftJson
open NodaTime
open TypeShape
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open Domain.Summary
open System
open AsyncOp

module ToUrl =
    let delimiter = '/'
    let example   ((exampleId, ordinal): ExampleRevisionId)  =  $"{exampleId}{delimiter}{ordinal}"
    let template ((templateId, ordinal): TemplateRevisionId) = $"{templateId}{delimiter}{ordinal}"
    let raw              ((id, ordinal): Guid * int)         =         $"{id}{delimiter}{ordinal}"
    let parse (input: string) =
        let (|Int|_|) (str:string) =
            match Int32.TryParse str with
            | true, int -> Some int
            | _ -> None
        let (|Guid|_|) (str:string) =
            match Guid.TryParse str with
            | true, guid -> Some guid
            | _ -> None
        let guid_int = String.split delimiter input
        if guid_int.Length = 2 then option {
            let! g =
                match guid_int.[0] with
                | Guid x -> Some x
                | _      -> None
            let! i =
                match guid_int.[1] with
                | Int x -> Some x
                | _      -> None
            return g, i
            }
        else None

open System.Linq
type TemplateInstance =
    { Ordinal: TemplateRevisionOrdinal
      TemplateId: TemplateId
      AuthorId: UserId
      Name: string
      Css: string
      Fields: Field list
      Meta: Meta
      LatexPre: string
      LatexPost: string
      CardTemplates: TemplateType
      EditSummary: string }
with
    member this.Id = this.TemplateId, this.Ordinal
    member this.FirstCardTemplate =
        match this.CardTemplates with
        | Cloze t -> t
        | Standard ts -> ts.[0]
    member this.JustCardTemplates =
        match this.CardTemplates with
        | Cloze t -> [t]
        | Standard ts -> ts
    member this.FrontBackFrontSynthBackSynth () = // medTODO split this up
        let fieldNameValueMap = this.Fields.Select(fun x -> x.Name, x.Name + " field") |> Seq.toList
        match this.CardTemplates with
        | Standard ts -> 
            ts.Select(fun t ->
                CardHtml.generate fieldNameValueMap t.Front t.Back this.Css CardHtml.Standard
            ).ToList()
        | Cloze t ->
            CardHtml.generate fieldNameValueMap t.Front t.Back this.Css (CardHtml.Cloze 0s)
            |> List.singleton |> toResizeArray

let toTemplateInstance o (t: Template) =
    let r = t.Revisions |> List.filter (fun x -> x.Ordinal = o) |> List.exactlyOne
    { Ordinal          = r.Ordinal
      TemplateId       = t.Id
      AuthorId         = t.AuthorId
      Name             = r.Name
      Css              = r.Css
      Fields           = r.Fields
      Meta             = r.Meta
      LatexPre         = r.LatexPre
      LatexPost        = r.LatexPost
      CardTemplates    = r.CardTemplates
      EditSummary      = r.EditSummary }

let toCurrentTemplateInstance (t: Template) =
    toTemplateInstance t.CurrentRevision.Ordinal t

let toTemplateRevision (i: TemplateInstance) : Domain.Summary.TemplateRevision =
    { Ordinal          = i.Ordinal
      Name             = i.Name
      Css              = i.Css
      Fields           = i.Fields
      Meta             = i.Meta
      LatexPre         = i.LatexPre
      LatexPost        = i.LatexPost
      CardTemplates    = i.CardTemplates
      EditSummary      = i.EditSummary }

type DeckSearch =
    { CommandIds: CommandId Set
      Id: DeckId
      IsDefault: bool
      SourceId: DeckId Option
      AuthorId: UserId
      Name: string
      Description: string
      ServerCreated: Instant
      ServerModified: Instant
      Visibility: Visibility
      
      Author: string
      ExampleCount: int
      SourceOf: int }
module DeckSearch =
    open Deck
    let n = Unchecked.defaultof<DeckSearch>
    let fromSummary (author: string) (exampleCount: int) (sourceOf: int) (deck: Deck) =
        [ nameof n.CommandIds     , deck.CommandIds     |> box
          nameof n.Id             , deck.Id             |> box
          nameof n.IsDefault      , deck.IsDefault      |> box
          nameof n.SourceId       , deck.SourceId       |> box
          nameof n.AuthorId       , deck.AuthorId       |> box
          nameof n.Name           , deck.Name           |> box
          nameof n.Description    , deck.Description    |> box
          nameof n.ServerCreated  , deck.ServerCreated  |> box
          nameof n.ServerModified , deck.ServerModified |> box
          nameof n.Visibility     , deck.Visibility     |> box
          nameof n.Author         , author              |> box
          nameof n.ExampleCount   , exampleCount        |> box
          nameof n.SourceOf       , sourceOf            |> box
        ] |> Map.ofList
    let fromEdited (edited: Events.Edited) =
        [ nameof n.Name           , edited.Name                        |> box
          nameof n.Description    , edited.Description                 |> box
          nameof n.ServerModified , edited.Meta.ServerReceivedAt.Value |> box
        ] |> Map.ofList
    let fromSummary' author exampleCount sourceOf (summary: Summary.Deck) =
        { CommandIds     = summary.CommandIds
          Id             = summary.Id
          IsDefault      = summary.IsDefault
          SourceId       = summary.SourceId
          AuthorId       = summary.AuthorId
          Name           = summary.Name
          Description    = summary.Description
          ServerCreated  = summary.ServerCreated
          ServerModified = summary.ServerModified
          Visibility     = summary.Visibility
          Author         = author
          ExampleCount   = exampleCount
          SourceOf       = sourceOf }

[<RequireQualifiedAccess>]
module Kvs =
    type TemplateRevision =
        { Ordinal: TemplateRevisionOrdinal
          Name: string
          Css: string
          Fields: Field list
          Meta: Meta
          LatexPre: string
          LatexPost: string
          CardTemplates: TemplateType
          Collectors: int
          EditSummary: string }
    
    type Template =
        { Id: TemplateId
          CommandIds: CommandId Set
          AuthorId: UserId
          Author: string
          Revisions: TemplateRevision list
          Visibility: Visibility }
      with
        member this.Collectors        = this.Revisions |> List.sumBy (fun x -> x.Collectors)
        member this.CurrentRevision   = this.Revisions |> List.maxBy (fun x -> x.Ordinal)
        member this.CurrentRevisionId = this.Id, this.CurrentRevision.Ordinal
    
    let toKvsTemplate author collectorsByOrdinal (template: Summary.Template) =
        let toKvsTemplateRevision (revision: Summary.TemplateRevision) =
            { Ordinal          = revision.Ordinal
              Name             = revision.Name
              Css              = revision.Css
              Fields           = revision.Fields
              Meta             = revision.Meta
              LatexPre         = revision.LatexPre
              LatexPost        = revision.LatexPost
              CardTemplates    = revision.CardTemplates
              Collectors       = collectorsByOrdinal |> Map.tryFind revision.Ordinal |> Option.defaultValue 0
              EditSummary      = revision.EditSummary }
        { Id         = template.Id
          CommandIds = template.CommandIds
          AuthorId   = template.AuthorId
          Author     = author
          Revisions  = template.Revisions |> List.map toKvsTemplateRevision
          Visibility = template.Visibility }

    let toTemplate (template: Template) =
        let toTemplateRevision (revision: TemplateRevision) : Summary.TemplateRevision =
            { Ordinal          = revision.Ordinal
              Name             = revision.Name
              Css              = revision.Css
              Fields           = revision.Fields
              Meta             = revision.Meta
              LatexPre         = revision.LatexPre
              LatexPost        = revision.LatexPost
              CardTemplates    = revision.CardTemplates
              EditSummary      = revision.EditSummary }
        { Id         = template.Id
          CommandIds = template.CommandIds
          AuthorId   = template.AuthorId
          Revisions  = template.Revisions |> List.map toTemplateRevision
          Visibility = template.Visibility }
    
    let toTemplateInstance ((templateId, ordinal): TemplateRevisionId) (template: Template) =
        if templateId <> template.Id then failwith "You passed in the wrong Template."
        let tr = template.Revisions |> List.filter (fun x -> x.Ordinal = ordinal) |> List.exactlyOne
        { Ordinal          = tr.Ordinal
          TemplateId       = template.Id
          AuthorId         = template.AuthorId
          Name             = tr.Name
          Css              = tr.Css
          Fields           = tr.Fields
          Meta             = tr.Meta
          LatexPre         = tr.LatexPre
          LatexPost        = tr.LatexPost
          CardTemplates    = tr.CardTemplates
          EditSummary      = tr.EditSummary }
    
    let allToTemplateInstance (template: Template) =
        template.Revisions |> List.map (fun x -> toTemplateInstance (template.Id, x.Ordinal) template)

    let evolveKvsTemplateEdited (edited: Template.Events.Edited) (template: Template) =
        let collectorsByOrdinal = template.Revisions |> List.map (fun x -> x.Ordinal, x.Collectors) |> Map.ofList
        template |> toTemplate |> Template.Fold.evolveEdited edited |> toKvsTemplate template.Author collectorsByOrdinal // lowTODO needs fixing after multiple authors implemented
    
    type ExampleRevision =
        { Ordinal: ExampleRevisionOrdinal
          Title: string
          TemplateInstance: TemplateInstance
          FieldValues: EditFieldAndValue list
          Collectors: int
          Meta: Meta
          EditSummary: string }
    
    type Example =
        { Id: ExampleId
          CommandIds: CommandId Set
          ParentId: ExampleId option
          Revisions: ExampleRevision list
          AuthorId: UserId
          Author: string
          AnkiNoteId: int64 option
          Visibility: Visibility
          Comments: Comment list }
      with
        member this.CurrentRevision = this.Revisions |> List.maxBy (fun x -> x.Ordinal)
        member this.CurrentRevisionId = this.Id, this.CurrentRevision.Ordinal
        member this.Collectors = this.Revisions |> List.sumBy (fun x -> x.Collectors)
    
    let toKvsExample author collectorsByOrdinal (templateInstances: TemplateInstance list) (example: Summary.Example) =
        let toKvsExampleRevision (revision: Summary.ExampleRevision) =
            { Ordinal          = revision.Ordinal
              Title            = revision.Title
              TemplateInstance = templateInstances |> Seq.filter (fun x -> x.Id = revision.TemplateRevisionId) |> Seq.head
              FieldValues      = revision.FieldValues
              Collectors       = collectorsByOrdinal |> Map.tryFind revision.Ordinal |> Option.defaultValue 0
              Meta             = revision.Meta
              EditSummary      = revision.EditSummary }
        { Id         = example.Id
          CommandIds = example.CommandIds
          ParentId   = example.ParentId
          Revisions  = example.Revisions |> List.map toKvsExampleRevision
          AuthorId   = example.AuthorId
          Author     = author
          AnkiNoteId = example.AnkiNoteId
          Visibility = example.Visibility
          Comments   = example.Comments }
    
    let toExample (example: Example) =
        let toExampleRevision (revision: ExampleRevision) =
            { Ordinal            = revision.Ordinal
              Title              = revision.Title
              TemplateRevisionId = revision.TemplateInstance.Id
              FieldValues        = revision.FieldValues
              Meta               = revision.Meta
              EditSummary        = revision.EditSummary }
        { Id         = example.Id
          CommandIds = example.CommandIds
          ParentId   = example.ParentId
          Revisions  = example.Revisions |> List.map toExampleRevision
          AuthorId   = example.AuthorId
          AnkiNoteId = example.AnkiNoteId
          Visibility = example.Visibility
          Comments   = example.Comments }

    let evolveKvsExampleEdited (edited: Example.Events.Edited) templateInstances (example: Example) =
        if example.CommandIds.Contains edited.Meta.CommandId then
            example
        else
            let collectorsByOrdinal = example.Revisions |> List.map (fun x -> x.Ordinal, x.Collectors) |> Map.ofList
            example |> toExample |> Example.Fold.evolveEdited edited |> toKvsExample example.Author collectorsByOrdinal templateInstances // lowTODO needs fixing after multiple authors implemented
    
    let evolveKvsExampleCommentAdded (commentAdded: Example.Events.CommentAdded) (example: Example) =
        if example.CommandIds.Contains commentAdded.Meta.CommandId then
            example
        else
            let collectorsByOrdinal = example.Revisions |> List.map (fun x -> x.Ordinal, x.Collectors) |> Map.ofList
            let templateInstances = example.Revisions |> List.map (fun x -> x.TemplateInstance)
            example |> toExample |> Example.Fold.evolveCommentAdded commentAdded |> toKvsExample example.Author collectorsByOrdinal templateInstances // lowTODO needs fixing after multiple authors implemented
    
    let decrementIncrementExample ordinalDec ordinalInc commandId (example: Example) =
        if example.CommandIds.Contains commandId then
            example
        else
            let tryHandle (revision: ExampleRevision) =
                { revision with
                    Collectors =
                        if   revision.Ordinal = ordinalInc then revision.Collectors + 1
                        elif revision.Ordinal = ordinalDec then revision.Collectors - 1
                        else revision.Collectors }
            { example with
                CommandIds = example.CommandIds |> Set.add commandId
                Revisions = example.Revisions |> List.map tryHandle }
    
    let private tryDecrementIncrementExample<'T> ordinalDec ordinalInc commandId (f: _ -> 'T) (oldExample: Example) =
        let newExample = decrementIncrementExample ordinalDec ordinalInc commandId oldExample
        let o =
            if newExample = oldExample
            then None
            else Some newExample
        o, f newExample
    let    incrementExample     x =    decrementIncrementExample     Example.Fold.impossibleExampleRevisionOrdinal x
    let    decrementExample     x =    decrementIncrementExample     x Example.Fold.impossibleExampleRevisionOrdinal
    let tryIncrementExample<'T> x = tryDecrementIncrementExample<'T> Example.Fold.impossibleExampleRevisionOrdinal x
    let tryDecrementExample<'T> x = tryDecrementIncrementExample<'T> x Example.Fold.impossibleExampleRevisionOrdinal
    
    let decrementIncrementTemplate ordinalDec ordinalInc commandId (template: Template) =
        if template.CommandIds.Contains commandId then
            template
        else
            let tryHandle (revision: TemplateRevision) =
                { revision with
                    Collectors =
                        if   revision.Ordinal = ordinalInc then revision.Collectors + 1
                        elif revision.Ordinal = ordinalDec then revision.Collectors - 1
                        else revision.Collectors }
            { template with
                CommandIds = template.CommandIds |> Set.add commandId
                Revisions = template.Revisions |> List.map tryHandle }
    
    let private tryDecrementIncrementTemplate<'T> ordinalDec ordinalInc commandId (f: _ -> 'T) (oldTemplate: Template) =
        let newTemplate = decrementIncrementTemplate ordinalDec ordinalInc commandId oldTemplate
        let o =
            if newTemplate = oldTemplate
            then None
            else Some newTemplate
        o, f newTemplate
    let    incrementTemplate     x =    decrementIncrementTemplate     Template.Fold.impossibleTemplateRevisionOrdinal x
    let    decrementTemplate     x =    decrementIncrementTemplate     x Template.Fold.impossibleTemplateRevisionOrdinal
    let tryIncrementTemplate<'T> x = tryDecrementIncrementTemplate<'T> Template.Fold.impossibleTemplateRevisionOrdinal x
    let tryDecrementTemplate<'T> x = tryDecrementIncrementTemplate<'T> x Template.Fold.impossibleTemplateRevisionOrdinal
    
    type DeckExtra =
        { Author: string
          ExampleRevisionIds: ExampleRevisionId Set
          SourceOf: int }
      with
        static member init author =
            { Author = author
              ExampleRevisionIds = Set.empty
              SourceOf = 0 }
    type Deck =
        { CommandIds: CommandId Set
          Id: DeckId
          IsDefault: bool
          SourceId: DeckId Option
          AuthorId: UserId
          Name: string
          Description: string
          ServerCreated: Instant
          ServerModified: Instant
          Visibility: Visibility
          
          Author: string
          ExampleRevisionIds: ExampleRevisionId Set
          SourceOf: int }
      with
          static member fromSummary (deck: Summary.Deck) =
              let extra = deck.Extra |> deserializeFromJson<DeckExtra>
              { CommandIds         = deck.CommandIds
                Id                 = deck.Id
                IsDefault          = deck.IsDefault
                SourceId           = deck.SourceId
                AuthorId           = deck.AuthorId
                Name               = deck.Name
                Description        = deck.Description
                ServerCreated      = deck.ServerCreated
                ServerModified     = deck.ServerModified
                Visibility         = deck.Visibility
                Author             = extra.Author
                ExampleRevisionIds = extra.ExampleRevisionIds
                SourceOf           = extra.SourceOf }
    
    type Profile =
        { CommandIds: CommandId Set
          Id: UserId
          DisplayName: string
          Decks: DeckSearch Set }
      with
        static member ProjectionId (userId: UserId) = $"P.{userId}"
        static member ProjectionId (userId: string) = $"P.{userId}"
        static member fromSummary decks (summary: Summary.User) =
            { CommandIds  = summary.CommandIds
              Id          = summary.Id
              DisplayName = summary.DisplayName
              Decks       = decks }
    let deckProjectionId =
        function
        | Deck.Fold.Active  x -> x.Id
        | Deck.Fold.Discard x -> x.Id
        | Deck.Fold.Initial   -> failwith "impossible"
        >> string
    let decrementIncrementDeckChanged decDecks incDecks exampleRevisionId getDecks profile commandId = async {
        let profile =
            if profile.CommandIds.Contains commandId then
                profile
            else
                { profile with
                    CommandIds = profile.CommandIds |> Set.add commandId
                    Decks =
                        profile.Decks
                        |> Set.map (fun deck ->
                            if   incDecks |> Set.contains deck.Id then { deck with ExampleCount = deck.ExampleCount + 1 }
                            elif decDecks |> Set.contains deck.Id then { deck with ExampleCount = deck.ExampleCount - 1 }
                            else deck
                        )
                }
        let updateExampleRevisionIds setOperation =
            Array.map (mapFst (Deck.Fold.mapActive (fun deck ->
                { deck with
                    CommandIds = deck.CommandIds |> Set.add commandId
                    Extra = deck.Extra |> mapJson (fun extra -> { extra with
                                                                    Deck.ExampleRevisionIds = extra.ExampleRevisionIds |> setOperation exampleRevisionId })
                })))
        let! decDecks = decDecks |> Set.toList |> getDecks |>% updateExampleRevisionIds Set.remove
        let! incDecks = incDecks |> Set.toList |> getDecks |>% updateExampleRevisionIds Set.add
        return Array.append decDecks incDecks |> List.ofArray, profile
        }
    let incrementDeckChanged x = decrementIncrementDeckChanged Set.empty x
    let decrementDeckChanged x = decrementIncrementDeckChanged x Set.empty
    
type ExampleInstance =
    { Ordinal: ExampleRevisionOrdinal
      ExampleId: ExampleId
      Title: string
      AuthorId: UserId
      Template: TemplateInstance
      FieldValues: EditFieldAndValue list
      EditSummary: string }
  with
    member this.Id = this.ExampleId, this.Ordinal
    member this.FrontBackFrontSynthBackSynth (pointer: CardTemplatePointer) =
        match pointer with
        | CardTemplatePointer.Normal g ->
            match this.Template.CardTemplates with
            | Standard ts ->
                let t = ts.Single(fun x -> x.Id = g)
                CardHtml.generate
                <| (this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> Seq.toList)
                <| t.Front
                <| t.Back
                <| this.Template.Css
                <| CardHtml.Standard
            | _ -> failwith "Must generate a standard view for a standard template."
        | CardTemplatePointer.Cloze i ->
            match this.Template.CardTemplates with
            | Cloze c ->
                CardHtml.generate
                <| (this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> Seq.toList)
                <| c.Front
                <| c.Back
                <| this.Template.Css
                <| CardHtml.Cloze (int16 i)
            | _ -> failwith "Must generate a cloze view for a cloze template."
    member this.MaxIndexInclusive =
        Helper.maxIndexInclusive
            (this.Template.CardTemplates)
            (this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> Map.ofSeq) // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls
    member this.FrontBackFrontSynthBackSynthAll =
        match this.Template.CardTemplates with
        | Standard ts ->
            ts |> List.map (fun t ->
                CardHtml.generate
                <| (this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> Seq.toList)
                <| t.Front
                <| t.Back
                <| this.Template.Css
                <| CardHtml.Standard
            )
        | Cloze c ->
            [0s .. this.MaxIndexInclusive] |> List.map (fun i ->
                CardHtml.generate
                <| (this.FieldValues.Select(fun x -> x.EditField.Name, x.Value |?? lazy "") |> Seq.toList)
                <| c.Front
                <| c.Back
                <| this.Template.Css
                <| CardHtml.Cloze i
            )

open FSharp.Control.Tasks
open System.Threading.Tasks

let toExampleInstance (e: Example) ordinal (getTemplateInstance: Func<TemplateRevisionId, Task<TemplateInstance>>) = task {
    let r = e.Revisions |> List.filter (fun x -> x.Ordinal = ordinal) |> List.exactlyOne
    let! templateInstance = getTemplateInstance.Invoke r.TemplateRevisionId
    return
        { Ordinal          = r.Ordinal
          ExampleId        = e.Id
          Title            = r.Title
          AuthorId         = e.AuthorId
          Template         = templateInstance
          FieldValues      = r.FieldValues
          EditSummary      = r.EditSummary }
    }

let toCurrentExampleInstance e x =
    toExampleInstance e e.CurrentRevision.Ordinal x

[<CLIMutable>]
type ExampleSearch =
    { Id: ExampleId
      ParentId: ExampleId option
      CurrentOrdinal: ExampleRevisionOrdinal
      Title: string
      AuthorId: UserId
      Author: string
      TemplateInstance: TemplateInstance
      FieldValues: Map<string, string>
      ServerCreatedAt: Instant
      ServerModifiedAt: Instant
      Collectors: int
      EditSummary: string }
  with
    member this.CurrentId = this.Id, this.CurrentOrdinal
    member this.MaxIndexInclusive =
        Helper.maxIndexInclusive
            (this.TemplateInstance.CardTemplates)
            (this.FieldValues.Select(fun x -> x.Key, x.Value |?? lazy "") |> Map.ofSeq) // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls
    member this.FrontBackFrontSynthBackSynthAll =
        match this.TemplateInstance.CardTemplates with
        | Standard ts ->
            ts |> List.map (fun t ->
                CardHtml.generate
                <| (this.FieldValues.Select(fun x -> x.Key, x.Value |?? lazy "") |> Seq.toList)
                <| t.Front
                <| t.Back
                <| this.TemplateInstance.Css
                <| CardHtml.Standard
            )
        | Cloze c ->
            [0s .. this.MaxIndexInclusive] |> List.map (fun i ->
                CardHtml.generate
                <| (this.FieldValues.Select(fun x -> x.Key, x.Value |?? lazy "") |> Seq.toList)
                <| c.Front
                <| c.Back
                <| this.TemplateInstance.Css
                <| CardHtml.Cloze i
            )
    member this.FirstFrontStripped =
        let front, _, _, _ = this.FrontBackFrontSynthBackSynthAll.Head
        MappingTools.stripHtmlTagsForDisplay front

[<CLIMutable>]
type Concept =
    { CommandIds: CommandId Set
      Id: ExampleId
      CurrentOrdinal: ExampleRevisionOrdinal
      Title: string
      AuthorId: UserId
      Author: string
      TemplateInstance: TemplateInstance
      FieldValues: Map<string, string>
      Collectors: int
      EditSummary: string
      Visibility: Visibility
      Children: Concept list
      Comments: Comment list }
    with
      member this.UrlId = ToUrl.example (this.Id, this.CurrentOrdinal)
      member this.MaxIndexInclusive =
          Helper.maxIndexInclusive
              (this.TemplateInstance.CardTemplates)
              (this.FieldValues.Select(fun x -> x.Key, x.Value |?? lazy "") |> Map.ofSeq) // null coalesce is because <EjsRichTextEditor @bind-Value=@Field.Value> seems to give us nulls
      member this.FrontBackFrontSynthBackSynthAll =
          match this.TemplateInstance.CardTemplates with
          | Standard ts ->
              ts |> List.map (fun t ->
                  CardHtml.generate
                  <| (this.FieldValues.Select(fun x -> x.Key, x.Value |?? lazy "") |> Seq.toList)
                  <| t.Front
                  <| t.Back
                  <| this.TemplateInstance.Css
                  <| CardHtml.Standard
              )
          | Cloze c ->
              [0s .. this.MaxIndexInclusive] |> List.map (fun i ->
                  CardHtml.generate
                  <| (this.FieldValues.Select(fun x -> x.Key, x.Value |?? lazy "") |> Seq.toList)
                  <| c.Front
                  <| c.Back
                  <| this.TemplateInstance.Css
                  <| CardHtml.Cloze i
              )
      static member FromExample children (example: Kvs.Example) =
        { CommandIds        = example.CommandIds
          Id                = example.Id
          CurrentOrdinal    = example.CurrentRevision.Ordinal
          Title             = example.CurrentRevision.Title
          AuthorId          = example.AuthorId
          Author            = example.Author
          TemplateInstance  = example.CurrentRevision.TemplateInstance
          FieldValues       = example.CurrentRevision.FieldValues |> Seq.map (fun x -> x.EditField.Name, x.Value) |> Map.ofSeq
          Collectors        = example.Collectors
          EditSummary       = example.CurrentRevision.EditSummary
          Visibility        = example.Visibility
          Children          = children
          Comments          = example.Comments }
      static member ProjectionId (exampleId: ExampleId) = $"C.{exampleId}"
      static member ProjectionId (exampleId: string)    = $"C.{exampleId}"
module Concept =
    let private incDecCollectors incdec commandId (concept: Concept) =
        if concept.CommandIds.Contains commandId then
            concept
        else
            { concept with
                CommandIds = concept.CommandIds |> Set.add commandId
                Collectors = incdec concept.Collectors 1 }
    let incrementCollectors = incDecCollectors (+)
    let decrementCollectors = incDecCollectors (-)
    
    let private tryIncDecCollectors<'T> incdec commandId (f: _ -> 'T) (oldConcept: Concept) =
        let newConcept = incDecCollectors incdec commandId oldConcept
        let o =
            if newConcept = oldConcept
            then None
            else Some newConcept
        o, f newConcept
    let tryIncrementCollectors<'T> = tryIncDecCollectors<'T> (+)
    let tryDecrementCollectors<'T> = tryIncDecCollectors<'T> (-)
    
let n = Unchecked.defaultof<ExampleSearch>
module ExampleSearch =
    let fromSummary (summary: Example) displayName (templateInstance: TemplateInstance) =
        [ nameof n.Id              , summary.Id                                                     |> box
          nameof n.ParentId        , summary.ParentId                                               |> box
          nameof n.CurrentOrdinal  , summary.CurrentRevision.Ordinal                                |> box
          nameof n.Title           , summary.CurrentRevision.Title                                  |> box
          nameof n.AuthorId        , summary.AuthorId                                               |> box
          nameof n.Author          , displayName                                                    |> box
          nameof n.TemplateInstance, templateInstance                                               |> box
          nameof n.FieldValues     , summary.CurrentRevision.FieldValues |> EditFieldAndValue.toMap |> box
          nameof n.ServerCreatedAt , summary.  FirstRevision.Meta.ServerReceivedAt.Value            |> box
          nameof n.ServerModifiedAt, summary.CurrentRevision.Meta.ServerReceivedAt.Value            |> box
          nameof n.EditSummary     , summary.CurrentRevision.EditSummary                            |> box
        ] |> Map.ofList
    let fromEdited (edited: Example.Events.Edited) (templateInstance: TemplateInstance) =
        [ nameof n.CurrentOrdinal  , edited.Ordinal                                |> box
          nameof n.Title           , edited.Title                                  |> box
          nameof n.TemplateInstance, templateInstance                              |> box
          nameof n.FieldValues     , edited.FieldValues |> EditFieldAndValue.toMap |> box
          nameof n.ServerModifiedAt, edited.Meta.ServerReceivedAt.Value            |> box
          nameof n.EditSummary     , edited.EditSummary                            |> box
        ] |> Map.ofList

[<CLIMutable>]
type TemplateSearch =
    { Id: TemplateId
      CurrentOrdinal: TemplateRevisionOrdinal
      AuthorId: UserId
      Author: string
      Name: string
      Css: string
      Fields: Field list
      ServerCreatedAt: Instant
      ServerModifiedAt: Instant
      LatexPre: string
      LatexPost: string
      CardTemplates: TemplateType
      Collectors: int }
type TemplateSearch_OnCollected =
    { TemplateId: TemplateId
      CollectorId: UserId
      Ordinal: TemplateRevisionOrdinal }
type TemplateSearch_OnDiscarded =
    { TemplateId: TemplateId
      DiscarderId: UserId }
module TemplateSearch =
    open Template
    let n = Unchecked.defaultof<TemplateSearch>
    let fromSummary (displayName: string) (template: Template) =
        [ nameof n.Id              , template.Id                                           |> box
          nameof n.CurrentOrdinal  , template.CurrentRevision.Ordinal                      |> box
          nameof n.AuthorId        , template.AuthorId                                     |> box
          nameof n.Author          , displayName                                           |> box
          nameof n.Name            , template.CurrentRevision.Name                         |> box
          nameof n.Css             , template.CurrentRevision.Css                          |> box
          nameof n.Fields          , template.CurrentRevision.Fields                       |> box
          nameof n.ServerCreatedAt , template.  FirstRevision.Meta.ServerReceivedAt.Value  |> box
          nameof n.ServerModifiedAt, template.CurrentRevision.Meta.ServerReceivedAt.Value  |> box
          nameof n.LatexPre        , template.CurrentRevision.LatexPre                     |> box
          nameof n.LatexPost       , template.CurrentRevision.LatexPost                    |> box
          nameof n.CardTemplates   , template.CurrentRevision.CardTemplates                |> box
        ] |> Map.ofList
    let fromEdited (edited: Events.Edited) =
        [ nameof n.CurrentOrdinal    , edited.Ordinal                     |> box
          nameof n.Name              , edited.Name                        |> box
          nameof n.Css               , edited.Css                         |> box
          nameof n.Fields            , edited.Fields                      |> box
          nameof n.ServerModifiedAt  , edited.Meta.ServerReceivedAt.Value |> box
          nameof n.LatexPre          , edited.LatexPre                    |> box
          nameof n.LatexPost         , edited.LatexPost                   |> box
          nameof n.CardTemplates     , edited.CardTemplates               |> box
        ] |> Map.ofList

type ClientEvent<'T> =
    { StreamId: Guid
      Event: 'T }

[<CLIMutable>]
type ViewDeck = {
    Id: DeckId
    Visibility: Visibility
    IsDefault: bool
    [<System.ComponentModel.DataAnnotations.StringLength(250, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 250 characters.")>] // medTODO 500 needs to be tied to the DB max somehow
    Name: string
    DueCount: int
    AllCount: int
}

open TaskOp
module Dexie =
    type CardInstance =
        { StackId: StackId
          CommandIds: CommandId Set
          AuthorId: UserId
          FrontPersonalField: string
          BackPersonalField: string
          Tags: string Set
          DeckIds: DeckId Set

          Pointer: CardTemplatePointer
          CardSettingId: CardSettingId
          EaseFactor: float
          IntervalOrStepsIndex: IntervalOrStepsIndex // highTODO bring all the types here. ALSO CONSIDER A BETTER NAME
          Due: Instant
          IsLapsed: bool
          Reviews: Review list
          State: CardState
          
          ExampleRevisionId: ExampleRevisionId Option
          Title: string
          TemplateId: TemplateId // highTODO should be UserTemplateId
          FieldValues: EditFieldAndValue list }
        member this.FrontBackFrontSynthBackSynth (pointer: CardTemplatePointer) (templateInstance: TemplateRevision) =
            FrontBackFrontSynthBackSynth.fromEditFieldAndValueList this.FieldValues pointer templateInstance
    module CardInstance =
        let toSummary (c: CardInstance) =
            { Pointer              = c.Pointer
              CardSettingId        = c.CardSettingId
              EaseFactor           = c.EaseFactor
              IntervalOrStepsIndex = c.IntervalOrStepsIndex
              Due                  = c.Due
              IsLapsed             = c.IsLapsed
              Reviews              = c.Reviews
              State                = c.State }
    
    open System.Globalization
    let private _user events =
        match User.Fold.foldInit events with
        | User.Fold.Initial -> failwith "impossible"
        | User.Fold.Active u ->
            [ "id"         , u.Id |> string
              "summary"    , serializeToJson u
            ] |> Map.ofList
    let private _deck events =
        match Deck.Fold.foldInit events with
        | Deck.Fold.Active d ->
            [ "id"         , d.Id |> string
              "name"       , d.Name
              "description", d.Description
              "summary"    , serializeToJson d
            ] |> Map.ofList |> Some
        | Deck.Fold.Discard _ -> None
        | Deck.Fold.Initial   -> None
    let private _template events =
        match Template.Fold.foldInit events with
        | Template.Fold.Active t ->
            [ "id"         , t.Id |> string
              "summary"    , serializeToJson t
            ] |> Map.ofList |> Some
        | Template.Fold.Initial -> None // lowTODO display something
        | Template.Fold.Dmca  _ -> None // lowTODO display something
    let private _example events =
        match Example.Fold.foldInit events with
        | Example.Fold.Active e ->
            [ "id"         , e.Id |> string
              "summary"    , serializeToJson e
            ] |> Map.ofList |> Some
        | Example.Fold.Initial -> None // lowTODO display something
        | Example.Fold.Dmca  _ -> None // lowTODO display something
    let private _stackAndCards (getExampleInstance: Func<ExampleRevisionId, Task<ExampleInstance>>) events =
        match Stack.Fold.foldInit events with
        | Stack.Fold.Active stack -> task {
            let stackSummary =
                [ "id"             , stack.Id |> string
                  "exampleId"      , stack.ExampleRevisionId |> string // highTODO consider deleting this line... why do we need it? Dexie currently uses it for `GetStackByExample`, but after this refactor do we really want to keep it? At the very least it needs fixing as its an `option`
                  "summary"        , serializeToJson stack
                ] |> Map.ofList
            let cardSummaries =
                stack.Cards |> List.map (fun card ->
                    let cardInstance =
                        { StackId              = stack.Id
                          CommandIds           = stack.CommandIds
                          AuthorId             = stack.AuthorId
                          FrontPersonalField   = stack.FrontPersonalField
                          BackPersonalField    = stack.BackPersonalField
                          Tags                 = stack.Tags
                          DeckIds              = stack.DeckIds
                          
                          Pointer              = card.Pointer
                          CardSettingId        = card.CardSettingId
                          EaseFactor           = card.EaseFactor
                          IntervalOrStepsIndex = card.IntervalOrStepsIndex
                          Due                  = card.Due
                          IsLapsed             = card.IsLapsed
                          Reviews              = card.Reviews
                          State                = card.State
                          
                          ExampleRevisionId    = stack.ExampleRevisionId
                          Title                = stack.Title
                          TemplateId           = fst stack.TemplateRevisionId
                          FieldValues          = stack.FieldValues }
                    let pointer =
                        match card.Pointer with
                        | CardTemplatePointer.Normal g -> $"Normal-{g}"
                        | CardTemplatePointer.Cloze i  -> $"Cloze-{i}"
                    [ "id"      , $"{stack.Id}-{pointer}"                                |> box
                      "due"     , card.Due.ToString("g", CultureInfo.InvariantCulture)   |> box
                      "deckIds" , stack.DeckIds |> Set.map string                        |> box
                      "state"   , card.State |> string                                   |> box
                      "summary" , serializeToJson cardInstance                           |> box
                    ] |> Map.ofList
                )
            return (stackSummary, cardSummaries) |> Some
            }
        | Stack.Fold.Discard _ -> None |> Task.singleton
        | Stack.Fold.Initial _ -> None |> Task.singleton
    let summarizeUsers (events: seq<ClientEvent<User.Events.Event>>) =
        events
        |> Seq.groupBy (fun x -> x.StreamId)
        |> Seq.map (fun (_, xs) -> xs |> Seq.map (fun x -> x.Event) |> _user)
    let summarizeDecks (events: seq<ClientEvent<Deck.Events.Event>>) =
        events
        |> Seq.groupBy (fun x -> x.StreamId)
        |> Seq.choose (fun (_, xs) -> xs |> Seq.map (fun x -> x.Event) |> _deck)
    let summarizeTemplates (events: seq<ClientEvent<Template.Events.Event>>) =
        events
        |> Seq.groupBy (fun x -> x.StreamId)
        |> Seq.choose (fun (_, xs) -> xs |> Seq.map (fun x -> x.Event) |> _template)
    let summarizeExamples (events: seq<ClientEvent<Example.Events.Event>>) =
        events
        |> Seq.groupBy (fun x -> x.StreamId)
        |> Seq.choose (fun (_, xs) -> xs |> Seq.map (fun x -> x.Event) |> _example)
    let summarizeStacksAndCards (events: seq<ClientEvent<Stack.Events.Event>>) getExampleInstance = task {
        let! stacksAndCards =
            events
            |> Seq.groupBy (fun x -> x.StreamId)
            |> Seq.map (fun (_, xs) -> xs |> Seq.map (fun x -> x.Event) |> _stackAndCards getExampleInstance)
            |> Task.WhenAll
            |>% Array.choose id
        let stacks = stacksAndCards |> Seq.map     (fun (s, _) -> s)
        let cards  = stacksAndCards |> Seq.collect (fun (_, c) -> c)
        return stacks, cards
        }
    
    let parseNextQuizCard (stackJson: string) =
        if stackJson = null then
            None
        else
            stackJson |> deserializeFromJson<CardInstance> |> Some
        


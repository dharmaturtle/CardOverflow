module Hedgehog

open Hedgehog
open CardOverflow.Pure
open CardOverflow.Test
open CardOverflow.Api
open Domain
open FSharp.UMX

// lowTODO this tries to shrink down to 1 element, which may be semantically incorrect depending on use case
module SeqGen =
    let traverse (f: Gen<'a> -> Gen<'b>) (ma: seq<Gen<'a>>): Gen<list<'b>> =
        let mutable cache = ResizeArray()
        gen {
            for a in ma do
                let! b = f a
                cache.Add b
            let r = cache |> Seq.toList 
            cache <- ResizeArray()
            return r
        }

    let sequence ma = traverse id ma

let tagsGen =
    GenX.auto<string>
    |> Gen.filter (Stack.validateTag >> Result.isOk)
    |> Gen.list (Range.linear 0 30)
    |> Gen.map Set.ofList

let unicode max = Gen.string (Range.constant 1 max) Gen.unicode
let standardCardTemplate fields =
    gen {
        let cardTemplateGen =
            gen {
                let! name = unicode 100
                let! front = Gen.item fields
                let! back  = Gen.item fields
                return
                    {   Name = name
                        Front = "{{" + front + "}}"
                        Back = "{{FrontSide}}<hr id=answer>{{" + back + "}}"
                        ShortFront = ""
                        ShortBack = ""
                    }
            }
        let! cardTemplates = GenX.cList 1 100 cardTemplateGen
        return Standard cardTemplates
    }
let clozeCardTemplate fields =
    gen {
        let! name  = unicode 100
        let! text  = Gen.item fields
        let! extra = Gen.item fields
        return
            {   Name = name
                Front = "{{cloze:" + text + "}}"
                Back = "{{cloze:" + text + "}}<br>{{" + extra + "}}"
                ShortFront = ""
                ShortBack = ""
            } |> Cloze
    }
let templateType fields =
    Gen.choice [
        standardCardTemplate fields
        clozeCardTemplate fields
    ]

open NodaTime
let instantGen = GenX.auto |> Gen.map Instant.FromDateTimeOffset
let durationGen = GenX.auto |> Gen.map Duration.FromTimeSpan
let localTimeGen = Range.linear 0 86399 |> Gen.int |> Gen.map LocalTime.FromSecondsSinceMidnight
let timezoneGen = TimezoneName.allNodaTime |> Gen.item
let nodaConfig =
    GenX.defaults
    |> AutoGenConfig.addGenerator instantGen
    |> AutoGenConfig.addGenerator durationGen
    |> AutoGenConfig.addGenerator timezoneGen
    |> AutoGenConfig.addGenerator localTimeGen

let fields = List.map (fun fieldName -> GenX.auto<Field> |> Gen.map(fun field -> { field with Name = fieldName })) >> SeqGen.sequence
let templateRevision templateType fieldNames =
    gen {
        let! fields = fieldNames |> fields
        let! id = Gen.guid
        let! name = Gen.latin1 |> GenX.lString 0 50
        let! templateId = Gen.guid
        let! css = Gen.latin1 |> GenX.lString 0 50
        let! created = instantGen
        let! modified = instantGen
        let! latexPre  = Gen.latin1 |> GenX.lString 0 50
        let! latexPost = Gen.latin1 |> GenX.lString 0 50
        let! editSummary = Gen.latin1 |> GenX.lString 0 50
        return {
            Id = id
            Name = name
            TemplateId = templateId
            Css = css
            Fields = fields
            Created = created
            Modified = Some modified
            LatexPre = latexPre
            LatexPost = latexPost
            CardTemplates = templateType
            EditSummary = editSummary
        }
    }

let clozeText =
    gen { // medTODO make more realistic
        let! a = Gen.alphaNum |> GenX.cString 1 100
        let! b = Gen.alphaNum |> GenX.cString 1 100
        let! c = Gen.alphaNum |> GenX.cString 1 100
        return sprintf "%s{{c1::%s}}%s" a b c
    }

let fieldNamesGen =
    Gen.unicode
    |> Gen.string (Range.linear 1 Template.fieldNameMax)
    |> Gen.filter (Template.validateFieldName >> Result.isOk)
    |> GenX.cList 1 100
    |> Gen.map List.distinct

let editConceptCommandGen =
    gen {
        let! fieldNames = fieldNamesGen
        let! templateType = templateType fieldNames
        let! templateRevision = templateRevision templateType fieldNames
        let values =
            match templateType with
            | Standard _ -> Gen.alphaNum |> Gen.string (Range.constant 1 100)
            | Cloze _ -> clozeText
        let! fields = fields fieldNames
        let! fields =
            fields
            |> List.map (fun f -> values |> Gen.map (fun value -> { EditField = f; Value = value }))
            |> SeqGen.sequence
        let! editSummary = GenX.auto<string> |> Gen.filter (Example.validateEditSummary >> Result.isOk)
        let! tags = tagsGen
        let! kind = GenX.auto<UpsertKind> |> Gen.map (fun k ->
            match k with
            | NewOriginal_TagIds _ ->
                NewOriginal_TagIds tags
            | NewCopy_SourceRevisionId_TagIds (x, _) ->
                NewCopy_SourceRevisionId_TagIds (x, tags)
            | _ -> k
            )
        let! ids = GenX.auto<UpsertIds>
        return {
            EditSummary = editSummary
            FieldValues = fields |> toResizeArray
            TemplateRevisionId = % templateRevision.Id
            Kind = kind
            Ids = ids
        }
    }

let userSummaryGen =
    nodaConfig
    |> GenX.autoWith<User.Events.Summary>
    |> Gen.filter (User.validateSummary >> Result.isOk)

let templateGen : Template.Events.Summary Gen = gen {
    let! fieldNames = fieldNamesGen
    let! fields = fieldNames |> fields
    let! id = Gen.guid
    let! revisionId = Gen.guid
    let! authorId = Gen.guid
    let! name = Gen.latin1 |> GenX.lString 1 Template.nameMax
    let! templateType = templateType fieldNames
    let! css = Gen.latin1 |> GenX.lString 0 50
    let! created = instantGen
    let! modified = instantGen
    let! latexPre  = Gen.latin1 |> GenX.lString 0 50
    let! latexPost = Gen.latin1 |> GenX.lString 0 50
    let! editSummary = Gen.latin1 |> GenX.lString 0 Template.editSummaryMax
    return
        { Id = % id
          RevisionIds = [% revisionId]
          AuthorId = % authorId
          Name = name
          Css = css
          Fields = fields
          Created = created
          Modified = modified
          LatexPre = latexPre
          LatexPost = latexPost
          CardTemplates = templateType
          EditSummary = editSummary }
    }
    
let templateEditGen = gen {
    let! template = templateGen
    let! edited =
        nodaConfig
        |> GenX.autoWith<Template.Events.Edited>
        |> Gen.filter (Template.validateEdited template template.AuthorId false >> Result.isOk)
    return template, edited
    }

let deckSummaryGen = gen {
    let! name = GenX.auto<string> |> Gen.filter (Deck.validateName >> Result.isOk)
    let! summary =
        nodaConfig
        |> GenX.autoWith<Deck.Events.Summary>
    return
        { summary with
            Name = name
            SourceId = None }
    }

let exampleSummaryGen = gen {
    let! title       = GenX.lString 0 Example.titleMax       Gen.latin1
    let! editSummary = GenX.lString 0 Example.editSummaryMax Gen.latin1
    let! revisionId  = GenX.auto
    return!
        nodaConfig
        |> GenX.autoWith<Example.Events.Summary>
        |> Gen.map (fun b ->
            { b with
                Title = title
                EditSummary = editSummary
                RevisionIds = [ revisionId ] })
        |> Gen.filter (Example.validateSummary >> Result.isOk)
    }

type ExampleEdit = { Summary: Example.Events.Summary; Edit: Example.Events.Edited }
let exampleEditGen = gen {
    let! exampleSummary = exampleSummaryGen
    let! title          = GenX.lString 0 Example.titleMax       Gen.latin1
    let! editSummary    = GenX.lString 0 Example.editSummaryMax Gen.latin1
    let! edit =
        nodaConfig
        |> GenX.autoWith<Example.Events.Edited>
        |> Gen.map (fun b ->
            { b with
                Title = title
                EditSummary = editSummary })
        |> Gen.filter (Example.validateEdit exampleSummary.AuthorId exampleSummary >> Result.isOk)
    return { Summary = exampleSummary; Edit = edit }
    }

let deckEditGen = gen {
    let! name = GenX.auto<string> |> Gen.filter (Deck.validateName >> Result.isOk)
    let! edited =
        nodaConfig
        |> GenX.autoWith<Deck.Events.Edited>
    return
        { edited with
            Name = name
            SourceId = None }
    }

let cardSettingsEditedListGen = gen {
    let! others = nodaConfig |> GenX.autoWith<CardSetting> |> GenX.lList 0 100 |> Gen.map (List.map (fun x -> { x with IsDefault = false }))
    let! theDefault  = nodaConfig |> GenX.autoWith<CardSetting>                     |> Gen.map           (fun x -> { x with IsDefault = true  })
    return
        { User.Events.CardSettingsEdited.CardSettings =
            {   Default = theDefault
                Others = others
            }
        }
    }

type NewOriginal = { NewOriginal: EditConceptCommand }
let newOriginalGen =
    gen {
        let! c = editConceptCommandGen
        let! tags = tagsGen
        let c = { c with Kind = UpsertKind.NewOriginal_TagIds tags }
        return { NewOriginal = c }
    }

type NewExample = { NewOriginal: EditConceptCommand; NewExample: EditConceptCommand; Template: Template.Events.Summary; ExampleTitle: string }
let newExampleGen =
    gen {
        let! { NewOriginal = newOriginal } = newOriginalGen
        let! template = templateGen
        let newOriginal = { newOriginal with TemplateRevisionId = template.RevisionIds.Head }
        let! title = GenX.auto<string>
        let! newExample = editConceptCommandGen
        let newExample =
            { newExample with
                Kind = UpsertKind.NewExample_Title title
                TemplateRevisionId = template.RevisionIds.Head
                Ids =
                    { newExample.Ids with
                        ConceptId = newOriginal.Ids.ConceptId } }
        return
            { NewOriginal  = newOriginal
              NewExample   = newExample
              ExampleTitle = title
              Template     = template }
    }

open Hedgehog.Xunit
type StandardConfig =
    static member __ =
        GenX.defaults
        |> AutoGenConfig.addGenerator userSummaryGen
        |> AutoGenConfig.addGenerator templateGen
        |> AutoGenConfig.addGenerator templateEditGen
        |> AutoGenConfig.addGenerator deckSummaryGen
        |> AutoGenConfig.addGenerator deckEditGen
        |> AutoGenConfig.addGenerator editConceptCommandGen
        |> AutoGenConfig.addGenerator cardSettingsEditedListGen
        |> AutoGenConfig.addGenerator instantGen
        |> AutoGenConfig.addGenerator durationGen
        |> AutoGenConfig.addGenerator timezoneGen
        |> AutoGenConfig.addGenerator localTimeGen
        |> AutoGenConfig.addGenerator newOriginalGen
        |> AutoGenConfig.addGenerator newExampleGen
        |> AutoGenConfig.addGenerator tagsGen
        |> AutoGenConfig.addGenerator exampleSummaryGen


type StandardProperty(i) =
    inherit PropertyAttribute(typeof<StandardConfig>, LanguagePrimitives.Int32WithMeasure i)
    new () = StandardProperty(100)

module Hedgehog

open Hedgehog
open CardOverflow.Pure
open CardOverflow.Test
open CardOverflow.Api
open Domain
open FSharp.UMX
open System
open Domain.Summary

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

let unicode max = Gen.string (Range.linear 1 max) Gen.unicode
let standardCardTemplate fields =
    gen {
        let cardTemplateGen =
            gen {
                let! name = unicode 100
                let! front = Gen.item fields
                let! back  = Gen.item fields
                return
                    {   Id = Guid.NewGuid()
                        Name = name
                        Front = "{{" + front + "}}"
                        Back = "{{FrontSide}}<hr id=answer>{{" + back + "}}"
                        ShortFront = ""
                        ShortBack = ""
                    }
            }
        let! cardTemplates = GenX.lList 1 50 cardTemplateGen
        return Standard cardTemplates
    }
let clozeCardTemplate fields =
    gen {
        let! name  = unicode 100
        let! text  = Gen.item fields
        let! extra = Gen.item fields
        return
            {   Id = Guid.NewGuid()
                Name = name
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
        let! id = Gen.int (Range.linear 0 1_000_000_000)
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
        let! a = Gen.alphaNum |> GenX.lString 1 100
        let! b = Gen.alphaNum |> GenX.lString 1 100
        let! c = Gen.alphaNum |> GenX.lString 1 100
        return sprintf "%s{{c1::%s}}%s" a b c
    }

let fieldNamesGen =
    Gen.unicode
    |> Gen.string (Range.linear 1 Template.fieldNameMax)
    |> Gen.filter (Template.validateFieldName >> Result.isOk)
    |> GenX.lList 1 100
    |> Gen.map List.distinct

let userSummaryGen =
    nodaConfig
    |> GenX.autoWith<User>
    |> Gen.filter (User.validateSummary >> Result.isOk)

let metaGen authorId = gen {
    let! serverCreatedAt = nodaConfig |> GenX.autoWith<Instant>
    return!
        nodaConfig
        |> GenX.autoWith<Meta>
        |> Gen.map (fun x ->
            { x with
                UserId = % authorId
                ServerCreatedAt = Some serverCreatedAt
            })
    }

let templateCreatedGen authorId : Template.Events.Created Gen = gen {
    let! fieldNames = fieldNamesGen
    let! fields = fieldNames |> fields
    let! id = Gen.guid
    let! name = Gen.latin1 |> GenX.lString 1 Template.nameMax
    let! templateType = templateType fieldNames
    let! css = Gen.latin1 |> GenX.lString 0 50
    let! latexPre  = Gen.latin1 |> GenX.lString 0 50
    let! latexPost = Gen.latin1 |> GenX.lString 0 50
    let! visibility = GenX.auto<Visibility>
    let! meta = metaGen authorId
    let! editSummary = Gen.latin1 |> GenX.lString 0 Template.editSummaryMax
    return
        { Meta = meta
          Id = % id
          Visibility = visibility
          Name = name
          Css = css
          Fields = fields
          LatexPre = latexPre
          LatexPost = latexPost
          CardTemplates = templateType
          EditSummary = editSummary }
    }
    
type TemplateEdit = { Author: User; TemplateCreated: Template.Events.Created; TemplateEdit: Template.Events.Edited }
let templateEditGen = gen {
    let! author = userSummaryGen
    let! created = templateCreatedGen author.Id
    let template = Template.Fold.evolveCreated created
    let! edited =
        nodaConfig
        |> GenX.autoWith<Template.Events.Edited>
        |> Gen.map (fun x -> { x with Revision = template.CurrentRevision + 1<templateRevisionOrdinal>})
        |> Gen.filter (Template.validateEdited template created.Meta.UserId >> Result.isOk)
    return { Author = author; TemplateCreated = created; TemplateEdit = edited }
    }

let deckSummaryGen = gen {
    let! name        = GenX.auto<string> |> Gen.filter (Deck.validateName        >> Result.isOk)
    let! description = GenX.auto<string> |> Gen.filter (Deck.validateDescription >> Result.isOk)
    let! summary =
        nodaConfig
        |> GenX.autoWith<Deck>
    return
        { summary with
            Name = name
            Description = description }
    }

let exampleSummaryGen = gen {
    let! title       = GenX.lString 0 Example.titleMax       Gen.latin1
    let! editSummary = GenX.lString 0 Example.editSummaryMax Gen.latin1
    return!
        nodaConfig
        |> GenX.autoWith<Example>
        |> Gen.map (fun b ->
            { b with
                Title = title
                EditSummary = editSummary
                CurrentRevision = 0<exampleRevisionOrdinal> })
        |> Gen.filter (Example.validateCreate >> Result.isOk)
    }

let cardGen = gen {
    let! card  = GenX.autoWith<Card> nodaConfig
    return card
    }

let stackGen = gen {
    let! cards = GenX.lList 1 50 cardGen
    let! tags  = tagsGen
    let! stack = GenX.autoWith<Stack> nodaConfig
    return { stack with Cards = cards; Tags = tags }
    }

type ExampleEdit = { Author: User; TemplateCreated: Template.Events.Created; ExampleSummary: Example; Edit: Example.Events.Edited; Stack: Stack }
let exampleEditGen = gen {
    let! author          =     userSummaryGen
    let! exampleSummary  =  exampleSummaryGen
    let! templateCreated = templateCreatedGen author.Id
    let fieldValues =
        match templateCreated.CardTemplates with
        | Standard _ -> templateCreated.Fields |> List.map (fun x -> x.Name, x.Name + " value")
        | Cloze    _ -> templateCreated.Fields |> List.map (fun x -> x.Name, "Some {{c1::words}}")
        |> Map.ofList
    let exampleSummary = { exampleSummary with FieldValues = fieldValues }
    let! title          = GenX.lString 0 Example.titleMax       Gen.latin1
    let! editSummary    = GenX.lString 0 Example.editSummaryMax Gen.latin1
    let! stack = stackGen
    let! edit =
        nodaConfig
        |> GenX.autoWith<Example.Events.Edited>
        |> Gen.map (fun b ->
            { b with
                Revision = exampleSummary.CurrentRevision + 1<exampleRevisionOrdinal>
                Title = title
                EditSummary = editSummary })
        |> Gen.filter (Example.validateEdit exampleSummary.AuthorId exampleSummary >> Result.isOk)
    let template = Template.Fold.evolveCreated templateCreated
    let pointers = fieldValues |> Template.getCardTemplatePointers (Template.toRevisionSummary template) |> Result.getOk
    let! cards = pointers |> List.map (fun _ -> GenX.autoWith<Card> nodaConfig) |> SeqGen.sequence
    let cards = cards |> List.mapi (fun i c -> { c with Pointer = pointers.Item i })
    let exampleSummary = { exampleSummary with AuthorId = author.Id; TemplateRevisionId = template.CurrentRevisionId }
    let edit           = { edit           with TemplateRevisionId = template.CurrentRevisionId; FieldValues = fieldValues }
    let stack          = { stack          with AuthorId = author.Id; ExampleRevisionId  = exampleSummary.CurrentRevisionId; Cards = cards }
    return { Author = author; TemplateCreated = templateCreated; ExampleSummary = exampleSummary; Edit = edit; Stack = stack }
    }

let deckEditGen = gen {
    let! name        = GenX.auto<string> |> Gen.filter (Deck.validateName        >> Result.isOk)
    let! description = GenX.auto<string> |> Gen.filter (Deck.validateDescription >> Result.isOk)
    let! edited =
        nodaConfig
        |> GenX.autoWith<Deck.Events.Edited>
    return
        { edited with
            Name = name
            Description = description }
    }

let cardSettingsEditedListGen = gen {
    let! nondefaults = nodaConfig |> GenX.autoWith<CardSetting> |> GenX.lList 0 100 |> Gen.map (List.map (fun x -> { x with IsDefault = false }))
    let! theDefault  = nodaConfig |> GenX.autoWith<CardSetting>                     |> Gen.map           (fun x -> { x with IsDefault = true  })
    return!
        theDefault :: nondefaults
        |> GenX.shuffle
        |> Gen.map (fun x -> { User.Events.CardSettingsEdited.CardSettings = x })
    }

open Hedgehog.Xunit
type StandardConfig =
    static member __ =
        GenX.defaults
        |> AutoGenConfig.addGenerator userSummaryGen
        |> AutoGenConfig.addGenerator templateEditGen
        |> AutoGenConfig.addGenerator deckSummaryGen
        |> AutoGenConfig.addGenerator deckEditGen
        |> AutoGenConfig.addGenerator cardSettingsEditedListGen
        |> AutoGenConfig.addGenerator instantGen
        |> AutoGenConfig.addGenerator durationGen
        |> AutoGenConfig.addGenerator timezoneGen
        |> AutoGenConfig.addGenerator localTimeGen
        |> AutoGenConfig.addGenerator tagsGen
        |> AutoGenConfig.addGenerator exampleSummaryGen
        |> AutoGenConfig.addGenerator exampleEditGen


type StandardProperty(i) =
    inherit PropertyAttribute(typeof<StandardConfig>, LanguagePrimitives.Int32WithMeasure i)
    new () = StandardProperty(100)

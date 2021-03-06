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

let genChar = Gen.alphaNum

let unicode max = Gen.string (Range.linear 1 max) genChar
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
        let! templateId = Gen.guid
        let! name = genChar |> GenX.lString 0 50
        let! css  = genChar |> GenX.lString 0 50
        let! created  = instantGen
        let! modified = instantGen
        let! latexPre    = genChar |> GenX.lString 0 50
        let! latexPost   = genChar |> GenX.lString 0 50
        let! editSummary = genChar |> GenX.lString 0 50
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
        let! a = genChar |> GenX.lString 1 100
        let! b = genChar |> GenX.lString 1 100
        let! c = genChar |> GenX.lString 1 100
        return sprintf "%s{{c1::%s}}%s" a b c
    }

let fieldNamesGen =
    genChar
    |> Gen.string (Range.linear 1 Template.fieldNameMax)
    |> Gen.filter (Template.validateFieldName >> Result.isOk)
    |> GenX.lList 1 100
    |> Gen.map List.distinct

let metaGen authorId = gen {
    let! serverReceivedAt = nodaConfig |> GenX.autoWith<Instant>
    let! clientSentAt     = nodaConfig |> GenX.autoWith<Instant>
    return!
        nodaConfig
        |> GenX.autoWith<Meta>
        |> Gen.map (fun x ->
            { x with
                UserId           = % authorId
                ServerReceivedAt = Some serverReceivedAt
                ClientSentAt     = Some clientSentAt
            })
    }

let userSignedUpGen userId = gen {
    let! meta = metaGen userId
    return!
        nodaConfig
        |> GenX.autoWith<User.Events.SignedUp>
        |> Gen.map (fun x -> { x with Meta = meta; CollectedTemplates = [] })
        |> Gen.filter (User.validateSignedUp >> Result.isOk)
    }

let templateCreatedGen authorId : Template.Events.Created Gen = gen {
    let! fieldNames = fieldNamesGen
    let! fields = fieldNames |> fields
    let! id = Gen.guid
    let! name = genChar |> GenX.lString 1 Template.nameMax
    let! templateType = templateType fieldNames
    let! css = genChar |> GenX.lString 0 50
    let! latexPre  = genChar |> GenX.lString 0 50
    let! latexPost = genChar |> GenX.lString 0 50
    let! meta = metaGen authorId
    let! editSummary = genChar |> GenX.lString 0 Template.editSummaryMax
    return
        { Meta = meta
          Id = % id
          Visibility = Public
          Name = name
          Css = css
          Fields = fields
          LatexPre = latexPre
          LatexPost = latexPost
          CardTemplates = templateType
          EditSummary = editSummary }
    }
    
let templateEditedGen authorId (template: Template) = gen {
    let! meta = metaGen authorId
    return!
        nodaConfig
        |> GenX.autoWith<Template.Events.Edited>
        |> Gen.map (fun x -> { x with Meta = meta; Ordinal = template.CurrentRevision.Ordinal + 1<templateRevisionOrdinal>})
        |> Gen.filter (Template.validateEdited template >> Result.isOk)
    }

type TemplateEdit = { TemplateCreated: Template.Events.Created; TemplateEdit: Template.Events.Edited; TemplateCollected: User.Events.TemplateCollected; TemplateDiscarded: User.Events.TemplateDiscarded }
let templateEditGen userId = gen {
    let! created = templateCreatedGen userId
    let template = Template.Fold.evolveCreated created
    let! edited = templateEditedGen userId template
    let! collectedMeta = metaGen userId
    let (collected : User.Events.TemplateCollected) = { Meta = collectedMeta; TemplateRevisionId = template.Id, Template.Fold.initialTemplateRevisionOrdinal }
    let! discardedMeta = metaGen userId
    let (discarded : User.Events.TemplateDiscarded) = { Meta = discardedMeta; TemplateRevisionId = template.Id, Template.Fold.initialTemplateRevisionOrdinal }
    return { TemplateCreated = created; TemplateEdit = edited; TemplateCollected = collected; TemplateDiscarded = discarded }
    }

let deckNameGen        = genChar |> GenX.lString 1 100 |> Gen.filter (Deck.validateName        >> Result.isOk)
let deckDescriptionGen = genChar |> GenX.lString 0 300 |> Gen.filter (Deck.validateDescription >> Result.isOk)

let deckCreatedGen authorId = gen {
    let! meta = metaGen authorId
    let! name        = deckNameGen
    let! description = deckDescriptionGen
    let! summary =
        nodaConfig
        |> GenX.autoWith<Deck.Events.Created>
    return
        { summary with
            Meta = meta
            Name = name
            Description = description }
    }

let toEditFieldAndValue map =
    map |> List.map (fun (k, v) ->
        {   EditField =
                {   Name = k
                    IsRightToLeft = false
                    IsSticky = false
                }
            Value = v
        })

let exampleCreatedGen (templateCreated: Template.Events.Created) authorId exampleId = gen {
    let! title       = GenX.lString 0 Example.titleMax       genChar
    let! editSummary = GenX.lString 0 Example.editSummaryMax genChar
    let! meta        = metaGen authorId
    let fieldValues =
        match templateCreated.CardTemplates with
        | Standard _ -> templateCreated.Fields |> List.map (fun x -> x.Name, x.Name + " value")
        | Cloze    _ -> templateCreated.Fields |> List.map (fun x -> x.Name, "Some {{c1::words}}")
        |> toEditFieldAndValue
    let template = templateCreated |> Template.Fold.evolveCreated |> Template.Fold.Active
    return!
        nodaConfig
        |> GenX.autoWith<Example.Events.Created>
        |> Gen.map (fun b ->
            { b with
                Id = exampleId
                Meta = meta
                Title = title
                TemplateRevisionId = templateCreated.Id, Template.Fold.initialTemplateRevisionOrdinal
                FieldValues = fieldValues
                EditSummary = editSummary })
        |> Gen.filter (Example.validateCreate template >> Result.isOk)
    }

let cardGen = gen {
    let! card  = GenX.autoWith<Card> nodaConfig
    return card
    }

let stackCreatedGen authorId exampleRevisionId fieldValues templateRevision = gen {
    let! tags  = tagsGen
    let! stack = GenX.autoWith<Stack.Events.Created> nodaConfig
    let! meta = metaGen authorId
    let pointers = fieldValues |> Template.getCardTemplatePointers templateRevision |> Result.getOk
    let! cards = pointers |> List.map (fun _ -> GenX.autoWith<Card> nodaConfig) |> SeqGen.sequence
    let cards = cards |> List.mapi (fun i c -> { c with Pointer = pointers.Item i })
    return
        { stack with
            ExampleRevisionId = exampleRevisionId
            DeckIds = Set.empty
            Meta = meta
            Cards = cards
            Tags = tags }
    }

let exampleEditedGen (templateCreated: Template.Events.Created) fieldValues (exampleSummary: Summary.Example) authorId = gen {
    let! meta = metaGen authorId
    let! title          = GenX.lString 0 Example.titleMax       genChar
    let! editSummary    = GenX.lString 0 Example.editSummaryMax genChar
    let template = templateCreated |> Template.Fold.evolveCreated |> Template.Fold.Active
    return!
        nodaConfig
        |> GenX.autoWith<Example.Events.Edited>
        |> Gen.map (fun b ->
            { b with
                Meta = meta
                Ordinal = exampleSummary.CurrentRevision.Ordinal + 1<exampleRevisionOrdinal>
                Title = title
                TemplateRevisionId = templateCreated.Id, Template.Fold.initialTemplateRevisionOrdinal
                FieldValues = fieldValues
                EditSummary = editSummary })
        |> Gen.filter (Example.validateEdit template exampleSummary >> Result.isOk)
    }

let tagsChangedGen authorId : Stack.Events.TagsChanged Gen = gen {
    let! meta = metaGen authorId
    let! tags = tagsGen
    return { Meta = meta; Tags = tags }
    }

let revisionChangedGen authorId exampleId = gen {
    let! meta = metaGen authorId
    let! rc = GenX.autoWith<Stack.Events.RevisionChanged> nodaConfig
    return
        { rc with
            Meta = meta
            RevisionId = exampleId, Example.Fold.initialExampleRevisionOrdinal + 1<exampleRevisionOrdinal> }
    }

type ExampleEdit = { TemplateCreated: Template.Events.Created; ExampleCreated: Example.Events.Created; Edit: Example.Events.Edited; StackCreated: Stack.Events.Created }
let exampleEditGen userId exampleId = gen {
    let! templateCreated = templateCreatedGen userId
    let! exampleCreated  = exampleCreatedGen templateCreated userId exampleId
    let exampleSummary = Example.Fold.evolveCreated exampleCreated
    let! edit = exampleEditedGen templateCreated exampleCreated.FieldValues exampleSummary userId
    let template = Template.Fold.evolveCreated templateCreated
    let! stackCreated = stackCreatedGen userId exampleSummary.CurrentRevisionId exampleCreated.FieldValues template.CurrentRevision
    return { TemplateCreated = templateCreated; ExampleCreated = exampleCreated; Edit = edit; StackCreated = stackCreated }
    }

let deckEditedGen authorId = gen {
    let! name        = deckNameGen
    let! description = deckDescriptionGen
    let! meta = metaGen authorId
    let! edited =
        nodaConfig
        |> GenX.autoWith<Deck.Events.Edited>
    return
        { edited with
            Meta = meta
            Name = name
            Description = description }
    }

let cardSettingsEditedGen authorId : User.Events.CardSettingsEdited Gen = gen {
    let! nondefaults = nodaConfig |> GenX.autoWith<CardSetting> |> GenX.lList 0 100 |> Gen.map (List.map (fun x -> { x with IsDefault = false }))
    let! theDefault  = nodaConfig |> GenX.autoWith<CardSetting>                     |> Gen.map           (fun x -> { x with IsDefault = true  })
    let! meta = metaGen authorId
    return!
        theDefault :: nondefaults
        |> GenX.shuffle
        |> Gen.map (fun x -> { CardSettings = x; Meta = meta })
    }

let optionsEditedGen authorId : User.Events.OptionsEdited Gen = gen {
    let! meta = metaGen authorId
    let! optionsEdited = nodaConfig |> GenX.autoWith<User.Events.OptionsEdited>
    return { optionsEdited with Meta = meta }
    }

type UserEdit = { OptionsEdited: User.Events.OptionsEdited; CardSettingsEdited: User.Events.CardSettingsEdited }
let userEditGen userId = gen {
    let! optionsEdited = optionsEditedGen      userId
    let! cardsSettings = cardSettingsEditedGen userId
    return { OptionsEdited = optionsEdited; CardSettingsEdited = cardsSettings }
    }

type DeckEdit = { DeckCreated: Deck.Events.Created; DeckEdited: Deck.Events.Edited }
let deckEditGen userId = gen {
    let! deckCreated = deckCreatedGen userId
    let! deckEdited  = deckEditedGen  userId
    return { DeckCreated = deckCreated; DeckEdited = deckEdited }
    }

open Hedgehog.Xunit
type StandardConfig =
    static member __ =
        let userId    = % Guid.NewGuid()
        let exampleId = % Guid.NewGuid()
        nodaConfig
        |> AutoGenConfig.addGenerator (userSignedUpGen    userId)
        |> AutoGenConfig.addGenerator (userEditGen        userId)
        |> AutoGenConfig.addGenerator (templateEditGen    userId)
        |> AutoGenConfig.addGenerator (tagsChangedGen     userId)
        |> AutoGenConfig.addGenerator (deckEditGen        userId)
        |> AutoGenConfig.addGenerator (exampleEditGen     userId exampleId)
        |> AutoGenConfig.addGenerator (metaGen            userId)
        |> AutoGenConfig.addGenerator (revisionChangedGen userId exampleId)


type StandardProperty(i) =
    inherit PropertyAttribute(typeof<StandardConfig>, LanguagePrimitives.Int32WithMeasure i)
    new () = StandardProperty(100)

type FastProperty() =
    inherit PropertyAttribute(typeof<StandardConfig>, Tests=1<tests>, Shrinks=0<shrinks>, Size = 100)

let eventConfig =
    nodaConfig
    |> AutoGenConfig.addGenerator (% Guid.NewGuid() |> metaGen)

let templateEventGen = GenX.autoWith<Template.Events.Event> eventConfig |> Gen.filter (not << Template.Fold.isOrigin)
let     userEventGen = GenX.autoWith<    User.Events.Event> eventConfig |> Gen.filter (not <<     User.Fold.isOrigin)
let     deckEventGen = GenX.autoWith<    Deck.Events.Event> eventConfig |> Gen.filter (not <<     Deck.Fold.isOrigin)
let  exampleEventGen = GenX.autoWith< Example.Events.Event> eventConfig |> Gen.filter (not <<  Example.Fold.isOrigin)
let    stackEventGen = GenX.autoWith<   Stack.Events.Event> eventConfig |> Gen.filter (not <<    Stack.Fold.isOrigin)

type EventConfig =
    static member __ =
        eventConfig
        |> AutoGenConfig.addGenerator templateEventGen
        |> AutoGenConfig.addGenerator     userEventGen
        |> AutoGenConfig.addGenerator     deckEventGen
        |> AutoGenConfig.addGenerator  exampleEventGen
        |> AutoGenConfig.addGenerator    stackEventGen

type EventProperty(i) =
    inherit PropertyAttribute(typeof<EventConfig>, LanguagePrimitives.Int32WithMeasure i)
    new () = EventProperty(100)

let meta () = % Guid.NewGuid () |> metaGen |> Gen.sample 100 1 |> List.head

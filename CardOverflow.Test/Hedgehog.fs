module Hedgehog

open Hedgehog
open CardOverflow.Pure
open CardOverflow.Test
open CardOverflow.Api
open Domain
open FSharp.UMX
open System
open Domain.Summary

let tagGen =
    GenX.auto<string>
    |> Gen.filter (Stack.validateTag >> Result.isOk)

let tagsGen =
    tagGen
    |> Gen.list (Range.linear 0 30)
    |> Gen.map Set.ofList

let genChar = Gen.alphaNum

let genStringMinMax min max = Gen.string (Range.linear min max) genChar
let genStringMax max = Gen.string (Range.linear 1 max) genChar
let standardCardTemplate fields =
    gen {
        let cardTemplateGen =
            gen {
                let! name = genStringMax 100
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
        let! name  = genStringMax 100
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

let fields = List.map (fun fieldName -> GenX.auto<Field> |> Gen.map(fun field -> { field with Name = fieldName })) >> ListGen.sequence

let fieldNamesGen =
    genChar
    |> Gen.string (Range.linear 1 PublicTemplate.fieldNameMax)
    |> Gen.filter (PublicTemplate.validateFieldName >> Result.isOk)
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

let templateCreatedGen authorId : PublicTemplate.Events.Created Gen = gen {
    let! fieldNames = fieldNamesGen
    let! fields = fieldNames |> fields
    let! id = Gen.guid
    let! name = genChar |> GenX.lString 1 PublicTemplate.nameMax
    let! templateType = templateType fieldNames
    let! css = genChar |> GenX.lString 0 50
    let! latexPre  = genChar |> GenX.lString 0 50
    let! latexPost = genChar |> GenX.lString 0 50
    let! meta = metaGen authorId
    let! editSummary = genChar |> GenX.lString 0 PublicTemplate.editSummaryMax
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
    
let templateEditedGen authorId (template: PublicTemplate) = gen {
    let! meta = metaGen authorId
    return!
        nodaConfig
        |> GenX.autoWith<PublicTemplate.Events.Edited>
        |> Gen.map (fun x -> { x with Meta = meta; Ordinal = template.CurrentRevision.Ordinal + 1<templateRevisionOrdinal>})
        |> Gen.filter (PublicTemplate.validateEdited template >> Result.isOk)
    }

type TemplateEdit = { TemplateCreated: PublicTemplate.Events.Created; TemplateEdit: PublicTemplate.Events.Edited; TemplateCollected: User.Events.TemplateCollected; TemplateDiscarded: User.Events.TemplateDiscarded }
let templateEditGen userId = gen {
    let! created = templateCreatedGen userId
    let template = PublicTemplate.Fold.evolveCreated created
    let! edited = templateEditedGen userId template
    let! collectedMeta = metaGen userId
    let (collected : User.Events.TemplateCollected) = { Meta = collectedMeta; TemplateRevisionId = template.Id, PublicTemplate.Fold.initialTemplateRevisionOrdinal }
    let! discardedMeta = metaGen userId
    let (discarded : User.Events.TemplateDiscarded) = { Meta = discardedMeta; TemplateRevisionId = template.Id, PublicTemplate.Fold.initialTemplateRevisionOrdinal }
    return { TemplateCreated = created; TemplateEdit = edited; TemplateCollected = collected; TemplateDiscarded = discarded }
    }

let deckNameGen        = genChar |> GenX.lString 1 100 |> Gen.filter (PrivateDeck.validateName        >> Result.isOk)
let deckDescriptionGen = genChar |> GenX.lString 0 300 |> Gen.filter (PrivateDeck.validateDescription >> Result.isOk)

let deckCreatedGen authorId = gen {
    let! meta = metaGen authorId
    let! name        = deckNameGen
    let! description = deckDescriptionGen
    let! summary =
        nodaConfig
        |> GenX.autoWith<PrivateDeck.Events.Created>
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

let exampleCreatedGen (templateCreated: PublicTemplate.Events.Created) authorId exampleId = gen {
    let! title       = GenX.lString 0 Example.titleMax       genChar
    let! editSummary = GenX.lString 0 Example.editSummaryMax genChar
    let! meta        = metaGen authorId
    let fieldValues =
        match templateCreated.CardTemplates with
        | Standard _ -> templateCreated.Fields |> List.map (fun x -> x.Name, x.Name + " value")
        | Cloze    _ -> templateCreated.Fields |> List.map (fun x -> x.Name, "Some {{c1::words}}")
        |> toEditFieldAndValue
    let template = templateCreated |> PublicTemplate.Fold.evolveCreated |> PublicTemplate.Fold.Active
    return!
        nodaConfig
        |> GenX.autoWith<Example.Events.Created>
        |> Gen.map (fun b ->
            { b with
                Id = exampleId
                Meta = meta
                Title = title
                TemplateRevisionId = templateCreated.Id, PublicTemplate.Fold.initialTemplateRevisionOrdinal
                FieldValues = fieldValues
                EditSummary = editSummary })
        |> Gen.filter (Example.validateCreate template >> Result.isOk)
    }

let stackCreatedGen authorId exampleRevisionId fieldValues templateRevision templateRevisionId = gen {
    let! tags  = tagsGen
    let! stack = GenX.autoWith<Stack.Events.Created> nodaConfig
    let! meta = metaGen authorId
    let pointers = fieldValues |> PublicTemplate.getCardTemplatePointers templateRevision |> Result.getOk
    let! cards = pointers |> List.map (fun _ -> GenX.autoWith<Card> nodaConfig) |> ListGen.sequence
    let cards = cards |> List.mapi (fun i c -> { c with Pointer = pointers.Item i })
    return
        { stack with
            FieldValues = fieldValues
            TemplateRevisionId = templateRevisionId
            ExampleRevisionId = Some exampleRevisionId
            DeckIds = Set.empty
            Meta = meta
            Cards = cards
            Tags = tags }
    }

let exampleEditedGen (templateCreated: PublicTemplate.Events.Created) fieldValues (exampleSummary: Summary.Example) authorId = gen {
    let! meta = metaGen authorId
    let! title          = GenX.lString 0 Example.titleMax       genChar
    let! editSummary    = GenX.lString 0 Example.editSummaryMax genChar
    let template = templateCreated |> PublicTemplate.Fold.evolveCreated |> PublicTemplate.Fold.Active
    return!
        nodaConfig
        |> GenX.autoWith<Example.Events.Edited>
        |> Gen.map (fun b ->
            { b with
                Meta = meta
                Ordinal = exampleSummary.CurrentRevision.Ordinal + 1<exampleRevisionOrdinal>
                Title = title
                TemplateRevisionId = templateCreated.Id, PublicTemplate.Fold.initialTemplateRevisionOrdinal
                FieldValues = fieldValues
                EditSummary = editSummary })
        |> Gen.filter (Example.validateEdit template exampleSummary >> Result.isOk)
    }

let tagAddedGen authorId : Stack.Events.TagAdded Gen = gen {
    let! meta = metaGen authorId
    let! tag = tagGen
    return { Meta = meta; Tag = tag }
    }

let revisionChangedGen authorId exampleId = gen {
    let! meta = metaGen authorId
    let! rc = GenX.autoWith<Stack.Events.RevisionChanged> nodaConfig
    let revisionId = Some (exampleId, Example.Fold.initialExampleRevisionOrdinal + 1<exampleRevisionOrdinal>) // medTODO consider None
    return
        { rc with
            Meta = meta
            RevisionId = revisionId }
    }

type ExampleEdit = { TemplateCreated: PublicTemplate.Events.Created; ExampleCreated: Example.Events.Created; Edit: Example.Events.Edited; StackCreated: Stack.Events.Created }
let exampleEditGen userId exampleId = gen {
    let! templateCreated = templateCreatedGen userId
    let! exampleCreated  = exampleCreatedGen templateCreated userId exampleId
    let exampleSummary = Example.Fold.evolveCreated exampleCreated
    let! edit = exampleEditedGen templateCreated exampleCreated.FieldValues exampleSummary userId
    let template = PublicTemplate.Fold.evolveCreated templateCreated
    let! stackCreated = stackCreatedGen userId exampleSummary.CurrentRevisionId exampleCreated.FieldValues template.CurrentRevision template.CurrentRevisionId
    return { TemplateCreated = templateCreated; ExampleCreated = exampleCreated; Edit = edit; StackCreated = stackCreated }
    }

let commentAddedGen = gen {
    let! meta = % Guid.NewGuid() |> metaGen
    let! text = genStringMinMax Example.commentMin Example.commentMax
    let! commentAdded = GenX.autoWith<Example.Events.CommentAdded> nodaConfig
    return { commentAdded with
               Text = text
               Meta = meta }
    }

let deckEditedGen authorId = gen {
    let! name        = deckNameGen
    let! description = deckDescriptionGen
    let! meta = metaGen authorId
    let! edited =
        nodaConfig
        |> GenX.autoWith<PrivateDeck.Events.Edited>
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

type DeckEdit = { DeckCreated: PrivateDeck.Events.Created; DeckEdited: PrivateDeck.Events.Edited }
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
        |> AutoGenConfig.addGenerator (tagAddedGen        userId)
        |> AutoGenConfig.addGenerator (deckEditGen        userId)
        |> AutoGenConfig.addGenerator (exampleEditGen     userId exampleId)
        |> AutoGenConfig.addGenerator (metaGen            userId)
        |> AutoGenConfig.addGenerator (revisionChangedGen userId exampleId)
        |> AutoGenConfig.addGenerator (commentAddedGen)


type StandardProperty(i) =
    inherit PropertyAttribute(typeof<StandardConfig>, LanguagePrimitives.Int32WithMeasure i)
    new () = StandardProperty(100)

type FastProperty() =
    inherit PropertyAttribute(typeof<StandardConfig>, Tests=1<tests>, Shrinks=0<shrinks>, Size = 100)

let eventConfig =
    nodaConfig
    |> AutoGenConfig.addGenerator (% Guid.NewGuid() |> metaGen)

let publicTemplateEventGen = GenX.autoWith<PublicTemplate.Events.Event> eventConfig |> Gen.filter (not << PublicTemplate.Fold.isOrigin)
let           userEventGen = GenX.autoWith<          User.Events.Event> eventConfig |> Gen.filter (not <<           User.Fold.isOrigin)
let           deckEventGen = GenX.autoWith<          PrivateDeck.Events.Event> eventConfig |> Gen.filter (not <<           PrivateDeck.Fold.isOrigin)
let        exampleEventGen = GenX.autoWith<       Example.Events.Event> eventConfig |> Gen.filter (not <<        Example.Fold.isOrigin)
let          stackEventGen = GenX.autoWith<         Stack.Events.Event> eventConfig |> Gen.filter (not <<          Stack.Fold.isOrigin)

type EventConfig =
    static member __ =
        eventConfig
        |> AutoGenConfig.addGenerator publicTemplateEventGen
        |> AutoGenConfig.addGenerator           userEventGen
        |> AutoGenConfig.addGenerator           deckEventGen
        |> AutoGenConfig.addGenerator        exampleEventGen
        |> AutoGenConfig.addGenerator          stackEventGen

type EventProperty(i) =
    inherit PropertyAttribute(typeof<EventConfig>, LanguagePrimitives.Int32WithMeasure i)
    new () = EventProperty(100)

let meta () = % Guid.NewGuid () |> metaGen |> Gen.sample 100 1 |> List.head

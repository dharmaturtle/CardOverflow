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

let userSignedUpGen = gen {
    let! userId = Gen.guid
    let! meta = metaGen (% userId)
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
    let! name = Gen.latin1 |> GenX.lString 1 Template.nameMax
    let! templateType = templateType fieldNames
    let! css = Gen.latin1 |> GenX.lString 0 50
    let! latexPre  = Gen.latin1 |> GenX.lString 0 50
    let! latexPost = Gen.latin1 |> GenX.lString 0 50
    let! meta = metaGen authorId
    let! editSummary = Gen.latin1 |> GenX.lString 0 Template.editSummaryMax
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

type TemplateEdit = { SignedUp: User.Events.SignedUp; TemplateCreated: Template.Events.Created; TemplateEdit: Template.Events.Edited; TemplateCollected: User.Events.TemplateCollected; TemplateDiscarded: User.Events.TemplateDiscarded }
let templateEditGen = gen {
    let! signedUp = userSignedUpGen
    let! created = templateCreatedGen signedUp.Meta.UserId
    let template = Template.Fold.evolveCreated created
    let! edited = templateEditedGen signedUp.Meta.UserId template
    let! collectedMeta = metaGen signedUp.Meta.UserId
    let (collected : User.Events.TemplateCollected) = { Meta = collectedMeta; TemplateRevisionId = template.Id, Template.Fold.initialTemplateRevisionOrdinal }
    let! discardedMeta = metaGen signedUp.Meta.UserId
    let (discarded : User.Events.TemplateDiscarded) = { Meta = discardedMeta; TemplateRevisionId = template.Id, Template.Fold.initialTemplateRevisionOrdinal }
    return { SignedUp = signedUp; TemplateCreated = created; TemplateEdit = edited; TemplateCollected = collected; TemplateDiscarded = discarded }
    }

let deckCreatedGen authorId = gen {
    let! meta = metaGen authorId
    let! name        = GenX.auto<string> |> Gen.filter (Deck.validateName        >> Result.isOk)
    let! description = GenX.auto<string> |> Gen.filter (Deck.validateDescription >> Result.isOk)
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

let exampleCreatedGen (templateCreated: Template.Events.Created) authorId = gen {
    let! title       = GenX.lString 0 Example.titleMax       Gen.latin1
    let! editSummary = GenX.lString 0 Example.editSummaryMax Gen.latin1
    let! meta        = metaGen authorId
    let fieldValues =
        match templateCreated.CardTemplates with
        | Standard _ -> templateCreated.Fields |> List.map (fun x -> x.Name, x.Name + " value")
        | Cloze    _ -> templateCreated.Fields |> List.map (fun x -> x.Name, "Some {{c1::words}}")
        |> toEditFieldAndValue
    let template = templateCreated |> Template.Fold.evolveCreated |> Template.Fold.Active |> Template.Fold.Extant
    return!
        nodaConfig
        |> GenX.autoWith<Example.Events.Created>
        |> Gen.map (fun b ->
            { b with
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

let stackCreatedGen authorId = gen {
    let! cards = GenX.lList 1 50 cardGen
    let! tags  = tagsGen
    let! stack = GenX.autoWith<Stack.Events.Created> nodaConfig
    let! meta = metaGen authorId
    return
        { stack with
            Meta = meta
            Cards = cards
            Tags = tags }
    }

let exampleEditedGen (templateCreated: Template.Events.Created) fieldValues (exampleSummary: Summary.Example) authorId = gen {
    let! meta = metaGen authorId
    let! title          = GenX.lString 0 Example.titleMax       Gen.latin1
    let! editSummary    = GenX.lString 0 Example.editSummaryMax Gen.latin1
    let template = templateCreated |> Template.Fold.evolveCreated |> Template.Fold.Active |> Template.Fold.Extant
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

type ExampleEdit = { SignedUp: User.Events.SignedUp; TemplateCreated: Template.Events.Created; TemplateEdited: Template.Events.Edited; ExampleCreated: Example.Events.Created; ExampleCreated2: Example.Events.Created; Edit: Example.Events.Edited; StackCreated: Stack.Events.Created; TagsChanged: Stack.Events.TagsChanged; RevisionChanged: Stack.Events.RevisionChanged }
let exampleEditGen = gen {
    let! userSignedUp    =    userSignedUpGen
    let! templateCreated = templateCreatedGen userSignedUp.Meta.UserId
    let! exampleCreated  =  exampleCreatedGen templateCreated userSignedUp.Meta.UserId
    let! stackCreated = stackCreatedGen userSignedUp.Meta.UserId
    let exampleSummary = Example.Fold.evolveCreated exampleCreated
    let! edit = exampleEditedGen templateCreated exampleCreated.FieldValues exampleSummary userSignedUp.Meta.UserId
    let template = Template.Fold.evolveCreated templateCreated
    let pointers = exampleCreated.FieldValues |> Template.getCardTemplatePointers template.CurrentRevision |> Result.getOk
    let! cards = pointers |> List.map (fun _ -> GenX.autoWith<Card> nodaConfig) |> SeqGen.sequence
    let cards = cards |> List.mapi (fun i c -> { c with Pointer = pointers.Item i })
    let exampleCreated = { exampleCreated with TemplateRevisionId = template.CurrentRevisionId }
    let! exampleCreatedMeta = metaGen userSignedUp.Meta.UserId
    let! exampleCreatedId = GenX.auto
    let exampleCreated2 = { exampleCreated with Meta = exampleCreatedMeta; Id = exampleCreatedId }
    let edit           = { edit           with TemplateRevisionId = template.CurrentRevisionId; FieldValues = exampleCreated.FieldValues }
    let stackCreated   = { stackCreated   with ExampleRevisionId  = exampleSummary.CurrentRevisionId; Cards = cards }
    let! templateEditedMeta = metaGen userSignedUp.Meta.UserId
    let templateEdited : Template.Events.Edited =
        { Meta          = templateEditedMeta
          Ordinal       = Template.Fold.initialTemplateRevisionOrdinal + 1<templateRevisionOrdinal>
          Name          = "something new"
          Css           = templateCreated.Css
          Fields        = templateCreated.Fields
          LatexPre      = templateCreated.LatexPre
          LatexPost     = templateCreated.LatexPost
          CardTemplates = templateCreated.CardTemplates
          EditSummary   = "done got edited" }
    let! tagsChanged   = tagsChangedGen userSignedUp.Meta.UserId
    let! rcMeta = metaGen userSignedUp.Meta.UserId
    let! revisionChanged = GenX.autoWith<Stack.Events.RevisionChanged> nodaConfig |> Gen.map (fun x -> { x with Meta = rcMeta; RevisionId = exampleCreated.Id, edit.Ordinal })
    return { SignedUp = userSignedUp; TemplateCreated = templateCreated; TemplateEdited = templateEdited; ExampleCreated = exampleCreated; ExampleCreated2 = exampleCreated2; Edit = edit; StackCreated = stackCreated; TagsChanged = tagsChanged; RevisionChanged = revisionChanged }
    }

let deckEditedGen authorId = gen {
    let! name        = GenX.auto<string> |> Gen.filter (Deck.validateName        >> Result.isOk)
    let! description = GenX.auto<string> |> Gen.filter (Deck.validateDescription >> Result.isOk)
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

let deckFollowedGen authorId deckId : User.Events.DeckFollowed Gen = gen {
    let! meta = metaGen authorId
    return { Meta = meta; DeckId = deckId }
    }

let deckUnfollowedGen authorId deckId : User.Events.DeckUnfollowed Gen = gen {
    let! meta = metaGen authorId
    return { Meta = meta; DeckId = deckId }
    }

let optionsEditedGen authorId : User.Events.OptionsEdited Gen = gen {
    let! meta = metaGen authorId
    let! optionsEdited = nodaConfig |> GenX.autoWith<User.Events.OptionsEdited>
    return { optionsEdited with Meta = meta }
    }

type UserEdit = { SignedUp: User.Events.SignedUp; DeckCreated: Deck.Events.Created; OptionsEdited: User.Events.OptionsEdited; DeckFollowed: User.Events.DeckFollowed; DeckUnfollowed: User.Events.DeckUnfollowed; CardSettingsEdited: User.Events.CardSettingsEdited }
let userEditGen = gen {
    let! signedUp      = userSignedUpGen
    let! deckCreated   = deckCreatedGen        signedUp.Meta.UserId
    let! optionsEdited = optionsEditedGen      signedUp.Meta.UserId
    let! followed      = deckFollowedGen       signedUp.Meta.UserId deckCreated.Id
    let! unfollowed    = deckUnfollowedGen     signedUp.Meta.UserId deckCreated.Id
    let! cardsSettings = cardSettingsEditedGen signedUp.Meta.UserId
    return { SignedUp = signedUp; DeckCreated = deckCreated; OptionsEdited = optionsEdited; DeckFollowed = followed; DeckUnfollowed = unfollowed; CardSettingsEdited = cardsSettings }
    }

type DeckEdit = { SignedUp: User.Events.SignedUp; DeckCreated: Deck.Events.Created; DeckEdited: Deck.Events.Edited }
let deckEditGen = gen {
    let! signedUp    = userSignedUpGen
    let! deckCreated = deckCreatedGen signedUp.Meta.UserId
    let! deckEdited  = deckEditedGen  signedUp.Meta.UserId
    return { SignedUp = signedUp; DeckCreated = deckCreated; DeckEdited = deckEdited }
    }

open Hedgehog.Xunit
type StandardConfig =
    static member __ =
        nodaConfig
        |> AutoGenConfig.addGenerator userSignedUpGen
        |> AutoGenConfig.addGenerator userEditGen
        |> AutoGenConfig.addGenerator templateEditGen
        |> AutoGenConfig.addGenerator deckEditGen
        |> AutoGenConfig.addGenerator tagsGen
        |> AutoGenConfig.addGenerator exampleEditGen
        |> AutoGenConfig.addGenerator (% Guid.NewGuid() |> metaGen)


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

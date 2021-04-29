module Domain.User

open FsCodec
open FsCodec.NewtonsoftJson
open TypeShape
open NodaTime
open CardOverflow.Pure
open FsToolkit.ErrorHandling
open Domain.Summary

let streamName (id: UserId) = StreamName.create "User" (id.ToString())

// NOTE - these types and the union case names reflect the actual storage formats and hence need to be versioned with care
[<RequireQualifiedAccess>]
module Events =

    type OptionsEdited =
        { DefaultDeckId: DeckId
          ShowNextReviewTime: bool
          ShowRemainingCardCount: bool
          StudyOrder: StudyOrder
          NextDayStartsAt: LocalTime
          LearnAheadLimit: Duration
          TimeboxTimeLimit: Duration
          IsNightMode: bool
          Timezone: DateTimeZone }

    type DeckFollowed   = { DeckId: DeckId }
    type DeckUnfollowed = { DeckId: DeckId }

    type CollectedTemplatesEdited = { TemplateRevisionIds: TemplateRevisionId list }

    type CardSettingsEdited = { CardSettings: CardSetting list }

    type Event =
        | CollectedTemplatesEdited of CollectedTemplatesEdited
        | CardSettingsEdited       of CardSettingsEdited
        | OptionsEdited            of OptionsEdited
        | DeckFollowed             of DeckFollowed
        | DeckUnfollowed           of DeckUnfollowed
        | Created                  of User
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =
    
    type State =
        | Initial
        | Active of User
    let initial = State.Initial

    let mapActive f = function
        | Active a -> f a |> Active
        | x -> x

    let evolveDeckFollowed (d: Events.DeckFollowed) (s: User) =
        { s with FollowedDecks = s.FollowedDecks |> Set.add d.DeckId }

    let evolveDeckUnfollowed (d: Events.DeckUnfollowed) (s: User) =
        { s with FollowedDecks = s.FollowedDecks |> Set.remove d.DeckId }

    let evolveCollectedTemplatesEdited (d: Events.CollectedTemplatesEdited) (s: User) =
        { s with CollectedTemplates = d.TemplateRevisionIds }

    let evolveCardSettingsEdited (cs: Events.CardSettingsEdited) (s: User) =
        { s with CardSettings = cs.CardSettings }

    let evolveOptionsEdited (o: Events.OptionsEdited) (s: User) =
        { s with
            DefaultDeckId          = o.DefaultDeckId
            ShowNextReviewTime     = o.ShowNextReviewTime
            ShowRemainingCardCount = o.ShowRemainingCardCount
            StudyOrder             = o.StudyOrder
            NextDayStartsAt        = o.NextDayStartsAt
            LearnAheadLimit        = o.LearnAheadLimit
            TimeboxTimeLimit       = o.TimeboxTimeLimit
            IsNightMode            = o.IsNightMode
            Timezone               = o.Timezone }

    let evolve state =
        function
        | Events.Created                  s -> State.Active s
        | Events.CollectedTemplatesEdited o -> state |> mapActive (evolveCollectedTemplatesEdited o)
        | Events.OptionsEdited            o -> state |> mapActive (evolveOptionsEdited o)
        | Events.CardSettingsEdited      cs -> state |> mapActive (evolveCardSettingsEdited cs)
        | Events.DeckFollowed             d -> state |> mapActive (evolveDeckFollowed d)
        | Events.DeckUnfollowed           d -> state |> mapActive (evolveDeckUnfollowed d)
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve

let init id displayName defaultDeckId now cardSettingsId : User =
    { Id = id
      DisplayName = displayName
      DefaultDeckId = defaultDeckId
      ShowNextReviewTime = true
      ShowRemainingCardCount = true
      StudyOrder = StudyOrder.Mixed
      NextDayStartsAt = LocalTime.FromHoursSinceMidnight 4
      LearnAheadLimit = Duration.FromMinutes 20.
      TimeboxTimeLimit = Duration.Zero
      IsNightMode = false
      Created = now
      Modified = now
      Timezone = DateTimeZone.Utc
      CardSettings = CardSetting.newUserCardSettings cardSettingsId |> List.singleton
      FollowedDecks = Set.empty
      CollectedTemplates = [] } // highTODO give 'em some templates to work with

let upgradeRevision (collectedTemplates: TemplateRevisionId list) currentRevision newRevision =
    if collectedTemplates |> List.contains currentRevision then
        collectedTemplates |> List.map (fun x -> if x = currentRevision then newRevision else x)
    else collectedTemplates @ [newRevision]

let validateDisplayName (displayName: string) =
    (4 <= displayName.Length && displayName.Length <= 18)
    |> Result.requireTrue $"The display name '{displayName}' must be between 4 and 18 characters."

let validateSummary (summary: User) = result {
    do! validateDisplayName summary.DisplayName
    do! Result.requireEqual
            (summary.DisplayName)
            (summary.DisplayName.Trim())
            $"Remove the spaces before and/or after your display name: '{summary.DisplayName}'."
    }

let isDeckFollowed (summary: User) deckId =
    summary.FollowedDecks.Contains deckId

let validateDeckFollowed (summary: User) deckId =
    Result.requireTrue
        $"You don't follow the deck '{deckId}'."
        (isDeckFollowed summary deckId)

let validateDeckNotFollowed (summary: User) deckId =
    Result.requireFalse
        $"You already follow the deck '{deckId}'."
        (isDeckFollowed summary deckId)

let validateDeck maybeDeck userId deckId =
    match maybeDeck with
    | Some deck -> result {
        do! match deck.Visibility with
            | Public -> Ok ()
            | Private -> Result.requireEqual deck.AuthorId userId $"You aren't allowed to see the deck '{deckId}'."
        return deck.Id
        }
    | None -> Error $"The deck '{deckId}' doesn't exist."

//let newTemplates incomingTemplates (author: User) = Set.difference (Set.ofList incomingTemplates) (Set.ofList author.CollectedTemplates)

let validateCollectedTemplatesEdited (templates: Events.CollectedTemplatesEdited) (nonexistingTemplates: TemplateRevisionId list) = result {
    let c1 = templates.TemplateRevisionIds |> Set.ofList |> Set.count
    let c2 = templates.TemplateRevisionIds |> List.length
    do! Result.requireEqual c1 c2 "You can't have duplicate template revisions."
    do! Result.requireNotEmpty "You must have at least 1 template revision" templates.TemplateRevisionIds
    do! Result.requireEmpty $"The following templates don't exist: {nonexistingTemplates}" nonexistingTemplates
    }

let decideCreate (summary: User) state =
    match state with
    | Fold.State.Active s -> Error $"User '{s.Id}' already exists."
    | Fold.State.Initial  -> validateSummary summary
    |> addEvent (Events.Created summary)

let decideCollectedTemplatesEdited (collected: Events.CollectedTemplatesEdited) userId nonexistingTemplates state =
    match state with
    | Fold.State.Initial  -> Error $"User '{userId}' doesn't exist."
    | Fold.State.Active _ -> validateCollectedTemplatesEdited collected nonexistingTemplates
    |> addEvent (Events.CollectedTemplatesEdited collected)

let decideOptionsEdited (o: Events.OptionsEdited) defaultDeckUserId state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit the options of a user that doesn't exist."
    | Fold.State.Active s -> result {
        do! Result.requireEqual s.Id defaultDeckUserId $"Deck {o.DefaultDeckId} doesn't belong to User {s.Id}"
    } |> addEvent (Events.OptionsEdited o)

let decideCardSettingsEdited (cs: Events.CardSettingsEdited) state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit the options of a user that doesn't exist."
    | Fold.State.Active _ -> result {
        do! cs.CardSettings |> List.filter (fun x -> x.IsDefault) |> List.length |> Result.requireEqualTo 1 "You must have 1 default card setting."
    } |> addEvent (Events.CardSettingsEdited cs)

let decideFollowDeck maybeDeck deckId state =
    match state with
    | Fold.State.Initial  -> Error "You can't follow a deck if you don't exist..."
    | Fold.State.Active s -> result {
        let! deckId = validateDeck maybeDeck s.Id deckId
        do! validateDeckNotFollowed s deckId
    } |> addEvent (Events.DeckFollowed { Events.DeckFollowed.DeckId = deckId })

let decideUnfollowDeck (deckId: DeckId) state =
    match state with
    | Fold.State.Initial  -> Error "You can't unfollow a deck if you don't exist..."
    | Fold.State.Active s -> result {
        do! validateDeckFollowed s deckId
    } |> addEvent (Events.DeckUnfollowed { Events.DeckUnfollowed.DeckId = deckId })

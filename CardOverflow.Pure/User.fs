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

    type SignedUp =
        { Meta: Meta
          DisplayName: string
          DefaultDeckId: DeckId
          
          ShowNextReviewTime: bool
          ShowRemainingCardCount: bool
          StudyOrder: StudyOrder
          NextDayStartsAt: LocalTime
          LearnAheadLimit: Duration
          TimeboxTimeLimit: Duration
          IsNightMode: bool
          Timezone: DateTimeZone
          CardSettings: CardSetting list
          FollowedDecks: DeckId Set
          CollectedTemplates: TemplateRevisionId list }

    type OptionsEdited =
        { Meta: Meta
          DefaultDeckId: DeckId
          ShowNextReviewTime: bool
          ShowRemainingCardCount: bool
          StudyOrder: StudyOrder
          NextDayStartsAt: LocalTime
          LearnAheadLimit: Duration
          TimeboxTimeLimit: Duration
          IsNightMode: bool
          Timezone: DateTimeZone }

    type DeckFollowed   = { Meta: Meta; DeckId: DeckId }
    type DeckUnfollowed = { Meta: Meta; DeckId: DeckId }

    type TemplateCollected = { Meta: Meta; TemplateRevisionId: TemplateRevisionId }
    type TemplateDiscarded = { Meta: Meta; TemplateRevisionId: TemplateRevisionId }

    type CardSettingsEdited = { Meta: Meta; CardSettings: CardSetting list }

    type Event =
        | TemplateCollected        of TemplateCollected
        | TemplateDiscarded        of TemplateDiscarded
        | CardSettingsEdited       of CardSettingsEdited
        | OptionsEdited            of OptionsEdited
        | DeckFollowed             of DeckFollowed
        | DeckUnfollowed           of DeckUnfollowed
        | SignedUp                 of SignedUp
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

    let evolveTemplateCollected (d: Events.TemplateCollected) (s: User) =
        { s with CollectedTemplates = d.TemplateRevisionId :: s.CollectedTemplates }

    let evolveTemplateDiscarded (d: Events.TemplateDiscarded) (s: User) =
        { s with CollectedTemplates = s.CollectedTemplates |> List.filter (fun x -> x <> d.TemplateRevisionId ) }

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

    let evolveSignedUp (e: Events.SignedUp) =
        {   Id                     = e.Meta.UserId
            DisplayName            = e.DisplayName
            DefaultDeckId          = e.DefaultDeckId
            ShowNextReviewTime     = e.ShowNextReviewTime
            ShowRemainingCardCount = e.ShowRemainingCardCount
            StudyOrder             = e.StudyOrder
            NextDayStartsAt        = e.NextDayStartsAt
            LearnAheadLimit        = e.LearnAheadLimit
            TimeboxTimeLimit       = e.TimeboxTimeLimit
            IsNightMode            = e.IsNightMode
            Created                = e.Meta.ServerCreatedAt.Value
            Modified               = e.Meta.ServerCreatedAt.Value
            Timezone               = e.Timezone
            CardSettings           = e.CardSettings
            FollowedDecks          = e.FollowedDecks
            CollectedTemplates     = e.CollectedTemplates }

    let evolve state =
        function
        | Events.SignedUp                 s -> s |> evolveSignedUp |> State.Active
        | Events.TemplateCollected        o -> state |> mapActive (evolveTemplateCollected o)
        | Events.TemplateDiscarded        o -> state |> mapActive (evolveTemplateDiscarded o)
        | Events.OptionsEdited            o -> state |> mapActive (evolveOptionsEdited o)
        | Events.CardSettingsEdited      cs -> state |> mapActive (evolveCardSettingsEdited cs)
        | Events.DeckFollowed             d -> state |> mapActive (evolveDeckFollowed d)
        | Events.DeckUnfollowed           d -> state |> mapActive (evolveDeckUnfollowed d)
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit      : Events.Event seq -> State = fold initial

let getActive state =
    match state with
    | Fold.State.Active u -> Ok u
    | _ -> Error "User doesn't exist."

let init meta displayName defaultDeckId cardSettingsId : Events.SignedUp =
    { Meta = meta
      DisplayName = displayName
      DefaultDeckId = defaultDeckId
      ShowNextReviewTime = true
      ShowRemainingCardCount = true
      StudyOrder = StudyOrder.Mixed
      NextDayStartsAt = LocalTime.FromHoursSinceMidnight 4
      LearnAheadLimit = Duration.FromMinutes 20.
      TimeboxTimeLimit = Duration.Zero
      IsNightMode = false
      Timezone = DateTimeZone.Utc
      CardSettings = CardSetting.newUserCardSettings cardSettingsId |> List.singleton
      FollowedDecks = Set.empty
      CollectedTemplates = [] } // highTODO give 'em some templates to work with

let validateDisplayName (displayName: string) =
    (4 <= displayName.Length && displayName.Length <= 18)
    |> Result.requireTrue $"The display name '{displayName}' must be between 4 and 18 characters."

let validateSignedUp (signedUp: Events.SignedUp) = result {
    do! validateDisplayName signedUp.DisplayName
    do! Result.requireEqual
            (signedUp.DisplayName)
            (signedUp.DisplayName.Trim())
            $"Remove the spaces before and/or after your display name: '{signedUp.DisplayName}'."
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

let validateDeck (deck: Deck.Fold.State) userId deckId = result {
    let! deck = deck |> Deck.getActive
    return!
        match deck.Visibility with
        | Public -> Ok ()
        | Private -> Result.requireEqual deck.AuthorId userId $"You aren't allowed to see the deck '{deckId}'."
    }

//let newTemplates incomingTemplates (author: User) = Set.difference (Set.ofList incomingTemplates) (Set.ofList author.CollectedTemplates)

let checkPermissions (meta: Meta) (u: User) =
    Result.requireEqual meta.UserId u.Id "You aren't allowed to edit this user."

let validateTemplateCollected (templateCollected: Events.TemplateCollected) (template: Template.Fold.State) (u: User) = result {
    do! checkPermissions templateCollected.Meta u
    do! u.CollectedTemplates |> List.exists ((=) templateCollected.TemplateRevisionId)
        |> Result.requireFalse "You can't have duplicate template revisions."
    let! template = template |> Template.getActive
    let isVisible =
        match template.Visibility with
        | Public -> true
        | _ -> false
    do! Result.requireTrue $"This template doesn't exist: {templateCollected.TemplateRevisionId}" isVisible
    }

let validateTemplateDiscarded (templateDiscarded: Events.TemplateDiscarded) (u: User) = result {
    do! checkPermissions templateDiscarded.Meta u
    do! u.CollectedTemplates |> List.exists ((=) templateDiscarded.TemplateRevisionId)
        |> Result.requireTrue $"You haven't collected this template: {templateDiscarded.TemplateRevisionId}."
    //let u = u |> Fold.evolveTemplateDiscarded templateDiscarded
    //do! Result.requireNotEmpty "You must have at least 1 template revision" u.CollectedTemplates
    }

let decideSignedUp (signedUp: Events.SignedUp) state =
    match state with
    | Fold.State.Active s -> Error $"User '{s.Id}' already exists."
    | Fold.State.Initial  -> validateSignedUp signedUp
    |> addEvent (Events.SignedUp signedUp)

let decideTemplateCollected (templateCollected: Events.TemplateCollected) template state =
    match state with
    | Fold.State.Initial  -> Error $"User '{templateCollected.Meta.UserId}' doesn't exist."
    | Fold.State.Active u -> validateTemplateCollected templateCollected template u
    |> addEvent (Events.TemplateCollected templateCollected)

let decideTemplateDiscarded (templateDiscarded: Events.TemplateDiscarded) state =
    match state with
    | Fold.State.Initial  -> Error $"User '{templateDiscarded.Meta.UserId}' doesn't exist."
    | Fold.State.Active u -> validateTemplateDiscarded templateDiscarded u
    |> addEvent (Events.TemplateDiscarded templateDiscarded)

let validateOptionsEdited (o: Events.OptionsEdited) ``option's default deck`` (s: User) = result {
    let! ``option's default deck`` = ``option's default deck`` |> Deck.getActive
    do! checkPermissions o.Meta s
    do! Result.requireEqual s.Id ``option's default deck``.AuthorId $"Deck {o.DefaultDeckId} doesn't belong to User {s.Id}"
    }

let decideOptionsEdited (o: Events.OptionsEdited) (``option's default deck``: Deck.Fold.State) state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit the options of a user that doesn't exist."
    | Fold.State.Active u -> validateOptionsEdited o ``option's default deck`` u
    |> addEvent (Events.OptionsEdited o)

let validateCardSettingsEdited (cs: Events.CardSettingsEdited) (u: User) = result {
    do! checkPermissions cs.Meta u
    do! cs.CardSettings |> List.filter (fun x -> x.IsDefault) |> List.length |> Result.requireEqualTo 1 "You must have 1 default card setting."
    }

let decideCardSettingsEdited cs state =
    match state with
    | Fold.State.Initial  -> Error "Can't edit the options of a user that doesn't exist."
    | Fold.State.Active u -> validateCardSettingsEdited cs u
    |> addEvent (Events.CardSettingsEdited cs)

let validateFollowDeck deck (deckFollowed: Events.DeckFollowed) (u: User) = result {
    do! checkPermissions deckFollowed.Meta u
    do! validateDeck deck u.Id deckFollowed.DeckId
    do! validateDeckNotFollowed u deckFollowed.DeckId
    }

let decideFollowDeck deck (deckFollowed: Events.DeckFollowed) state =
    match state with
    | Fold.State.Initial  -> Error "You can't follow a deck if you don't exist..."
    | Fold.State.Active u -> validateFollowDeck deck deckFollowed u
    |> addEvent (Events.DeckFollowed deckFollowed)

let validateUnfollowDeck (deckUnfollowed: Events.DeckUnfollowed) (u: User) = result {
    do! checkPermissions deckUnfollowed.Meta u
    do! validateDeckFollowed u deckUnfollowed.DeckId
    }

let decideUnfollowDeck deckUnfollowed state =
    match state with
    | Fold.State.Initial  -> Error "You can't unfollow a deck if you don't exist..."
    | Fold.State.Active u -> validateUnfollowDeck deckUnfollowed u
    |> addEvent (Events.DeckUnfollowed deckUnfollowed)

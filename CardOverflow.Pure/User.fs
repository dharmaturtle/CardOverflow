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
          
          ShowNextReviewTime: bool
          ShowRemainingCardCount: bool
          StudyOrder: StudyOrder
          NextDayStartsAt: LocalTime
          LearnAheadLimit: Duration
          TimeboxTimeLimit: Duration
          IsNightMode: bool
          Timezone: DateTimeZone
          CardSettings: CardSetting list
          CollectedTemplates: TemplateRevisionId list }

    type OptionsEdited =
        { Meta: Meta
          ShowNextReviewTime: bool
          ShowRemainingCardCount: bool
          StudyOrder: StudyOrder
          NextDayStartsAt: LocalTime
          LearnAheadLimit: Duration
          TimeboxTimeLimit: Duration
          IsNightMode: bool
          Timezone: DateTimeZone }

    type TemplateCollected = { Meta: Meta; TemplateRevisionId: TemplateRevisionId }
    type TemplateDiscarded = { Meta: Meta; TemplateRevisionId: TemplateRevisionId }

    type CardSettingsEdited = { Meta: Meta; CardSettings: CardSetting list }

    module Compaction =
        type State =
            | Initial
            | Active of User
        type Snapshotted = { State: State }

    type Event =
        | TemplateCollected        of TemplateCollected
        | TemplateDiscarded        of TemplateDiscarded
        | CardSettingsEdited       of CardSettingsEdited
        | OptionsEdited            of OptionsEdited
        | SignedUp                 of SignedUp
        | // revise this tag if you break the unfold schema
          //[<System.Runtime.Serialization.DataMember(Name="snapshot-v1")>]
          Snapshotted              of Compaction.Snapshotted
        interface UnionContract.IUnionContract
    
    let codec = Codec.Create<Event> jsonSerializerSettings

module Fold =
    type State =
        | Initial
        | Active of User
    let initial = State.Initial

    let toSnapshot (s: State) : Events.Compaction.Snapshotted =
        match s with
        | Initial  -> { State = Events.Compaction.Initial  }
        | Active x -> { State = Events.Compaction.Active x }
    let ofSnapshot ({ State = s }: Events.Compaction.Snapshotted) : State =
        match s with
        | Events.Compaction.Initial  -> Initial
        | Events.Compaction.Active x -> Active x

    let mapActive f = function
        | Active x -> x |> f |> Active
        | x -> x

    let guard (old: User) (meta: Meta) updated =
        if old.CommandIds.Contains meta.CommandId
        then old
        else { updated with
                   ServerModifiedAt = meta.ServerReceivedAt.Value
                   CommandIds = old.CommandIds.Add meta.CommandId }

    let evolveTemplateCollected (e: Events.TemplateCollected) (s: User) =
        guard s e.Meta { s with CollectedTemplates = e.TemplateRevisionId :: s.CollectedTemplates }

    let evolveTemplateDiscarded (e: Events.TemplateDiscarded) (s: User) =
        guard s e.Meta { s with CollectedTemplates = s.CollectedTemplates |> List.filter (fun x -> x <> e.TemplateRevisionId ) }

    let evolveCardSettingsEdited (e: Events.CardSettingsEdited) (s: User) =
        guard s e.Meta { s with CardSettings = e.CardSettings }

    let evolveOptionsEdited (e: Events.OptionsEdited) (s: User) =
        guard s e.Meta
            { s with
                ShowNextReviewTime     = e.ShowNextReviewTime
                ShowRemainingCardCount = e.ShowRemainingCardCount
                StudyOrder             = e.StudyOrder
                NextDayStartsAt        = e.NextDayStartsAt
                LearnAheadLimit        = e.LearnAheadLimit
                TimeboxTimeLimit       = e.TimeboxTimeLimit
                IsNightMode            = e.IsNightMode
                Timezone               = e.Timezone }

    let evolveSignedUp (e: Events.SignedUp) =
        {   Id                     = e.Meta.UserId
            CommandIds             = e.Meta.CommandId |> Set.singleton
            DisplayName            = e.DisplayName
            ShowNextReviewTime     = e.ShowNextReviewTime
            ShowRemainingCardCount = e.ShowRemainingCardCount
            StudyOrder             = e.StudyOrder
            NextDayStartsAt        = e.NextDayStartsAt
            LearnAheadLimit        = e.LearnAheadLimit
            TimeboxTimeLimit       = e.TimeboxTimeLimit
            IsNightMode            = e.IsNightMode
            ServerCreatedAt        = e.Meta.ServerReceivedAt.Value
            ServerModifiedAt       = e.Meta.ServerReceivedAt.Value
            Timezone               = e.Timezone
            CardSettings           = e.CardSettings
            CollectedTemplates     = e.CollectedTemplates }

    let evolve state =
        function
        | Events.SignedUp                 s -> s |> evolveSignedUp |> Active
        | Events.TemplateCollected        o -> state |> mapActive (evolveTemplateCollected o)
        | Events.TemplateDiscarded        o -> state |> mapActive (evolveTemplateDiscarded o)
        | Events.OptionsEdited            o -> state |> mapActive (evolveOptionsEdited o)
        | Events.CardSettingsEdited      cs -> state |> mapActive (evolveCardSettingsEdited cs)
        | Events.Snapshotted              s -> s |> ofSnapshot
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    let foldInit :      Events.Event seq -> State = Seq.fold evolve initial
    let isOrigin = function Events.Snapshotted _ -> true | _ -> false
    let snapshot (state: State) : Events.Event =
        state |> toSnapshot |> Events.Snapshotted

let getActive state =
    match state with
    | Fold.Active x -> Ok x
    | _ -> Error "User doesn't exist."

let init meta displayName cardSettingsId : Events.SignedUp =
    { Meta = meta
      DisplayName = displayName
      ShowNextReviewTime = true
      ShowRemainingCardCount = true
      StudyOrder = StudyOrder.Mixed
      NextDayStartsAt = LocalTime.FromHoursSinceMidnight 4
      LearnAheadLimit = Duration.FromMinutes 20.
      TimeboxTimeLimit = Duration.Zero
      IsNightMode = false
      Timezone = DateTimeZone.Utc
      CardSettings = CardSetting.newUserCardSettings cardSettingsId |> List.singleton
      CollectedTemplates = [] } // highTODO give 'em some templates to work with

let validateDisplayName (displayName: string) =
    (4 <= displayName.Length && displayName.Length <= 18)
    |> Result.requireTrue (CError $"The display name '{displayName}' must be between 4 and 18 characters.")

let validateSignedUp (signedUp: Events.SignedUp) = result {
    do! validateDisplayName signedUp.DisplayName
    do! Result.requireEqual
            (signedUp.DisplayName)
            (signedUp.DisplayName.Trim())
            (CError $"Remove the spaces before and/or after your display name: '{signedUp.DisplayName}'.")
    }

//let newTemplates incomingTemplates (author: User) = Set.difference (Set.ofList incomingTemplates) (Set.ofList author.CollectedTemplates)

let checkMeta (meta: Meta) (u: User) = result {
    do! Result.requireEqual meta.UserId u.Id (CError "You aren't allowed to edit this user.")
    do! idempotencyCheck meta u.CommandIds
    }

let validateTemplateCollected (templateCollected: Events.TemplateCollected) (template: PublicTemplate.Fold.State) (u: User) = result {
    do! checkMeta templateCollected.Meta u
    do! u.CollectedTemplates |> List.exists ((=) templateCollected.TemplateRevisionId)
        |> Result.requireFalse (CError "You can't have duplicate template revisions.")
    }

let validateTemplateDiscarded (templateDiscarded: Events.TemplateDiscarded) (u: User) = result {
    do! checkMeta templateDiscarded.Meta u
    do! u.CollectedTemplates |> List.exists ((=) templateDiscarded.TemplateRevisionId)
        |> Result.requireTrue (CError $"You haven't collected this template: {templateDiscarded.TemplateRevisionId}.")
    //let u = u |> Fold.evolveTemplateDiscarded templateDiscarded
    //do! Result.requireNotEmpty "You must have at least 1 template revision" u.CollectedTemplates
    }

let decideSignedUp (signedUp: Events.SignedUp) state =
    match state with
    | Fold.Active       s -> idempotencyCheck signedUp.Meta s.CommandIds |> bindCCError $"User '{s.Id}' already exists."
    | Fold.State.Initial  -> validateSignedUp signedUp
    |> addEvent (Events.SignedUp signedUp)

let decideTemplateCollected (templateCollected: Events.TemplateCollected) template state =
    match state with
    | Fold.State.Initial  -> idempotencyBypass |> bindCCError $"User '{templateCollected.Meta.UserId}' doesn't exist."
    | Fold.Active       u -> validateTemplateCollected templateCollected template u
    |> addEvent (Events.TemplateCollected templateCollected)

let decideTemplateDiscarded (templateDiscarded: Events.TemplateDiscarded) state =
    match state with
    | Fold.State.Initial  -> idempotencyBypass |> bindCCError $"User '{templateDiscarded.Meta.UserId}' doesn't exist."
    | Fold.Active       u -> validateTemplateDiscarded templateDiscarded u
    |> addEvent (Events.TemplateDiscarded templateDiscarded)

let validateOptionsEdited (o: Events.OptionsEdited) (s: User) = result {
    do! checkMeta o.Meta s
    }

let decideOptionsEdited (o: Events.OptionsEdited) state =
    match state with
    | Fold.State.Initial  -> idempotencyBypass |> bindCCError "Can't edit the options of a user that doesn't exist."
    | Fold.Active       u -> validateOptionsEdited o u
    |> addEvent (Events.OptionsEdited o)

let validateCardSettingsEdited (cs: Events.CardSettingsEdited) (u: User) = result {
    do! checkMeta cs.Meta u
    do! cs.CardSettings |> List.filter (fun x -> x.IsDefault) |> List.length |> Result.requireEqualTo 1 (CError "You must have 1 default card setting.")
    }

let decideCardSettingsEdited (cs: Events.CardSettingsEdited) state =
    match state with
    | Fold.State.Initial  -> idempotencyBypass |> bindCCError "Can't edit the options of a user that doesn't exist."
    | Fold.Active       u -> validateCardSettingsEdited cs u
    |> addEvent (Events.CardSettingsEdited cs)

module Pentive.Client.CardSetting

open Domain.User
open CardOverflow.Pure
open Domain
open System
open Elmish
open Bolero
open Bolero.Html
open Pentive.Client.Auth
open Pentive.Client

type AuthenticatedPayload =
    {
        Selected: Guid
        CardSettings: Events.UsersCardSettings
    }

let selected model =
    model.CardSettings.all
    |> List.find (fun x -> x.Id = model.Selected)

type Msg =
    | Selected of Guid
    | Added
    | Saved of Events.UsersCardSettings
    | DetailMsg of CardSettingDetail.Msg

type Cmd =
    | Save of Events.UsersCardSettings

let updateGenerateDetailMsg msg model =
    model
    |> selected
    |> CardSettingDetail.generate msg
    |> function
    | Some cmd ->
        match cmd with
        | CardSettingDetail.MakeDefault newDefault ->
            { model with CardSettings = Events.setDefault model.CardSettings newDefault }, []
        | CardSettingDetail.Save ->
            model, [Save model.CardSettings]
    | None -> model, []

let update message model =
    match model with
    | Anonymous -> model
    | Authenticated model ->
        match message with
        | Selected x -> { model with Selected = x }
        | Added ->
        let id = Guid.NewGuid()
        let cs = CardOverflow.Pure.CardSetting.initialCardSettings id "My New Card Setting"
        { model with
            Selected = id
            CardSettings = Events.addCardSetting model.CardSettings cs
        }
        | Saved _ -> model
    | DetailMsg msg ->
        let model, _ = updateGenerateDetailMsg msg model
        { model with
            CardSettings =
                model
                |> selected
                    |> CardSettingDetail.update msg
                |> Events.updateCardSetting model.CardSettings
        }
        |> Authenticated

let generate message (model: Model) =
    match model with
    | Anonymous -> []
    | Authenticated model ->
        match message with
        | Selected _ -> []
        | Added      -> []
        | Saved m    -> [Save m]
    | DetailMsg msg ->
        let _, cmd = updateGenerateDetailMsg msg model
        cmd

type MainTemplate = Template<"wwwroot/CardSettingEditor.html">

let view (auth: Auth.Model) (model: Model) dispatch =
    match auth with
    | Authenticating _ ->
        text "Authenticating..."
    | Anonymous _ ->
        text "You must login to see the CardSettingEditor page."
    | Authenticated model ->
        let isDefault (s: CardSetting) = model.CardSettings.Default.Id = s.Id
        let selected =
            let selected = selected model
            selected
            |> CardSettingDetail.view (isDefault selected) (DetailMsg >> dispatch)
        let detailListItems =
            let defaultButton (name: string) isDefault id =
                MainTemplate.DetailListItem()
                    .Name(name)
                    .Default(if isDefault then span [ attr.``class`` "oi oi-star"] [] else empty)
                    .Selected(fun _ -> id |> Selected |> dispatch)
                    .Elt()
            model.CardSettings.all
            |> List.map (fun x -> defaultButton x.Name (isDefault x) x.Id)
            |> concat
        MainTemplate()
            .SelectedDetail(selected)
            .DetailListItems(detailListItems)
            .Elt()

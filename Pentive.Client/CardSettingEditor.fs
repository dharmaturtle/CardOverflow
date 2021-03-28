module Pentive.Client.CardSettingEditor

open Domain.User
open CardOverflow.Pure
open Domain
open System
open Elmish
open Bolero
open Bolero.Html
open Pentive.Client.Auth
open Pentive.Client

type Model =
    {
        Selected: Guid
        CardSettings: Events.UsersCardSettings
    }

let selected model =
    model.CardSettings.all
    |> List.find (fun x -> x.Id = model.Selected)

type Msg =
    | Selected of Guid
    | Add
    | DetailMsg of CardSettingEditorDetail.Msg

type Cmd =
    | Save of Events.UsersCardSettings

let updateGenerateDetailMsg msg model =
    model
    |> selected
    |> CardSettingEditorDetail.generate msg
    |> function
    | Some cmd ->
        match cmd with
        | CardSettingEditorDetail.MakeDefault newDefault ->
            { model with CardSettings = Events.setDefault model.CardSettings newDefault }, []
        | CardSettingEditorDetail.Save ->
            model, [Save model.CardSettings]
    | None -> model, []

let update message model =
    match message with
    | Selected x -> { model with Selected = x }
    | Add ->
        let id = Guid.NewGuid()
        let cs = CardOverflow.Pure.CardSetting.defaultCardSettings id "My New Card Setting"
        { model with
            Selected = id
            CardSettings = Events.addCardSetting model.CardSettings cs
        }
    | DetailMsg msg ->
        let model, _ = updateGenerateDetailMsg msg model
        { model with
            CardSettings =
                model
                |> selected
                |> CardSettingEditorDetail.update msg
                |> Events.updateCardSetting model.CardSettings
        }

let generate message (model: Model) =
    match message with
    | Selected _ -> []
    | Add        -> []
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
    | Authenticated _ ->
        let isDefault (s: CardSetting) = model.CardSettings.Default.Id = s.Id
        let selected =
            let selected = selected model
            selected
            |> CardSettingEditorDetail.view (isDefault selected) (DetailMsg >> dispatch)
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

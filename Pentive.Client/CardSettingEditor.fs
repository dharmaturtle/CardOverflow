module Pentive.Client.CardSettingEditor

open Domain.User
open CardOverflow.Pure
open System
open Elmish
open Bolero
open Bolero.Html
open Pentive.Client.Auth
open Pentive.Client

type Model =
    {
        Selected: Guid
        CardSettings: CardSetting list
    }

let selected model =
    model.CardSettings
    |> List.find (fun x -> x.Id = model.Selected)

type Msg =
    | Selected of Guid
    | Add
    | DetailMsg of CardSettingEditorDetail.Msg

type Cmd =
    | Save of CardSetting list

let updateGenerateDetailMsg msg model =
    model
    |> selected
    |> CardSettingEditorDetail.generate msg
    |> function
    | Some cmd ->
        match cmd with
        | CardSettingEditorDetail.MakeDefault id ->
            let cardSettings =
                model.CardSettings
                |> List.map (fun cs -> { cs with IsDefault = cs.Id = id })
            { model with CardSettings = cardSettings }, []
        | CardSettingEditorDetail.Save ->
            model, [Save model.CardSettings]
    | None -> model, []

let update message model =
    match message with
    | Selected x -> { model with Selected = x }
    | Add ->
        let id = Guid.NewGuid()
        let cs = CardOverflow.Pure.CardSetting.defaultCardSettings id "My New Card Setting" false
        { model with
            Selected = id
            CardSettings = cs :: model.CardSettings }
    | DetailMsg msg ->
        let selected = model |> selected |> CardSettingEditorDetail.update msg
        let settings =
            model.CardSettings
            |> List.map (fun x ->
                if x.Id = selected.Id then
                    selected
                else x
            )
        let model, _ = updateGenerateDetailMsg msg model
        { model with CardSettings = settings }

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
        let selected =
            selected model
            |> CardSettingEditorDetail.view (DetailMsg >> dispatch)
        let detailListItems =
            let defaultButton (name: string) isDefault id =
                MainTemplate.DetailListItem()
                    .Name(name)
                    .Default(if isDefault then span [ attr.``class`` "oi oi-star"] [] else empty)
                    .Selected(fun _ -> id |> Selected |> dispatch)
                    .Elt()
            model.CardSettings
            |> List.map (fun x -> defaultButton x.Name x.IsDefault x.Id)
            |> concat
        MainTemplate()
            .SelectedDetail(selected)
            .DetailListItems(detailListItems)
            .Elt()

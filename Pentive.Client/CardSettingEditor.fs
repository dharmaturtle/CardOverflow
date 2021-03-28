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
type Model =
    | Anonymous
    | Authenticated of AuthenticatedPayload

let init = Anonymous
let init2 (auth: Auth.Model) =
    match auth with
    | Auth.Authenticated m ->
        {   Selected = m.CardSettings.Default.Id
            CardSettings = m.CardSettings
        } |> Authenticated
    | _ -> Anonymous
    
let fromAuth (auth: Auth.Model) (model: Model) =
    match auth with
    | Auth.Authenticated m ->
        {   Selected =
                match model with
                | Anonymous -> m.CardSettings.Default.Id
                | Authenticated m -> m.Selected
            CardSettings = m.CardSettings
        } |> Authenticated
    | _ -> Anonymous

let selected model =
    model.CardSettings.all
    |> List.find (fun x -> x.Id = model.Selected)

type Msg =
    | Initialized of Events.UsersCardSettings
    | Selected of Guid
    | Added
    | Saved of Events.UsersCardSettings
    | DetailMsg of CardSettingDetail.Msg

type Cmd =
    | Save       of Events.UsersCardSettings

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

let update (auth: Auth.Model) message model =
    
    match fromAuth auth model with
    | Authenticated model ->
        printfn "BEING UPDATED; other length is %A" <| model.CardSettings.Others.Length
        match message with
        | Selected x -> { model with Selected = x }
        | Initialized auth ->
            {   Selected = auth.Default.Id
                CardSettings = auth
            }
        | Added ->
            let id = Guid.NewGuid()

            printfn "DUDE WHAT %A" <| id
            let cs = CardOverflow.Pure.CardSetting.initialCardSettings id "My New Card Setting"
            printfn "LENGTH %A" <| (Events.addCardSetting model.CardSettings cs).Others.Length
            //printfn "DUDE WHAT %A" <| Events.addCardSetting model.CardSettings cs
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
    | _ -> Anonymous

let generate message (model: Model) =
    match model with
    | Anonymous -> []
    | Authenticated model ->
        match message with
        | Selected _    -> []
        | Added         -> []
        | Initialized _ -> []
        | Saved m       -> [Save m]
        | DetailMsg msg ->
            let _, cmd = updateGenerateDetailMsg msg model
            cmd

type MainTemplate = Template<"wwwroot/CardSettingEditor.html">

let view (auth: Auth.Model) (model: Model) dispatch =
    match model with
    | Anonymous ->
        text "You must login to see the CardSetting page."
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
            .Add( fun _ -> Added |> dispatch)
            .Save(fun _ -> model.CardSettings |> Saved |> dispatch)
            .Elt()

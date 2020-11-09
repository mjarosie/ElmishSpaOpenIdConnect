[<RequireQualifiedAccess>]
module Callback

open Fable.Core.JsInterop
open Fable.React
open Elmish
open Elmish.Navigation
open Browser

type Model = {
    Code: string
    Scopes: string list
    State: string
    SessionState: string
}

type Msg =
    | HandleSigninRedirectCallback
    | OnSigninCallbackSuccess of Fable.OidcClient.User
    | OnSigninCallbackError of exn

let init (code: string) (scopes: string list) (state: string) (sessionState: string) : Model * Cmd<Msg> =
    let model = {
        Code = code
        Scopes = scopes
        State = state
        SessionState = sessionState }

    model, Cmd.ofMsg HandleSigninRedirectCallback

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | HandleSigninRedirectCallback ->
        let mgr: Fable.OidcClient.UserManager =
            Fable.OidcClient.Oidc.UserManager.Create !!{| response_mode = Some "query" |}
        model, Cmd.OfPromise.either mgr.signinRedirectCallback () OnSigninCallbackSuccess OnSigninCallbackError
    | OnSigninCallbackSuccess user ->
        console.error "OnSigninCallbackSuccess should be handled from the parent application"
        model, Navigation.modifyUrl "#"
    | OnSigninCallbackError e ->
        console.error "OnSigninCallbackError should be handled from the parent application"
        model, Navigation.modifyUrl "#"

let view (model: Model) =
    div [ ] [
        div [] [
            str (sprintf "code: %s" model.Code)
        ]
        div [] [
            str (model.Scopes |> List.fold (fun r s -> r + s + "\n") "scopes:\n")
        ]
        div [] [
            str (sprintf "state: %s" model.State)
        ]
        div [] [
            str (sprintf "session state: %s" model.SessionState)
        ]
    ]

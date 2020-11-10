[<RequireQualifiedAccess>]
module Callback

open Fable.Core.JsInterop
open Elmish
open Elmish.Navigation
open Browser

type CallbackQueryParams = {
    code: string
    scopes: string list
    state: string
    session_state: string
}

// Model available for debugging purposes.
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

let init (callbackParams: CallbackQueryParams) : Model * Cmd<Msg> =
    let model = {
        Code = callbackParams.code
        Scopes = callbackParams.scopes
        State = callbackParams.state
        SessionState = callbackParams.session_state }

    model, Cmd.ofMsg HandleSigninRedirectCallback

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | HandleSigninRedirectCallback ->
        let mgr: Fable.OidcClient.UserManager =
            Fable.OidcClient.Oidc.UserManager.Create !!{| response_mode = Some "query" |}
        model, Cmd.OfPromise.either mgr.signinRedirectCallback () OnSigninCallbackSuccess OnSigninCallbackError
    | OnSigninCallbackSuccess user ->
        console.error "OnSigninCallbackSuccess should be handled from the parent application"
        model, Navigation.newUrl "#"
    | OnSigninCallbackError e ->
        console.error "OnSigninCallbackError should be handled from the parent application"
        model, Navigation.newUrl "#"

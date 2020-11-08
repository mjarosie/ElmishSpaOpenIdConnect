module App

open Elmish
open Elmish.UrlParser
open Fable.React
open Fable.Core.JsInterop
open Elmish.Navigation
open Fable.OidcClient
open Browser

module Router =
    type CallbackQueryParams = {
        code: string
        scopes: string list
        state: string
        session_state: string
    }

    type Route =
        | Home
        | Callback of CallbackQueryParams option

        with static member CallbackQueryParams (code: Option<string>) (scope: Option<string>) (state: Option<string>) (session_state: Option<string>) : Route =
            match code, scope, state, session_state with
            | Some code, Some scopesRaw, Some state, Some session_state ->
                let scopesList = scopesRaw.Split [|' '|] |> Seq.toList
                Callback (Some { code = code; scopes = scopesList; state = state; session_state = session_state })
            | _ -> Callback None

    open Elmish.UrlParser
        
    let router: Parser<Route -> Route, Route>  =
        oneOf [
            map Home (top)
            map Route.CallbackQueryParams (s "callback" <?> stringParam "code" <?> stringParam "scope" <?> stringParam "state" <?> stringParam "session_state" )
        ]

let settings: UserManagerSettings = 
    !!{| 
        authority = Some "https://localhost:5001"
        client_id = Some "js"
        redirect_uri = Some "https://localhost:8080/#/callback"
        response_type = Some "code"
        scope = Some "openid profile scope1"
        post_logout_redirect_uri = Some "https://localhost:8080"
    
        filterProtocolClaims = Some true
        loadUserInfo = Some true
    |}
    
let mgr: UserManager = Oidc.UserManager.Create settings

type Page =
    | Home
    | Callback of Callback.Model

type Model =
    { CurrentPage: Page }

type Msg =
    | HomeMsg of Home.Msg
    | CallbackMsg of Callback.Msg

let init (initialRoute:Option<Router.Route>): Model * Cmd<Msg> =
    let currentRoute = initialRoute |> Option.defaultValue Router.Route.Home
    match currentRoute with
    | Router.Route.Home ->
        { CurrentPage = Page.Home}, Cmd.none

    | Router.Route.Callback callbackQueryParams ->
        match callbackQueryParams with
        | Some parameters ->
            let callbackModel, callbackCmd = Callback.init parameters.code parameters.scopes parameters.state parameters.session_state
            { CurrentPage = Page.Callback callbackModel} , Cmd.map CallbackMsg callbackCmd

        | None -> { CurrentPage = Page.Home}, Cmd.none

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg, model.CurrentPage with
    | HomeMsg (Home.Msg.NavigateToLogin), Home ->
        model, Cmd.ofSub (fun _ -> mgr.signinRedirect() |> Promise.start)
    | HomeMsg (Home.Msg.NavigateToLogout), Home ->
        model, Cmd.ofSub (fun _ -> mgr.signoutRedirect() |> Promise.start)
    | HomeMsg _, _ ->
        model, Cmd.none
    | CallbackMsg callbackMsg, Callback callbackModel ->
        let updatedCallbackState, callbackCmd = Callback.update callbackMsg callbackModel
        { model with CurrentPage = Page.Callback updatedCallbackState }, Cmd.map CallbackMsg callbackCmd
    | CallbackMsg _, _ ->
        model, Cmd.none

let view (model : Model) (dispatch : Msg -> unit) =
    let pageHtml  =
        match model.CurrentPage with
        | Home -> Home.view (HomeMsg >> dispatch)
        | Callback callbackModel -> Callback.view callbackModel

    div [] [ pageHtml ]

let urlUpdate (result:Option<Router.Route>) (model: Model) : Model * Cmd<Msg> =
    match result with
    | Some Router.Route.Home ->
        { model with CurrentPage = Home }, Cmd.none

    | Some (Router.Route.Callback queryParameters) ->
        match queryParameters with
        | Some parameters ->
            let callbackModel, callbackCmd = Callback.init parameters.code parameters.scopes parameters.state parameters.session_state
            { model with CurrentPage = Page.Callback callbackModel }, Cmd.map CallbackMsg callbackCmd
        | None -> model, Cmd.none

    | None ->
        ( model, Navigation.newUrl "#" ) // no matching route - go home

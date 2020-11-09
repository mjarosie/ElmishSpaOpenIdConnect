module App

open Elmish
open Elmish.UrlParser
open Fable.React
open Fable.Core.JsInterop
open Elmish.Navigation
open Fable.OidcClient
open Elmish.UrlParser
open System
open Types
open Browser

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

type CallbackQueryParams = {
    code: string
    scopes: string list
    state: string
    session_state: string
}

type Url =
    | Home
    | Login
    | Logout
    | Callback of CallbackQueryParams option

    with static member CallbackQueryParams (code: Option<string>) (scope: Option<string>) (state: Option<string>) (session_state: Option<string>) : Url =
        match code, scope, state, session_state with
        | Some code, Some scopesRaw, Some state, Some session_state ->
            let scopesList = scopesRaw.Split [|' '|] |> Seq.toList
            Callback (Some { code = code; scopes = scopesList; state = state; session_state = session_state })
        | _ -> Callback None
    
let router: Parser<Url -> Url, Url>  =
    oneOf [
        map Home (top)
        map Login (s "login")
        map Logout (s "logout")
        map Url.CallbackQueryParams (s "callback" <?> stringParam "code" <?> stringParam "scope" <?> stringParam "state" <?> stringParam "session_state" )
    ]

type Page =
    | Home
    | Callback of Callback.Model

type Model =
    { User: ApplicationUser
      CurrentUrl: Url
      CurrentPage: Page }

type Msg =
    | CallbackMsg of Callback.Msg
    | UrlChanged of Url

let init (initialUrl:Option<Url>): Model * Cmd<Msg> =
    console.log (sprintf "init: initialRoute: %A" initialUrl)
    let currentUrl = initialUrl |> Option.defaultValue Url.Home

    let defaultState = {
        User = Anonymous
        CurrentUrl = Url.Home
        CurrentPage = Page.Home
    }

    defaultState, Cmd.ofMsg (UrlChanged currentUrl)

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg, model.CurrentPage with
    | CallbackMsg (Callback.Msg.OnSigninCallbackSuccess user), Callback callbackModel ->
        console.log (sprintf "handling CallbackMsg (OnSigninCallbackSuccess user)")
        console.log user
        let subjectId = Guid.Parse user.profile.sub
        let applicationUser = LoggedIn { SubjectId = subjectId; AccessToken = user.access_token; Profile = user.profile }

        let updatedModel = { model with User = applicationUser }

        // Get rid of the "#/callback" url and go to the homepage.
        let commands = Cmd.batch [ Navigation.modifyUrl "#"; Cmd.ofMsg (UrlChanged Url.Home) ]

        updatedModel, commands
            
    | CallbackMsg (Callback.Msg.OnSigninCallbackError err), Callback callbackModel ->
        console.log (sprintf "handling CallbackMsg (OnSigninCallbackError err)")
        console.log err
        model, Cmd.ofMsg (UrlChanged Url.Home)
    | CallbackMsg callbackMsg, Callback callbackModel ->
        let updatedCallbackState, callbackCmd = Callback.update callbackMsg callbackModel
        { model with CurrentPage = Page.Callback updatedCallbackState }, Cmd.map CallbackMsg callbackCmd
    | CallbackMsg _, _ ->
        model, Cmd.none
    | UrlChanged url, _ ->
        match url with
        | Url.Login ->
            model, Cmd.ofSub (fun _ -> mgr.signinRedirect() |> Promise.start)

        | Url.Logout ->
            model, Cmd.ofSub (fun _ -> mgr.signoutRedirect() |> Promise.start)

        | Url.Home ->
            { model with CurrentPage = Page.Home; CurrentUrl = url }, Cmd.none

        | (Url.Callback queryParameters) ->
            match queryParameters with
            | Some parameters ->
                let callbackModel, callbackCmd = Callback.init parameters.code parameters.scopes parameters.state parameters.session_state
                { model with CurrentPage = Page.Callback callbackModel; CurrentUrl = url }, Cmd.map CallbackMsg callbackCmd
            | None -> model, Cmd.ofMsg (UrlChanged Url.Home)

let view (model : Model) (dispatch : Msg -> unit) =
    let pageHtml  =
        match model.CurrentPage with
        | Home -> Home.view model.User
        | Callback callbackModel -> Callback.view callbackModel

    div [] [ pageHtml ]

let urlUpdate (result:Option<Url>) (model: Model) : Model * Cmd<Msg> =
    match result with
    | Some url ->
        console.log (sprintf "url changed to %A" url)
        model, Cmd.ofMsg (UrlChanged url)
    | None ->
        model, Navigation.modifyUrl "#" // no matching route - go home

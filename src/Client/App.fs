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
        redirect_uri = Some "http://localhost:8080/#/callback"
        response_type = Some "code"
        scope = Some "openid profile scope1"
        post_logout_redirect_uri = Some "http://localhost:8080"
    
        filterProtocolClaims = Some true
        loadUserInfo = Some true
    |}
    
let mgr: UserManager = Oidc.UserManager.Create settings

type Url =
    | Home
    | Login
    | Logout
    | Callback of Callback.CallbackQueryParams

    with static member CallbackQueryParams (code: Option<string>) (scope: Option<string>) (state: Option<string>) (session_state: Option<string>) : Url =
            match code, scope, state, session_state with
            | Some code, Some scopesRaw, Some state, Some session_state ->
                let scopesList = scopesRaw.Split [|' '|] |> Seq.toList
                Callback ({ code = code; scopes = scopesList; state = state; session_state = session_state })
            | _ -> Home
    
let router: Parser<Url -> Url, Url>  =
    oneOf [
        map Home (top)
        map Login (s "login")
        map Logout (s "logout")
        map Url.CallbackQueryParams (s "callback" <?> stringParam "code" <?> stringParam "scope" <?> stringParam "state" <?> stringParam "session_state" )
    ]

type Page =
    | Home of Home.Model
    | Login // Dummy page, our app won't display it but will redirect to our Identity Provider.
    | Logout // Dummy page, our app won't display it but will redirect to our Identity Provider.
    | Callback of Callback.Model

type Model =
    { User: ApplicationUser
      CurrentUrl: Url
      CurrentPage: Page }

type Msg =
    | HomeMsg of Home.Msg
    | CallbackMsg of Callback.Msg
    | UrlChanged of Url

let init (initialUrl:Option<Url>): Model * Cmd<Msg> =
    let currentUrl = initialUrl |> Option.defaultValue Url.Home
    match currentUrl with
    | Url.Home -> 
        let homeModel, homeCmd = Home.init Anonymous
        { User = Anonymous; CurrentPage = Page.Home homeModel; CurrentUrl = currentUrl }, Cmd.map HomeMsg homeCmd

    | Url.Callback callbackQueryParams ->
        let callbackModel, callbackCmd = Callback.init callbackQueryParams
        { User = Anonymous; CurrentPage = Page.Callback callbackModel; CurrentUrl = currentUrl }, Cmd.map CallbackMsg callbackCmd

    | Url.Login ->
        { User = Anonymous; CurrentPage = Page.Login; CurrentUrl = currentUrl }, Cmd.ofSub (fun _ -> mgr.signinRedirect() |> Promise.start)

    | Url.Logout ->
        { User = Anonymous; CurrentPage = Page.Logout; CurrentUrl = currentUrl }, Cmd.ofSub (fun _ -> mgr.signoutRedirect() |> Promise.start)

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg, model.CurrentPage with
    | HomeMsg homeMsg, Home homeModel ->
        let updatedHomeModel, homeCmd = Home.update homeMsg homeModel
        { model with CurrentPage = Page.Home updatedHomeModel }, Cmd.map HomeMsg homeCmd 

    | HomeMsg _, _ ->
        model, Cmd.none

    | CallbackMsg (Callback.Msg.OnSigninCallbackSuccess user), Callback _ ->
        console.log "access_token:"
        console.log user.access_token
        console.log "id_token:"
        console.log user.id_token
        let subjectId = Guid.Parse user.profile.sub
        let applicationUser = LoggedIn { SubjectId = subjectId; AccessToken = user.access_token; Profile = user.profile }

        let updatedModel = { model with User = applicationUser }

        updatedModel, Navigation.newUrl "#"
            
    | CallbackMsg (Callback.Msg.OnSigninCallbackError err), Callback _ ->
        console.error err
        model, Navigation.newUrl "#"

    | CallbackMsg callbackMsg, Callback callbackModel ->
        let updatedCallbackModel, callbackCmd = Callback.update callbackMsg callbackModel
        { model with CurrentPage = Page.Callback updatedCallbackModel }, Cmd.map CallbackMsg callbackCmd

    | CallbackMsg _, _ ->
        model, Cmd.none

    | UrlChanged url, _ ->
        match url with
        | Url.Login ->
            { model with CurrentPage = Page.Login; CurrentUrl = url }, Cmd.ofSub (fun _ -> mgr.signinRedirect() |> Promise.start)

        | Url.Logout ->
            { model with CurrentPage = Page.Logout; CurrentUrl = url }, Cmd.ofSub (fun _ -> mgr.signoutRedirect() |> Promise.start)

        | Url.Home ->
            let homeModel, homeCmd = Home.init model.User
            { model with CurrentPage = Page.Home homeModel; CurrentUrl = url }, Cmd.map HomeMsg homeCmd

        | (Url.Callback queryParameters) ->
            console.error "Invalid redirection - UrlChanged Url.Callback should happen only during the application initialisation"
            model, Navigation.newUrl "#" // no matching route - go home

let view (model : Model) (dispatch : Msg -> unit) =
    let pageHtml =
        match model.CurrentPage with
        | Home homeModel -> Home.view homeModel
        | Login -> div [] []
        | Logout -> div [] []
        | Callback _ -> div [] []

    div [] [ pageHtml ]

let urlUpdate (result:Option<Url>) (model: Model) : Model * Cmd<Msg> =
    match result with
    | Some url ->
        model, Cmd.ofMsg (UrlChanged url)
    | None ->
        model, Navigation.newUrl "#" // no matching route - go home

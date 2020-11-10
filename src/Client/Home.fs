[<RequireQualifiedAccess>]
module Home

open Fable.React
open Fable.React.Props
open Elmish
open Types
open Fable.SimpleHttp
open Thoth.Json
open Browser

type Model = { 
    User: ApplicationUser
    Todos: Deferred<Result<List<Todo>, string>>
}

type Msg =
    | LoadUserTodos of AsyncOperationStatus<Result<List<Todo>, string>>

let init (user: ApplicationUser) : Model * Cmd<Msg> =
    match user with
    | Anonymous -> { User = user; Todos = HasNotStartedYet }, Cmd.none
    | LoggedIn u-> { User = user; Todos = HasNotStartedYet }, Cmd.ofMsg (LoadUserTodos Started)

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg, model.User with
    | LoadUserTodos Started, LoggedIn user ->
        
        let getTodos : Async<Msg> = async {
            let! response =
                Http.request "https://localhost:6001/todos"
                |> Http.method GET
                |> Http.header (Headers.contentType "application/json")
                |> Http.header (Headers.authorization ("Bearer " + user.AccessToken))
                |> Http.send

            console.log(response.responseText)

            let todosResult = 
                match response.statusCode with
                | 200 -> Decode.Auto.fromString<List<Todo>>(response.responseText, caseStrategy=CamelCase)
                | _ -> Error response.responseText
            
            return LoadUserTodos (Finished todosResult)
        }
        { model with Todos = InProgress }, Cmd.fromAsync getTodos

    | LoadUserTodos Started, Anonymous ->
        // User not logged in.
        model, Cmd.none

    | LoadUserTodos (Finished todosResult), LoggedIn user ->
        { model with Todos = Resolved todosResult}, Cmd.none

    | LoadUserTodos (Finished todos), Anonymous ->
        // User not logged in.
        model, Cmd.none

let renderTodos (todos: Deferred<Result<List<Todo>, string>>): ReactElement =
    match todos with
    | HasNotStartedYet ->
        div [] [
            str "API call not started yet"
        ]
    | InProgress ->
        div [] [
            str "Waiting for API response..."
        ]
    | Resolved (Ok todos) ->
        div [] [
            str "Your todos:"
            div [] [
                
                ul [] (List.map (fun (todo: Todo) -> li [ Key (todo.Id.ToString()) ] [ str todo.Description ]) todos)
            ]
        ]
    | Resolved (Error todosError) ->
        div [] [
            str ("Got an error when retrieving Todos: " + todosError)
        ]

let view (model: Model) =
    let topDivChildren = 
        match model.User with
        | LoggedIn user ->
            [
                h1 [] [
                    str (sprintf "Hello, %A" user.Profile.name)
                ]
                div [] [
                    str (sprintf "Your subject ID: %A" user.SubjectId)
                ]
                renderTodos (model.Todos)
                div [] [
                    a [ Href "#/logout" ] [
                        button [] [ str "Logout" ]
                    ]
                ]
            ]
        | Anonymous ->
            [
                h1 [] [
                    str "Hello, anonymous"
                ]
                div [] [
                    a [ Href "#/login" ] [
                        button [] [ str "Login" ]
                    ]
                ]
            ]

    div [ ] topDivChildren
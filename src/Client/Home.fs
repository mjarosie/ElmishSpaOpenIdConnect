[<RequireQualifiedAccess>]
module Home

open Fable.React
open Fable.React.Props
open Types

let view (user: ApplicationUser) =
    div [ ] [
        match user with
        | LoggedIn user ->
            str (sprintf "Hello, %A (#%A)" user.Profile.name user.SubjectId)
            div [] [
                a [ Href "#/logout" ] [
                    button [] [ str "Logout" ]
                ]
            ]
        | Anonymous ->
            str "Hello, anonymous"
            div [] [
                a [ Href "#/login" ] [
                    button [] [ str "Login" ]
                ]
            ]
    ]

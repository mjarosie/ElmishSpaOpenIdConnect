[<RequireQualifiedAccess>]
module Home

open Fable.React
open Fable.React.Props

type Msg =
    | NavigateToLogin
    | NavigateToLogout

let view (dispatch : Msg -> unit) =
    div [ ] [
        str "Hello"
        div [] [
            button [ OnClick (fun _ -> dispatch NavigateToLogin) ] [ str "Login" ]
        ]
        div [] [
            button [ OnClick (fun _ -> dispatch NavigateToLogout) ] [ str "Logout" ]
        ]
    ]

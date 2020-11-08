module Main

open Elmish
open Elmish.React

#if DEBUG
open Elmish.Debug
open Elmish.HMR
open Elmish.UrlParser

#endif

Program.mkProgram App.init App.update App.view
|> Program.toNavigable (parseHash App.Router.router) App.urlUpdate
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run

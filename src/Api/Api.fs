module Server

open Microsoft.AspNetCore.Http
open Saturn
open Giraffe
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open System
open System.Threading.Tasks
open System.IdentityModel.Tokens.Jwt

type Todo =
    { Id : Guid
      Description : string }

module Todo =
    let isValid (description: string) =
        String.IsNullOrWhiteSpace description |> not

    let create (description: string) =
        { Id = Guid.NewGuid()
          Description = description }

type UserId = Guid

type UserTodos = Map<Guid, Todo>

type Storage () =
    let mutable usersTodos: Map<UserId, UserTodos> = Map.empty<UserId, UserTodos>

    member __.GetTodos (userGuid: UserId): List<Todo> =
        match usersTodos.TryFind(userGuid) with
        | Some(todos) ->
            let values = todos |> Map.toSeq |> Seq.map snd
            values |> Seq.toList
        | None -> List.Empty

    member __.AddTodo (userGuid: UserId) (todo: Todo): Result<unit, string> =
        if Todo.isValid todo.Description then
            if usersTodos.ContainsKey userGuid then
                usersTodos <- Map.map (fun (guid: UserId) (todos: UserTodos) ->
                    if guid = userGuid then
                        if todos.ContainsKey todo.Id then
                            todos
                        else
                            todos.Add(todo.Id, todo)
                    else todos ) usersTodos
            else
                usersTodos <- usersTodos.Add(userGuid, Map.empty.Add(todo.Id, todo))
            Ok ()
        else Error "Invalid todo"

let storage = Storage()

let aliceId = Guid("fe86e79a-0421-4f82-89ee-d19d1a2a8904")
let bobId = Guid("84ef55fa-9277-4a38-88d0-afad507288e3")

storage.AddTodo aliceId (Todo.create "Create a new project") |> ignore
storage.AddTodo aliceId (Todo.create "Write my app") |> ignore
storage.AddTodo bobId (Todo.create "Secure my app with OpenID Connect") |> ignore

let api = pipeline {
    requires_authentication (Giraffe.Auth.challenge "Bearer")
}

// https://github.com/giraffe-fsharp/Giraffe/blob/master/DOCUMENTATION.md#return-early
let earlyReturn : HttpFunc = Some >> Task.FromResult

let getTodosController : HttpHandler =
    fun (next : HttpFunc) (ctx: HttpContext) ->
        let userId = 
            ctx.User.Claims 
                |> Seq.tryFind (fun x -> x.Type = "sub") 
                |> Option.map (fun x -> x.Value)
                |> Option.map (fun x -> Guid.Parse x)

        let userTodos = userId |> Option.map (fun x -> storage.GetTodos x)

        match userTodos with
        | Some todos -> 
            (setStatusCode 200 >=> json todos) next ctx
        | _ -> (setStatusCode 403 >=> json "Forbidden") earlyReturn ctx

let appRouter = router {
    pipe_through api
    get "/todos" getTodosController
}

// https://stackoverflow.com/questions/60115053
// https://mderriey.com/2019/06/23/where-are-my-jwt-claims/
JwtSecurityTokenHandler.DefaultMapInboundClaims <- false

let app =
    application {
        url "https://0.0.0.0:6001"
        use_router appRouter
        memory_cache
        use_static "public"
        use_gzip
        use_jwt_authentication_with_config (fun (cfg: JwtBearerOptions) ->
            cfg.Authority <- "https://localhost:5001"

            let tvp = new TokenValidationParameters()
            tvp.ValidateAudience <- false
            cfg.TokenValidationParameters <- tvp
        )
        use_cors "default" (fun policy ->
            policy.WithOrigins("http://localhost:8080").AllowAnyHeader().AllowAnyMethod() |> ignore
        )
    }

run app

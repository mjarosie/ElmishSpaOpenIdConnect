module Server

open Microsoft.AspNetCore.Http
open Saturn
open Giraffe
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open System

type UserId = Guid

let api = pipeline {
    requires_authentication (Giraffe.Auth.challenge "Bearer")
}

type Claim =
    { Type : string
      Value : string }

let identityController : HttpHandler =
    fun (next : HttpFunc) (ctx: HttpContext) ->
        (setStatusCode 200 >=> json ctx.User.Claims) next ctx

let appRouter = router {
    pipe_through api
    get "/claims" identityController
}

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
            policy.WithOrigins("https://localhost:5003").AllowAnyHeader().AllowAnyMethod() |> ignore
        )
    }

run app

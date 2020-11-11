# Elmish single-page application with OpenID Connect authentication

This repository shows an example use of [Fable.OidcClient](https://github.com/mjarosie/Fable.OidcClient) library to carry out user authentication and authorization in [Elmish](https://elmish.github.io/) single-page application.

## Project structure

The solution consists of 3 projects:

- `Api`: [Saturn framework](https://saturnframework.org/) web server that returns user's predefined to-do list when called under `GET /todos`.
- `IdentityServer`: Identity Provider service that issues access and identity tokens to client apps.
- `Client`: [Elmish](https://elmish.github.io/) single-page application - the API client.

## Install pre-requisites

You'll need to install the following pre-requisites in order to build the application:

* The [.NET Core SDK](https://www.microsoft.com/net/download) 3.1 or higher.
* [npm](https://nodejs.org/en/download/) package manager.
* [Node LTS](https://nodejs.org/en/download/).

## Starting the application

Before you run the project **for the first time only** you must install dotnet "local tools" with this command:

```bash
dotnet tool restore
```

To concurrently run the server, IdentityServer4 (OpenID Connect identity provider) and the client components in watch mode use the following command:

```bash
dotnet fake build -t run
```

Then open `http://localhost:8080` in your browser.

The application is very simple. Depending on whether the user is authenticated or not - it displays a `login` button, or user's name, ID, hardcoded TODO list (retrieved over an API - different for every user) and a `logout` button.

There are 2 predefined users that you can try out:

- `alice` (password: `alice`)
- `bob` (password: `bob`)

### HTTPS

Note that the Client app is not served over https with Webpack. You can change it by:

- modifying `webpack.config.js` configuration file by setting `CONFIG.httpsEnabled` to `true`
- modify the client by changing `src/Client/App.fs`: `settings: UserManagerSettings` record fields `redirect_uri` and `post_logout_redirect_uri` should be prefixed with `https`
- modify IdentityServer configuration by changing `src/IdentityServer/Config.cs`: JavaScript client fields `RedirectUris`, `PostLogoutRedirectUris` and `AllowedCorsOrigins` should be prefixed with `https`
- modify the API server by changing `src/Api/App.fs`: modify the `use_cors` operation of the `app` computation expression (it should mention `https`)

You will need to accept invalid certificate risk when navigating between pages on Chrome. Firefox will not even allow you to temporarily accept the invalid certificate.


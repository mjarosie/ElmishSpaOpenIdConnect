# Elmish Single Page Application with OpenID Connect authentication

This repository shows an example implementation of Elmish SPA leveraging the OpenID Connect protocol for carrying out user authentication and authorization.

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

Then open `https://localhost:8080` in your browser.

You might be asked to accept the risk of invalid local certificate in your browser.

module Types

open System

type User = {
    SubjectId: Guid
    AccessToken: string
    Profile: Fable.OidcClient.Profile
}

type ApplicationUser =
    | Anonymous
    | LoggedIn of User


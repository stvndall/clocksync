module msgraph.ClientFactory

open Azure.Identity
open Microsoft.Graph
let scopes = [ "Calendars.Read" ]

let createClient creds =
    GraphServiceClient(creds, scopes)
    |> GraphConnector

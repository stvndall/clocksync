module syncer.ClientFactory

open Azure.Identity
open Microsoft.Graph
open syncer.MsGraph
let scopes = [ "Calendars.Read" ]

let createClient creds =
    GraphServiceClient(creds, scopes)
    |> GraphConnector

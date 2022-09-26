module syncer.MsGraph

open System
open Microsoft.Graph
open syncer


type GraphConnector(client: GraphServiceClient) =

    member this.Calendar() =
        client.Me.Calendars.Request().GetAsync()

    interface ICalendarConnector with

        member this.GetEvents c_id (startDate: DateTime) (endDate: DateTime) =
            
            task {
                let! r =
                    client
                        .Me
                        .Calendars[ c_id ]
                        .Events.Request()
                        .Filter($"(start/dateTime ge '{(startDate.ToString())}') and (start/dateTime le '{(endDate.ToString())}')")
                        .OrderBy("start/datetime")
                        .Top(1000)
                        .GetAsync()

                return r |> List.ofSeq |> List.map CalendarEntry.From
            }

        member this.GetCalendarView (startDate: DateTime) (endDate: DateTime) =
            task {
                let queryOptions =
                    List.map
                        (fun o -> o :> Option)
                        [ QueryOption("startdatetime", startDate.ToString())
                          QueryOption("enddatetime", endDate.ToString()) ]

                let! r =
                    client
                        .Me
                        .CalendarView
                        .Request(queryOptions)
                        .OrderBy("start/datetime")
                        .Select("start,end,subject,categories,SeriesMasterId")
                        .Top(1000)
                        .GetAsync()

                return
                    r
                    |> List.ofSeq
                    |> List.map CalendarEntry.From
                    |> List.filter (fun x -> x.EntryTitle.StartsWith("Canceled: ") |> not)
            }

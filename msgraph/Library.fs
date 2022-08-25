namespace msgraph

open System
open Microsoft.Graph

type Entry =
    { Start: DateTime
      End: DateTime
      Category: string list
      EntryTitle: string
      Series: string
      Captured: bool }
    static member From(evt: Event) =
        { Start = (evt.Start.DateTime |> DateTime.Parse).AddHours(2)
          End = (evt.End.DateTime |> DateTime.Parse).AddHours(2)
          Category = evt.Categories |> List.ofSeq
          EntryTitle = evt.Subject
          Series = evt.SeriesMasterId
          Captured = false }

    member this.Duration =
        (this.End - this.Start).TotalMinutes
        |> Math.Ceiling
        |> int

and TimeEntry =
    { Start: DateTime
      End: DateTime
      EntryTitle: string
      Project: string }




type GraphConnector(client: GraphServiceClient) =

    member this.Calendar() =
        client.Me.Calendars.Request().GetAsync()


    member this.GetEvents c_id (startDate: DateTime) (endDate: DateTime) =
        let start = startDate.ToString()
        let endD = endDate.ToString()

        task {
            let! r =
                client
                    .Me
                    .Calendars[ c_id ]
                    .Events.Request()
                    .Filter($"(start/dateTime ge '{start}') and (start/dateTime le '{endD}')")
                    .OrderBy("start/datetime")
                    .Top(1000)
                    .GetAsync()

            return r |> List.ofSeq |> List.map Entry.From
        }

    member this.GetCalendarView c_id (startDate: DateTime) (endDate: DateTime) =
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
                |> List.map Entry.From
                |> List.filter (fun x -> x.EntryTitle.StartsWith("Canceled: ") |> not)
        }

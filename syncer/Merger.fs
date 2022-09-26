namespace syncer

open syncer

type Merger() =
    interface IMerger with
        member this.FindUnSynced (timeEntries) (calendarEvents) =

            let events =
                calendarEvents |> List.sortBy (fun x -> x.Start)

            let times =
                timeEntries |> List.sortBy (fun x -> x.Start)

            events
            |> List.filter (fun event ->
                times
                |> List.tryFindIndex (fun x ->
                    x.Start = event.Start
                    && x.EntryTitle = event.EntryTitle)
                |> fun x -> x.IsNone)

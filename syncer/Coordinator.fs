namespace syncer

type Coordinator(clockify: IClockifyConnector, calendar: ICalendarConnector, merger: IMerger, projectFinder:IProjectFinder) =

    interface ICoordinator with
        member this.SyncFor options calendarId startDate endDate =
            task {


                let! events =
                    match calendarId with
                    | Some (c_id) -> calendar.GetEvents c_id startDate endDate
                    | None -> calendar.GetCalendarView startDate endDate
                
                printfn $"Found {events.Length} events from the calander"

                let! timeEntries = clockify.FetchEntries startDate endDate

                printfn $"Found {timeEntries.Length} entries from clockify"
                
                let requiresSync =
                    merger.FindUnSynced timeEntries events
                    
                printfn $"Found {requiresSync.Length} entries that require sync"

                let firstSeries = requiresSync |> List.find (fun x -> x.Series <> "")
                
                let project = projectFinder.FindForSeries firstSeries.Series
                printfn $"selected {project}" 
                
                if options.dryRun then
                    printfn "found the following items need to be synced"
                    printfn "no changes will be made"

                    // requiresSync
                    // |> List.iter (fun x ->
                    //     printfn
                    //         $"start {x.Start} end {x.End} name {x.EntryTitle} in categories {x.Category} in series {x.Series}")
                

                return ResizeArray(requiresSync)
            }

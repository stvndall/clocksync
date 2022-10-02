namespace syncer

open System.Threading
open System.Threading.Tasks
open Spectre.Console

type Coordinator
    (
        clockify: IClockifyConnector,
        calendar: ICalendarConnector,
        merger: IMerger,
        projectFinder: IProjectFinder,
        options: RunOptions
    ) =

    interface ICoordinator with
        member this.SyncFor calendarId startDate endDate =
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

                let bySeries =
                    requiresSync
                    |> List.groupBy (fun x -> x.Series)
                    |> dict

                let entries =
                    bySeries.Keys
                    |> List.ofSeq
                    |> List.filter (fun x -> x <> "" && not (isNull x))
                    |> List.collect (fun x ->
                        let alreadyCaptured =
                            projectFinder.FindForSeries(x)
                            |> Async.AwaitTask
                            |> Async.RunSynchronously

                        match alreadyCaptured with
                        | Some (project) ->
                            let mapper =
                                project
                                |> TimeEntryProject.From
                                |> TimeEntry.ForCapture

                            bySeries.Item(x) |> List.map mapper
                        | _ ->
                            let first = bySeries.Item(x).Head
                            AnsiConsole.MarkupLineInterpolated $"No project is known for the series"

                            AnsiConsole.MarkupLineInterpolated
                                $"First event seen in the series has the following details"

                            AnsiConsole.MarkupLineInterpolated
                                $"Name: {first.EntryTitle}\n Start time: {first.Start}\n Duration: {(first.End - first.Start).Minutes}\nA total of {bySeries.Item(x).Length} are found"

                            let mutable confirmAdd =
                                ConfirmationPrompt("Would you like to record a project for this series")

                            let add = AnsiConsole.Prompt(confirmAdd)

                            if add then
                                let project =
                                    projectFinder.AssignProjectToSeries x
                                    |> Async.AwaitTask
                                    |> Async.RunSynchronously

                                let mapper =
                                    project
                                    |> TimeEntryProject.From
                                    |> TimeEntry.ForCapture

                                bySeries.Item(x) |> List.map mapper
                            else
                                AnsiConsole.MarkupLineInterpolated $"Skipping"
                                [])

                AnsiConsole.MarkupLineInterpolated $"Moving onto stand alone entries"

                let standAlone = []
                // bySeries.Item(null)
                // |> List.map (fun x ->
                //     AnsiConsole.MarkupLineInterpolated $"No project is known for the series"
                //     AnsiConsole.MarkupLineInterpolated $"First event seen in the series has the following details"
                //
                //     AnsiConsole.MarkupLineInterpolated
                //         $"Name: {x.EntryTitle}\n Start time: {x.Start}\n Duration: {(x.End - x.Start).Minutes}\n"
                //
                //     let mutable confirmAdd =
                //         ConfirmationPrompt("Would you like to record a project for this series")
                //
                //     let add = AnsiConsole.Prompt(confirmAdd)
                //
                //     if add then
                //         let project =
                //             projectFinder.FindProjectPure()
                //             |> TimeEntryProject.From
                //
                //         Some(TimeEntry.ForCapture project x)
                //     else
                //         None)
                // |> List.choose id

                let all = entries @ standAlone


                if options.dryRun then
                    printfn "found the following items need to be synced"
                    printfn "no changes will be made"
                else
                    let options = ParallelOptions()
                    options.MaxDegreeOfParallelism <- 1044
                    

                    Parallel.ForEach(
                        all,
                        options,
                        (fun x ->
                            task {
                                let add = clockify.AddNewEntry x
                                let waiter = Task.Delay(1000)
                                do! Task.WhenAll(add, waiter)
                                return ()
                            }
                            |> Async.AwaitTask
                            |> Async.RunSynchronously)
                    ) |> ignore

                return ResizeArray(requiresSync)
            }

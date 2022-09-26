namespace syncer

open System
open System.Collections.Generic
open System.Threading.Tasks
open Clockify.Net.Models
open Clockify.Net.Models.Projects
open Clockify.Net.Models.Reports
open Clockify.Net.Models.TimeEntries
open Clockify.Net.Models.Users

type ClockifyConnector(apiKey: string) =
    let mutable workspaceId: string option =
        None

    let client =
        Clockify.Net.ClockifyClient(apiKey)

    let mutable projects = []
        

    let mutable (currentUser: Option<CurrentUserDto>) =
        None





    let FetchWorkspace () =
        match workspaceId with
        | Some w -> task { return w }
        | None ->
            task {
                let! workspaces = client.GetWorkspacesAsync()
                let id = workspaces.Data.[0].Id
                workspaceId <- Some(id)
                return id
            }

    let GetCurrentUser () =
        task {
            match currentUser with
            | Some (u) -> return u
            | None ->

                let! resp = client.GetCurrentUserAsync()
                currentUser <- Some(resp.Data)
                return resp.Data
        }

    let rec FetchEntriesInternal (startDate: DateTime) (endDate: DateTime) (page: int) =
        task {
            let! user = GetCurrentUser()
            let! workspaceId = FetchWorkspace()
            let rep = DetailedReportRequest()
            let users = UsersFilterDto()
            let filter = DetailedFilterDto()
            filter.PageSize <- 200
            filter.Page <- page
            filter.SortColumn <- SortColumnType.DATE
            users.Ids <- [ user.Id ] |> ResizeArray<string>
            rep.Users <- users
            rep.SortOrder <- SortOrderType.ASCENDING
            rep.DateRangeStart <- startDate
            rep.DateRangeEnd <- endDate
            rep.DetailedFilter <- filter
            let! r = client.GetDetailedReportAsync(workspaceId, rep)

            let mutable list =
                r.Data.TimeEntries |> List.ofSeq

            if (r.Data.TimeEntries.Count > 0) then
                let! nextPage = FetchEntriesInternal startDate endDate (page + 1)
                list <- List.append list nextPage
            return list
        }

    interface IClockifyConnector with
    
        member this.FetchProjects () =
            if projects.Length = 0 then
                task {
                    let! workspaceId = FetchWorkspace()
                    let! r = client.FindAllProjectsOnWorkspaceAsync(workspaceId)
                    projects <- (r.Data |> List.ofSeq)
                    return projects
                }
            else
                task { return projects }
        member this.FetchEntries (startDate) (endDate) =
            task {
                let! list = FetchEntriesInternal startDate endDate 1
                return list |> List.map TimeEntry.From
            }

        member this.AddNewEntry(evt: TimeEntry) =
            task {
                let! workspaceId = FetchWorkspace()
                let time = TimeIntervalDto()
                time.Start <- DateTime.SpecifyKind(evt.Start, DateTimeKind.Local)
                time.End <- DateTime.SpecifyKind(evt.Start, DateTimeKind.Local)
                let entry = TimeEntryRequest()
                entry.Description <- evt.EntryTitle
                entry.Start <- DateTime.SpecifyKind(evt.Start, DateTimeKind.Local)
                entry.End <- DateTime.SpecifyKind(evt.Start, DateTimeKind.Local)
                entry.ProjectId <- evt.Project.Id
                entry.WorkspaceId <- workspaceId
                let! c = client.CreateTimeEntryAsync(workspaceId, entry)
                return ()
            }

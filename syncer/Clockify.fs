module syncer.Clockify

open System
open System.Collections.Generic
open System.Threading.Tasks
open Clockify.Net.Models
open Clockify.Net.Models.Projects
open Clockify.Net.Models.Reports
open Clockify.Net.Models.TimeEntries
open Clockify.Net.Models.Users

type ClockifyClient(apiKey: string) =
    let workspaceId = ""

    let client =
        Clockify.Net.ClockifyClient(apiKey)

    let mutable projects =
        List<ProjectDtoImpl>()

    let mutable (currentUser: Option<CurrentUserDto>) =
        None


    member this.FetchProjects() =
        if projects.Count = 0 then
            task {
                let! r = client.FindAllProjectsOnWorkspaceAsync(workspaceId)
                projects <- r.Data
                return projects
            }
        else
            task { return projects }

    member this.GetCurrentUser() =
        task {
            match currentUser with
            | Some (u) -> return u
            | None ->

                let! resp = client.GetCurrentUserAsync()
                currentUser <- Some(resp.Data)
                return resp.Data
        }



    member this.FetchEntries(startDate: DateTime, endDate: DateTime) =
        task {
            let! user = this.GetCurrentUser()
            let rep = DetailedReportRequest()
            let users = UsersFilterDto()
            users.Ids <- [ user.Id ] |> ResizeArray<string>
            rep.Users <- users
            rep.SortOrder <- SortOrderType.ASCENDING
            rep.DateRangeStart <- startDate
            rep.DateRangeEnd <- endDate
            let! r = client.GetDetailedReportAsync(workspaceId, rep)

            return
                r.Data.TimeEntries
                |> List.ofSeq
                |> List.map TimeEntry.From
                |> ResizeArray<TimeEntry>
        }

    member this.AddNewEntry(evt: TimeEntry) =
        task {
            let time = TimeIntervalDto()
            time.Start <- DateTime.SpecifyKind(evt.Start, DateTimeKind.Local)
            time.End <- DateTime.SpecifyKind(evt.Start, DateTimeKind.Local)
            let entry = TimeEntryRequest()
            entry.Description <- evt.EntryTitle
            entry.Start <- DateTime.SpecifyKind(evt.Start, DateTimeKind.Local)
            entry.End <- DateTime.SpecifyKind(evt.Start, DateTimeKind.Local)
            entry.ProjectId <- evt.Project.Id
            entry.WorkspaceId <- workspaceId
            entry.UserId
            return! client.CreateTimeEntryAsync(workspaceId, entry)
        }

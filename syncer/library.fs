namespace syncer

open System
open Clockify.Net.Models.Projects
open Microsoft.Graph
open Clockify.Net.Models.Reports
open System.Threading.Tasks

type RunOptions = { dryRun: bool }
type Project = ProjectDtoImpl
type CalendarEntry =
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
      Project: TimeEntryProject
      Captured: bool }
    static member From(evt: TimeEntryDto) =

        { Start = evt.TimeInterval.Start.Value.DateTime
          End = evt.TimeInterval.End.Value.DateTime
          EntryTitle = evt.Description
          Project =
            { Id = evt.ProjectId
              Name = evt.ProjectName }
          Captured = true }
    static member ForCapture (prj:TimeEntryProject) (evt: CalendarEntry) =
        {
            Start = evt.Start
            End = evt.End
            Captured = false
            Project = prj
            EntryTitle = evt.EntryTitle
        }

and TimeEntryProject =
   { Id: String; Name: string
      }
   static member From(prj: Project) =
       {
           Id=prj.Id
           Name = prj.Name
       }

type IClockifyConnector =
    abstract member FetchProjects: unit -> Project list Task
    abstract member FetchEntries: DateTime -> DateTime -> TimeEntry list Task
    abstract member AddNewEntry: TimeEntry -> Task

type ICalendarConnector =
    abstract member GetEvents: string -> DateTime -> DateTime -> CalendarEntry list Task
    abstract member GetCalendarView: DateTime -> DateTime -> CalendarEntry list Task

type IMerger =
    abstract member FindUnSynced: TimeEntry list -> CalendarEntry list -> CalendarEntry list

type ICoordinator =
    abstract member SyncFor: string option -> DateTime -> DateTime -> CalendarEntry ResizeArray Task
    
type IProjectFinder =
    abstract member FindForSeries:  seriesId:string -> Project option Task
    abstract member AssignProjectToSeries:  seriesId:string -> Project Task
    abstract member FindProjectPure: unit -> Project 
    abstract member FindForDescription:  string -> string

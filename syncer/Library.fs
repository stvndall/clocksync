﻿namespace syncer

open System
open Clockify.Net.Models.Reports
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

and TimeEntryProject = { Id: String; Name: string }

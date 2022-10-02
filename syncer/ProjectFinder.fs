namespace syncer

open System.Collections.Generic
open System.Diagnostics
open System.Threading.Tasks
open Clockify.Net.Models.Projects
open Microsoft.Graph
open Spectre.Console


type IFileReader =
    abstract member fetchIfExists: key: string -> string option Task
    abstract member upsertValue: key: string -> value: string -> unit Task


type ProjectFinder(file: IFileReader, clockifyConnector: IClockifyConnector, options: RunOptions) =
    let mutable projectPrepared: IDictionary<string, Project> option =
        None

    let mutable clientProjects: IDictionary<string, Project list> option =
        None

    let rawProjects () =
        match projectPrepared with
        | Some (projects) -> projects
        | None ->
            let projects =
                clockifyConnector.FetchProjects()
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> List.map (fun x -> (x.Id, x))
                |> dict

            projectPrepared <- Some(projects)
            projects

    let projectsByClients () =
        let projects =
            rawProjects().Values
            |> List.ofSeq
            |> List.groupBy (fun p -> p.ClientName)
            |> dict

        clientProjects <- Some(projects)
        projects

    let getClient (askFor: string) : string =
        let title =
            $"{askFor} \n Select the client for the "

        let options = projectsByClients ()
        let clients = options.Keys |> List.ofSeq

        let mutable prompt =
            SelectionPrompt<string>()

        prompt <- SelectionPromptExtensions.Title(prompt, title)
        prompt <- SelectionPromptExtensions.PageSize(prompt, 8)
        prompt <- SelectionPromptExtensions.AddChoices(prompt, clients)
        // prompt <- SelectionPromptExtensions.UseConverter(prompt, converter)

        prompt <-
            SelectionPromptExtensions.MoreChoicesText(
                prompt,
                "There are additional choices, use the cursor keys to select"
            )

        prompt |> AnsiConsole.Prompt

    let getProjectFromClient (askFor: string) (clientName: string) =
        let title =
            $"{askFor} \n Select the project to assign to"

        let options = projectsByClients ()

        let projects = options.Item(clientName)
        // |> List.map (fun x -> x.Name)

        let mutable prompt =
            SelectionPrompt<Project>()

        prompt <- SelectionPromptExtensions.Title(prompt, title)
        prompt <- SelectionPromptExtensions.PageSize(prompt, 8)
        prompt <- SelectionPromptExtensions.AddChoices(prompt, projects)
        prompt <- SelectionPromptExtensions.UseConverter(prompt, (fun p -> p.Name))

        prompt <-
            SelectionPromptExtensions.MoreChoicesText(
                prompt,
                "There are additional choices, use the cursor keys to select"
            )

        prompt |> AnsiConsole.Prompt



    let AssignProjectToEntry (askFor: string) : Project =
        getClient askFor |> getProjectFromClient askFor




    interface IProjectFinder with


        member this.FindForDescription(keyToFind) = failwith "todo"

        member this.FindForSeries seriesId =
            task {
                let! project = file.fetchIfExists seriesId
                return Option.map (fun x -> rawProjects().Item(x)) project
            }

        member this.AssignProjectToSeries(seriesId) =
            task {
                let project = AssignProjectToEntry seriesId
                do! file.upsertValue seriesId project.Id
                return project
            }

        member this.FindProjectPure() = AssignProjectToEntry ""

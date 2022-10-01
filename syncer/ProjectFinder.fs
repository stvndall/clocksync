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


type FileReader() =
    interface IFileReader with
        member this.fetchIfExists(key) = task { return None }
        member this.upsertValue (key) (value) = task { return () }

type ProjectFinder(file: IFileReader, clockifyConnector: IClockifyConnector, options: RunOptions) =
    let mutable projectPrepared: IDictionary<string, ProjectDtoImpl list> option =
        None

    let projects () =
        match projectPrepared with
        | Some (projects) -> projects
        | None ->
            let projects =
                clockifyConnector.FetchProjects()
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> List.groupBy (fun p -> p.ClientName)
                |> dict

            // let projects =
            //     projects
            //     |> List.map (fun x -> $"{x.ClientName} {x.Name}")
            //     |> List.reduce (fun acc n -> $"{acc}\n{n}")

            projectPrepared <- Some(projects)
            projects

    let getClient (askFor: string) : string =
        let title =
            $"{askFor} \n Select the client for the "

        let options = projects ()
        let clients = options.Keys |> List.ofSeq
        let mutable prompt = SelectionPrompt<'a>()

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

        let options = projects ()

        let projects = options.Item(clientName)
        // |> List.map (fun x -> x.Name)

        let mutable prompt =
            SelectionPrompt<ProjectDtoImpl>()

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



    let AssignProjectToEntry (askFor: string) : ProjectDtoImpl =
        getClient askFor |> getProjectFromClient askFor




    interface IProjectFinder with


        member this.FindForDescription(keyToFind) = failwith "todo"

        member this.FindForSeries seriesId =
            task {
                match! file.fetchIfExists seriesId with
                | Some (value) -> return value
                | None ->
                    let project = AssignProjectToEntry seriesId
                    do! file.upsertValue seriesId project.Id
                    return project.Id
            }

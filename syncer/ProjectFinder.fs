namespace syncer

open System.Diagnostics
open System.Threading.Tasks
open Microsoft.Graph


type IFileReader =
    abstract member fetchIfExists: key: string -> string option Task
    abstract member upsertValue: key: string -> value: string -> unit Task


type  FileReader() =
    interface IFileReader with
        member this.fetchIfExists(key) = task{return None}
        member this.upsertValue(key) (value) = task{return ()}
    
type ProjectFinder(file: IFileReader, clockifyConnector: IClockifyConnector) =
    let mutable projectPrepared: string option =
        None

    let projects () =
        match projectPrepared with
        | Some (projects) -> projects
        | None ->
            let projects =
                clockifyConnector.FetchProjects()
                |> Async.AwaitTask
                |> Async.RunSynchronously

            let projects =
                projects
                |> List.map (fun x -> $"{x.ClientName} {x.Name}")
                |> List.reduce (fun acc n -> $"{acc}\n{n}")

            projectPrepared <- Some(projects)
            projects

    let FindNewBinding (askFor: string) =
        let options = projects ()
        let mutable psi = ProcessStartInfo("fzf")
        psi.RedirectStandardInput <- true
        psi.
        let proc = Process.Start("fzf")
        proc.StandardInput.Write(options)
        let mutable line:string = ""
        while (not proc.StandardOutput.EndOfStream) do
            line <- proc.StandardOutput.ReadLine()
        
        printfn $"typed {line} in the output"
        line



    interface IProjectFinder with


        member this.FindForDescription(keyToFind) = failwith "todo"

        member this.FindForSeries seriesId =
            task {
                match! file.fetchIfExists seriesId with
                | Some (value) -> return value
                | None ->
                    let project = FindNewBinding seriesId
                    do! file.upsertValue seriesId project
                    return project
            }

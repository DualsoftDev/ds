namespace Engine.Runtime
open System
open Dual.Common.Core.FS
open IO.Core
open Engine.Cpu
open Engine.Core
open Engine.Parser.FS
open System.IO
open System.IO.Compression

type FilePath = string

type CompiledModel(zipDsPath:FilePath) =
    member x.SourceDsZipPath = zipDsPath

    member x.UnZipSystem() =

        let extractName = PathManager.getFileNameWithoutExtension(zipDsPath|>DsFile)
        let extractPath = PathManager.combineFullPathDirectory([|PathManager.getTempPath();$"DsUnzippedFiles/{extractName}"|])
        // Ensure the temporary folder exists
        Directory.CreateDirectory(extractPath) |> ignore

        // Open the zip file for reading
        use archive = ZipFile.OpenRead(x.SourceDsZipPath)

        // Extract each entry to the subfolder
        for entry in archive.Entries do
            let entryPath = PathManager.getFullPath (entry.FullName.TrimStart('/')|>DsFile) (extractPath|>DsDirectory)
            let entryDir = PathManager.getDirectoryName(entryPath|>DsFile)

            // Ensure the directory for the entry exists
            Directory.CreateDirectory(entryDir) |>ignore

            // Extract the entry to the subfolder
            entry.ExtractToFile(entryPath, true)

        // Return the path where the files are extracted
        PathManager.getFullPath ($"{extractName}.json"|>DsFile)(extractPath|>DsDirectory)


type RuntimeModel(zipDsPath:FilePath) =
    let compiledModel = CompiledModel(zipDsPath)
    //let mutable zmqInfo = Zmq.InitializeServer "zmqsettings.json" |> Some
    let mutable zmqInfo: ZmqInfo option = None
    let mutable dsCPU : DsCPU option = None

    do
        let unZipJsonPath = compiledModel.UnZipSystem()

        let model = ParserLoader.LoadFromConfig unZipJsonPath 
        dsCPU <- Some(DsCpuExt.GetDsCPU(model.System, RuntimePackage.StandardPC))


        // todo: compiledModel <- ....
        ()

    interface IDisposable with
        member x.Dispose() = x.Dispose()
    member x.ModelSource = zipDsPath

    member x.CompiledModel = compiledModel
    //member x.IoHubInfo = zmqInfo

    member x.IoServer = zmqInfo |> map (fun x -> x.Server) |> Option.toObj
    //member x.IoHubClient = zmqInfo |> map (fun x -> x.Client) |> Option.toObj
    member x.IoSpec      = zmqInfo |> map (fun x -> x.IOSpec) |> Option.toObj

    member x.Dispose() =
        match zmqInfo with
        | Some info ->
            info.Dispose()
            zmqInfo <- None
        | None -> failwith "IoHubInfo is already disposed"

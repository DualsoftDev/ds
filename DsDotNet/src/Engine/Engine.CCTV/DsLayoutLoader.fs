module DsLayoutLoaderModule

open Engine.Core
open Engine.Info
open Engine.Parser.FS
open System.IO
open System.Collections.Generic
open System.Linq
open System
open Engine.CodeGenCPU

[<AutoOpen>]
type ScreenInfo() =
    member val Id = 0 with get, set
    member val URL = "" with get, set

[<AutoOpen>]
type DsLayoutLoader() =
    let mutable _dsSystem:DsSystem option = None

    do
        let commonAppSettings = DSCommonAppSettings.Load(Path.Combine(AppContext.BaseDirectory, "CommonAppSettings.json"))
        let connectionString = commonAppSettings.LoggerDBSettings.ConnectionString
        let dsFileJson = DBLogger.GetDsFilePath(connectionString)

        let model = ParserLoader.LoadFromConfig(dsFileJson)
        _dsSystem <- Some model.System
        CpuLoaderExt.LoadStatements(model.System, Storages()) |> ignore
        let querySet = QuerySet(CommonAppSettings = commonAppSettings)
        DBLogger.InitializeLogReaderOnDemandAsync(querySet, [model.System] |> List).Result |> ignore

    member x.DsSystem = _dsSystem.Value

    member x.GetScreens() =
        let screens = HashSet<ScreenInfo>()

        let chs = x.DsSystem.LayoutChannels.ToList()
        for i = 0 to chs.Count - 1 do
            screens.Add(ScreenInfo(Id = i + 1, URL = $"{chs.[i]}")) |> ignore

        screens

    member x.GetScreen(id:string) =
        let find = Convert.ToInt32(id)
        x.GetScreens().FirstOrDefault(fun x -> x.Id = find)

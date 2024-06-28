namespace Engine.Core

open System
open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS

[<AutoOpen>]
module CommonAppSettings =
    #if DEBUG
        let IsDebugVersion = true
    #else
        let IsDebugVersion = false
    #endif


type LoggerDBSettings(connectionPath:string, syncIntervalSeconds:int) = 
    member x.ConnectionString = $"Data Source={Path.Combine(AppContext.BaseDirectory, x.ConnectionPath)}"
    member val ConnectionPath = connectionPath with get, set
    member val SyncIntervalSeconds = syncIntervalSeconds with get, set

/// 여러 application(.exe) 들 간의 공유할 정보
/// "CommonAppSettings.json" 파일
type DSCommonAppSettings(loggerDBSettings:LoggerDBSettings) =
    member val HmiWebServer = "" with get, set
    member val LoggerDBSettings = loggerDBSettings with get, set
    static member Load(jsonPath:string) =
        jsonPath
        |> File.ReadAllText
        |> JsonConvert.DeserializeObject<DSCommonAppSettings>


type ModelCompileInfo(dsConfigJson:string, pptPath:string ) =
    member val PptPath = pptPath with get, set
    member val ConfigPath = dsConfigJson with get, set
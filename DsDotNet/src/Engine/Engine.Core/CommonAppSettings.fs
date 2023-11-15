namespace Engine.Core

open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS

type LoggerDBSettings() =
    member val ConnectionString = "" with get, set
    member val SyncIntervalSeconds = 1.0 with get, set

/// 여러 application(.exe) 들 간의 공유할 정보
/// "CommonAppSettings.json" 파일
type DSCommonAppSettings() =
    member val HmiWebServer = "" with get, set
    member val LoggerDBSettings = getNull<LoggerDBSettings>() with get, set
    static member Load(jsonPath:string) =
        jsonPath
        |> File.ReadAllText
        |> JsonConvert.DeserializeObject<DSCommonAppSettings>


type ModelCompileInfo(dsConfigJson:string, pptPath:string ) =
    member val PptPath = pptPath with get, set
    member val ConfigPath = dsConfigJson with get, set
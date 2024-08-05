namespace Engine.Core

open System
open System.IO
open Microsoft.Data.Sqlite
open Newtonsoft.Json
open Dual.Common.Core.FS
open System.Reactive.Linq

[<AutoOpen>]
module CommonAppSettings =
#if DEBUG
    let IsDebugVersion = true
#else
    let IsDebugVersion = false
#endif

    let internal createConnectionWith (connStr) =
        new SqliteConnection(connStr) |> tee (fun conn -> conn.Open())



/// ServerSettings 에서부터 연결되어 오므로, System.Text.Json 으로 serialize 가능해야 함.
type LoggerDBSettings(sqlitePath:string, dbWriter:string, modelFilePath:string, syncIntervalMilliSeconds:int) = 
    do
        noop()
    new() = LoggerDBSettings("", "", "", 0)
    [<JsonIgnore>]
    member val SyncIntervalMilliSeconds = syncIntervalMilliSeconds with get, set
    member val ConnectionPath = sqlitePath with get, set
    member val DbWriter = dbWriter with get, set
    member val ModelFilePath = modelFilePath with get, set
    member val ModelId = -1 with get, set
    member val UseLogDB:bool = true with get, set


type LoggerDBSettings with
    /// Serialiation 문제로 extension proproty 로 정의함
    member x.SyncInterval = Observable.Interval(TimeSpan.FromMilliseconds(x.SyncIntervalMilliSeconds))

/// 여러 application(.exe) 들 간의 공유할 정보
/// "CommonAppSettings.json" 파일
[<AllowNullLiteral>]
type DSCommonAppSettings(loggerDBSettings:LoggerDBSettings) =
    do
        // 생성자 호출 후에는 FillModelId() 확장 메서드 호출 필요.
        noop()
    member val HmiWebServer = "" with get, set
    member val RedisServerExePath = "" with get, set
    member val DsConfigPath = "" with get, set
    member val LoggerDBSettings = loggerDBSettings with get, set
    /// 호출 후에는 FillModelId() 확장 메서드 호출 필요.
    static member Load(jsonPath:string) : DSCommonAppSettings =
        jsonPath
        |> File.ReadAllText
        |> JsonConvert.DeserializeObject<DSCommonAppSettings>


type ModelCompileInfo(dsConfigJson:string, pptPath:string ) =
    member val PptPath = pptPath with get, set
    member val ConfigPath = dsConfigJson with get, set
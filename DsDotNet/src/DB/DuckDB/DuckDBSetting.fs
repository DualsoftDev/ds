namespace DB.DuckDB

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Text.Json
open System.Text.Json.Serialization
open System.IO

// 설정 타입 및 로더
[<AutoOpen>]
module DuckDBSetting =
    type DuckDBSetting = {
        DatabaseDir: string
        LogFlushIntervalMs: int
    }

    let loadSettings () : DuckDBSetting =
        let json = File.ReadAllText("DuckDBSetting.json")
        let options = JsonSerializerOptions()
        options.PropertyNameCaseInsensitive <- true
        options.AllowTrailingCommas <- true
        JsonSerializer.Deserialize<DuckDBSetting>(json, options)


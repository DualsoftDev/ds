namespace DB.DuckDB

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO

[<AutoOpen>]
module DBCommon =
    // 경로 설정
    let DBFileDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dualsoft", "DB")

    let getDBPath systemName= Path.Combine(DBFileDirectory, $"{systemName}.duckdb")

    type TagLogEntry(tagName: string, time: DateTime, value: obj) =
        new() = TagLogEntry("", DateTime.MinValue, null)
        member val TagName = tagName with get, set
        member val Time = time with get, set
        member val Value = value with get, set

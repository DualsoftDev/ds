namespace DB.DuckDB

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Text.Json
open System.Text.Json.Serialization
open System.IO


[<AutoOpen>]
module DBCommon =
    // 경로 설정

    type TagLogEntry(tagName: string, time: DateTime, value: obj) =
        new() = TagLogEntry("", DateTime.MinValue, null)
        member val TagName = tagName with get, set
        member val Time = time with get, set
        member val Value = value with get, set

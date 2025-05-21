namespace DB.DuckDB

open System
open System.IO
open System.Collections.Generic
open System.Text.Json
open DuckDB.NET.Data

module DuckDBReader =

    type ReaderDB(systemName: string) =

        let setting = DuckDBSetting.loadSettings()
        let dbPath = Path.Combine(setting.DatabaseDir, $"{systemName}.duckdb")

        let openConnection () =
            let conn = new DuckDBConnection($"DataSource={dbPath}")
            conn.Open()
            conn

        let addParam (cmd: DuckDBCommand) (value: obj) =
            let p = cmd.CreateParameter()
            p.Value <- value
            cmd.Parameters.Add(p) |> ignore

        let createTempTagTable (conn: DuckDBConnection) =
            use cmd = conn.CreateCommand()
            cmd.CommandText <- "CREATE TEMP TABLE IF NOT EXISTS TempTags (TagName TEXT PRIMARY KEY);"
            cmd.ExecuteNonQuery() |> ignore

        let clearTempTags (conn: DuckDBConnection) =
            use cmd = conn.CreateCommand()
            cmd.CommandText <- "DELETE FROM TempTags;"
            cmd.ExecuteNonQuery() |> ignore

        let populateTempTags (conn: DuckDBConnection) (tagNames: seq<string>) =
            for tag in tagNames do
                use insert = conn.CreateCommand()
                insert.CommandText <- "INSERT OR IGNORE INTO TempTags (TagName) VALUES (?);"
                addParam insert (tag :> obj)
                insert.ExecuteNonQuery() |> ignore

        /// ✅ 태그 로그 조회 (기간 내)
        member _.LoadLogs(tagNames: List<string>, start: DateTime, end': DateTime, boolTypeOnly: bool) : Dictionary<string, List<TagLogEntry>> =
            if tagNames = null || tagNames.Count = 0 then
                Dictionary<string, List<TagLogEntry>>() 
            else
                use conn = openConnection ()
                createTempTagTable conn
                clearTempTags conn
                populateTempTags conn (tagNames |> Seq.distinct)

                use cmd = conn.CreateCommand()
                cmd.CommandText <- """
                    SELECT TagName, Time, NewValue 
                    FROM TagLogView 
                    WHERE TagName IN (SELECT TagName FROM TempTags)
                      AND Time BETWEEN ? AND ?
                    ORDER BY TagName, Time;
                """
                addParam cmd (start :> obj)
                addParam cmd (end' :> obj)

                let logs = ResizeArray<TagLogEntry>()
                use reader = cmd.ExecuteReader()
                while reader.Read() do
                    let tag = reader.GetString(0)
                    let time = reader.GetDateTime(1)
                    let value = if not (reader.IsDBNull(2)) then reader.GetString(2) :> obj else null
                    if not boolTypeOnly || (value <> null && Boolean.TryParse(value.ToString()) |> fst) then
                        logs.Add(TagLogEntry(tag, time, value))

                logs
                |> Seq.groupBy (fun x -> x.TagName)
                |> Seq.map (fun (k, v) -> k, v |> List)
                |> dict
                |> Dictionary

        /// ✅ 태그 로그 조회 (최신 N건)
        member _.LoadLogRecents(tagNames: List<string>, count: int) : Dictionary<string, List<TagLogEntry>> =
            if tagNames = null || tagNames.Count = 0 || count <= 0 then
                Dictionary<string, List<TagLogEntry>>() 
            else
                use conn = openConnection ()
                createTempTagTable conn
                clearTempTags conn
                populateTempTags conn (tagNames |> Seq.distinct)

                use cmd = conn.CreateCommand()
                cmd.CommandText <- """
                    SELECT TagName, Time, NewValue
                    FROM (
                        SELECT TagName, Time, NewValue,
                               ROW_NUMBER() OVER (PARTITION BY TagName ORDER BY Time DESC) AS rn
                        FROM TagLogView
                        WHERE TagName IN (SELECT TagName FROM TempTags)
                    )
                    WHERE rn <= ?
                    ORDER BY TagName, Time DESC;
                """
                addParam cmd (count :> obj)

                let logs = ResizeArray<TagLogEntry>()
                use reader = cmd.ExecuteReader()
                while reader.Read() do
                    let tag = reader.GetString(0)
                    let time = reader.GetDateTime(1)
                    let value = if not (reader.IsDBNull(2)) then reader.GetString(2) :> obj else null
                    logs.Add(TagLogEntry(tag, time, value))

                logs
                |> Seq.groupBy (fun x -> x.TagName)
                |> Seq.map (fun (k, v) -> k, v |> List)
                |> dict
                |> Dictionary

        /// ✅ 개별 파라미터 조회
        member _.GetParameter(name: string) : string option =
            use conn = openConnection ()
            use cmd = conn.CreateCommand()
            cmd.CommandText <- "SELECT Value FROM SystemParameter WHERE Name = ?;"
            addParam cmd (name :> obj)
            let result = cmd.ExecuteScalar()
            if result = null || result = DBNull.Value then None
            else Some(result.ToString())

        /// ✅ JSON 파라미터 역직렬화
        member this.GetJson<'T>(name: string) : 'T option =
            match this.GetParameter(name) with
            | Some json ->
                try Some(JsonSerializer.Deserialize<'T>(json))
                with _ -> None
            | None -> None

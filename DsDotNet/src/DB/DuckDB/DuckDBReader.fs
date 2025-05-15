namespace DB.DuckDB

open System
open System.IO
open System.Collections.Generic
open System.Threading.Tasks
open DuckDB.NET.Data

module DuckDBReader =

    let getDBPath systemName =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dualsoft", "DB", $"{systemName}.duckdb")

    let openConnection systemName =
        let conn = new DuckDBConnection($"DataSource={getDBPath systemName}")
        conn.Open()
        conn

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
            let p = insert.CreateParameter()
            p.Value <- tag
            insert.Parameters.Add(p) |> ignore
            insert.ExecuteNonQuery() |> ignore

    let loadLogs systemName (tagNames: List<string>) (start: DateTime) (end': DateTime) (boolTypeOnly: bool) =
        task {
            if tagNames = null || tagNames.Count = 0 then
                return Dictionary<string, List<TagLogEntry>>()
            else
                use conn = openConnection systemName
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
                let addParam (v: obj) =
                    let p = cmd.CreateParameter()
                    p.Value <- v
                    cmd.Parameters.Add(p) |> ignore

                addParam (start :> obj)
                addParam (end' :> obj)

                let logs = ResizeArray<TagLogEntry>()
                use reader = cmd.ExecuteReader()
                while reader.Read() do
                    let tag = reader.GetString(0)
                    let time = reader.GetDateTime(1)
                    let value = if not (reader.IsDBNull(2)) then reader.GetString(2) :> obj else null
                    if not boolTypeOnly || (value <> null && Boolean.TryParse(value.ToString()) |> fst) then
                        logs.Add(TagLogEntry(tag, time, value))

                return
                    logs
                    |> Seq.groupBy (fun x -> x.TagName)
                    |> Seq.map (fun (k, v) -> k, v |> List)
                    |> dict |> Dictionary<string, List<TagLogEntry>>
        }

    let loadLogRecents systemName (tagNames: List<string>) (count: int) =
        task {
            if tagNames = null || tagNames.Count = 0 || count <= 0 then
                return Dictionary<string, List<TagLogEntry>>() 
            else
                use conn = openConnection systemName
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
                let p = cmd.CreateParameter()
                p.Value <- count
                cmd.Parameters.Add(p) |> ignore

                let logs = ResizeArray<TagLogEntry>()
                use reader = cmd.ExecuteReader()
                while reader.Read() do
                    let tag = reader.GetString(0)
                    let time = reader.GetDateTime(1)
                    let value = if not (reader.IsDBNull(2)) then reader.GetString(2) :> obj else null
                    logs.Add(TagLogEntry(tag, time, value))

                return
                    logs
                    |> Seq.groupBy (fun x -> x.TagName)
                    |> Seq.map (fun (k, v) -> k, v |> List)
                    |> dict |> Dictionary<string, List<TagLogEntry>>
        }

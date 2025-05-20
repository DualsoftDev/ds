namespace DB.DuckDB

open System
open System.IO
open System.Timers
open System.Collections.Concurrent
open System.Collections.Generic
open System.Text.Json
open DuckDB.NET.Data

module DuckDBWriter =

    type WriterDB(systemName: string) =

        let setting = DuckDBSetting.loadSettings()
        let dbPath = Path.Combine(setting.DatabaseDir, $"{systemName}.duckdb")

        let createConnection () =
            let conn = new DuckDBConnection($"DataSource={dbPath}")
            conn.Open()
            conn

        let addParam (cmd: DuckDBCommand) (value: obj) =
            let p = cmd.CreateParameter()
            p.Value <- value
            cmd.Parameters.Add(p) |> ignore

        let createSchema () =
            use conn = createConnection ()
            use cmd = conn.CreateCommand()
            cmd.CommandText <- """
                CREATE TABLE IF NOT EXISTS TagNameTable (
                    Id BIGINT PRIMARY KEY,
                    Name TEXT UNIQUE NOT NULL
                );

                CREATE TABLE IF NOT EXISTS TagLog (
                    Id BIGINT PRIMARY KEY,
                    Time TIMESTAMP NOT NULL,
                    TagNameId BIGINT NOT NULL,
                    NewValue TEXT,
                    FOREIGN KEY(TagNameId) REFERENCES TagNameTable(Id)
                );

                CREATE OR REPLACE VIEW TagLogView AS
                SELECT l.Id, l.Time, t.Name AS TagName, l.NewValue
                FROM TagLog l
                JOIN TagNameTable t ON l.TagNameId = t.Id
                ORDER BY l.Id DESC;

                CREATE TABLE IF NOT EXISTS SystemParameter (
                    Name TEXT PRIMARY KEY,
                    Value TEXT,
                    UpdatedAt TIMESTAMP
                );
            """
            cmd.ExecuteNonQuery() |> ignore

        let logQueue = ConcurrentQueue<DateTime * string * obj>()
        let tagIdCache = Dictionary<string, int>()

        let flushAll () =
            if logQueue.IsEmpty then () else

            let items = ResizeArray<_>()
            while not logQueue.IsEmpty do
                match logQueue.TryDequeue() with
                | true, item -> items.Add(item)
                | _ -> ()

            if items.Count = 0 then () else

            use conn = createConnection ()

            let nextId =
                use cmd = conn.CreateCommand()
                cmd.CommandText <- "SELECT COALESCE(MAX(Id), 0) + 1 FROM TagLog;"
                Convert.ToInt64(cmd.ExecuteScalar())

            let mutable idCounter = nextId

            for (time, tagName, newValue) in items do
                let tagNameId =
                    match tagIdCache.TryGetValue(tagName) with
                    | true, id -> id
                    | false, _ ->
                        try
                            use insertCmd = conn.CreateCommand()
                            insertCmd.CommandText <- "INSERT INTO TagNameTable (Id, Name) VALUES (?, ?);"
                            addParam insertCmd (idCounter :> obj)
                            addParam insertCmd (tagName :> obj)
                            insertCmd.ExecuteNonQuery() |> ignore
                        with _ -> ()

                        use selectCmd = conn.CreateCommand()
                        selectCmd.CommandText <- "SELECT Id FROM TagNameTable WHERE Name = ?;"
                        addParam selectCmd (tagName :> obj)
                        let id = Convert.ToInt32(selectCmd.ExecuteScalar())
                        tagIdCache[tagName] <- id
                        id

                use logCmd = conn.CreateCommand()
                logCmd.CommandText <- "INSERT INTO TagLog (Id, Time, TagNameId, NewValue) VALUES (?, ?, ?, ?);"
                addParam logCmd (idCounter :> obj)
                addParam logCmd (time :> obj)
                addParam logCmd (tagNameId :> obj)
                addParam logCmd (if newValue <> null then newValue.ToString() :> obj else null)
                logCmd.ExecuteNonQuery() |> ignore

                idCounter <- idCounter + 1L

        let timer =
            let t = new Timer(float setting.LogFlushIntervalMs)
            t.Elapsed.Add(fun _ -> flushAll ())
            t.AutoReset <- true
            t.Start()
            t

        do
            createSchema () |> ignore
            timer |> ignore

        // ✅ 태그 로그
        member _.LogTagChange(tagName: string, newValue: obj) =
            let now = DateTime.Now
            logQueue.Enqueue((now, tagName, newValue))

        // ✅ 시스템 파라미터 저장
        member _.SetParameter(name: string, value: obj) =
            use conn = createConnection ()
            use cmd = conn.CreateCommand()
            cmd.CommandText <- """
                INSERT INTO SystemParameter (Name, Value, UpdatedAt)
                VALUES (?, ?, ?)
                ON CONFLICT(Name) DO UPDATE SET
                    Value = excluded.Value,
                    UpdatedAt = excluded.UpdatedAt;
            """
            addParam cmd (name :> obj)
            addParam cmd (if value <> null then value.ToString() :> obj else null)
            addParam cmd (DateTime.Now :> obj)
            cmd.ExecuteNonQuery() |> ignore

        // ✅ 시스템 파라미터 읽기
        member _.GetParameter(name: string) : string option =
            use conn = createConnection ()
            use cmd = conn.CreateCommand()
            cmd.CommandText <- "SELECT Value FROM SystemParameter WHERE Name = ?;"
            addParam cmd (name :> obj)
            let result = cmd.ExecuteScalar()
            if result = null || result = DBNull.Value then None
            else Some(result.ToString())

        // ✅ 전체 파라미터 조회
        member _.GetAllParameters() : Dictionary<string, string> =
            use conn = createConnection ()
            use cmd = conn.CreateCommand()
            cmd.CommandText <- "SELECT Name, Value FROM SystemParameter;"
            let result = Dictionary<string, string>()
            use reader = cmd.ExecuteReader()
            while reader.Read() do
                let key = reader.GetString(0)
                let value = reader.GetString(1)
                result[key] <- value
            result

        // ✅ HeadTag 저장
        member this.SetHeadTag(groupName: string, headTag: string) =
            let key = $"HeadTag:{groupName}"
            this.SetParameter(key, headTag)

        // ✅ HeadTag 조회
        member this.GetHeadTag(groupName: string) : string option =
            let key = $"HeadTag:{groupName}"
            this.GetParameter(key)

        // ✅ 모든 HeadTag 조회
        member this.GetAllHeadTags() : Dictionary<string, string> =
            this.GetAllParameters()
            |> Seq.filter (fun kvp -> kvp.Key.StartsWith("HeadTag:"))
            |> Seq.map (fun kvp -> kvp.Key.Substring("HeadTag:".Length), kvp.Value)
            |> dict |> Dictionary

        // ✅ JSON 파라미터 저장
        member this.SetJson<'T>(name: string, value: 'T) =
            let json = JsonSerializer.Serialize(value)
            this.SetParameter(name, json)

        // ✅ JSON 파라미터 읽기
        member this.GetJson<'T>(name: string) : 'T option =
            match this.GetParameter(name) with
            | Some json ->
                try Some(JsonSerializer.Deserialize<'T>(json))
                with _ -> None
            | None -> None

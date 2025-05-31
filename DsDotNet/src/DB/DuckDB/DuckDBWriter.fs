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
                        // 먼저 존재하는지 확인
                        use checkCmd = conn.CreateCommand()
                        checkCmd.CommandText <- "SELECT Id FROM TagNameTable WHERE Name = ?;"
                        addParam checkCmd (tagName :> obj)
                        let existingId = checkCmd.ExecuteScalar()

                        let id =
                            if existingId <> null && existingId <> box DBNull.Value then
                                Convert.ToInt32(existingId)
                            else
                                use insertCmd = conn.CreateCommand()
                                insertCmd.CommandText <- "INSERT INTO TagNameTable (Id, Name) VALUES (?, ?);"
                                addParam insertCmd (idCounter :> obj)
                                addParam insertCmd (tagName :> obj)
                                insertCmd.ExecuteNonQuery() |> ignore
                                let newId = idCounter
                                idCounter <- idCounter + 1L
                                int newId

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



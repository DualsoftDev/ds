namespace DB.DuckDB

open System
open System.IO
open System.Timers
open System.Collections.Concurrent
open System.Collections.Generic
open DuckDB.NET.Data

module DuckDBWriter =

    let getDBPath systemName =
        let baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dualsoft", "DB")
        Directory.CreateDirectory(baseDir) |> ignore
        Path.Combine(baseDir, $"{systemName}.duckdb")

    type LoggerPG(systemName: string) =

        let dbPath = getDBPath systemName

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
            let t = new Timer(5000.0)
            t.Elapsed.Add(fun _ -> flushAll ())
            t.AutoReset <- true
            t.Start()
            t

        do
            createSchema () |> ignore
            timer |> ignore

        member _.LogTagChange(tagName: string, newValue: obj) =
            let now = DateTime.Now
            logQueue.Enqueue((now, tagName, newValue))

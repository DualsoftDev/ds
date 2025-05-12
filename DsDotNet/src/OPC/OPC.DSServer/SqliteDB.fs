namespace OPC.DSServer

open System
open System.Data
open Microsoft.Data.Sqlite
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Timers
open System.Threading.Tasks

module SQLiteLogger =

    let DBFileDirectory =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Dualsoft", "DSPilot"
        )

    let getDbPath sysName =
        Path.Combine(DBFileDirectory, $"{sysName}.db")

    let getConnStr sysName =
        $"Data Source={getDbPath sysName};"

    let createConnection sysName =
        let conn = new SqliteConnection(getConnStr sysName)
        conn.Open()
        conn

    let enableForeignKeys (conn: SqliteConnection) =
        use pragma = conn.CreateCommand()
        pragma.CommandText <- "PRAGMA foreign_keys = ON;"
        pragma.ExecuteNonQuery() |> ignore

    let createSchema (conn: SqliteConnection) =
        use cmd = conn.CreateCommand()
        cmd.CommandText <- """
            CREATE TABLE IF NOT EXISTS TagNameTable (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT UNIQUE NOT NULL
            );
            CREATE TABLE IF NOT EXISTS TagLog (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Time TEXT NOT NULL,
                TagNameId INTEGER NOT NULL,
                NewValue TEXT,
                FOREIGN KEY (TagNameId) REFERENCES TagNameTable(Id)
            );
            CREATE VIEW IF NOT EXISTS TagLogView AS
            SELECT l.Id, l.Time, t.Name AS TagName, l.NewValue
            FROM TagLog l
            JOIN TagNameTable t ON l.TagNameId = t.Id
            ORDER BY l.Id DESC;
        """
        cmd.ExecuteNonQuery() |> ignore

  

    let logQueue = ConcurrentQueue<string * string * string>()
    let lockObj = obj()
    let batchSize = 20
    let mutable isSaving = false
    let mutable isInitialized = false
    let tagIdCache = Dictionary<string, int64>()
    let flushIfAny (sysName: string) =
        if isSaving then () else

        let shouldSave =
            lock lockObj (fun () ->
                if isSaving then false
                else
                    isSaving <- true
                    true
            )

        if not shouldSave || logQueue.IsEmpty then () else

        try
            let itemsToSave = ResizeArray<_>()
            while itemsToSave.Count < batchSize do
                match logQueue.TryDequeue() with
                | true, log -> itemsToSave.Add(log)
                | _ -> ()

            if itemsToSave.Count = 0 then () else

            use conn = createConnection sysName
            enableForeignKeys conn
            use tx = conn.BeginTransaction()

            for (time, tagName, newValue) in itemsToSave do
                let tagNameId =
                    match tagIdCache.TryGetValue(tagName) with
                    | true, id -> id
                    | false, _ ->
                        use cmd = conn.CreateCommand()
                        cmd.CommandText <- "INSERT OR IGNORE INTO TagNameTable (Name) VALUES (@name);"
                        cmd.Parameters.AddWithValue("@name", tagName) |> ignore
                        cmd.ExecuteNonQuery() |> ignore

                        cmd.CommandText <- "SELECT Id FROM TagNameTable WHERE Name = @name;"
                        let idObj = cmd.ExecuteScalar()
                        let id = if isNull idObj then 0L else Convert.ToInt64(idObj)
                        tagIdCache[tagName] <- id
                        id

                use cmd = conn.CreateCommand()
                cmd.CommandText <- "INSERT INTO TagLog (Time, TagNameId, NewValue) VALUES (@time, @tagNameId, @newValue);"
                cmd.Parameters.AddWithValue("@time", time) |> ignore
                cmd.Parameters.AddWithValue("@tagNameId", tagNameId) |> ignore
                cmd.Parameters.AddWithValue("@newValue", newValue) |> ignore
                cmd.ExecuteNonQuery() |> ignore

            tx.Commit()
        finally
            isSaving <- false


    let startFlushTimer (sysName: string) =
        let timer = new Timer(5000.0)
        timer.Elapsed.Add(fun _ -> flushIfAny sysName)
        timer.AutoReset <- true
        timer.Start()
        timer

    let initialize (sysName: string) =
        Directory.CreateDirectory(DBFileDirectory) |> ignore
        use conn = createConnection sysName
        enableForeignKeys conn
        createSchema conn
        startFlushTimer sysName |> ignore

    let logTagChange (sysName: string) (tagName: string) (newValue: string) =
        if not isInitialized then 
            initialize sysName

            isInitialized <- true

        let now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
        logQueue.Enqueue(now, tagName, newValue)
        if logQueue.Count >= batchSize then
            flushIfAny(sysName) |> ignore

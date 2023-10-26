namespace Engine.Info

open System
open System.Configuration;
open System.Collections.Concurrent
open Dapper
open Engine.Core
open Microsoft.Data.Sqlite
open Dual.Common.Core.FS
open Dual.Common.Db
open System.Collections.Generic
open System.Data
open System.Reactive.Disposables
open System.Threading.Tasks
open DBLoggerORM

[<AutoOpen>]
module internal DBLoggerImpl =
    let mutable connectionString = "";
    let createConnection() =
        new SqliteConnection(connectionString) |> tee (fun conn -> conn.Open())

    type EnumEx() =
        static member Extract<'T when 'T: struct>() =
            let typ = typeof<'T>
            let values =
                Enum.GetValues(typ) :?> 'T[]
                |> Seq.cast<int> |> toArray

            let names = Enum.GetNames(typ) |> map (fun n -> $"{typ.Name}.{n}")
            Array.zip values names


    let createLoggerDBSchema(connStr:string) =
        task {
            connectionString <- connStr
            use conn = createConnection()
            use! tr = conn.BeginTransactionAsync()
            let! exists = conn.IsTableExistsAsync(Tn.Log)
            if not exists then
                let! _ = conn.ExecuteAsync(sqlCreateSchema, tr)
                ()

            let! exists = conn.IsTableExistsAsync(Tn.TagKind)
            if not exists then
                let enums =
                    EnumEx.Extract<SystemTag>()
                    @ EnumEx.Extract<FlowTag>()
                    @ EnumEx.Extract<VertexTag>()
                    @ EnumEx.Extract<ApiItemTag>()
                    @ EnumEx.Extract<ActionTag>()

                for (id, name) in enums do
                    let query = $"INSERT INTO [{Tn.TagKind}] (id, name) VALUES (@Id, @Name);"
                    let! _ = conn.ExecuteAsync(query, {|Id=id; Name=name|}, tr)
                    ()

            do! tr.CommitAsync()
        }






    let ormLog2Log (logSet:LogSet) (l:ORMLog) =
        let storage = logSet.StoragesById[l.StorageId]
        Log(l.Id, storage, l.At, l.Value)// |> tee (fun ll -> ll.Id = l.Id)

    type LogSet with
        member x.InitializeForReader(ormLogs:ORMLog seq) =
            x.StoragesById <-
                x.Storages.Values
                |> map (fun s -> s.Id, s)
                |> Tuple.toDictionary

            x.Logs <- ormLogs |> Seq.sortByDescending(fun l -> l.Id) |> map (ormLog2Log x) |> toList
            if x.Logs.any() then
                x.LastLogId <- x.Logs |> List.maxBy(fun l -> l.Id) |> fun l -> l.Id



    let mutable logSet = getNull<LogSet>()
    let systemPowerStorage = Storage(0, 0, "System.Power", "Boolean", "system-power")
    
    let private createLogInfoSetCommonAsync(systems:DsSystem seq, conn:IDbConnection, tr:IDbTransaction, isReader:bool) : Task<LogSet> =
        Log4NetWrapper.logWithTrace <- true

        task {
            let systemStorages: Storage array =
                systems
                |> map (fun s -> s.TagManager)
                |> distinct
                |> Seq.collect (fun tagManager -> tagManager.Storages.Values)
                |> filter (fun s -> s.TagKind <> InnerTag) // 내부변수
                |> distinct
                |> map Storage
                |> toArray
            
            let! dbStorages = conn.QueryAsync<Storage>($"SELECT * FROM [{Tn.Storage}]")
            let dbStorages =
                dbStorages |> map (fun s -> getStorageKey s, s)
                |> Tuple.toDictionary


            let existingStorages, newStorages = 
                systemStorages
                |> Seq.partition (fun s -> dbStorages.ContainsKey(getStorageKey s))

            for s in existingStorages do
                s.Id <- dbStorages[getStorageKey s].Id
                

            for s in newStorages do
                let! id = conn.InsertAndQueryLastRowIdAsync(tr,
                    $"""INSERT INTO [{Tn.Storage}]
                        (name, fqdn, tagKind, dataType)
                        VALUES (@Name, @Fqdn, @TagKind, @DataType)
                        ;
                    """, {|Name=s.Name; Fqdn=s.Fqdn; TagKind=s.TagKind; DataType=s.DataType|})
                s.Id <- id

            return new LogSet( [systemPowerStorage] @ existingStorages @ newStorages, isReader)
        }

    /// 주기적으로 DB -> memory 로 log 를 read
    let fetchNewLogs() =
        use conn = createConnection()
        let newLogs = conn.Query<ORMLog>($"SELECT * FROM [{Tn.Log}] WHERE id > {logSet.LastLogId} ORDER BY id DESC;")            
        if newLogs.any() then
            let newLogs = newLogs |> map (ormLog2Log logSet) |> toList
            logSet.Logs <- newLogs @ logSet.Logs
            logSet.LastLogId <- newLogs |> List.maxBy(fun l -> l.Id) |> fun l -> l.Id
            logDebug $"Feteched {newLogs.length()} new logs.  Total logs = {logSet.Logs.length()}"

    /// 주기적으로 memory -> DB 로 log 를 write
    let queue = ConcurrentQueue<ORMLog>()
    // 큐를 배열로 바꾸고 비우는 함수
    let drainQueueToArray (queue: ConcurrentQueue<'T>) : 'T[] =
        // 결과를 저장할 리스트
        let results = new ResizeArray<'T>()

        // 큐가 빌 때까지 반복해서 항목을 Dequeue
        let rec drainQueue () =
            match queue.TryDequeue() with
            | true, item ->
                results.Add(item)
                drainQueue ()
            | false, _ -> 
                () // 큐가 비었으므로 종료
    
        // 큐 드레이닝 시작
        drainQueue ()

        // 리스트를 배열로 변환하여 반환
        results.ToArray()

    let dequeAndWriteDBAsync() =
        task {
            let newLogs = drainQueueToArray(queue)
            if newLogs.any() then
                logDebug $"Writing {newLogs.length()} new logs."
                use conn = createConnection()
                use! tr = conn.BeginTransactionAsync()
                for l in newLogs do
                    let query =
                        $"""INSERT INTO [{Tn.Log}]
                            (at, storageId, value)
                            VALUES (@At, @StorageId, @Value)
                        """
                    let! _ = conn.ExecuteAsync(query, {| At=l.At; StorageId=l.StorageId; Value=l.Value |})
                    ()
                do! tr.CommitAsync()
        }

    let private toDecimal (value:obj) =
        match toBool value with
        | Bool b -> (if b then 1 else 0) |> decimal
        | UInt64 d -> decimal d
        | _ -> failwith "ERROR"


    let enqueLogsForInsert(xs:DsLog seq) =
        verify(not logSet.IsLogReader)
        for x in xs do
            match  x.Storage.Target with
            | Some t ->
                let key = x.Storage.TagKind, t.QualifiedName
                let storageId = logSet.Storages[key].Id
                let value = toDecimal x.Storage.BoxedValue
                ORMLog(-1, storageId, x.Time, value) |> queue.Enqueue
            | None ->
                failwith "NOT yet!!"

    let enqueLogForInsert(x:DsLog) = enqueLogsForInsert([x])

    let createLoggerInfoSetForReaderAsync(systems:DsSystem seq) : Task<LogSet> =
        task {
            use conn = createConnection()
            use! tr = conn.BeginTransactionAsync()

            let! li = createLogInfoSetCommonAsync(systems, conn, tr, true)

            let! existingLogs = conn.QueryAsync<ORMLog>($"SELECT * FROM [{Tn.Log}]")

            do! tr.CommitAsync()

            li.InitializeForReader(existingLogs)
            return li
        }


    let createLogInfoSetForWriterAsync(systems:DsSystem seq) : Task<LogSet> =
        task {
            use conn = createConnection()
            use! tr = conn.BeginTransactionAsync()
            let isReader = false
            let! li = createLogInfoSetCommonAsync(systems, conn, tr, isReader)
            do! tr.CommitAsync()

            return li
        }

    let markSystemStart() =
        use conn = createConnection()
        let lastLog = conn.QueryFirstOrDefault<ORMLog>($"SELECT * FROM [{Tn.Log}] ORDER BY id DESC LIMIT 1;")
        if isItNull(lastLog) then
            ()
        else if lastLog.StorageId <> 0 || lastLog.Value <> 0 then
            ignore
            <| conn.Execute($"""INSERT INTO [{Tn.Log}]
                    (at, storageId, value)
                    VALUES (@At, @StorageId, @Value)"""
                    , {| At=lastLog.At; StorageId=0; Value=false |})

        ORMLog(-1, 0, DateTime.Now, true) |> queue.Enqueue

    let markSystemShutdown() =
        ORMLog(-1, 0, DateTime.Now, false) |> queue.Enqueue
        dequeAndWriteDBAsync().Wait()

    let interval = System.Reactive.Linq.Observable.Interval(TimeSpan.FromSeconds(1))

    let initializeLogReaderOnDemandAsync(systems:DsSystem seq) =

        task {
            let! li = createLoggerInfoSetForReaderAsync(systems)
            logSet <- li

            interval.Subscribe(fun counter -> fetchNewLogs())
            |> li.Disposables.Add

            return (li :> IDisposable)
        }

    let initializeLogWriterOnDemandAsync(systems:DsSystem seq) =
        task {
            let! li = createLogInfoSetForWriterAsync(systems)
            logSet <- li

            markSystemStart()

            Disposable.Create(fun () -> markSystemShutdown())
            |> li.Disposables.Add
            interval.Subscribe(fun counter -> dequeAndWriteDBAsync().Wait())
            |> li.Disposables.Add

            return (li :> IDisposable)
        }


    //let countLogAsync(fqdn:string, tagKind:int, value:bool) =
    //    use conn = createConnection()
    //    conn.QuerySingleAsync<int>(
    //        $"""SELECT COUNT(*) FROM [{Vn.Log}]
    //            WHERE fqdn=@Fqdn AND tagKind=@TagKind AND value=@Value;""", {|Fqdn=fqdn; TagKind=tagKind; Value=value|})

    let countLog(logSet:LogSet, fqdns:string seq, tagKinds:int seq, value:bool) =
        let fqdns = fqdns |> HashSet
        let tagKinds = tagKinds |> HashSet
        logSet.Logs
            |> Seq.count(fun l ->
                l.Value = value
                && fqdns.Contains(l.Storage.Fqdn)
                && tagKinds.Contains(l.Storage.TagKind))
                
    // 지정된 조건에 따라 마지막 'Value'를 반환하는 함수
    let getLastValue (logSet:LogSet, fqdn: string, tagKind: int) : bool option =
        logSet.Logs
            |> Seq.tryFind(fun l -> l.Storage.TagKind = tagKind && l.Storage.Fqdn = fqdn)
            |> bind (fun l -> (|Bool|_|) l.Value)


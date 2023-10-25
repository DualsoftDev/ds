namespace Engine.Info

open System
open System.Configuration;
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
    let connectionString = ConfigurationManager.ConnectionStrings["DBLoggerConnectionString"].ConnectionString;
    let createConnection() =
        new SqliteConnection(connectionString) |> tee (fun conn -> conn.Open())

    let createLoggerDBSchema() =
        use conn = createConnection()
        if not <| conn.IsTableExistsAsync(Tn.Log).Result then
            conn.Execute(sqlCreateSchema, null) |> ignore


    let ormLog2Log (loggerInfo:LoggerInfoSet) (l:ORMLog) =
        let storage = loggerInfo.StoragesById[l.StorageId]
        Log(l.Id, storage, l.At, l.Value)// |> tee (fun ll -> ll.Id = l.Id)

    type LoggerInfoSet with
        member x.InitializeForReader(ormLogs:ORMLog seq) =
            x.StoragesById <-
                x.Storages.Values
                |> map (fun s -> s.Id, s)
                |> Tuple.toDictionary

            x.Logs <- ormLogs |> Seq.sortByDescending(fun l -> l.Id) |> map (ormLog2Log x) |> toList
            if x.Logs.any() then
                x.LastLogId <- x.Logs |> List.maxBy(fun l -> l.Id) |> fun l -> l.Id



    let mutable loggerInfo = getNull<LoggerInfoSet>()
    
    let private createLogInfoSetCommonAsync(systems:DsSystem seq, conn:IDbConnection, tr:IDbTransaction, isReader:bool) : Task<LoggerInfoSet> =
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

            return LoggerInfoSet(existingStorages @ newStorages, isReader)
        }

    let fetchNewLogs() =
        use conn = createConnection()
        let newLogs = conn.Query<ORMLog>($"SELECT * FROM [{Tn.Log}] WHERE id > {loggerInfo.LastLogId} ORDER BY id DESC;")            
        if newLogs.any() then
            let newLogs = newLogs |> map (ormLog2Log loggerInfo) |> toList
            loggerInfo.Logs <- newLogs @ loggerInfo.Logs
            loggerInfo.LastLogId <- newLogs |> List.maxBy(fun l -> l.Id) |> fun l -> l.Id
            logDebug $"Feteched {newLogs.length()} new logs.  Total logs = {loggerInfo.Logs.length()}"

    let createLoggerInfoSetForReaderAsync(systems:DsSystem seq) : Task<LoggerInfoSet> =
        task {
            use conn = createConnection()
            use! tr = conn.BeginTransactionAsync()

            let! li = createLogInfoSetCommonAsync(systems, conn, tr, true)

            let! existingLogs = conn.QueryAsync<ORMLog>($"SELECT * FROM [{Tn.Log}]")

            do! tr.CommitAsync()

            li.InitializeForReader(existingLogs)
            return li
        }


    let createLogInfoSetForWriterAsync(systems:DsSystem seq) : Task<LoggerInfoSet> =
        task {
            use conn = createConnection()
            use! tr = conn.BeginTransactionAsync()
            let isReader = false
            let! li = createLogInfoSetCommonAsync(systems, conn, tr, isReader)
            do! tr.CommitAsync()

            return li
        }

    let mutable readerDisposables = new CompositeDisposable()
    let initializeLogReaderOnDemandAsync(systems:DsSystem seq) =
        dispose readerDisposables
        readerDisposables <- new CompositeDisposable()

        task {
            let! li = createLoggerInfoSetForReaderAsync(systems)
            let subs =
                System.Reactive.Linq.Observable.Interval(TimeSpan.FromSeconds(1))
                    .Subscribe(fun counter -> fetchNewLogs())
            readerDisposables.Add(subs)
            loggerInfo <- li
        }

    let initializeLogWriterOnDemandAsync(systems:DsSystem seq) =
        task {
            let! li = createLogInfoSetForWriterAsync(systems)
            loggerInfo <- li
        }

    let private toDecimal (value:obj) =
        match toBool value with
        | Bool b -> (if b then 1 else 0) |> decimal
        | UInt64 d -> decimal d
        | _ -> failwith "ERROR"


    let insertDBLogsAsync(xs:DsLog seq) =
        verify(not loggerInfo.IsLogReader)
        use conn = createConnection()
        task {
            use! tr = conn.BeginTransactionAsync()
            for x in xs do
                match  x.Storage.Target with
                | Some t ->
                    let key = x.Storage.TagKind, t.QualifiedName
                    let storageId = loggerInfo.Storages[key].Id
                    let value = toDecimal x.Storage.BoxedValue
                    let! _ = conn.ExecuteAsync(
                        $"""INSERT INTO [{Tn.Log}]
                            (at, storageId, value)
                            VALUES (@At, @StorageId, @Value)
                        """, {| At=x.Time; StorageId=storageId; Value=value |})
                    ()
                | None ->
                    failwith "NOT yet!!"
            do! tr.CommitAsync()
        }

    let insertDBLogAsync(x:DsLog) = insertDBLogsAsync([x])

    //let countLogAsync(fqdn:string, tagKind:int, value:bool) =
    //    use conn = createConnection()
    //    conn.QuerySingleAsync<int>(
    //        $"""SELECT COUNT(*) FROM [{Vn.Log}]
    //            WHERE fqdn=@Fqdn AND tagKind=@TagKind AND value=@Value;""", {|Fqdn=fqdn; TagKind=tagKind; Value=value|})

    let countLog(loggerInfo:LoggerInfoSet, fqdns:string seq, tagKinds:int seq, value:bool) =
        let fqdns = fqdns |> HashSet
        let tagKinds = tagKinds |> HashSet
        loggerInfo.Logs
            |> Seq.count(fun l ->
                l.Value = value
                && fqdns.Contains(l.Storage.Fqdn)
                && tagKinds.Contains(l.Storage.TagKind))
                
    // 지정된 조건에 따라 마지막 'Value'를 반환하는 함수
    let getLastValue (loggerInfo:LoggerInfoSet, fqdn: string, tagKind: int) : bool option =
        loggerInfo.Logs
            |> Seq.tryFind(fun l -> l.Storage.TagKind = tagKind && l.Storage.Fqdn = fqdn)
            |> bind (fun l -> (|Bool|_|) l.Value)


namespace Engine.Info

open System.Runtime.CompilerServices
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Dual.Common.Core.FS
open System.Data
open Dapper
open Dual.Common.Db
open System.Reactive.Subjects
open System.Threading
open Dual.Common.Base.FS
open Newtonsoft.Json

[<AutoOpen>]
module LoggerDB =
    let DBLogSubject = new Subject<ORMLog>()
    /// ORMLog 제외한 나머지 DB 항목의 subject
    let DBSubject = new Subject<IDBRow>()

    /// Log 혹은 Storage 항목으로부터 뷰 항목 생성하기 위해서 필요한 data table 항목 cache (ORMVwLog, VwStorage 생성) 용 DTO(Data Transfer Object)
    /// Json 으로 Serialize/Deserialize 가능
    type ORMDBSkeletonDTO(model:ORMModel option, properties:ORMProperty seq, storages:ORMStorage seq, tagKinds:ORMTagKind seq) =
        new() = ORMDBSkeletonDTO(None, [], [], [])
        member val Model = model with get, set
        member val Properties = properties.ToArray() with get, set
        member val Storages = storages.ToArray() with get, set
        member val TagKinds = tagKinds.ToArray() with get, set
        static member private settings =
            let settings = new JsonSerializerSettings(TypeNameHandling = TypeNameHandling.All)
            settings.Converters.Insert(0, new ObjTypePreservingConverter([|"Value"|]))
            settings
        static member Deserialize(json: string) = NewtonsoftJson.DeserializeObject<ORMDBSkeletonDTO>(json, ORMDBSkeletonDTO.settings)
        member x.Serialize():string = NewtonsoftJson.SerializeObject(x, ORMDBSkeletonDTO.settings)


    /// Log 혹은 Storage 항목으로부터 뷰 항목 생성하기 위해서 필요한 data table 항목 cache (ORMVwLog, VwStorage 생성)
    type ORMDBSkeleton(logDbBase:ORMDBSkeletonDTO) =
        member x.Model = logDbBase.Model
        member x.Properties = logDbBase.Properties
        member val Storages = logDbBase.Storages |> map(fun s -> s.Id, s) |> Tuple.toDictionary
        member val TagKinds = logDbBase.TagKinds |> map(fun t -> t.Id, t) |> Tuple.toDictionary

    /// db 의 log table 을 주기적으로 모니터링하여 추가된 row 가 존재하면 DBLogSubject 에 log 를 발행
    let StartLogMonitor(connStr: string, sleepMs:int, cancellationToken:CancellationToken) =
        let monitorTask () =
            async {
                let conn = createConnectionWith(connStr)
                let lastRow = conn.QueryFirstOrDefault<ORMLog>($"SELECT * FROM {Tn.Log} ORDER BY Id DESC LIMIT 1")
                let mutable lastId = if isItNull(lastRow) then -1 else lastRow.Id
                conn.Dispose()

                while not cancellationToken.IsCancellationRequested do
                    try
                        use conn = createConnectionWith(connStr)
                        let! logs = conn.QueryAsync<ORMLog>($"SELECT * FROM [{Tn.Log}] WHERE id > {lastId};") |> Async.AwaitTask
                        for log in logs do
                            DBLogSubject.OnNext(log)
                            lastId <- log.Id

                        do! Async.Sleep(sleepMs) // 5초마다 체크
                    with ex ->
                        printfn "Exception: %s" ex.Message
                        do! Async.Sleep(10000) // 예외 발생 시 10초 대기
            }
        
        Async.Start(monitorTask(), cancellationToken)
        

[<AutoOpen>]
[<Extension>]
type ORMDBSkeletonDTOExt =
    [<Extension>]
    static member CreateAsync(modelId:int, conn:IDbConnection, tr:IDbTransaction): Task<ORMDBSkeletonDTO> =
        task {
            use _ = conn.TransactionScope(tr)
            let! models     = conn.QueryAsync<ORMModel>   ($"SELECT * FROM model    WHERE id      = {modelId}", tr)
            let! properties = conn.QueryAsync<ORMProperty>($"SELECT * FROM property WHERE modelId = {modelId}", tr)
            let! storages   = conn.QueryAsync<ORMStorage> ($"SELECT * FROM storage  WHERE modelId = {modelId}", tr)
            let! tagKinds   = conn.QueryAsync<ORMTagKind> ($"SELECT * FROM tagKind",                            tr)

            let model = models |> Seq.tryHead
            return ORMDBSkeletonDTO(model, properties, storages, tagKinds)
        }
    [<Extension>]
    static member CreateAsync(modelId:int, connStr:string): Task<ORMDBSkeletonDTO> =
        use conn = new SqliteConnection(connStr) |> tee (fun conn -> conn.Open())
        ORMDBSkeletonDTOExt.CreateAsync(modelId, conn, null)

    [<Extension>]
    static member CreateLoggerDBAsync(modelId:int, connStr:string): Task<ORMDBSkeleton> =
        task {
            let! dbDTO = ORMDBSkeletonDTOExt.CreateAsync(modelId, connStr)
            return dbDTO |> ORMDBSkeleton
        }

    /// ORMLog 를 다른 table join 을 통해서 ORM
    [<Extension>]
    static member ToView(db:ORMDBSkeleton, log:ORMLog): ORMVwLog =
        match db.Storages.TryFind(log.StorageId) with
        | Some stg ->
            let tagKind = db.TagKinds[stg.TagKind]
            ORMVwLog(log.Id, log.StorageId, stg.Name, stg.Fqdn, stg.TagKind, tagKind.Name, log.At, log.Value, log.ModelId, log.Token)
        | None ->
            failwith $"Failed to expand log item to view:: {log.Serialize()}"

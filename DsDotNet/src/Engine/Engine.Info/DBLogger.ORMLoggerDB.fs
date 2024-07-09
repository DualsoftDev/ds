namespace Engine.Info

open System.Runtime.CompilerServices
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Engine.Core
open Dual.Common.Core.FS
open System.Data
open Dapper
open Dual.Common.Db
open System.Reactive.Subjects

[<AutoOpen>]
module LoggerDB =
    let DBLogSubject = new Subject<ORMLog>()
    /// ORMLog 제외한 나머지 DB 항목의 subject
    let DBSubject = new Subject<IDBRow>()

    /// Log 혹은 Storage 항목으로부터 뷰 항목 생성하기 위해서 필요한 data table 항목 cache (ORMVwLog, VwStorage 생성)
    type ORMLoggerDBBase(model:ORMModel, properties:ORMProperty seq, storages:ORMStorage seq, tagKinds:ORMTagKind seq) =
        new() = ORMLoggerDBBase(getNull<ORMModel> (), [], [], [])
        member val Model = model with get, set
        member val Properties = properties.ToArray() with get, set
        member val Storages = storages.ToArray() with get, set
        member val TagKinds = tagKinds.ToArray() with get, set

    type ORMLoggerDB(logDbBase:ORMLoggerDBBase) =
        member x.Model = logDbBase.Model
        member x.Properties = logDbBase.Properties
        member val Storages = logDbBase.Storages |> map(fun s -> s.Id, s) |> Tuple.toDictionary
        member val TagKinds = logDbBase.TagKinds |> map(fun t -> t.Id, t) |> Tuple.toDictionary



[<AutoOpen>]
[<Extension>]
type ORMLoggerDBBaseExt =
    [<Extension>]
    static member CreateAsync(modelId:int, conn:IDbConnection, tr:IDbTransaction): Task<ORMLoggerDBBase> =
        task {
            use _ = conn.TransactionScope(tr)
            let! models     = conn.QueryAsync<ORMModel>   ($"SELECT * FROM model    WHERE id      = {modelId}", tr)
            let! properties = conn.QueryAsync<ORMProperty>($"SELECT * FROM property WHERE modelId = {modelId}", tr)
            let! storages   = conn.QueryAsync<ORMStorage> ($"SELECT * FROM storage  WHERE modelId = {modelId}", tr)
            let! tagKinds   = conn.QueryAsync<ORMTagKind> ($"SELECT * FROM tagKind  WHERE modelId = {modelId}", tr)

            let model = models |> Seq.head
            return ORMLoggerDBBase(model, properties, storages, tagKinds)
        }
    [<Extension>]
    static member CreateAsync(modelId:int, connStr:string): Task<ORMLoggerDBBase> =
        use conn = new SqliteConnection(connStr) |> tee (fun conn -> conn.Open())
        ORMLoggerDBBaseExt.CreateAsync(modelId, conn, null)

    [<Extension>]
    static member ToView(db:ORMLoggerDB, log:ORMLog): ORMVwLog =
        let stg = db.Storages[log.StorageId]
        let tagKind = db.TagKinds[stg.TagKind]
        ORMVwLog(log.Id, log.StorageId, stg.Name, stg.Fqdn, stg.TagKind, tagKind.Name, log.At, log.Value)

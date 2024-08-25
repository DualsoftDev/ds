namespace Engine.Info

open System
open System.IO
open Microsoft.Data.Sqlite
open Engine.Core
open Dual.Common.Core.FS
open System.Collections.Generic
open System.Data
open Dapper
open Dual.Common.Db
open System.Runtime.CompilerServices




/// DB logging query 기준
/// StartTime: 조회 시작 기간.
/// Null 이면 사전 지정된 start time 을 사용.  (사전 지정된 값이 없을 경우, DateTime.MinValue 와 동일)
/// 모든 데이터 조회하려면 DateTime.MinValue 를 사용
[<AllowNullLiteral>]
type QueryCriteria(commonAppSettings:DSCommonAppSettings, modelId:int, startAt: DateTime option, endAt: DateTime option) =
    //new() = QuerySet(getNull<DSCommonAppSettings>(), -1, None, None)

    new(commonAppSettings, modelId, startAt: Nullable<DateTime>, endAt: Nullable<DateTime>) =
        QueryCriteria(commonAppSettings, modelId, startAt |> Option.ofNullable, endAt |> Option.ofNullable)

    member x.ModelId = modelId
    /// 사용자 지정: 조회 start time
    member x.TargetStart = startAt
    /// 사용자 지정: 조회 end time
    member x.TargetEnd = endAt
    member val StartTime = startAt |? DateTime.MinValue with get, set
    member val EndTime = endAt |? DateTime.MaxValue with get, set
    member val CommonAppSettings = commonAppSettings
    member val DsConfigJsonPath = "" with get, set

[<AutoOpen>]
module DBLoggerORM2 =

    type Fqdn = string
    type StorageKey = TagKind * Fqdn

    let getStorageKey (s: ORMStorage) : StorageKey = s.TagKind, s.Fqdn

    /// StorageKey(-> TagKind*Fqdn) 로 주어진 항목에 대한 조회 기간 전체의 summary (-> Count, Sum)
    /// durations: sec 단위 개별 실행 duration
    type Summary(logSet: LogSet, storageKey: StorageKey, durations:float seq) =
        /// storageKey 에 해당하는 모든 durations.  variance 를 구하기 위해서 모든 instance 필요.
        member val Durations = ResizeArray durations
        /// Number rising
        member x.Count = x.Durations.Count
        member x.Sum = x.Durations |> Seq.sum
        /// 평균
        member x.Average = x.Durations.ToOption().Map(Seq.average) |? 0.0
        /// 분산
        member x.Variance =
            if x.Count > 1 then
                let mean = x.Average
                x.Durations
                |> map (fun x -> (x - mean) ** 2.0)
                |> Seq.average
            else
                0.0
        /// 표준 편차
        member x.StdDev = sqrt x.Variance
        /// 표준 편차
        member x.Sigma  = sqrt x.Variance

        /// Container reference
        member x.LogSet = logSet
        member x.StorageKey = storageKey


    /// DB logging 관련 전체 설정
    and LogSet(queryCriteria: QueryCriteria, systems: DsSystem seq, storages: ORMStorage seq, readerWriterType: DBLoggerType) as this =
        let summaryDic =
            storages
            |> map (fun s ->
                let key = getStorageKey s
                key, Summary(this, key, [||]))
            |> Tuple.toReadOnlyDictionary

        interface ILogSet

        member x.Summaries = summaryDic
        member x.ModelId = queryCriteria.ModelId
        member x.ReaderWriterType = readerWriterType
        member x.GetSummary(storageKey: StorageKey) = summaryDic[storageKey]

        member val Systems = systems |> toArray
        member val QuerySet = queryCriteria with get, set
        member val Storages = storages |> map (fun s -> getStorageKey s, s) |> Tuple.toReadOnlyDictionary

        // { mutables
        member val StoragesById = Dictionary<int, ORMStorage>()
        member val LastLogs = Dictionary<ORMStorage, Log>()
        member val TheLastLog: Log option = None with get, set
        //member val Disposables = new CompositeDisposable()
        // } mutables


    /// property table 항목 조회
    let queryPropertyAsync (modelId:int, propertyName: string, conn: IDbConnection, tr: IDbTransaction) =
        conn.QueryFirstOrDefaultAsync<string>(
            $"SELECT value FROM [{Tn.Property}] WHERE name = @Name AND modelId=@ModelId",
            {| Name = propertyName; ModelId=modelId |},
            tr
        )

    /// property table 항목 수정
    let updatePropertyAsync (modelId:int, propertyName: string, value: string, conn: IDbConnection, tr: IDbTransaction) =
        conn.ExecuteSilentlyAsync(
            $"""INSERT OR REPLACE INTO [{Tn.Property}]
                (name, value, modelId)
                VALUES(@Name, @Value, @ModelId);""",
            {| Name = propertyName; Value = value; ModelId=modelId |},
            tr
        )


    type QueryCriteria with

        /// 조회 기간 target 설정 값 필요시 db 에 반영하고, target 에 맞게 조회 기간 변경
        member x.SetQueryRangeAsync(modelId:int, conn: IDbConnection, tr: IDbTransaction) =
            task {
                match x.TargetStart with
                | Some s ->
                    do! updatePropertyAsync (modelId, PropName.Start, s.ToString(), conn, tr)
                    x.StartTime <- s
                | _ ->
                    let! str = queryPropertyAsync (modelId, PropName.Start, conn, tr)

                    x.StartTime <-
                        if isNull (str) then
                            DateTime.MinValue
                        else
                            DateTime.Parse(str)


                match x.TargetEnd with
                | Some e ->
                    do! updatePropertyAsync (modelId, PropName.End, e.ToString(), conn, tr)
                    x.EndTime <- e
                | _ ->
                    let! str = queryPropertyAsync (modelId, PropName.End, conn, tr)

                    x.EndTime <-
                        if isNull (str) then
                            DateTime.MaxValue
                        else
                            DateTime.Parse(str)

                logInfo $"Query range set: [{x.StartTime} ~ {x.EndTime}]"
            }


    let createConnectionWith (connStr) =
        new SqliteConnection(connStr) |> tee (fun conn -> conn.Open())

    let getNewTagKindInfosAsync (conn: IDbConnection, tr: IDbTransaction) =
        let tagKindInfos = GetAllTagKinds ()

        task {
            let! existingTagKindMap = conn.QueryAsync<ORMTagKind>($"SELECT * FROM [{Tn.TagKind}];", null, tr)       // WHERE modelId = {modelId}

            let existingTagKindHash =
                existingTagKindMap |> map (fun t -> t.Id, t.Name) |> HashSet

            return tagKindInfos |> filter (fun t -> not <| existingTagKindHash.Contains(t))
        }

type LoggerDBSettingsExt =
    [<Extension>]
    static member CreateConnection(loggerDBSettings:LoggerDBSettings): SqliteConnection =
        let connStr = $"Data Source={loggerDBSettings.ConnectionPath}"
        createConnectionWith connStr

    [<Extension>]
    static member DropDatabase(loggerDBSettings:LoggerDBSettings) =
        use conn = loggerDBSettings.CreateConnection()
        conn.DropDatabase()

    [<Extension>]
    static member ComputeModelId(loggerDBSettings:LoggerDBSettings): int =
        use conn = loggerDBSettings.CreateConnection()
        // db 가 아직 초기화되지 않은 경우의 처리???
        failwith "Not implemented"

    [<Extension>]
    static member FillModelId(loggerDBSettings:LoggerDBSettings): int*string =
        Directory.CreateDirectory(Path.GetDirectoryName(loggerDBSettings.ConnectionPath)) |> ignore
        use conn = loggerDBSettings.CreateConnection()
        use tr = conn.BeginTransaction()
        let tableExists = conn.IsTableExistsAsync(Tn.Model).Result

        let mutable path = loggerDBSettings.ModelFilePath
        let mutable runtime = loggerDBSettings.DbWriter
        let lastModelInfoSpecified = path.NonNullAny() && runtime.NonNullAny()


        if not tableExists then
            if not lastModelInfoSpecified then
                failwithlog "ModelFilePath and DbWriter must be set for empty database!"

            // schema 새로 생성
            conn.ExecuteSilentlyAsync(sqlCreateSchema, tr).Wait()

        if not lastModelInfoSpecified then
            let propDic = conn.Query<ORMProperty>($"SELECT * FROM [{Tn.Property}]", null, tr) |> map (fun p -> p.Name, p.Value) |> Tuple.toDictionary
            path <- propDic[PropName.ModelFilePath]
            if runtime.IsNullOrEmpty() then
                runtime <- propDic[PropName.ModelRuntime]

        let param = {|Path = path; Runtime = runtime |}
        let optModel =
            let sql =
                $"""SELECT * FROM {Tn.Model}
                        WHERE path = @Path
                        AND runtime = @Runtime"""
            conn.TryQuerySingle<ORMModel>(sql, param, tr)

        match optModel with
        | Some m ->
            let propSql =
                $"""INSERT OR REPLACE INTO [{Tn.Property}]
                    (name, value)
                    VALUES(@Name, @Value);"""
            conn.Execute(propSql, {| Name = PropName.ModelFilePath; Value = path |}, tr) |> ignore
            conn.Execute(propSql, {| Name = PropName.ModelRuntime; Value = runtime |}, tr) |> ignore

            logInfo $"With new Model: id = {m.Id}, path={path}"
            loggerDBSettings.ModelId <- m.Id
        | _ ->
            let modelId =
                conn.InsertAndQueryLastRowIdAsync(tr,
                    $"""INSERT INTO [{Tn.Model}]
                        (path, runtime)
                        VALUES (@Path, @Runtime)
                    """,
                    param
                ).Result
            loggerDBSettings.ModelId <- modelId

            let newTagKindInfos = (getNewTagKindInfosAsync (conn, tr)).Result

            for (id, name) in newTagKindInfos do
                let query = $"INSERT INTO [{Tn.TagKind}] (id, name) VALUES (@Id, @Name);"
                conn.Execute(query, {| Id = id; Name = name |}, tr) |> ignore


        tr.Commit()

        loggerDBSettings.ModelId, path




[<AutoOpen>]
module DBLoggerORM3 =
    type DSCommonAppSettings with
        member x.ConnectionString = $"Data Source={x.LoggerDBSettings.ConnectionPath}"
        member x.CreateConnection(): SqliteConnection = x.LoggerDBSettings.CreateConnection()




